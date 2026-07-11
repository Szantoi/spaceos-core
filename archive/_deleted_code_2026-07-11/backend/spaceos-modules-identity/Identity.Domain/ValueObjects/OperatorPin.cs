// Identity.Domain/ValueObjects/OperatorPin.cs

namespace Identity.Domain.ValueObjects;

/// <summary>
/// 4-digit PIN for factory operators (shopfloor quick authentication)
/// </summary>
public sealed record OperatorPin
{
    public string Value { get; }

    private OperatorPin(string value)
    {
        Value = value;
    }

    public static OperatorPin? FromString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();

        if (trimmed.Length != 4)
            throw new ArgumentException("OperatorPin must be exactly 4 digits.", nameof(value));

        if (!trimmed.All(char.IsDigit))
            throw new ArgumentException("OperatorPin must contain only digits.", nameof(value));

        return new OperatorPin(trimmed);
    }

    public static implicit operator string?(OperatorPin? pin) => pin?.Value;

    public override string ToString() => Value;
}
