namespace SpaceOS.Modules.Joinery.Domain.ValueObjects;

/// <summary>
/// Immutable value object representing a single cut-part line within a cutting list snapshot.
/// </summary>
public sealed record CuttingListLine(
    string ComponentName,
    string ComponentType,
    decimal Width,
    decimal Height,
    decimal CuttingWidth,
    decimal CuttingHeight,
    string Material,
    decimal Thickness,
    int Quantity,
    int SortOrder);
