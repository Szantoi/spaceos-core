using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Events;

/// <summary>
/// Raised when a new <see cref="SpaceOS.Kernel.Domain.Entities.Tenant"/> is created.
/// </summary>
public readonly record struct TenantCreatedEvent(
    TenantId       TenantId,
    DateTimeOffset OccurredOn) : IDomainEvent;
