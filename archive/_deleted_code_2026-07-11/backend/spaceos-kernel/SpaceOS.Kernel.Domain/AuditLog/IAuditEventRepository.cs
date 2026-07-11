// SpaceOS.Kernel.Domain/AuditLog/IAuditEventRepository.cs

using Ardalis.Specification;

namespace SpaceOS.Kernel.Domain.AuditLog;

/// <summary>
/// Write-only repository for <see cref="AuditEvent"/> aggregates.
/// Audit events are append-only — no update or delete operations are exposed.
/// </summary>
public interface IAuditEventRepository
{
    /// <summary>
    /// Stages a new <see cref="AuditEvent"/> for insertion.
    /// Persist via <c>IUnitOfWork.SaveChangesAsync</c>.
    /// </summary>
    /// <param name="auditEvent">The audit event to append.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task AddAsync(AuditEvent auditEvent, CancellationToken ct = default);

    /// <summary>
    /// Returns all <see cref="AuditEvent"/> instances matching the given specification.
    /// </summary>
    /// <param name="specification">The filter/ordering specification to apply.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<IReadOnlyList<AuditEvent>> ListAsync(ISpecification<AuditEvent> specification, CancellationToken ct = default);

    /// <summary>
    /// Returns the total count of <see cref="AuditEvent"/> instances matching the given specification.
    /// </summary>
    /// <param name="specification">The filter specification (must not apply Skip/Take).</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<int> CountAsync(ISpecification<AuditEvent> specification, CancellationToken ct = default);

    /// <summary>Returns the <see cref="AuditEvent.StateHash"/> of the most recent audit event for the given tenant, or <c>"GENESIS"</c> if none exists.</summary>
    /// <param name="tenantId">The tenant whose latest hash is requested.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<string> GetLastHashAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>
    /// Returns all audit events for the given tenant within the optional date range,
    /// ordered by <see cref="AuditEvent.OccurredAt"/> ascending.
    /// Used for chain integrity verification.
    /// </summary>
    /// <param name="tenantId">The tenant whose chain to fetch.</param>
    /// <param name="from">Inclusive lower bound; <see langword="null"/> means unbounded.</param>
    /// <param name="to">Inclusive upper bound; <see langword="null"/> means unbounded.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<IReadOnlyList<AuditEvent>> GetChainAsync(
        Guid tenantId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken ct = default);
}
