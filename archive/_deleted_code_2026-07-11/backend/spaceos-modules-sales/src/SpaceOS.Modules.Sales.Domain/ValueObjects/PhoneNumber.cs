using Ardalis.Result;

namespace SpaceOS.Modules.Sales.Domain.ValueObjects;

/// <summary>Validated phone number value object (max 50 chars).</summary>
public sealed record PhoneNumber
{
    /// <summary>The normalized phone number string.</summary>
    public string Value { get; }

    private PhoneNumber(string value) => Value = value;

    /// <summary>Creates a validated <see cref="PhoneNumber"/> from a raw string.</summary>
    public static Result<PhoneNumber> From(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Result.Invalid(new ValidationError("PhoneNumber cannot be empty."));
        if (raw.Length > 50)
            return Result.Invalid(new ValidationError("PhoneNumber must be at most 50 characters."));
        return Result.Success(new PhoneNumber(raw.Trim()));
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
