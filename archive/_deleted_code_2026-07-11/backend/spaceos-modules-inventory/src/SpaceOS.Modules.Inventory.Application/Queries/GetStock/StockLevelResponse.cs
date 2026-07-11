namespace SpaceOS.Modules.Inventory.Application.Queries.GetStock;

public sealed record StockLevelResponse(
    string MaterialType,
    int FullPanelCount,
    int WidthMm,
    int HeightMm,
    int OffcutCount);
