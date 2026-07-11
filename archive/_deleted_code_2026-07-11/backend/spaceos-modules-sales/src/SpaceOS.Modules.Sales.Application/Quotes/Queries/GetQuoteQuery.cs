using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Sales.Application.Common;
using SpaceOS.Modules.Sales.Application.DTOs;
using SpaceOS.Modules.Sales.Application.Quotes.Commands;
using SpaceOS.Modules.Sales.Domain.Interfaces;

namespace SpaceOS.Modules.Sales.Application.Quotes.Queries;

public sealed record GetQuoteQuery(Guid QuoteId) : IRequest<Result<QuoteResponse>>;

public sealed class GetQuoteQueryHandler(
    IQuoteRepository quotes,
    ITenantContext tenantContext) : IRequestHandler<GetQuoteQuery, Result<QuoteResponse>>
{
    public async Task<Result<QuoteResponse>> Handle(GetQuoteQuery query, CancellationToken ct)
    {
        var quote = await quotes.GetByIdWithLinesAsync(query.QuoteId, ct).ConfigureAwait(false);
        if (quote is null) return Result.NotFound();
        var guard = tenantContext.EnsureSameTenant(quote);
        if (!guard.IsSuccess) return guard;
        return Result.Success(CreateQuoteCommandHandler.MapToResponse(quote));
    }
}
