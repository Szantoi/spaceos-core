using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Sales.Application.Common;
using SpaceOS.Modules.Sales.Application.DTOs;
using SpaceOS.Modules.Sales.Application.Quotes.Commands;
using SpaceOS.Modules.Sales.Application.Specifications.Quotes;
using SpaceOS.Modules.Sales.Domain.Enums;
using SpaceOS.Modules.Sales.Domain.Interfaces;

namespace SpaceOS.Modules.Sales.Application.Quotes.Queries;

public sealed record ListQuotesQuery(
    QuoteStatus? Status,
    Guid? CustomerId,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int Skip = 0,
    int Take = 50) : IRequest<Result<SalesPagedResult<QuoteSummary>>>;

public sealed class ListQuotesQueryHandler(
    IQuoteRepository quotes,
    ITenantContext tenantContext) : IRequestHandler<ListQuotesQuery, Result<SalesPagedResult<QuoteSummary>>>
{
    public async Task<Result<SalesPagedResult<QuoteSummary>>> Handle(ListQuotesQuery query, CancellationToken ct)
    {
        var spec = new QuotesByTenantSpec(
            tenantContext.TenantId, query.Status, query.CustomerId,
            query.From, query.To, query.Skip, query.Take);
        var list = await quotes.ListAsync(spec, ct).ConfigureAwait(false);
        var total = await quotes.CountActiveAsync(tenantContext.TenantId, ct).ConfigureAwait(false);

        var items = list.Select(q => new QuoteSummary(
            q.Id, q.CustomerId, q.Number.Value, q.Status, q.Currency,
            q.TotalGross.Amount, q.CreatedAt, q.IsArchived)).ToList();

        return Result.Success(new SalesPagedResult<QuoteSummary>(items, total, query.Skip, query.Take));
    }
}
