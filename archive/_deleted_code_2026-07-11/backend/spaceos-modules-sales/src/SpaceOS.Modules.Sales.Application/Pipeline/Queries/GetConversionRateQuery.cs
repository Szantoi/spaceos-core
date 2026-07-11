using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Sales.Application.Common;
using SpaceOS.Modules.Sales.Application.DTOs;
using SpaceOS.Modules.Sales.Application.Specifications.Quotes;
using SpaceOS.Modules.Sales.Domain.Enums;
using SpaceOS.Modules.Sales.Domain.Interfaces;

namespace SpaceOS.Modules.Sales.Application.Pipeline.Queries;

public sealed record GetConversionRateQuery(DateTimeOffset? From, DateTimeOffset? To)
    : IRequest<Result<ConversionRateResponse>>;

public sealed class GetConversionRateQueryHandler(
    IQuoteRepository quotes,
    ITenantContext tenantContext) : IRequestHandler<GetConversionRateQuery, Result<ConversionRateResponse>>
{
    public async Task<Result<ConversionRateResponse>> Handle(GetConversionRateQuery query, CancellationToken ct)
    {
        var spec = new QuotesByStatusFunnelSpec(tenantContext.TenantId, query.From, query.To);
        var all = await quotes.ListAsync(spec, ct).ConfigureAwait(false);

        var totalSent = all.Count(q => q.Status >= QuoteStatus.Sent);
        var totalAccepted = all.Count(q => q.Status >= QuoteStatus.Accepted && q.Status != QuoteStatus.Rejected);
        var totalConverted = all.Count(q => q.Status == QuoteStatus.Converted);

        decimal sentToAccepted = totalSent > 0 ? Math.Round((decimal)totalAccepted / totalSent, 4) : 0m;
        decimal acceptedToConverted = totalAccepted > 0 ? Math.Round((decimal)totalConverted / totalAccepted, 4) : 0m;

        return Result.Success(new ConversionRateResponse(
            totalSent, totalAccepted, totalConverted, sentToAccepted, acceptedToConverted));
    }
}
