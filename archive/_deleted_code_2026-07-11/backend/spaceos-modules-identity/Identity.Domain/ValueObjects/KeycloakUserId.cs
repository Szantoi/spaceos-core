// Identity.Domain/ValueObjects/KeycloakUserId.cs

namespace Identity.Domain.ValueObjects;

public sealed class KeycloakUserId : IEquatable<KeycloakUserId>
{
    public string Value { get; }

    private KeycloakUserId(string value) => Value = value;

    public static KeycloakUserId From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("KeycloakUserId cannot be empty.", nameof(value));
        return new(value.Trim());
    }

    public bool Equals(KeycloakUserId? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is KeycloakUserId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);
    public override string ToString() => Value;

    public static bool operator ==(KeycloakUserId? left, KeycloakUserId? right) =>
        left is null ? right is null : left.Equals(right);
    public static bool operator !=(KeycloakUserId? left, KeycloakUserId? right) => !(left == right);
}
