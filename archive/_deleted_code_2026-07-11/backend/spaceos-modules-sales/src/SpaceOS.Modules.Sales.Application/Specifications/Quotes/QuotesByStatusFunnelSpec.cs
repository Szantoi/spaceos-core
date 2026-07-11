using Ardalis.Specification;
using SpaceOS.Modules.Sales.Domain.Aggregates;

namespace SpaceOS.Modules.Sales.Application.Specifications.Quotes;

/// <summary>Loads all non-archived quotes for a tenant to build the pipeline funnel read-model.</summary>
public sealed class QuotesByStatusFunnelSpec : Specification<Quote>
{
    public QuotesByStatusFunnelSpec(Guid tenantId, DateTimeOffset? from, DateTimeOffset? to)
    {
        Query.Where(q => q.TenantId == tenantId && !q.IsArchived).AsNoTracking();
        if (from.HasValue) Query.Where(q => q.CreatedAt >= from.Value);
        if (to.HasValue) Query.Where(q => q.CreatedAt <= to.Value);
    }
}
