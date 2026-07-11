using SpaceOS.Modules.Contracts.Inventory.Enums;

namespace SpaceOS.Modules.Contracts.Inventory.DTOs;

/// <summary>
/// Filter criteria for querying reservations via <c>IInventoryProvider.GetReservationsAsync</c>.
/// At least one filter property must be non-null/non-default — empty filter is rejected as a DoS guard.
/// Maximum Take is 500.
/// ConsumerModule: optional filter by consumer module name.
/// Status: optional filter by reservation lifecycle status.
/// CorrelationId: optional filter by exact consumer-provided correlation ID.
/// CreatedAfter: optional — returns only reservations created on or after this UTC timestamp.
/// CreatedBefore: optional — returns only reservations created on or before this UTC timestamp.
/// Skip: pagination offset (default 0).
/// Take: page size (default 100, max 500).
/// </summary>
public sealed record ReservationFilter(
    string? ConsumerModule,
    ReservationStatus? Status,
    Guid? CorrelationId,
    DateTimeOffset? CreatedAfter,
    DateTimeOffset? CreatedBefore,
    int Skip = 0,
    int Take = 100
);
