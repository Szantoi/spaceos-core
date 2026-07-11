using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Sales.Domain.Aggregates;
using SpaceOS.Modules.Sales.Domain.Interfaces;
using SpaceOS.Modules.Sales.Infrastructure.Persistence;

namespace SpaceOS.Modules.Sales.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IQuoteRepository"/>.</summary>
internal sealed class QuoteRepository(SalesDbContext db) : IQuoteRepository
{
    /// <inheritdoc/>
    public async Task<Quote?> GetByIdAsync(Guid id, CancellationToken ct)
        => await db.Quotes.FirstOrDefaultAsync(q => q.Id == id, ct).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<Quote?> GetByIdWithLinesAsync(Guid id, CancellationToken ct)
        => await db.Quotes
            .Include(q => q.Lines)
            .FirstOrDefaultAsync(q => q.Id == id, ct)
            .ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Quote>> ListAsync(ISpecification<Quote> spec, CancellationToken ct)
        => await db.Quotes.WithSpecification(spec).ToListAsync(ct).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task AddAsync(Quote quote, CancellationToken ct)
        => await db.Quotes.AddAsync(quote, ct).ConfigureAwait(false);

    /// <inheritdoc/>
    public void Update(Quote quote) => db.Quotes.Update(quote);

    /// <inheritdoc/>
    public async Task<int> SaveChangesAsync(CancellationToken ct)
        => await db.SaveChangesAsync(ct).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<int> CountActiveAsync(Guid tenantId, CancellationToken ct)
        => await db.Quotes
            .CountAsync(q => q.TenantId == tenantId && !q.IsArchived, ct)
            .ConfigureAwait(false);
}
