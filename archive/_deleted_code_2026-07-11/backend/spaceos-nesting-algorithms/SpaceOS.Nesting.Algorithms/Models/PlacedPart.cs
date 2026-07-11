namespace SpaceOS.Nesting.Algorithms.Models;

/// <summary>A part that has been placed on a panel at a specific position.</summary>
public sealed record PlacedPart(
    string PartId,
    string Name,
    decimal X,
    decimal Y,
    decimal WidthMm,
    decimal HeightMm,
    bool IsRotated);
