namespace SpaceOS.Modules.Contracts.Inventory.DTOs;

/// <summary>
/// A single reserved line item within a <see cref="ReservationDto"/>.
/// Id: unique item identifier.
/// StockItemId: identifier of the reserved stock item.
/// MaterialCode: material code of the reserved stock.
/// QuantityReserved: quantity held; always greater than zero (I-06).
/// QuantityConsumed: quantity consumed so far; never exceeds QuantityReserved (I-07).
/// </summary>
public sealed record ReservationItemDto(
    Guid Id,
    Guid StockItemId,
    string MaterialCode,
    decimal QuantityReserved,
    decimal QuantityConsumed
);
