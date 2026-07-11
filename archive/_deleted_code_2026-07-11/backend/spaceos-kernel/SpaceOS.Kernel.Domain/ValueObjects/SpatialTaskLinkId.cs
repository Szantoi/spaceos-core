using System;
using SpaceOS.Kernel.Domain.Exceptions;

namespace SpaceOS.Kernel.Domain.ValueObjects;

/// <summary>
/// Strongly-typed identifier for a <see cref="SpaceOS.Kernel.Domain.Entities.SpatialTaskLink"/> entity.
/// Wraps a non-empty <see cref="Guid"/> and enforces the invariant at construction time.
/// </summary>
public readonly record struct SpatialTaskLinkId
{
    /// <summary>Gets the underlying <see cref="Guid"/> value.</summary>
    public Guid Value { get; }

    /// <summary>
    /// Initialises a new <see cref="SpatialTaskLinkId"/> with the given value.
    /// </summary>
    /// <param name="value">A non-empty <see cref="Guid"/>.</param>
    /// <exception cref="DomainException">Thrown when <paramref name="value"/> is <see cref="Guid.Empty"/>.</exception>
    private SpatialTaskLinkId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("SpatialTaskLinkId cannot be empty.");
        }

        Value = value;
    }

    /// <summary>Wraps an existing <see cref="Guid"/> as a <see cref="SpatialTaskLinkId"/>.</summary>
    /// <param name="value">A non-empty <see cref="Guid"/>.</param>
    /// <exception cref="DomainException">Thrown when <paramref name="value"/> is <see cref="Guid.Empty"/>.</exception>
    public static SpatialTaskLinkId From(Guid value) => new(value);

    /// <summary>Creates a new <see cref="SpatialTaskLinkId"/> with a freshly generated <see cref="Guid"/>.</summary>
    public static SpatialTaskLinkId New() => new(Guid.NewGuid());
}
