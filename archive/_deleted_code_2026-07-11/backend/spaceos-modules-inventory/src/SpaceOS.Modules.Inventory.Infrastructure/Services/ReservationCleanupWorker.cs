using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Inventory.Domain.Enums;
using SpaceOS.Modules.Inventory.Infrastructure.Observability;
using SpaceOS.Modules.Inventory.Infrastructure.Persistence;

namespace SpaceOS.Modules.Inventory.Infrastructure.Services;

/// <summary>
/// Background service that periodically marks expired Active reservations as <see cref="ReservationStatus.Expired"/>.
/// Runs at a configurable interval (default: 15 minutes) and processes up to <c>inventory:cleanup:batchSize</c>
/// reservations per iteration (default: 100, max: 500).
/// </summary>
public sealed class ReservationCleanupWorker : BackgroundService
{
    private const string WorkerName = "inventory-cleanup-worker";
    private const int DefaultIntervalMinutes = 15;
    private const int DefaultBatchSize = 100;
    private const int MaxBatchSize = 500;

    private readonly IServiceProvider _services;
    private readonly IWorkerHeartbeatStore _heartbeat;
    private readonly ILogger<ReservationCleanupWorker> _logger;
    private readonly TimeSpan _interval;
    private readonly int _batchSize;

    public ReservationCleanupWorker(
        IServiceProvider services,
        IConfiguration configuration,
        IWorkerHeartbeatStore heartbeat,
        ILogger<ReservationCleanupWorker> logger)
    {
        _services = services;
        _heartbeat = heartbeat;
        _logger = logger;

        var intervalMinutes = configuration.GetValue("inventory:cleanup:intervalMinutes", DefaultIntervalMinutes);
        _interval = TimeSpan.FromMinutes(intervalMinutes);

        var configuredBatch = configuration.GetValue("inventory:cleanup:batchSize", DefaultBatchSize);
        if (configuredBatch > MaxBatchSize)
            throw new InvalidOperationException(
                $"inventory:cleanup:batchSize ({configuredBatch}) exceeds maximum allowed value of {MaxBatchSize}.");

        _batchSize = configuredBatch;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "ReservationCleanupWorker started — interval: {IntervalMinutes} min, batchSize: {BatchSize}",
            _interval.TotalMinutes, _batchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCleanupIterationAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown — do not log as error
                break;
            }
#pragma warning disable CA1031 // catch general exception so the worker loop does not crash
            catch (Exception ex)
            {
                // Worker must not die on transient errors; log and continue
                _logger.LogError(ex, "ReservationCleanupWorker: unhandled error in iteration");
            }
#pragma warning restore CA1031

            await Task.Delay(_interval, stoppingToken).ConfigureAwait(false);
        }

        _logger.LogInformation("ReservationCleanupWorker stopped.");
    }

    private async Task RunCleanupIterationAsync(CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        await using var scope = _services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryWorkerDbContext>();

        var expired = await db.Reservations
            .Where(r => r.Status == ReservationStatus.Active && r.ExpiresAt < DateTimeOffset.UtcNow)
            .Take(_batchSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (expired.Count == 0)
        {
            _logger.LogDebug("ReservationCleanupWorker: no expired reservations found");
        }
        else
        {
            foreach (var reservation in expired)
            {
                reservation.MarkExpired(isWorkerContext: true);
            }

            await db.SaveChangesAsync(ct).ConfigureAwait(false);

            _logger.LogInformation(
                "ReservationCleanupWorker: marked {Count} reservation(s) as Expired",
                expired.Count);
        }

        sw.Stop();

        ReservationMetrics.ReservationsExpired.Add(expired.Count);
        ReservationMetrics.CleanupIterationMs.Record(sw.Elapsed.TotalMilliseconds);

        await _heartbeat.TickAsync(WorkerName, ct).ConfigureAwait(false);
    }
}
