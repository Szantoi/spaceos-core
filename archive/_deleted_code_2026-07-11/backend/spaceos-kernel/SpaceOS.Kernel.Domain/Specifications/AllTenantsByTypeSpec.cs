// SpaceOS.Kernel.Domain/Specifications/AllTenantsByTypeSpec.cs
using Ardalis.Specification;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Enums;

namespace SpaceOS.Kernel.Domain.Specifications;

/// <summary>Returns all <see cref="Tenant"/> aggregates that match the given <see cref="TenantType"/>.</summary>
public sealed class AllTenantsByTypeSpec : Specification<Tenant>
{
    /// <summary>Initialises the specification with a <see cref="TenantType"/> filter.</summary>
    /// <param name="type">The ecosystem actor type to filter by.</param>
    public AllTenantsByTypeSpec(TenantType type) =>
        Query.Where(t => t.TenantType == type);
}
