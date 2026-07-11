namespace SpaceOS.Modules.Contracts.Inventory.DTOs;

/// <summary>Represents a named storage location within the warehouse.</summary>
public sealed record StockLocationDto(
    Guid Id,
    string Code,
    string? Description);
