namespace SpaceOS.Modules.Inventory.Application.Queries.GetOffcuts;

public sealed record OffcutResponse(
    Guid Id,
    decimal WidthMm,
    decimal HeightMm,
    Guid MaterialCatalogId,
    Guid? OriginCuttingSheetId);
