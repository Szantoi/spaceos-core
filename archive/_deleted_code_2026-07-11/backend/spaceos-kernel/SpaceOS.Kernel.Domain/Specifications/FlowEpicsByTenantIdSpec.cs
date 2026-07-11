// SpaceOS.Kernel.Domain/Specifications/FlowEpicsByTenantIdSpec.cs
using Ardalis.Specification;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Specifications;

/// <summary>
/// Returns all non-archived <see cref="FlowEpic"/> instances belonging to the specified tenant.
/// </summary>
public sealed class FlowEpicsByTenantIdSpec : Specification<FlowEpic>
{
    /// <summary>Initialises the specification with a tenant filter.</summary>
    /// <param name="tenantId">The tenant to filter by.</param>
    public FlowEpicsByTenantIdSpec(TenantId tenantId)
    {
        Query.Where(e => e.TenantId == tenantId);
    }
}
