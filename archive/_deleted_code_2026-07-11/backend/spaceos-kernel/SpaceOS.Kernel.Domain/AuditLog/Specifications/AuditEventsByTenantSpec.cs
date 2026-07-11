// SpaceOS.Kernel.Domain/AuditLog/Specifications/AuditEventsByTenantSpec.cs

using Ardalis.Specification;

namespace SpaceOS.Kernel.Domain.AuditLog.Specifications;

/// <summary>
/// Returns all <see cref="AuditEvent"/> records belonging to the specified tenant,
/// ordered by <see cref="AuditEvent.OccurredAt"/> descending (most recent first).
/// </summary>
public sealed class AuditEventsByTenantSpec : Specification<AuditEvent>
{
    /// <summary>
    /// Initialises the specification for the given tenant identifier.
    /// </summary>
    /// <param name="tenantId">The tenant whose audit events to retrieve.</param>
    public AuditEventsByTenantSpec(Guid tenantId)
    {
        Query
            .Where(e => e.TenantId == tenantId)
            .OrderByDescending(e => e.OccurredAt);
    }
}
