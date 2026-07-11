// SpaceOS.Kernel.Domain/Specifications/WorkStationsByTenantPagedSpec.cs
using Ardalis.Specification;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Specifications;

/// <summary>Returns a single page of non-archived <see cref="WorkStation"/> aggregates belonging to the specified tenant.</summary>
public sealed class WorkStationsByTenantPagedSpec : PagedSpecification<WorkStation>
{
    /// <summary>Initialises the specification with a tenant filter and paging constraints.</summary>
    /// <param name="tenantId">The tenant to filter by.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Maximum items per page.</param>
    public WorkStationsByTenantPagedSpec(TenantId tenantId, int page, int pageSize)
        : base(page, pageSize)
    {
        Query.Where(ws => ws.TenantId == tenantId);
    }
}
