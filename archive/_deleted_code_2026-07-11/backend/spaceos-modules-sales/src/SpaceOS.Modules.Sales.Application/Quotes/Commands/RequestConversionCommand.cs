using System.Text.Json;
using Ardalis.Result;
using MediatR;
using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Sales.Abstractions.Contracts;
using SpaceOS.Modules.Sales.Application.Common;
using SpaceOS.Modules.Sales.Application.Outbox;
using SpaceOS.Modules.Sales.Domain.Common;
using SpaceOS.Modules.Sales.Domain.Events;
using SpaceOS.Modules.Sales.Domain.Interfaces;

namespace SpaceOS.Modules.Sales.Application.Quotes.Commands;

public sealed record RequestConversionCommand(Guid QuoteId) : IRequest<Result>;

public sealed class RequestConversionCommandHandler(
    IQuoteRepository quotes,
    IOutboxRepository outbox,
    IClock clock,
    ILogger<RequestConversionCommandHandler> log) : IRequestHandler<RequestConversionCommand, Result>
{
    public async Task<Result> Handle(RequestConversionCommand cmd, CancellationToken ct)
    {
        var quote = await quotes.GetByIdWithLinesAsync(cmd.QuoteId, ct).ConfigureAwait(false);
        if (quote is null) return Result.NotFound();

        // FSM transition (idempotent)
        var transition = quote.RequestConversion(clock);
        if (!transition.IsSuccess) return transition;

        // Outbox write — same DB transaction as the Quote state change.
        // BE-S-02: the QuoteConversionRequested domain event is for audit/notification ONLY.
        // The outbox INSERT is the handler's explicit responsibility.
        var payload = new OrderConversionRequest(
            QuoteId: quote.Id,
            TenantId: quote.TenantId,
            CustomerId: quote.CustomerId,
            LinkedTenantId: null,
            Currency: quote.Currency,
            TotalNet: quote.TotalNet.Amount,
            TotalVat: quote.TotalVat.Amount,
            TotalGross: quote.TotalGross.Amount,
            Lines: quote.Lines.Select(l => new OrderConversionLine(
                l.SourceTemplateId, l.Description, l.Quantity,
                l.UnitPrice.Amount, l.VatRate, l.DiscountPercent, l.SortOrder)).ToList(),
            ContentHash: quote.ContentHash ?? string.Empty);

        await outbox.AddMessageAsync(
            tenantId: quote.TenantId,
            aggregateId: quote.Id,
            operation: nameof(QuoteConversionRequested),
            payloadJson: JsonSerializer.Serialize(payload),
            idempotencyKey: quote.Id.ToString("N"),
            clock: clock,
            ct: ct).ConfigureAwait(false);

        await quotes.SaveChangesAsync(ct).ConfigureAwait(false);

        log.LogInformation("Conversion requested for Quote {QuoteId} (tenant {TenantId})",
            quote.Id, quote.TenantId);
        return Result.Success();
    }
}
