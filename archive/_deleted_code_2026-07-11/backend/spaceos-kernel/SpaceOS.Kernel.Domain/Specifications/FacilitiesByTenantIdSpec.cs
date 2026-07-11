using Ardalis.Specification;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Specifications;

/// <summary>
/// Returns all <see cref="Facility"/> instances belonging to the specified tenant.
/// </summary>
public sealed class FacilitiesByTenantIdSpec : Specification<Facility>
{
    public FacilitiesByTenantIdSpec(TenantId tenantId)
    {
        Query.Where(f => f.TenantId == tenantId);
    }
}
