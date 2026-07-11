using Ardalis.Specification;
using SpaceOS.Kernel.Domain.Entities;

namespace SpaceOS.Kernel.Domain.Specifications;

/// <summary>Returns all <see cref="Tenant"/> aggregates with no filter applied.</summary>
public sealed class AllTenantsSpec : Specification<Tenant>;
