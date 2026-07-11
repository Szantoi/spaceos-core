namespace SpaceOS.Nesting.Algorithms.Models;

/// <summary>A part to be cut from a panel.</summary>
public sealed record NestingPart(
    string PartId,
    string Name,
    decimal WidthMm,
    decimal HeightMm,
    bool CanRotate = true,
    int Quantity = 1);
