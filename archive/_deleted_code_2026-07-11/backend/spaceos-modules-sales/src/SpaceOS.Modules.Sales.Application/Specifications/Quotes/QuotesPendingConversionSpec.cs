using Ardalis.Specification;
using SpaceOS.Modules.Sales.Domain.Aggregates;
using SpaceOS.Modules.Sales.Domain.Enums;

namespace SpaceOS.Modules.Sales.Application.Specifications.Quotes;

/// <summary>Quotes that are Accepted with a pending conversion request (UI badge / worker polling UI).</summary>
public sealed class QuotesPendingConversionSpec : Specification<Quote>
{
    public QuotesPendingConversionSpec(Guid tenantId)
    {
        Query
            .Where(q => q.TenantId == tenantId
                && q.Status == QuoteStatus.Accepted
                && q.ConversionRequestedAt != null
                && !q.IsArchived)
            .AsNoTracking();
    }
}
