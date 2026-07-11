// Identity.Domain/ValueObjects/SpaceOSUserId.cs

namespace Identity.Domain.ValueObjects;

public sealed class SpaceOSUserId : IEquatable<SpaceOSUserId>
{
    public Guid Value { get; }

    private SpaceOSUserId(Guid value) => Value = value;

    public static SpaceOSUserId New() => new(Guid.NewGuid());

    public static SpaceOSUserId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("SpaceOSUserId cannot be empty.", nameof(value));
        return new(value);
    }

    public bool Equals(SpaceOSUserId? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is SpaceOSUserId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString();

    public static bool operator ==(SpaceOSUserId? left, SpaceOSUserId? right) =>
        left is null ? right is null : left.Equals(right);
    public static bool operator !=(SpaceOSUserId? left, SpaceOSUserId? right) => !(left == right);
}
