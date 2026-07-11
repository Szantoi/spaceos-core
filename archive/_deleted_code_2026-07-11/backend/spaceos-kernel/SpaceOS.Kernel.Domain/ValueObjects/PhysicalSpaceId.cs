using System;
using SpaceOS.Kernel.Domain.Exceptions;

namespace SpaceOS.Kernel.Domain.ValueObjects;

/// <summary>
/// Strongly-typed identifier for a <see cref="SpaceOS.Kernel.Domain.Aggregates.PhysicalSpace"/> aggregate.
/// Wraps a non-empty <see cref="Guid"/> and enforces the invariant at construction time.
/// </summary>
public readonly record struct PhysicalSpaceId
{
    /// <summary>Gets the underlying <see cref="Guid"/> value.</summary>
    public Guid Value { get; }

    /// <summary>
    /// Initialises a new <see cref="PhysicalSpaceId"/> with the given value.
    /// </summary>
    /// <param name="value">A non-empty <see cref="Guid"/>.</param>
    /// <exception cref="DomainException">Thrown when <paramref name="value"/> is <see cref="Guid.Empty"/>.</exception>
    private PhysicalSpaceId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("PhysicalSpaceId cannot be empty.");
        }

        Value = value;
    }

    /// <summary>Wraps an existing <see cref="Guid"/> as a <see cref="PhysicalSpaceId"/>.</summary>
    /// <param name="value">A non-empty <see cref="Guid"/>.</param>
    /// <exception cref="DomainException">Thrown when <paramref name="value"/> is <see cref="Guid.Empty"/>.</exception>
    public static PhysicalSpaceId From(Guid value) => new(value);

    /// <summary>Creates a new <see cref="PhysicalSpaceId"/> with a freshly generated <see cref="Guid"/>.</summary>
    public static PhysicalSpaceId New() => new(Guid.NewGuid());
}
