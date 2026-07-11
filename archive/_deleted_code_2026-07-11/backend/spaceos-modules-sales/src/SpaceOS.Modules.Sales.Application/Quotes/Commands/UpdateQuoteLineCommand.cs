using Ardalis.Result;
using FluentValidation;
using MediatR;
using SpaceOS.Modules.Sales.Application.Common;
using SpaceOS.Modules.Sales.Application.DTOs;
using SpaceOS.Modules.Sales.Domain.Entities;
using SpaceOS.Modules.Sales.Domain.Enums;
using SpaceOS.Modules.Sales.Domain.Interfaces;
using SpaceOS.Modules.Sales.Domain.ValueObjects;

namespace SpaceOS.Modules.Sales.Application.Quotes.Commands;

public sealed record UpdateQuoteLineCommand(
    Guid QuoteId,
    Guid LineId,
    QuoteLineType LineType,
    Guid? SourceTemplateId,
    string Description,
    decimal Quantity,
    decimal UnitPriceAmount,
    decimal VatRate,
    decimal? DiscountPercent,
    int SortOrder) : IRequest<Result<QuoteResponse>>;

public sealed class UpdateQuoteLineCommandValidator : AbstractValidator<UpdateQuoteLineCommand>
{
    public UpdateQuoteLineCommandValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPriceAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.VatRate).InclusiveBetween(0, 1);
        RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 1).When(x => x.DiscountPercent.HasValue);
    }
}

public sealed class UpdateQuoteLineCommandHandler(
    IQuoteRepository quotes,
    ITenantContext tenantContext) : IRequestHandler<UpdateQuoteLineCommand, Result<QuoteResponse>>
{
    public async Task<Result<QuoteResponse>> Handle(UpdateQuoteLineCommand cmd, CancellationToken ct)
    {
        var quote = await quotes.GetByIdWithLinesAsync(cmd.QuoteId, ct).ConfigureAwait(false);
        if (quote is null) return Result.NotFound();
        var guard = tenantContext.EnsureSameTenant(quote);
        if (!guard.IsSuccess) return guard;

        // Remove old, add new with updated data
        var removeResult = quote.RemoveLine(cmd.LineId);
        if (!removeResult.IsSuccess) return Result.Invalid(removeResult.ValidationErrors.ToArray());

        var unitPrice = new Money(cmd.UnitPriceAmount, quote.Currency);
        var lineResult = QuoteLine.Create(
            quote.TenantId, cmd.LineType, cmd.SourceTemplateId,
            cmd.Description, cmd.Quantity, unitPrice,
            cmd.VatRate, cmd.DiscountPercent, cmd.SortOrder);
        if (!lineResult.IsSuccess) return Result.Invalid(lineResult.ValidationErrors.ToArray());

        var addResult = quote.AddLine(lineResult.Value);
        if (!addResult.IsSuccess) return Result.Invalid(addResult.ValidationErrors.ToArray());

        quotes.Update(quote);
        await quotes.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Success(CreateQuoteCommandHandler.MapToResponse(quote));
    }
}
