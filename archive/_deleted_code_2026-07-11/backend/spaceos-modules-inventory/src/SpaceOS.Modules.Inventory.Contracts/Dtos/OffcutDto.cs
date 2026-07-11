namespace SpaceOS.Modules.Inventory.Contracts.Dtos;

/// <summary>Represents a leftover panel offcut available for reuse in cutting optimisation.</summary>
public sealed record OffcutDto(
    Guid Id,
    string MaterialType,
    int WidthMm,
    int HeightMm,
    Guid OriginCuttingSheetId);
