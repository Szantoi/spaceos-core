using SpaceOS.Modules.Inventory.Domain.Common;

namespace SpaceOS.Modules.Inventory.Domain.Events;

/// <summary>Raised when a new <see cref="Aggregates.Reservation"/> is created in Active state.</summary>
public sealed record StockReservedDomainEvent(
    Guid ReservationId,
    Guid TenantId,
    Guid CorrelationId,
    string ConsumerModule,
    DateTimeOffset ExpiresAt) : IDomainEvent;
