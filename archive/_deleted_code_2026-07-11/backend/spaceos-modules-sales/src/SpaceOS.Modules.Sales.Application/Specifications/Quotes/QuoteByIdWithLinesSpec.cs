using Ardalis.Specification;
using SpaceOS.Modules.Sales.Domain.Aggregates;

namespace SpaceOS.Modules.Sales.Application.Specifications.Quotes;

/// <summary>Loads a quote with all its lines (tracking — for mutations).</summary>
public sealed class QuoteByIdWithLinesSpec : Specification<Quote>
{
    public QuoteByIdWithLinesSpec(Guid id)
    {
        Query.Where(q => q.Id == id);
    }
}
