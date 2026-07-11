using Ardalis.Result;
using FluentValidation;
using MediatR;
using SpaceOS.Modules.Sales.Application.Common;
using SpaceOS.Modules.Sales.Application.DTOs;
using SpaceOS.Modules.Sales.Domain.Common;
using SpaceOS.Modules.Sales.Domain.Interfaces;

namespace SpaceOS.Modules.Sales.Application.Quotes.Commands;

public sealed record RejectQuoteCommand(Guid QuoteId, string Reason) : IRequest<Result<QuoteResponse>>;

public sealed class RejectQuoteCommandValidator : AbstractValidator<RejectQuoteCommand>
{
    public RejectQuoteCommandValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public sealed class RejectQuoteCommandHandler(
    IQuoteRepository quotes,
    ITenantContext tenantContext,
    IClock clock) : IRequestHandler<RejectQuoteCommand, Result<QuoteResponse>>
{
    public async Task<Result<QuoteResponse>> Handle(RejectQuoteCommand cmd, CancellationToken ct)
    {
        var quote = await quotes.GetByIdWithLinesAsync(cmd.QuoteId, ct).ConfigureAwait(false);
        if (quote is null) return Result.NotFound();
        var guard = tenantContext.EnsureSameTenant(quote);
        if (!guard.IsSuccess) return guard;

        var result = quote.Reject(cmd.Reason, clock);
        if (!result.IsSuccess) return Result.Invalid(result.ValidationErrors.ToArray());

        quotes.Update(quote);
        await quotes.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Success(CreateQuoteCommandHandler.MapToResponse(quote));
    }
}
