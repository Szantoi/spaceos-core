using SpaceOS.Kernel.Domain.Exceptions;

namespace SpaceOS.Kernel.Domain.ValueObjects;

/// <summary>
/// Strongly-typed identifier for a <see cref="SpaceOS.Kernel.Domain.Entities.SpaceLayer"/> aggregate.
/// Wraps a non-empty <see cref="Guid"/> and enforces the invariant at construction time.
/// </summary>
public readonly record struct SpaceLayerId
{
    public Guid Value { get; }

    private SpaceLayerId(Guid value)
    {
        if (value == Guid.Empty)
            throw new DomainException("SpaceLayerId cannot be empty.");
        Value = value;
    }

    /// <summary>Creates a new <see cref="SpaceLayerId"/> with a freshly generated <see cref="Guid"/>.</summary>
    public static SpaceLayerId New()           => new(Guid.NewGuid());

    /// <summary>Wraps an existing <see cref="Guid"/> as a <see cref="SpaceLayerId"/>.</summary>
    /// <param name="value">A non-empty <see cref="Guid"/>.</param>
    /// <exception cref="SpaceOS.Kernel.Domain.Exceptions.DomainException">Thrown when <paramref name="value"/> is <see cref="Guid.Empty"/>.</exception>
    public static SpaceLayerId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();

    /// <summary>Implicitly converts a <see cref="SpaceLayerId"/> to its underlying <see cref="Guid"/> value.</summary>
    public static implicit operator Guid(SpaceLayerId id) => id.Value;
}
