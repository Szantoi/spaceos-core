using Ardalis.Specification;
using SpaceOS.Modules.Sales.Domain.Aggregates;

namespace SpaceOS.Modules.Sales.Application.Specifications.Customers;

/// <summary>Loads a single customer by ID (tracking — for mutations).</summary>
public sealed class CustomerByIdSpec : Specification<Customer>
{
    public CustomerByIdSpec(Guid id)
    {
        Query.Where(c => c.Id == id);
    }
}
