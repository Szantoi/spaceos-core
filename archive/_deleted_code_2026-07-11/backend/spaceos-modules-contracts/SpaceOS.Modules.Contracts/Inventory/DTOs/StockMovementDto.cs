using SpaceOS.Modules.Contracts.Inventory.Enums;

namespace SpaceOS.Modules.Contracts.Inventory.DTOs;

/// <summary>
/// Records a single stock quantity change for audit and ledger purposes.
/// Quantity is positive for inbound movements; negative for consumption/scrap.
/// </summary>
public sealed record StockMovementDto(
    Guid StockItemId,
    StockMovementType MovementType,
    int Quantity,
    StockReferenceType ReferenceType,
    Guid? ReferenceId);
