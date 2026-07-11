// SpaceOS.Kernel.Domain/Events/TenantArchivedEvent.cs
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Events;

/// <summary>Raised when a <see cref="Entities.Tenant"/> is archived (soft-deleted).</summary>
/// <param name="TenantId">The identifier of the archived tenant.</param>
/// <param name="OccurredOn">The UTC timestamp when the archive occurred.</param>
public readonly record struct TenantArchivedEvent(TenantId TenantId, DateTimeOffset OccurredOn) : IDomainEvent;
