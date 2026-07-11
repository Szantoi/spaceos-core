using System;

namespace SpaceOS.Kernel.Domain.Common;

/// <summary>
/// Abstract base class for entities that are scoped to a specific tenant.
/// Provides a <see cref="TenantId"/> and <see cref="Id"/> for all derived entities (BE-P3A-07).
/// </summary>
public abstract class TenantScopedEntity
{
    /// <summary>Gets the unique identifier of this entity.</summary>
    public Guid Id { get; protected set; }

    /// <summary>Gets the identifier of the tenant that owns this entity.</summary>
    public Guid TenantId { get; protected set; }
}
