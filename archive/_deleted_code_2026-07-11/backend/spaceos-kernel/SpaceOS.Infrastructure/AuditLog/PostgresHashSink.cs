// SpaceOS.Infrastructure/AuditLog/PostgresHashSink.cs

using System.Diagnostics.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SpaceOS.Infrastructure.Persistence;
using SpaceOS.Kernel.Application.AuditLog;

namespace SpaceOS.Infrastructure.AuditLog;

/// <summary>
/// Production implementation of <see cref="IExternalAuditSink"/> that persists audit hash
/// records to the <c>spaceos_audit_sink</c> PostgreSQL database via <see cref="HashSinkDbContext"/>.
/// </summary>
/// <remarks>
/// <para>
/// Each write uses a short-lived <see cref="HashSinkDbContext"/> obtained from the factory
/// and disposed immediately after the insert, avoiding Scoped-lifetime disposal conflicts
/// with fire-and-forget sink writes that may outlive the originating HTTP request scope.
/// </para>
/// <para>
/// Sink failures are caught, logged, and counted — they are never rethrown.
/// A sink outage must not interrupt the primary audit write path (Golden Rule #12).
/// </para>
/// <para>
/// <b>Escrow feature flag: OFF.</b> This PostgreSQL sink (two databases, one instance) is
/// the Phase 1.5 implementation. The escrow GA upgrade gate requires migrating to
/// S3 Object Lock or Azure Immutable Blob before the escrow feature can be enabled.
/// Chain divergence can be detected by comparing <c>AuditEvents.StateHash</c> with
/// <c>hash_chain_records.StateHash</c> for matching <c>EventId</c> values. A row present
/// in <c>AuditEvents</c> but absent in <c>hash_chain_records</c> indicates a sink failure;
/// a row present in <c>hash_chain_records</c> but absent in <c>AuditEvents</c> indicates
/// tampering with the primary store.
/// </para>
/// <para>
/// <b>Prometheus metrics:</b>
/// <list type="bullet">
///   <item><c>spaceos_hashsink_write_total</c> — incremented on every successful write.</item>
///   <item><c>spaceos_hashsink_write_failures_total</c> — incremented on every caught exception.</item>
/// </list>
/// Alerting thresholds (configure in Prometheus/Grafana):
/// <list type="bullet">
///   <item><c>rate(5m) &gt; 0</c> → WARNING: operator investigation required.</item>
///   <item><c>rate(5m) &gt; 10</c> → CRITICAL: escrow feature flag remains OFF.</item>
///   <item><c>failures/total &gt; 1%</c> → CRITICAL: PagerDuty webhook.</item>
/// </list>
/// </para>
/// </remarks>
internal sealed class PostgresHashSink : IExternalAuditSink
{
    // .NET 8 built-in metrics — no additional NuGet package required.
    private static readonly Meter _meter = new("SpaceOS.Infrastructure.AuditLog", "1.0");

    private static readonly Counter<long> _writeSuccessCounter =
        _meter.CreateCounter<long>(
            "spaceos_hashsink_write_total",
            description: "Total number of successful hash chain record writes to the audit sink database.");

    private static readonly Counter<long> _writeFailureCounter =
        _meter.CreateCounter<long>(
            "spaceos_hashsink_write_failures_total",
            description: "Total number of failed hash chain record writes to the audit sink database.");

    private readonly IDbContextFactory<HashSinkDbContext> _factory;
    private readonly ILogger<PostgresHashSink> _logger;

