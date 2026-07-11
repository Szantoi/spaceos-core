using SpaceOS.Modules.Inventory.Domain.Common;

namespace SpaceOS.Modules.Inventory.Domain.Events;

/// <summary>Raised by the expiry worker when TTL elapses on an Active reservation.</summary>
public sealed record ReservationExpiredDomainEvent(
    Guid ReservationId,
    Guid TenantId) : IDomainEvent;
