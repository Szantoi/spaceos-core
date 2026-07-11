namespace SpaceOS.Modules.Inventory.Contracts.Dtos;

/// <summary>Current stock level for a given material type, including panel dimensions and available offcuts.</summary>
public sealed record PanelStockDto(
    string MaterialType,
    int FullPanelCount,
    int WidthMm,
    int HeightMm,
    IReadOnlyList<OffcutDto> Offcuts);
