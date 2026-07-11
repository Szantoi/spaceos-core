using System.Text.RegularExpressions;
using Ardalis.Result;

namespace SpaceOS.Modules.Sales.Domain.ValueObjects;

/// <summary>
/// Per-tenant monotonic quote number in the format Q-{YYYY}-{NNNNN} (D-09).
/// </summary>
public readonly record struct QuoteNumber(string Value)
{
    private static readonly Regex Pattern = new(@"^Q-\d{4}-\d{5}$", RegexOptions.Compiled);

    /// <summary>Parses and validates a raw quote number string.</summary>
    public static Result<QuoteNumber> From(string raw)
        => Pattern.IsMatch(raw)
            ? Result.Success(new QuoteNumber(raw))
            : Result.Invalid(new ValidationError("QuoteNumber must match Q-YYYY-NNNNN."));
}
