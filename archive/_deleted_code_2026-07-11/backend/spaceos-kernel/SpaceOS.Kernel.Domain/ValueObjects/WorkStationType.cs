using SpaceOS.Kernel.Domain.Exceptions;

namespace SpaceOS.Kernel.Domain.ValueObjects;

/// <summary>
/// Strongly-typed value object representing the type classification of a
/// <see cref="SpaceOS.Kernel.Domain.Entities.WorkStation"/>.
/// Enforces non-empty and maximum-length invariants at construction time.
/// </summary>
public readonly record struct WorkStationType
{
    /// <summary>Gets the trimmed string value of this type.</summary>
    public string Value { get; }

    private WorkStationType(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("WorkStation type cannot be empty.");

        if (value.Length > 50)
            throw new DomainException("WorkStation type cannot exceed 50 characters.");

        Value = value.Trim();
    }

    /// <summary>
    /// Wraps an existing string as a <see cref="WorkStationType"/>.
    /// </summary>
    /// <param name="value">A non-empty, non-whitespace string of at most 50 characters.</param>
    /// <exception cref="DomainException">Thrown when <paramref name="value"/> is invalid.</exception>
    public static WorkStationType From(string value) => new(value);

    /// <inheritdoc/>
    public override string ToString() => Value;

    /// <summary>Implicitly converts a <see cref="WorkStationType"/> to its underlying string value.</summary>
    public static implicit operator string(WorkStationType type) => type.Value;
}
