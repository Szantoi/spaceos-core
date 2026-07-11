using SpaceOS.Modules.Sales.Domain.Interfaces;
using SpaceOS.Modules.Sales.Domain.ValueObjects;

namespace SpaceOS.Modules.Sales.Tests.Helpers;

/// <summary>Deterministic IQuoteNumberGenerator for unit tests.</summary>
public sealed class FakeQuoteNumberGenerator : IQuoteNumberGenerator
{
    private int _counter;

    public Task<QuoteNumber> NextAsync(Guid tenantId, int year, CancellationToken ct)
    {
        _counter++;
        var number = $"Q-{year}-{_counter:D5}";
        return Task.FromResult(new QuoteNumber(number));
    }
}
