using Ardalis.Specification;
using SpaceOS.Modules.Sales.Domain.Aggregates;

namespace SpaceOS.Modules.Sales.Domain.Interfaces;

/// <summary>Repository contract for the Quote aggregate.</summary>
public interface IQuoteRepository
{
    Task<Quote?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Quote?> GetByIdWithLinesAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Quote>> ListAsync(ISpecification<Quote> spec, CancellationToken ct);
    Task AddAsync(Quote quote, CancellationToken ct);
    void Update(Quote quote);
    Task<int> SaveChangesAsync(CancellationToken ct);
    Task<int> CountActiveAsync(Guid tenantId, CancellationToken ct);
}
