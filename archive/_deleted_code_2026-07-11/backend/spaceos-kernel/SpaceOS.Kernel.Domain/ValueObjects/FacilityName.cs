using System;
using SpaceOS.Kernel.Domain.Exceptions;

namespace SpaceOS.Kernel.Domain.ValueObjects;

/// <summary>
/// Strongly-typed value object for a facility's display name.
/// Enforces non-empty and maximum-length invariants at construction time.
/// </summary>
public readonly record struct FacilityName
{
    /// <summary>Gets the trimmed string value of this name.</summary>
    public string Value { get; }

    /// <summary>
    /// Initialises a new <see cref="FacilityName"/> with the given value.
    /// </summary>
    /// <param name="value">A non-empty, non-whitespace string of at most 100 characters.</param>
    /// <exception cref="DomainException">Thrown when <paramref name="value"/> is empty, whitespace, or exceeds 100 characters.</exception>
    public FacilityName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("FacilityName cannot be empty or whitespace.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > 100)
        {
            throw new DomainException("FacilityName is too long. Maximum 100 characters allowed.");
        }

        Value = trimmed;
    }

    /// <summary>Wraps an existing string as a <see cref="FacilityName"/>.</summary>
    /// <param name="value">The raw name string.</param>
    public static FacilityName From(string value) => new(value);

    /// <inheritdoc/>
    public override string ToString() => Value;

    /// <summary>Implicitly converts a <see cref="FacilityName"/> to its underlying string value.</summary>
    public static implicit operator string(FacilityName name) => name.Value;
}
