using System.Text.Json;
using SpaceOS.Modules.Contracts.Inventory.Enums;

namespace SpaceOS.Modules.Contracts.Inventory.DTOs;

/// <summary>
/// Read model representing a soft stock reservation.
/// Id: unique identifier of the reservation.
/// TenantId: tenant that owns this reservation.
/// CorrelationId: consumer-provided idempotency key.
/// ConsumerModule: registered consumer module that created the reservation.
/// ConsumerContextJson: optional structured context provided by the consumer at creation time.
/// CreatedByUserId: user identity that created the reservation, if available.
/// CreatedAt: UTC timestamp when the reservation was created.
/// ExpiresAt: UTC timestamp after which the reservation expires if not consumed.
/// Status: current lifecycle status of the reservation.
/// Items: reserved line items; contains at least one item.
/// </summary>
public sealed record ReservationDto(
    Guid Id,
    Guid TenantId,
    Guid CorrelationId,
    string ConsumerModule,
    JsonDocument? ConsumerContextJson,
    Guid? CreatedByUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    ReservationStatus Status,
    IReadOnlyList<ReservationItemDto> Items
);
