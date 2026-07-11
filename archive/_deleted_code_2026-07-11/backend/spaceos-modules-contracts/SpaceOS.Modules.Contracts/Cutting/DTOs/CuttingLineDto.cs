namespace SpaceOS.Modules.Contracts.Cutting.DTOs;

/// <summary>
/// Represents a single line item in a cutting sheet — one component to be cut.
/// ComponentName: max 100 chars. CanRotate: allow 90 degree rotation in nesting.
/// </summary>
public sealed record CuttingLineDto(
    string ComponentName,
    string ComponentType,
    decimal Width,
    decimal Height,
    decimal CuttingWidth,
    decimal CuttingHeight,
    string Material,
    decimal Thickness,
    int Quantity,
    int SortOrder,
    bool CanRotate);
