using SpaceOS.Kernel.Domain.Exceptions;

namespace SpaceOS.Kernel.Domain.ValueObjects;

/// <summary>
/// Strongly-typed identifier for a <see cref="SpaceOS.Kernel.Domain.Entities.WorkStation"/> aggregate.
/// Wraps a non-empty <see cref="Guid"/> and enforces the invariant at construction time.
/// </summary>
public readonly record struct WorkStationId
{
    /// <summary>Gets the underlying <see cref="Guid"/> value.</summary>
    public Guid Value { get; }

    private WorkStationId(Guid value)
    {
        if (value == Guid.Empty)
            throw new DomainException("WorkStation ID cannot be empty.");

        Value = value;
    }

    /// <summary>Creates a new <see cref="WorkStationId"/> with a freshly generated <see cref="Guid"/>.</summary>
    public static WorkStationId New() => new(Guid.NewGuid());

    /// <summary>
    /// Wraps an existing <see cref="Guid"/> as a <see cref="WorkStationId"/>.
    /// </summary>
    /// <param name="value">A non-empty <see cref="Guid"/>.</param>
    /// <exception cref="DomainException">Thrown when <paramref name="value"/> is <see cref="Guid.Empty"/>.</exception>
    public static WorkStationId From(Guid value) => new(value);

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();

    /// <summary>Implicitly converts a <see cref="WorkStationId"/> to its underlying <see cref="Guid"/>.</summary>
    public static implicit operator Guid(WorkStationId id) => id.Value;
}
