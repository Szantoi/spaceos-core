using SpaceOS.Kernel.Domain.Exceptions;

namespace SpaceOS.Kernel.Domain.ValueObjects;

/// <summary>
/// Strongly-typed value object representing the display name of a
/// <see cref="SpaceOS.Kernel.Domain.Entities.WorkStation"/>.
/// Enforces non-empty and maximum-length invariants at construction time.
/// </summary>
public readonly record struct WorkStationName
{
    /// <summary>Gets the trimmed string value of this name.</summary>
    public string Value { get; }

    /// <summary>
    /// Initialises a new <see cref="WorkStationName"/> with validation.
    /// </summary>
    /// <param name="value">A non-empty, non-whitespace string of at most 100 characters.</param>
    /// <exception cref="DomainException">Thrown when <paramref name="value"/> is invalid.</exception>
    public WorkStationName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("WorkStation name cannot be empty.");

        var trimmed = value.Trim();
        if (trimmed.Length > 100)
            throw new DomainException("WorkStation name cannot exceed 100 characters.");

        Value = trimmed;
    }

    /// <summary>Wraps an existing string as a <see cref="WorkStationName"/>.</summary>
    public static WorkStationName From(string value) => new(value);

    /// <inheritdoc/>
    public override string ToString() => Value;

    /// <summary>Implicitly converts a <see cref="WorkStationName"/> to its underlying string value.</summary>
    public static implicit operator string(WorkStationName name) => name.Value;
}
