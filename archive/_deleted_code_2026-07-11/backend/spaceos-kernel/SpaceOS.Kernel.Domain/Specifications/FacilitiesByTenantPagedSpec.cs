// SpaceOS.Kernel.Domain/Specifications/FacilitiesByTenantPagedSpec.cs
using Ardalis.Specification;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Specifications;

/// <summary>Returns a single page of <see cref="Facility"/> aggregates belonging to the specified tenant.</summary>
public sealed class FacilitiesByTenantPagedSpec : PagedSpecification<Facility>
{
    /// <summary>Initialises the specification with a tenant filter and paging constraints.</summary>
    /// <param name="tenantId">The tenant to filter by.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Maximum items per page.</param>
    public FacilitiesByTenantPagedSpec(TenantId tenantId, int page, int pageSize)
        : base(page, pageSize)
    {
        Query.Where(f => f.TenantId == tenantId);
    }
}
