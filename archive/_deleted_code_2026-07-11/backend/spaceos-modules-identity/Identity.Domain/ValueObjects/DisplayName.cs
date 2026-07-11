// Identity.Domain/ValueObjects/DisplayName.cs

namespace Identity.Domain.ValueObjects;

public sealed class DisplayName : IEquatable<DisplayName>
{
    public const int MaxLength = 100;

    public string FirstName { get; }
    public string LastName { get; }
    public string FullName => $"{FirstName} {LastName}";

    private DisplayName(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    public static DisplayName From(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("FirstName cannot be empty.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("LastName cannot be empty.", nameof(lastName));

        var first = firstName.Trim();
        var last = lastName.Trim();

        if (first.Length > MaxLength)
            throw new ArgumentException($"FirstName cannot exceed {MaxLength} characters.", nameof(firstName));
        if (last.Length > MaxLength)
            throw new ArgumentException($"LastName cannot exceed {MaxLength} characters.", nameof(lastName));

        return new(first, last);
    }

    public bool Equals(DisplayName? other) =>
        other is not null && FirstName == other.FirstName && LastName == other.LastName;
    public override bool Equals(object? obj) => obj is DisplayName other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(FirstName, LastName);
    public override string ToString() => FullName;

    public static bool operator ==(DisplayName? left, DisplayName? right) =>
        left is null ? right is null : left.Equals(right);
    public static bool operator !=(DisplayName? left, DisplayName? right) => !(left == right);
}
