namespace SpaceOS.Kernel.Domain.ValueObjects;

/// <summary>
/// Represents a single cell in a <see cref="SpatialGrid"/> identified by column and row indices.
/// </summary>
public readonly record struct GridCell(int Col, int Row);
