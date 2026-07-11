using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Inventory.Infrastructure.Persistence;

namespace SpaceOS.Modules.Inventory.Infrastructure.Services;

/// <summary>
/// Polls InventoryReorderOutboxes and forwards Pending rows to Procurement via HTTP.
/// Uses BYPASSRLS worker connection (InventoryWorkerDbContext).
/// Retry: transient errors only (5xx, HttpRequestException, timeout).
/// Permanent 4xx → immediately Failed (peer should not retry endlessly).
/// Circuit-breaker: pauses delivery if Procurement is repeatedly unavailable.
/// </summary>
public sealed class ReorderAlertWorker : BackgroundService
{
    private const string WorkerName = "inventory-reorder-alert-worker";
    private const int MaxAttempts = 5;
    private const int CircuitBreakerThreshold = 5;
    private static readonly TimeSpan CircuitOpenPeriod = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan[] BackoffDelays =
    [
        TimeSpan.FromSeconds(30),
        TimeSpan.FromSeconds(60),
        TimeSpan.FromSeconds(120),
        TimeSpan.FromSeconds(300),
        TimeSpan.FromSeconds(600)
    ];

    private readonly IServiceProvider _services;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ReorderAlertWorker> _logger;
    private readonly string _procurementUrl;
    private readonly TimeSpan _pollInterval;

    // In-memory circuit-breaker state
    private int _consecutiveFailures;
    private DateTimeOffset _circuitOpenUntil = DateTimeOffset.MinValue;

    public ReorderAlertWorker(
        IServiceProvider services,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ReorderAlertWorker> logger)
    {
        _services = services;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _procurementUrl = configuration["PROCUREMENT_INTERNAL_URL"]
            ?? Environment.GetEnvironmentVariable("PROCUREMENT_INTERNAL_URL")
            ?? "http://127.0.0.1:5006";
        var pollSec = configuration.GetValue("inventory:reorderAlert:pollSeconds", 30);
        _pollInterval = TimeSpan.FromSeconds(pollSec);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "{Worker} started — procurement endpoint: {Url}, poll interval: {Interval}s",
            WorkerName, _procurementUrl, _pollInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunIterationAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
#pragma warning disable CA1031
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Worker}: unhandled error in poll iteration", WorkerName);
            }
#pragma warning restore CA1031

            await Task.Delay(_pollInterval, stoppingToken).ConfigureAwait(false);
        }

        _logger.LogInformation("{Worker} stopped.", WorkerName);
    }

    private async Task RunIterationAsync(CancellationToken ct)
    {
        // Circuit-breaker: if open, skip this iteration
        if (DateTimeOffset.UtcNow < _circuitOpenUntil)
        {
            _logger.LogWarning("{Worker}: circuit open until {Until} — skipping iteration", WorkerName, _circuitOpenUntil);
            return;
        }

        await using var scope = _services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryWorkerDbContext>();

        var leaseTtl = TimeSpan.FromMinutes(2);
        var now = DateTimeOffset.UtcNow;

        // Claim: Pending + due, OR InFlight + lease expired (reclaim stale)
        var row = await db.InventoryReorderOutboxes
            .Where(o =>
                (o.Status == "Pending" && o.NextAttemptAt <= now) ||
                (o.Status == "InFlight" && o.LeaseUntil < now))
            .OrderBy(o => o.NextAttemptAt)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (row is null) return;

        row.ClaimLease(now + leaseTtl);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // Tenant DiD assert (SEC-P-04 equivalent)
        if (row.TenantId == Guid.Empty)
        {
            _logger.LogCritical("{Worker}: outbox row {Id} has empty TenantId — aborting (DiD violation)", WorkerName, row.Id);
            row.MarkFailed("DiD: empty TenantId");
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            return;
        }

        var secret = Environment.GetEnvironmentVariable("SPACEOS_INTERNAL_SECRET") ?? string.Empty;
        var targetUrl = $"{_procurementUrl.TrimEnd('/')}/procurement/internal/from-reorder-alert";

        HttpResponseMessage? response = null;
        Exception? lastEx = null;

        try
        {
            using var httpClient = _httpClientFactory.CreateClient(WorkerName);
            using var req = new HttpRequestMessage(HttpMethod.Post, targetUrl)
            {
                Content = new StringContent(row.Payload, System.Text.Encoding.UTF8, "application/json")
            };
            req.Headers.TryAddWithoutValidation("Authorization", $"Bearer {secret}");
            req.Headers.TryAddWithoutValidation("X-SpaceOS-TenantId", row.TenantId.ToString());

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            response = await httpClient.SendAsync(req, cts.Token).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            lastEx = ex;
        }

        var isTransient = IsTransient(response, lastEx);
        var isPermanent = !isTransient && lastEx is null && response is not null && !response.IsSuccessStatusCode;
        var isSuccess = lastEx is null && response is not null &&
                        ((int)response.StatusCode is >= 200 and <= 299 or 409); // 409 = idempotent duplicate → success

        if (isSuccess)
        {
            row.MarkCompleted();
            Interlocked.Exchange(ref _consecutiveFailures, 0); // reset circuit-breaker
            _logger.LogInformation("{Worker}: row {Id} → Completed (HTTP {Status})", WorkerName, row.Id, response!.StatusCode);
        }
        else if (isPermanent || row.AttemptCount >= MaxAttempts)
        {
            var reason = isPermanent
                ? $"Permanent HTTP {(int)response!.StatusCode}"
                : $"MaxAttempts exceeded ({MaxAttempts})";
            row.MarkFailed(reason);
            _logger.LogWarning("{Worker}: row {Id} → Failed ({Reason})", WorkerName, row.Id, reason);
        }
        else
        {
            // Transient: schedule retry with backoff
            var backoffIdx = Math.Min(row.AttemptCount - 1, BackoffDelays.Length - 1);
            var nextAttempt = DateTimeOffset.UtcNow + BackoffDelays[backoffIdx];
            var errorMsg = lastEx?.Message ?? response?.StatusCode.ToString() ?? "unknown";
            row.MarkRetry(nextAttempt, errorMsg);

            var newFailures = Interlocked.Increment(ref _consecutiveFailures);
            if (newFailures >= CircuitBreakerThreshold)
            {
                _circuitOpenUntil = DateTimeOffset.UtcNow + CircuitOpenPeriod;
                _logger.LogWarning(
                    "{Worker}: circuit opened for {Period} after {Failures} consecutive failures",
                    WorkerName, CircuitOpenPeriod, newFailures);
            }

            _logger.LogWarning("{Worker}: row {Id} → retry at {NextAttempt} (attempt {Attempt})",
                WorkerName, row.Id, nextAttempt, row.AttemptCount);
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private static bool IsTransient(HttpResponseMessage? response, Exception? ex)
    {
        if (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
            return true;
        if (response is null) return false;
        var code = (int)response.StatusCode;
        return code >= 500 || response.StatusCode == HttpStatusCode.TooManyRequests;
    }
}
