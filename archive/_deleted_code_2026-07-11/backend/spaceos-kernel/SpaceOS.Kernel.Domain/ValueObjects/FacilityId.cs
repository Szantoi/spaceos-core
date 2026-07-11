using System;
using SpaceOS.Kernel.Domain.Exceptions;

namespace SpaceOS.Kernel.Domain.ValueObjects;

/// <summary>
/// Strongly-typed identifier for a <see cref="SpaceOS.Kernel.Domain.Entities.Facility"/> aggregate.
/// Wraps a non-empty <see cref="Guid"/> and enforces the invariant at construction time.
/// </summary>
public readonly record struct FacilityId
{
    /// <summary>Gets the underlying <see cref="Guid"/> value.</summary>
    public Guid Value { get; }

    /// <summary>
    /// Initialises a new <see cref="FacilityId"/> with the given value.
    /// </summary>
    /// <param name="value">A non-empty <see cref="Guid"/>.</param>
    /// <exception cref="DomainException">Thrown when <paramref name="value"/> is <see cref="Guid.Empty"/>.</exception>
    private FacilityId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("FacilityId cannot be empty.");
        }

        Value = value;
    }

    /// <summary>Wraps an existing <see cref="Guid"/> as a <see cref="FacilityId"/>.</summary>
    /// <param name="value">A non-empty <see cref="Guid"/>.</param>
    /// <exception cref="DomainException">Thrown when <paramref name="value"/> is <see cref="Guid.Empty"/>.</exception>
    public static FacilityId From(Guid value) => new(value);

    /// <summary>Creates a new <see cref="FacilityId"/> with a freshly generated <see cref="Guid"/>.</summary>
    public static FacilityId New() => new(Guid.NewGuid());
}
