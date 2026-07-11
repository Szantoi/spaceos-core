// SpaceOS.Kernel.Application/AuditLog/IExternalAuditSink.cs
namespace SpaceOS.Kernel.Application.AuditLog;

/// <summary>
/// A single hash record as persisted in the external audit sink.
/// Used to cross-validate the primary audit chain.
/// </summary>
/// <param name="OccurredAt">The UTC timestamp at which the domain event occurred.</param>
/// <param name="TenantId">The owning tenant identifier.</param>
/// <param name="EventType">The fully-qualified domain event type name.</param>
/// <param name="PreviousHash">The SHA-256 hash of the preceding event in the chain.</param>
/// <param name="StateHash">The SHA-256 hex digest of the current event state.</param>
public sealed record ExternalAuditHashRecord(
    DateTimeOffset OccurredAt,
    Guid TenantId,
    string EventType,
    string PreviousHash,
    string StateHash);

/// <summary>
/// Append-and-read external sink for audit event hashes.
/// Write operations must be append-only and fire-and-forget safe.
/// Read operations are used during chain verification.
/// </summary>
public interface IExternalAuditSink
{
    /// <summary>
    /// Appends the hash record to the external sink.
    /// Failures are swallowed — a sink outage must not block the primary audit write.
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant that owns the audit event.</param>
    /// <param name="eventType">The fully-qualified domain event type name.</param>
    /// <param name="stateHash">The SHA-256 hex digest of the current event state.</param>
    /// <param name="previousHash">The SHA-256 hash of the preceding event in the chain.</param>
    /// <param name="occurredAt">The UTC timestamp at which the domain event occurred.</param>
    /// <param name="ct">Cancellation token.</param>
    Task WriteAsync(
        Guid           tenantId,
        string         eventType,
        string         stateHash,
        string         previousHash,
        DateTimeOffset occurredAt,
        CancellationToken ct = default);

    /// <summary>
    /// Returns hash records from the sink for the given tenant and optional date range,
    /// ordered by <see cref="ExternalAuditHashRecord.OccurredAt"/> ascending.
    /// Returns an empty list when the sink cannot be read.
    /// </summary>
    /// <param name="tenantId">The tenant whose records to retrieve.</param>
    /// <param name="from">Inclusive lower bound on <see cref="ExternalAuditHashRecord.OccurredAt"/>; <see langword="null"/> means unbounded.</param>
    /// <param name="to">Inclusive upper bound on <see cref="ExternalAuditHashRecord.OccurredAt"/>; <see langword="null"/> means unbounded.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<ExternalAuditHashRecord>> ReadHashesAsync(
        Guid tenantId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken ct = default);
}