    /// <summary>
    /// Initialises a new <see cref="PostgresHashSink"/>.
    /// </summary>
    /// <param name="factory">Factory used to create short-lived <see cref="HashSinkDbContext"/> instances.</param>
    /// <param name="logger">Logger used to record sink write failures.</param>
    public PostgresHashSink(
        IDbContextFactory<HashSinkDbContext> factory,
        ILogger<PostgresHashSink> logger)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(logger);
        _factory = factory;
        _logger  = logger;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// A new <see cref="HashSinkDbContext"/> is created and disposed for each write
    /// so the context lifetime is fully decoupled from the calling HTTP request scope.
    /// The <c>InsertedAt</c> column is left at its default (<c>now()</c>) — the DB sets it.
    /// </remarks>
    public async Task WriteAsync(
        Guid           tenantId,
        string         eventType,
        string         stateHash,
        string         previousHash,
        DateTimeOffset occurredAt,
        CancellationToken ct = default)
    {
        try
        {
            await using var ctx = await _factory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(false);

            ctx.HashChainRecords.Add(new HashChainRecord
            {
                TenantId   = tenantId,
                // EventId is not surfaced through IExternalAuditSink — derive a stable ID
                // from the stateHash so the same event cannot produce duplicate sink rows
                // even if the dispatcher retries (hash chain guarantees uniqueness per event).
                EventId    = DeriveEventId(tenantId, stateHash, occurredAt),
                StateHash  = stateHash,
                OccurredAt = occurredAt,
                // InsertedAt: left as default — the database DEFAULT now() sets it
            });

            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);

            _writeSuccessCounter.Add(1);
        }
        catch (Exception ex)
        {
            _writeFailureCounter.Add(1);
            _logger.LogError(ex,
                "PostgresHashSink: write FAILED for TenantId={TenantId} StateHash={StateHash}",
                tenantId, stateHash);

            // NOT rethrown — a sink failure must never interrupt the primary audit write path.
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Returns hash records ordered by <see cref="HashChainRecord.OccurredAt"/> ascending.
    /// Used during chain verification to cross-validate the primary <c>AuditEvents</c> store.
    /// Returns an empty list on failure so a sink outage does not break the verify-chain query.
    /// </remarks>
    public async Task<IReadOnlyList<ExternalAuditHashRecord>> ReadHashesAsync(
        Guid tenantId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken ct = default)
    {
        try
        {
            await using var ctx = await _factory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(false);

            var query = ctx.HashChainRecords
                .AsNoTracking()
                .Where(r => r.TenantId == tenantId);

            if (from.HasValue)
                query = query.Where(r => r.OccurredAt >= from.Value);

            if (to.HasValue)
                query = query.Where(r => r.OccurredAt <= to.Value);

            var rows = await query
                .OrderBy(r => r.OccurredAt)
                .ThenBy(r => r.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return rows
                .Select(r => new ExternalAuditHashRecord(
                    OccurredAt:   r.OccurredAt,
                    TenantId:     r.TenantId,
                    EventType:    string.Empty,  // not stored in sink — type is in primary AuditEvents DB
                    PreviousHash: string.Empty,  // not stored in sink — chain links live in primary DB
                    StateHash:    r.StateHash))
                .ToList()
                .AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "PostgresHashSink: ReadHashesAsync FAILED for TenantId={TenantId}", tenantId);

            // Return empty so verify-chain reports ExternalSinkMatch=false rather than throwing.
            return Array.Empty<ExternalAuditHashRecord>();
        }
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Derives a deterministic <see cref="Guid"/> from the sink write parameters so the
    /// <c>EventId UNIQUE</c> constraint prevents duplicate rows when a retry occurs.
    /// Uses a name-based (version 5 / SHA-1 namespace) UUID approach via XOR folding
    /// of a SHA-256 of the combined key — sufficient for a deduplication gate.
    /// </summary>
    private static Guid DeriveEventId(Guid tenantId, string stateHash, DateTimeOffset occurredAt)
    {
        // Combine the three values that are unique per audit event in the hash chain.
        var combined = $"{tenantId:N}:{stateHash}:{occurredAt:O}";
        var bytes    = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(combined));

        // Fold to 16 bytes for Guid (XOR the two halves so we use all 32 bytes of SHA-256).
        var guidBytes = new byte[16];
        for (var i = 0; i < 16; i++)
            guidBytes[i] = (byte)(bytes[i] ^ bytes[i + 16]);

        // Set version bits (4 = randomly generated — closest to a derived UUID).
        guidBytes[6] = (byte)((guidBytes[6] & 0x0F) | 0x40);
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);

        return new Guid(guidBytes);
    }
}
