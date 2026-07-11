// SpaceOS.Kernel.Domain/Common/TenantScopedAggregateRoot.cs
using System;
using SpaceOS.Kernel.Domain.Primitives;

namespace SpaceOS.Kernel.Domain.Common;

/// <summary>
/// Base class for tenant-scoped aggregate roots that both carry a <see cref="TenantId"/> + <see cref="Id"/>
/// and raise domain events. Combines <see cref="AggregateRoot"/> event machinery with the
/// <see cref="TenantScopedEntity"/> identity contract.
/// </summary>
public abstract class TenantScopedAggregateRoot : AggregateRoot
{
    /// <summary>Gets the unique identifier of this aggregate.</summary>
    public Guid Id { get; protected set; }

    /// <summary>Gets the identifier of the tenant that owns this aggregate.</summary>
    public Guid TenantId { get; protected set; }
}
