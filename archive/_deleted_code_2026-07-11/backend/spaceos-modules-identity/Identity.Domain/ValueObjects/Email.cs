// Identity.Domain/ValueObjects/Email.cs

using System.Text.RegularExpressions;

namespace Identity.Domain.ValueObjects;

public sealed class Email : IEquatable<Email>
{
    private static readonly Regex Rfc5322Pattern = new(
        @"^[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(250));

    public string Value { get; }

    private Email(string value) => Value = value;

    public static Email From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty.", nameof(value));

        var normalized = value.Trim().ToLowerInvariant();

        if (!Rfc5322Pattern.IsMatch(normalized))
            throw new ArgumentException($"'{value}' is not a valid email address.", nameof(value));

        return new(normalized);
    }

    public bool Equals(Email? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is Email other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);
    public override string ToString() => Value;

    public static bool operator ==(Email? left, Email? right) =>
        left is null ? right is null : left.Equals(right);
    public static bool operator !=(Email? left, Email? right) => !(left == right);
}
