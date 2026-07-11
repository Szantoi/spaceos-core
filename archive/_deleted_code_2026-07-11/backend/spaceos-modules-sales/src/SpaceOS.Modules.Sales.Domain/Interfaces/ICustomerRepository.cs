using Ardalis.Specification;
using SpaceOS.Modules.Sales.Domain.Aggregates;

namespace SpaceOS.Modules.Sales.Domain.Interfaces;

/// <summary>Repository contract for the Customer aggregate.</summary>
public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Customer>> ListAsync(ISpecification<Customer> spec, CancellationToken ct);
    Task AddAsync(Customer customer, CancellationToken ct);
    void Update(Customer customer);
    Task<int> SaveChangesAsync(CancellationToken ct);
    Task<int> CountActiveAsync(Guid tenantId, CancellationToken ct);
}
