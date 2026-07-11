namespace SpaceOS.Modules.Contracts.Cutting.DTOs;

/// <summary>
/// Represents the physical placement of a cutting piece on a panel after nesting.
/// CuttingLineIndex links back to the source CuttingLineDto (CD-09).
/// </summary>
public sealed record PlacedPieceDto(
    int CuttingLineIndex,
    string ComponentName,
    decimal X,
    decimal Y,
    decimal Width,
    decimal Height,
    bool IsRotated);
