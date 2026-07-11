using SpaceOS.Kernel.Domain.Exceptions;

namespace SpaceOS.Kernel.Domain.ValueObjects;

/// <summary>
/// Strongly-typed value object for a <see cref="SpaceOS.Kernel.Domain.Entities.FlowEpic"/> title.
/// Enforces non-empty and maximum-length invariants at construction time.
/// </summary>
public readonly record struct FlowEpicTitle
{
    /// <summary>Gets the trimmed string value of this title.</summary>
    public string Value { get; }

    /// <summary>
    /// Initialises a new <see cref="FlowEpicTitle"/> with the given value.
    /// </summary>
    /// <param name="value">A non-empty, non-whitespace string of at most 200 characters.</param>
    /// <exception cref="DomainException">Thrown when <paramref name="value"/> is empty, whitespace, or exceeds 200 characters.</exception>
    public FlowEpicTitle(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("FlowEpic title cannot be empty.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > 200)
        {
            throw new DomainException("FlowEpic title cannot exceed 200 characters.");
        }

        Value = trimmed;
    }

    /// <summary>Wraps an existing string as a <see cref="FlowEpicTitle"/>.</summary>
    /// <param name="value">The raw title string.</param>
    public static FlowEpicTitle From(string value) => new(value);

    /// <inheritdoc/>
    public override string ToString() => Value;

    /// <summary>Implicitly converts a <see cref="FlowEpicTitle"/> to its underlying string value.</summary>
    public static implicit operator string(FlowEpicTitle title) => title.Value;
}
