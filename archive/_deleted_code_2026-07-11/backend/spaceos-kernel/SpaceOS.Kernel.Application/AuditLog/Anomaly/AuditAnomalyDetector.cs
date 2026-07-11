// SpaceOS.Kernel.Application/AuditLog/Anomaly/AuditAnomalyDetector.cs

using Microsoft.Extensions.Logging;
using SpaceOS.Kernel.Application.AuditLog.Queries;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.AuditLog;
using SpaceOS.Kernel.Domain.AuditLog.Specifications;

namespace SpaceOS.Kernel.Application.AuditLog.Anomaly;

/// <summary>
/// Checks for anomalous patterns in the audit log for a single tenant and fires alerts
/// via <see cref="IAlertService"/> when violations are detected.
/// </summary>
/// <remarks>
/// Three checks are performed:
/// <list type="bullet">
///   <item><b>AuditGap</b> — no audit events recorded in the last <see cref="AuditGapThresholdMinutes"/> minutes.</item>
///   <item><b>BurstClosedDone</b> — more than <see cref="BurstClosedDoneThreshold"/> "FlowEpicClosedEvent" records in the last 5 minutes.</item>
///   <item><b>ChainBreak</b> — at least one broken link detected in the audit chain.</item>
/// </list>
/// </remarks>
public sealed class AuditAnomalyDetector
{
    /// <summary>Minutes of silence after which an AuditGap alert is raised.</summary>
    public const int AuditGapThresholdMinutes = 10;

    /// <summary>Number of ClosedDone events within 5 minutes that triggers a BurstClosedDone alert.</summary>
    public const int BurstClosedDoneThreshold = 10;

    private const string ClosedDoneEventType = "FlowEpicClosedEvent";

    private readonly IAuditEventRepository _repository;
    private readonly IAlertService _alertService;
    private readonly IGenesisHashProvider _genesisHashProvider;
    private readonly ILogger<AuditAnomalyDetector> _logger;

    /// <summary>Initialises a new <see cref="AuditAnomalyDetector"/>.</summary>
    /// <param name="repository">The audit event repository.</param>
    /// <param name="alertService">The alert delivery service.</param>
    /// <param name="genesisHashProvider">Provides the genesis hash for chain verification.</param>
    /// <param name="logger">Logger for internal diagnostics.</param>
    public AuditAnomalyDetector(
        IAuditEventRepository repository,
        IAlertService alertService,
        IGenesisHashProvider genesisHashProvider,
        ILogger<AuditAnomalyDetector> logger)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(alertService);
        ArgumentNullException.ThrowIfNull(genesisHashProvider);
        ArgumentNullException.ThrowIfNull(logger);
        _repository          = repository;
        _alertService        = alertService;
        _genesisHashProvider = genesisHashProvider;
        _logger              = logger;
    }

    /// <summary>
    /// Runs all anomaly checks for the specified tenant and sends alerts for any violations found.
    /// This method is designed to be called from a background worker; it never throws.
    /// </summary>
    /// <param name="tenantId">The tenant to inspect.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task DetectAnomaliesAsync(Guid tenantId, CancellationToken ct)
    {
        try
        {
            var now = DateTimeOffset.UtcNow;

            await CheckAuditGapAsync(tenantId, now, ct).ConfigureAwait(false);
            await CheckBurstClosedDoneAsync(tenantId, now, ct).ConfigureAwait(false);
            await CheckChainBreakAsync(tenantId, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AuditAnomalyDetector: unexpected error while checking tenant {TenantId}.", tenantId);
        }
    }

    // -------------------------------------------------------------------------
    // Private checks
    // -------------------------------------------------------------------------

    private async Task CheckAuditGapAsync(Guid tenantId, DateTimeOffset now, CancellationToken ct)
    {
        var windowStart = now.AddMinutes(-AuditGapThresholdMinutes);
        var spec = new AuditEventsByTenantFilterSpec(tenantId, null, windowStart, now);
        var count = await _repository.CountAsync(spec, ct).ConfigureAwait(false);

        if (count == 0)
        {
            await _alertService.SendAlertAsync(
                "AuditGap",
                $"No audit events recorded for tenant {tenantId} in the last {AuditGapThresholdMinutes} minutes.",
                ct).ConfigureAwait(false);
        }
    }

    private async Task CheckBurstClosedDoneAsync(Guid tenantId, DateTimeOffset now, CancellationToken ct)
    {
        var windowStart = now.AddMinutes(-5);
        var spec = new AuditEventsByTenantFilterSpec(tenantId, ClosedDoneEventType, windowStart, now);
        var count = await _repository.CountAsync(spec, ct).ConfigureAwait(false);

        if (count > BurstClosedDoneThreshold)
        {
            await _alertService.SendAlertAsync(
                "BurstClosedDone",
                $"Tenant {tenantId} produced {count} {ClosedDoneEventType} events in the last 5 minutes (threshold: {BurstClosedDoneThreshold}).",
                ct).ConfigureAwait(false);
        }
    }

    private async Task CheckChainBreakAsync(Guid tenantId, CancellationToken ct)
    {
        var events = await _repository
            .GetChainAsync(tenantId, from: null, to: null, ct)
            .ConfigureAwait(false);

        if (events.Count == 0)
            return;

        var genesisHash = await _genesisHashProvider
            .GetGenesisHashAsync(ct)
            .ConfigureAwait(false);

        var expected = genesisHash;
        foreach (var ev in events)
        {
            if (!string.Equals(ev.PreviousHash, expected, StringComparison.Ordinal))
            {
                await _alertService.SendAlertAsync(
                    "ChainBreak",
                    $"Audit chain broken for tenant {tenantId} at event {ev.Id} (OccurredAt: {ev.OccurredAt:O}). " +
                    $"Expected PreviousHash '{expected}' but found '{ev.PreviousHash}'.",
                    ct).ConfigureAwait(false);
                return;
            }

            expected = ev.StateHash;
        }
    }
}
