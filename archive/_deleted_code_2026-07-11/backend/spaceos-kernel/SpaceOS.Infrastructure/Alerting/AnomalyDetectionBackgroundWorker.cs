// SpaceOS.Infrastructure/Alerting/AnomalyDetectionBackgroundWorker.cs

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpaceOS.Kernel.Application.AuditLog.Anomaly;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.Specifications;

namespace SpaceOS.Infrastructure.Alerting;

/// <summary>
/// Hosted background service that periodically runs <see cref="AuditAnomalyDetector"/>
/// for every active tenant. Checks run every 60 seconds.
/// Uses a DI scope per cycle to keep EF Core contexts properly isolated.
/// </summary>
public sealed class AnomalyDetectionBackgroundWorker : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(60);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AnomalyDetectionBackgroundWorker> _logger;

    /// <summary>
    /// Initialises a new <see cref="AnomalyDetectionBackgroundWorker"/>.
    /// </summary>
    /// <param name="scopeFactory">Factory for creating DI scopes per poll cycle.</param>
    /// <param name="logger">Structured logger.</param>
    public AnomalyDetectionBackgroundWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<AnomalyDetectionBackgroundWorker> logger)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(logger);
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("AnomalyDetectionBackgroundWorker started.");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await RunDetectionCycleAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in AnomalyDetectionBackgroundWorker poll cycle.");
            }

            await Task.Delay(PollInterval, ct).ConfigureAwait(false);
        }

        _logger.LogInformation("AnomalyDetectionBackgroundWorker stopped.");
    }

    private async Task RunDetectionCycleAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var tenantRepository = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
        var detector         = scope.ServiceProvider.GetRequiredService<AuditAnomalyDetector>();

        var tenants = await tenantRepository
            .ListAsync(new AllTenantsSpec(), ct)
            .ConfigureAwait(false);

        _logger.LogDebug(
            "AnomalyDetectionBackgroundWorker: running checks for {Count} tenant(s).",
            tenants.Count);

        foreach (var tenant in tenants)
        {
            ct.ThrowIfCancellationRequested();
            await detector.DetectAnomaliesAsync(tenant.Id.Value, ct).ConfigureAwait(false);
        }
    }
}
