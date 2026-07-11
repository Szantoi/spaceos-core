using SpaceOS.Modules.Contracts.Inventory.Enums;

namespace SpaceOS.Modules.Contracts.Inventory.DTOs;

/// <summary>Represents a single stock item (full panel or offcut) in the inventory system.</summary>
public sealed record StockItemDto(
    Guid Id,
    string MaterialCode,
    decimal Width,
    decimal Height,
    decimal Thickness,
    StockItemType ItemType,
    int Quantity,
    string? LocationCode);
