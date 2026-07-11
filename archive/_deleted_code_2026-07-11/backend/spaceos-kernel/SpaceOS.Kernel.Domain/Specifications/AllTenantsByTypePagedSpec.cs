// SpaceOS.Kernel.Domain/Specifications/AllTenantsByTypePagedSpec.cs
using Ardalis.Specification;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Enums;

namespace SpaceOS.Kernel.Domain.Specifications;

/// <summary>Returns a single page of <see cref="Tenant"/> aggregates filtered by <see cref="TenantType"/>.</summary>
public sealed class AllTenantsByTypePagedSpec : PagedSpecification<Tenant>
{
    /// <summary>Initialises the specification with a type filter and paging constraints.</summary>
    /// <param name="type">The ecosystem actor type to filter by.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Maximum items per page.</param>
    public AllTenantsByTypePagedSpec(TenantType type, int page, int pageSize) : base(page, pageSize) =>
        Query.Where(t => t.TenantType == type);
}
