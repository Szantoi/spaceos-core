using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Sales.Domain.Interfaces;
using SpaceOS.Modules.Sales.Domain.ValueObjects;
using SpaceOS.Modules.Sales.Infrastructure.Persistence;

namespace SpaceOS.Modules.Sales.Infrastructure.Generators;

/// <summary>
/// Calls the SECURITY DEFINER PostgreSQL function that uses an advisory lock to guarantee
/// race-free, monotonic quote number generation per tenant/year (DB-S-03).
/// </summary>
internal sealed class QuoteNumberGenerator(SalesDbContext db) : IQuoteNumberGenerator
{
    /// <inheritdoc/>
    public async Task<QuoteNumber> NextAsync(Guid tenantId, int year, CancellationToken ct)
    {
        var raw = await db.Database
            .SqlQueryRaw<string>(
                "SELECT spaceos_sales.fn_next_quote_number({0}::uuid, {1}::int)",
                tenantId.ToString(),
                year)
            .FirstAsync(ct)
            .ConfigureAwait(false);

        return new QuoteNumber(raw);
    }
}
