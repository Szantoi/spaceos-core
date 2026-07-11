using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Sales.Application.Common;
using SpaceOS.Modules.Sales.Domain.Common;
using SpaceOS.Modules.Sales.Domain.Interfaces;

namespace SpaceOS.Modules.Sales.Application.Quotes.Commands;

public sealed record ArchiveQuoteCommand(Guid QuoteId) : IRequest<Result>;

public sealed class ArchiveQuoteCommandHandler(
    IQuoteRepository quotes,
    ITenantContext tenantContext,
    IClock clock) : IRequestHandler<ArchiveQuoteCommand, Result>
{
    public async Task<Result> Handle(ArchiveQuoteCommand cmd, CancellationToken ct)
    {
        var quote = await quotes.GetByIdAsync(cmd.QuoteId, ct).ConfigureAwait(false);
        if (quote is null) return Result.NotFound();
        var guard = tenantContext.EnsureSameTenant(quote);
        if (!guard.IsSuccess) return guard;

        var result = quote.Archive(clock);
        if (!result.IsSuccess) return result;

        quotes.Update(quote);
        await quotes.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Success();
    }
}
