using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Sales.Application.Common;
using SpaceOS.Modules.Sales.Application.DTOs;
using SpaceOS.Modules.Sales.Domain.Common;
using SpaceOS.Modules.Sales.Domain.Interfaces;

namespace SpaceOS.Modules.Sales.Application.Quotes.Commands;

public sealed record AcceptQuoteCommand(Guid QuoteId) : IRequest<Result<QuoteResponse>>;

public sealed class AcceptQuoteCommandHandler(
    IQuoteRepository quotes,
    ITenantContext tenantContext,
    IClock clock) : IRequestHandler<AcceptQuoteCommand, Result<QuoteResponse>>
{
    public async Task<Result<QuoteResponse>> Handle(AcceptQuoteCommand cmd, CancellationToken ct)
    {
        var quote = await quotes.GetByIdWithLinesAsync(cmd.QuoteId, ct).ConfigureAwait(false);
        if (quote is null) return Result.NotFound();
        var guard = tenantContext.EnsureSameTenant(quote);
        if (!guard.IsSuccess) return guard;

        var result = quote.Accept(clock);
        if (!result.IsSuccess) return Result.Invalid(result.ValidationErrors.ToArray());

        quotes.Update(quote);
        await quotes.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Success(CreateQuoteCommandHandler.MapToResponse(quote));
    }
}
