using Ardalis.Result;

namespace SpaceOS.Modules.Sales.Domain.ValueObjects;

/// <summary>Validated email address value object (max 320 chars, basic format check).</summary>
public sealed record Email
{
    /// <summary>The normalized email address string.</summary>
    public string Value { get; }

    private Email(string value) => Value = value;

    /// <summary>Creates a validated <see cref="Email"/> from a raw string.</summary>
    public static Result<Email> From(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Result.Invalid(new ValidationError("Email cannot be empty."));
        if (raw.Length > 320)
            return Result.Invalid(new ValidationError("Email must be at most 320 characters."));
        if (!raw.Contains('@') || raw.IndexOf('@') == 0 || raw.IndexOf('@') == raw.Length - 1)
            return Result.Invalid(new ValidationError("Email must contain a valid '@' separator."));
        return Result.Success(new Email(raw.Trim().ToLowerInvariant()));
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
