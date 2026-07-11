using SpaceOS.Modules.Sales.Domain.ValueObjects;

namespace SpaceOS.Modules.Sales.Domain.Interfaces;

/// <summary>
/// Domain-level port for per-tenant monotonic quote number generation (D-09).
/// Implementation uses PostgreSQL advisory lock to be race-condition-free (DB-S-03).
/// </summary>
public interface IQuoteNumberGenerator
{
    /// <summary>Generates the next sequential QuoteNumber for the given tenant and year.</summary>
    Task<QuoteNumber> NextAsync(Guid tenantId, int year, CancellationToken ct);
}
