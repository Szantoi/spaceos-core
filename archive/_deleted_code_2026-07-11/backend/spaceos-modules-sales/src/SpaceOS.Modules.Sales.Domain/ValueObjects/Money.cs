namespace SpaceOS.Modules.Sales.Domain.ValueObjects;

/// <summary>
/// Immutable monetary value. Currency may be empty when loaded from DB
/// (the Quote.Currency single-currency design is authoritative — BE-S-04).
/// </summary>
public readonly record struct Money(decimal Amount, string Currency = "")
{
    /// <summary>Returns a zero-value Money in the given currency.</summary>
    public static Money Zero(string ccy) => new(0m, ccy);

    /// <summary>Sums a sequence of Money values, overriding the currency with <paramref name="ccy"/>.</summary>
    public static Money Sum(IEnumerable<Money> items, string ccy)
        => new(items.Sum(m => m.Amount), ccy);
}
