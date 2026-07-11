using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Sales.Application.Common;
using SpaceOS.Modules.Sales.Application.DTOs;
using SpaceOS.Modules.Sales.Application.Specifications.Quotes;
using SpaceOS.Modules.Sales.Domain.Enums;
using SpaceOS.Modules.Sales.Domain.Interfaces;

namespace SpaceOS.Modules.Sales.Application.Pipeline.Queries;

public sealed record GetSalesFunnelQuery(DateTimeOffset? From, DateTimeOffset? To)
    : IRequest<Result<SalesFunnelResponse>>;

public sealed class GetSalesFunnelQueryHandler(
    IQuoteRepository quotes,
    ITenantContext tenantContext) : IRequestHandler<GetSalesFunnelQuery, Result<SalesFunnelResponse>>
{
    public async Task<Result<SalesFunnelResponse>> Handle(GetSalesFunnelQuery query, CancellationToken ct)
    {
        var spec = new QuotesByStatusFunnelSpec(tenantContext.TenantId, query.From, query.To);
        var all = await quotes.ListAsync(spec, ct).ConfigureAwait(false);

        var stages = all
            .GroupBy(q => q.Status)
            .Select(g => new FunnelStageDto(
                g.Key,
                g.Count(),
                g.Sum(q => q.TotalGross.Amount)))
            .OrderBy(s => s.Status)
            .ToList();

        // Ensure all statuses are present even with zero count
        var allStatuses = Enum.GetValues<QuoteStatus>().ToList();
        foreach (var s in allStatuses.Where(s => stages.All(x => x.Status != s)))
            stages.Add(new FunnelStageDto(s, 0, 0m));

        return Result.Success(new SalesFunnelResponse(
            stages.OrderBy(s => s.Status).ToList()));
    }
}
