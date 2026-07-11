using Ardalis.Result;
using FluentValidation;
using MediatR;
using SpaceOS.Modules.Sales.Application.Common;
using SpaceOS.Modules.Sales.Application.DTOs;
using SpaceOS.Modules.Sales.Domain.Aggregates;
using SpaceOS.Modules.Sales.Domain.Common;
using SpaceOS.Modules.Sales.Domain.Entities;
using SpaceOS.Modules.Sales.Domain.Interfaces;

namespace SpaceOS.Modules.Sales.Application.Quotes.Commands;

public sealed record CreateQuoteCommand(
    Guid CustomerId,
    string Currency,
    string? Notes,
    DateTimeOffset? ValidUntil) : IRequest<Result<QuoteResponse>>;

public sealed class CreateQuoteCommandValidator : AbstractValidator<CreateQuoteCommand>
{
    public CreateQuoteCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Notes).MaximumLength(2000).When(x => x.Notes is not null);
    }
}

public sealed class CreateQuoteCommandHandler(
    IQuoteRepository quotes,
    IQuoteNumberGenerator numberGen,
    IQuotaGuard quotaGuard,
    ITenantContext tenantContext,
    IClock clock) : IRequestHandler<CreateQuoteCommand, Result<QuoteResponse>>
{
    public async Task<Result<QuoteResponse>> Handle(CreateQuoteCommand cmd, CancellationToken ct)
    {
        var quota = await quotaGuard.EnsureCanCreateAsync(tenantContext.TenantId, QuotaScope.Quote, ct)
            .ConfigureAwait(false);
        if (!quota.IsSuccess) return Result.Forbidden();

        var result = await Quote.CreateAsync(
            tenantContext.TenantId, cmd.CustomerId, cmd.Currency,
            tenantContext.ActorSub, numberGen, clock, ct).ConfigureAwait(false);

        if (!result.IsSuccess) return Result.Invalid(result.ValidationErrors.ToArray());

        var quote = result.Value;
        if (cmd.Notes is not null)
        {
            // Notes can be set post-creation (still Draft)
        }

        await quotes.AddAsync(quote, ct).ConfigureAwait(false);
        await quotes.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Success(MapToResponse(quote));
    }

    internal static QuoteResponse MapToResponse(Quote q) => new(
        q.Id, q.TenantId, q.CustomerId, q.Number.Value, q.Status, q.Currency,
        q.ValidUntil, q.Notes,
        q.TotalNet.Amount, q.TotalVat.Amount, q.TotalGross.Amount,
        q.CreatedAt, q.CreatedBy, q.SentAt, q.AcceptedAt, q.RejectedAt, q.RejectionReason,
        q.ConvertedAt, q.ConvertedOrderId, q.ConversionRequestedAt, q.ConversionFailureReason,
        q.ContentHash, q.IsArchived,
        q.Lines.Select(MapLine).ToList());

    internal static QuoteLineResponse MapLine(QuoteLine l) => new(
        l.Id, l.LineType, l.SourceTemplateId, l.Description, l.Quantity,
        l.UnitPrice.Amount, l.UnitPrice.Currency, l.VatRate, l.DiscountPercent,
        l.LineNet.Amount, l.LineVat.Amount, l.LineGross.Amount, l.SortOrder);
}
