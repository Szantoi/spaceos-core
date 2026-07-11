using SpaceOS.Modules.Inventory.Domain.Common;

namespace SpaceOS.Modules.Inventory.Domain.Events;

/// <summary>Raised when all reserved items have been consumed and the reservation is closed.</summary>
public sealed record ReservationConsumedDomainEvent(
    Guid ReservationId,
    Guid TenantId) : IDomainEvent;
