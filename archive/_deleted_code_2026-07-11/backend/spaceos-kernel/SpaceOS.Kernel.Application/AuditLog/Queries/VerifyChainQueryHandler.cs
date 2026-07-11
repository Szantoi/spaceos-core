// SpaceOS.Kernel.Application/AuditLog/Queries/VerifyChainQueryHandler.cs

using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.AuditLog;
using SpaceOS.Kernel.Domain.Services;

namespace SpaceOS.Kernel.Application.AuditLog.Queries;

/// <summary>
/// Handles <see cref="VerifyChainQuery"/>: walks the audit event chain for a tenant and
/// reports whether every <c>PreviousHash</c> link is intact and whether the primary database
/// hashes match those recorded in the external sink.
/// </summary>
internal sealed class VerifyChainQueryHandler
    : IRequestHandler<VerifyChainQuery, Result<ChainVerificationResultDto>>
{
    private readonly IAuditEventRepository _repository;
    private readonly IExternalAuditSink _sink;
    private readonly IGenesisHashProvider _genesisHashProvider;
    private readonly IProofStorageService _proofStorage;

    /// <summary>Initialises a new <see cref="VerifyChainQueryHandler"/>.</summary>
    /// <param name="repository">The audit event repository.</param>
    /// <param name="sink">The external audit sink used for cross-validation.</param>
    /// <param name="genesisHashProvider">Provides the deployment-time genesis hash.</param>
    /// <param name="proofStorage">Proof storage service for WORM availability check (SEC-P3B-05).</param>
    public VerifyChainQueryHandler(
        IAuditEventRepository repository,
        IExternalAuditSink sink,
        IGenesisHashProvider genesisHashProvider,
        IProofStorageService proofStorage)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(sink);
        ArgumentNullException.ThrowIfNull(genesisHashProvider);
        ArgumentNullException.ThrowIfNull(proofStorage);
        _repository          = repository;
        _sink                = sink;
        _genesisHashProvider = genesisHashProvider;
        _proofStorage        = proofStorage;
    }

    /// <summary>Executes the chain verification and returns the result DTO.</summary>
    public async Task<Result<ChainVerificationResultDto>> Handle(
        VerifyChainQuery request,
        CancellationToken ct)
    {
        // SEC-P3B-05: check WORM storage availability — never throws, always returns 200.
        bool wormAvailable;
        string? diagnosticMessage = null;
        try
        {
            wormAvailable = await _proofStorage.IsAvailableAsync(ct).ConfigureAwait(false);
            if (!wormAvailable)
                diagnosticMessage = $"WORM proof storage ({_proofStorage.ProviderName}) is unavailable — cross-validation skipped.";
        }
        catch (Exception ex)
        {
            wormAvailable     = false;
            diagnosticMessage = $"WORM proof storage check failed: {ex.GetType().Name}.";
        }

        var events = await _repository
            .GetChainAsync(request.TenantId, request.From, request.To, ct)
            .ConfigureAwait(false);

        if (events.Count == 0)
        {
            return Result.Success(new ChainVerificationResultDto(
                IsValid: true,
                TotalRecordsChecked: 0,
                FirstBrokenAt: null,
                ExternalSinkMatch: true,
                WormStorageAvailable: wormAvailable,
                DiagnosticMessage: diagnosticMessage));
        }

        var genesisHash = await _genesisHashProvider
            .GetGenesisHashAsync(ct)
            .ConfigureAwait(false);

        DateTimeOffset? firstBrokenAt = null;
        var expectedPrevious = genesisHash;

        for (var i = 0; i < events.Count; i++)
        {
            var ev = events[i];

            if (!string.Equals(ev.PreviousHash, expectedPrevious, StringComparison.Ordinal))
            {
                firstBrokenAt = ev.OccurredAt;
                break;
            }

            expectedPrevious = ev.StateHash;
        }

        var chainValid = firstBrokenAt is null;

        // Cross-validate against the external sink.
        var sinkRecords = await _sink
            .ReadHashesAsync(request.TenantId, request.From, request.To, ct)
            .ConfigureAwait(false);

        var externalSinkMatch = CheckSinkMatch(events, sinkRecords);

        return Result.Success(new ChainVerificationResultDto(
            IsValid: chainValid,
            TotalRecordsChecked: events.Count,
            FirstBrokenAt: firstBrokenAt,
            ExternalSinkMatch: externalSinkMatch,
            WormStorageAvailable: wormAvailable,
            DiagnosticMessage: diagnosticMessage));
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns <see langword="true"/> when every event in <paramref name="events"/> has a
    /// matching entry in <paramref name="sinkRecords"/> with an identical <c>StateHash</c>.
    /// When the sink is empty (stub or unavailable) the match is reported as <see langword="false"/>
    /// unless there are also no primary events.
    /// </summary>
    private static bool CheckSinkMatch(
        IReadOnlyList<AuditEvent> events,
        IReadOnlyList<ExternalAuditHashRecord> sinkRecords)
    {
        if (events.Count == 0)
            return true;

        if (sinkRecords.Count == 0)
            return false;

        // Build a fast lookup: OccurredAt → StateHash from the sink.
        // OccurredAt is treated as the correlation key because it is the only field
        // present in both the primary store and the external sink.
        var sinkLookup = sinkRecords
            .GroupBy(r => r.OccurredAt)
            .ToDictionary(g => g.Key, g => g.First().StateHash);

        foreach (var ev in events)
        {
            if (!sinkLookup.TryGetValue(ev.OccurredAt, out var sinkHash))
                return false;

            if (!string.Equals(sinkHash, ev.StateHash, StringComparison.Ordinal))
                return false;
        }

        return true;
    }
}
