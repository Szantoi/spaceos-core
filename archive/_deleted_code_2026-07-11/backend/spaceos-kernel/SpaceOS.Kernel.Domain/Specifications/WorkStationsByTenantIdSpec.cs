// SpaceOS.Kernel.Domain/Specifications/WorkStationsByTenantIdSpec.cs
using Ardalis.Specification;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Specifications;

/// <summary>
/// Returns all non-archived <see cref="WorkStation"/> instances belonging to the specified tenant.
/// </summary>
public sealed class WorkStationsByTenantIdSpec : Specification<WorkStation>
{
    /// <summary>Initialises the specification with a tenant filter.</summary>
    /// <param name="tenantId">The tenant to filter by.</param>
    public WorkStationsByTenantIdSpec(TenantId tenantId)
    {
        Query.Where(ws => ws.TenantId == tenantId);
    }
}
