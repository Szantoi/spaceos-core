// SpaceOS.Kernel.Domain/Specifications/AllTenantsPagedSpec.cs
using SpaceOS.Kernel.Domain.Entities;

namespace SpaceOS.Kernel.Domain.Specifications;

/// <summary>Returns a single page of all <see cref="Tenant"/> aggregates with no filter.</summary>
public sealed class AllTenantsPagedSpec : PagedSpecification<Tenant>
{
    /// <summary>Initialises the specification with the requested page and page size.</summary>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Maximum items per page.</param>
    public AllTenantsPagedSpec(int page, int pageSize) : base(page, pageSize) { }
}
