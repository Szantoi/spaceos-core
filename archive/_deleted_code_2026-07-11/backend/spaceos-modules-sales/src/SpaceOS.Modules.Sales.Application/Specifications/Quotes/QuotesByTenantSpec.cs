using Ardalis.Specification;
using SpaceOS.Modules.Sales.Domain.Aggregates;
using SpaceOS.Modules.Sales.Domain.Enums;

namespace SpaceOS.Modules.Sales.Application.Specifications.Quotes;

/// <summary>Filtered, paged list of quotes for a tenant.</summary>
public sealed class QuotesByTenantSpec : Specification<Quote>
{
    public QuotesByTenantSpec(
        Guid tenantId,
        QuoteStatus? status,
        Guid? customerId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int skip,
        int take)
    {
        Query.Where(q => q.TenantId == tenantId && !q.IsArchived).AsNoTracking();
        if (status is not null)
            Query.Where(q => q.Status == status);
        if (customerId.HasValue)
            Query.Where(q => q.CustomerId == customerId.Value);
        if (from.HasValue)
            Query.Where(q => q.CreatedAt >= from.Value);
        if (to.HasValue)
            Query.Where(q => q.CreatedAt <= to.Value);
        Query.OrderByDescending(q => q.CreatedAt).Skip(skip).Take(take);
    }
}
