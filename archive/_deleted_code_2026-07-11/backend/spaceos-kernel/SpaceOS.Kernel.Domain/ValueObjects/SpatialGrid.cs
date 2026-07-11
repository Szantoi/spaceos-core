using System;
using System.Collections.Generic;
using SpaceOS.Kernel.Domain.Aggregates;

namespace SpaceOS.Kernel.Domain.ValueObjects;

/// <summary>
/// Value object representing a 2D spatial grid overlay for a <see cref="PhysicalSpace"/> (BE-P3A-06).
/// Used for coarse-grained spatial queries before BVH traversal.
/// </summary>
public sealed record SpatialGrid(
    Guid PhysicalSpaceId,
    int CellSizeMm,
    int WidthCells,
    int DepthCells)
{
    /// <summary>
    /// Creates a <see cref="SpatialGrid"/> from a <see cref="PhysicalSpace"/> aggregate,
    /// computing cell counts by ceiling-dividing dimensions by cell size.
    /// </summary>
    /// <param name="space">The physical space to derive the grid from.</param>
    /// <returns>A new <see cref="SpatialGrid"/> instance.</returns>
    public static SpatialGrid From(PhysicalSpace space) => new(
        space.Id,
        space.CellSizeMm,
        (int)Math.Ceiling((double)space.Dimensions.WidthMm / space.CellSizeMm),
        (int)Math.Ceiling((double)space.Dimensions.DepthMm / space.CellSizeMm));

    /// <summary>
    /// Returns all grid cells that intersect with the given bounding box query,
    /// clamped to the grid bounds.
    /// </summary>
    /// <param name="query">The bounding box to test against the grid.</param>
    /// <returns>An enumerable of intersecting <see cref="GridCell"/> instances.</returns>
    public IEnumerable<GridCell> GetIntersectingCells(BoundingBox query)
    {
        int colMin = Math.Max(0, query.MinX / CellSizeMm);
        int colMax = Math.Min(WidthCells - 1, query.MaxX / CellSizeMm);
        int rowMin = Math.Max(0, query.MinZ / CellSizeMm);
        int rowMax = Math.Min(DepthCells - 1, query.MaxZ / CellSizeMm);
        for (int col = colMin; col <= colMax; col++)
            for (int row = rowMin; row <= rowMax; row++)
                yield return new GridCell(col, row);
    }
}
