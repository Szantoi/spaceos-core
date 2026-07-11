using SpaceOS.Kernel.Domain.Exceptions;

namespace SpaceOS.Kernel.Domain.ValueObjects;

/// <summary>
/// Strongly-typed identifier for a <see cref="SpaceOS.Kernel.Domain.Entities.Tenant"/> aggregate.
/// Wraps a non-empty <see cref="Guid"/> and enforces the invariant at construction time.
/// </summary>
public readonly record struct TenantId
{
    /// <summary>Gets the underlying <see cref="Guid"/> value.</summary>
    public Guid Value { get; }

    /// <summary>
    /// Initialises a new <see cref="TenantId"/> with the given value.
    /// </summary>
    /// <param name="value">A non-empty <see cref="Guid"/>.</param>
    /// <exception cref="DomainException">Thrown when <paramref name="value"/> is <see cref="Guid.Empty"/>.</exception>
    private TenantId(Guid value)
    {
        if (value == Guid.Empty)
            throw new DomainException("TenantId cannot be empty.");

        Value = value;
    }

    /// <summary>Wraps an existing <see cref="Guid"/> as a <see cref="TenantId"/>.</summary>
    /// <param name="value">A non-empty <see cref="Guid"/>.</param>
    /// <exception cref="DomainException">Thrown when <paramref name="value"/> is <see cref="Guid.Empty"/>.</exception>
    public static TenantId From(Guid value) => new(value);

    /// <summary>Creates a new <see cref="TenantId"/> with a freshly generated <see cref="Guid"/>.</summary>
    public static TenantId New() => new(Guid.NewGuid());

    /// <summary>Implicitly converts a <see cref="TenantId"/> to its underlying <see cref="Guid"/>.</summary>
    public static implicit operator Guid(TenantId id) => id.Value;

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
