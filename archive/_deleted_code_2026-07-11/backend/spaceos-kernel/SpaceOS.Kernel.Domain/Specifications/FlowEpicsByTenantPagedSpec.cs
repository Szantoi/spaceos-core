// SpaceOS.Kernel.Domain/Specifications/FlowEpicsByTenantPagedSpec.cs
using Ardalis.Specification;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Specifications;

/// <summary>Returns a single page of non-archived <see cref="FlowEpic"/> aggregates belonging to the specified tenant.</summary>
public sealed class FlowEpicsByTenantPagedSpec : PagedSpecification<FlowEpic>
{
    /// <summary>Initialises the specification with a tenant filter and paging constraints.</summary>
    /// <param name="tenantId">The tenant to filter by.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Maximum items per page.</param>
    public FlowEpicsByTenantPagedSpec(TenantId tenantId, int page, int pageSize)
        : base(page, pageSize)
    {
        Query.Where(e => e.TenantId == tenantId);
    }
}
