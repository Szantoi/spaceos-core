namespace SpaceOS.Modules.Contracts.Cutting.DTOs;

/// <summary>Describes which pieces are placed on a single stock panel after nesting.</summary>
public sealed record PanelAssignmentDto(
    Guid PanelStockId,
    string MaterialCode,
    decimal PanelWidth,
    decimal PanelHeight,
    IReadOnlyList<PlacedPieceDto> Pieces,
    IReadOnlyList<CuttingOffcutResultDto> ResultingOffcuts,
    decimal WasteArea);
