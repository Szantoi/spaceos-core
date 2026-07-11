using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Sales.Domain.Aggregates;
using SpaceOS.Modules.Sales.Domain.Interfaces;
using SpaceOS.Modules.Sales.Infrastructure.Persistence;

namespace SpaceOS.Modules.Sales.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="ICustomerRepository"/>.</summary>
internal sealed class CustomerRepository(SalesDbContext db) : ICustomerRepository
{
    /// <inheritdoc/>
    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct)
        => await db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Customer>> ListAsync(ISpecification<Customer> spec, CancellationToken ct)
        => await db.Customers.WithSpecification(spec).ToListAsync(ct).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task AddAsync(Customer customer, CancellationToken ct)
        => await db.Customers.AddAsync(customer, ct).ConfigureAwait(false);

    /// <inheritdoc/>
    public void Update(Customer customer) => db.Customers.Update(customer);

    /// <inheritdoc/>
    public async Task<int> SaveChangesAsync(CancellationToken ct)
        => await db.SaveChangesAsync(ct).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<int> CountActiveAsync(Guid tenantId, CancellationToken ct)
        => await db.Customers
            .CountAsync(c => c.TenantId == tenantId && !c.IsArchived, ct)
            .ConfigureAwait(false);
}
