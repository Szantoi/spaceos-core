using SpaceOS.Modules.Inventory.Domain.Common;

namespace SpaceOS.Modules.Inventory.Domain.Events;

/// <summary>Raised when a reservation is manually released before expiry.</summary>
public sealed record ReservationReleasedDomainEvent(
    Guid ReservationId,
    Guid TenantId,
    string? Reason) : IDomainEvent;
