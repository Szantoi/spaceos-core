// SpaceOS.Kernel.Domain/AuditLog/Specifications/AuditEventsByTenantPagedSpec.cs

using Ardalis.Specification;
using SpaceOS.Kernel.Domain.Specifications;

namespace SpaceOS.Kernel.Domain.AuditLog.Specifications;

/// <summary>
/// Returns a single page of <see cref="AuditEvent"/> records belonging to the specified tenant,
/// optionally filtered by an inclusive date range, ordered by <see cref="AuditEvent.OccurredAt"/> descending.
/// </summary>
public sealed class AuditEventsByTenantPagedSpec : PagedSpecification<AuditEvent>
{
    /// <summary>
    /// Initialises the specification with a tenant filter, optional date range, and paging constraints.
    /// </summary>
    /// <param name="tenantId">The tenant whose audit events to retrieve. <see langword="null"/> matches all tenants.</param>
    /// <param name="eventType">The event type name to filter by (e.g. TenantCreatedEvent). <see langword="null"/> matches all types.</param>
    /// <param name="from">Inclusive lower bound for <see cref="AuditEvent.OccurredAt"/>. <see langword="null"/> means no lower bound.</param>
    /// <param name="to">Inclusive upper bound for <see cref="AuditEvent.OccurredAt"/>. <see langword="null"/> means no upper bound.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Maximum items per page.</param>
    public AuditEventsByTenantPagedSpec(
        Guid? tenantId,
        string? eventType,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize)
        : base(page, pageSize)
    {
        Query
            .Where(e => tenantId == null || e.TenantId == tenantId.Value)
            .Where(e => eventType == null || e.EventType == eventType)
            .Where(e => from == null || e.OccurredAt >= from.Value)
            .Where(e => to == null || e.OccurredAt <= to.Value)
            .OrderByDescending(e => e.OccurredAt);
    }
}
