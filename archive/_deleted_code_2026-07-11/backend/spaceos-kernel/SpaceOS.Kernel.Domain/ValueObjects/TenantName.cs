using SpaceOS.Kernel.Domain.Exceptions;

namespace SpaceOS.Kernel.Domain.ValueObjects;

/// <summary>
/// Strongly-typed value object for a tenant's display name.
/// Enforces non-empty and maximum-length invariants at construction time.
/// </summary>
public readonly record struct TenantName
{
    /// <summary>Gets the trimmed string value of this name.</summary>
    public string Value { get; }

    /// <summary>
    /// Initialises a new <see cref="TenantName"/> with the given value.
    /// </summary>
    /// <param name="value">A non-empty, non-whitespace string of at most 100 characters.</param>
    /// <exception cref="DomainException">Thrown when <paramref name="value"/> is empty, whitespace, or exceeds 100 characters.</exception>
    public TenantName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("TenantName cannot be empty.");

        var trimmed = value.Trim();
        if (trimmed.Length > 100)
            throw new DomainException("TenantName cannot exceed 100 characters.");

        Value = trimmed;
    }

    /// <summary>Wraps an existing string as a <see cref="TenantName"/>.</summary>
    public static TenantName From(string value) => new(value);

    /// <summary>Implicitly converts a <see cref="TenantName"/> to its underlying string value.</summary>
    public static implicit operator string(TenantName name) => name.Value;

    /// <inheritdoc/>
    public override string ToString() => Value;
}
