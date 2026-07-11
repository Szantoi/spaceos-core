// SpaceOS.Kernel.Domain/AuditLog/Specifications/AuditEventsByTenantFilterSpec.cs

using Ardalis.Specification;

namespace SpaceOS.Kernel.Domain.AuditLog.Specifications;

/// <summary>
/// Returns all <see cref="AuditEvent"/> records belonging to the specified tenant,
/// optionally filtered by an inclusive date range.
/// Use this specification for count queries — it applies no Skip/Take.
/// </summary>
public sealed class AuditEventsByTenantFilterSpec : Specification<AuditEvent>
{
    /// <summary>
    /// Initialises the specification with a tenant filter and optional date range.
    /// </summary>
    /// <param name="tenantId">The tenant whose audit events to count. <see langword="null"/> matches all tenants.</param>
    /// <param name="eventType">The event type name to filter by (e.g. TenantCreatedEvent). <see langword="null"/> matches all types.</param>
    /// <param name="from">Inclusive lower bound for <see cref="AuditEvent.OccurredAt"/>. <see langword="null"/> means no lower bound.</param>
    /// <param name="to">Inclusive upper bound for <see cref="AuditEvent.OccurredAt"/>. <see langword="null"/> means no upper bound.</param>
    public AuditEventsByTenantFilterSpec(
        Guid? tenantId,
        string? eventType,
        DateTimeOffset? from,
        DateTimeOffset? to)
    {
        Query
            .Where(e => tenantId == null || e.TenantId == tenantId.Value)
            .Where(e => eventType == null || e.EventType == eventType)
            .Where(e => from == null || e.OccurredAt >= from.Value)
            .Where(e => to == null || e.OccurredAt <= to.Value);
    }
}
