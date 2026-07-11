using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.Domain.Spatial;

/// <summary>
/// Unit tests for <see cref="SpatialGrid.GetIntersectingCells"/> — 4 cases:
/// fully inside, boundary clamped, fully outside, overlapping multiple cells
/// (DoD: SpatialGrid.GetIntersectingCells unit tests).
/// </summary>
public sealed class SpatialGridTests
{
    /// <summary>
    /// A 10x10 grid with 500mm cells (5000mm width, 5000mm depth).
    /// </summary>
    private static SpatialGrid CreateGrid() =>
        new(PhysicalSpaceId: Guid.NewGuid(), CellSizeMm: 500, WidthCells: 10, DepthCells: 10);

    [Fact]
    public void GetIntersectingCells_QueryFullyInsideGrid_ReturnsCells()
    {
        // Arrange — query spans cells (1,1) to (2,2) on the grid
        var grid = CreateGrid();
        var query = new BoundingBox(MinX: 500, MinY: 0, MinZ: 500, MaxX: 1499, MaxY: 100, MaxZ: 1499);

        // Act
        var cells = grid.GetIntersectingCells(query).ToList();

        // Assert — should hit cells col=[1,2] x row=[1,2] = 4 cells
        Assert.Equal(4, cells.Count);
        Assert.Contains(new GridCell(1, 1), cells);
        Assert.Contains(new GridCell(2, 1), cells);
        Assert.Contains(new GridCell(1, 2), cells);
        Assert.Contains(new GridCell(2, 2), cells);
    }

    [Fact]
    public void GetIntersectingCells_QueryOnBoundary_ClampedToGrid()
    {
        // Arrange — query extends beyond grid bounds (negative and past max)
        var grid = CreateGrid();
        var query = new BoundingBox(MinX: -100, MinY: 0, MinZ: -100, MaxX: 200, MaxY: 100, MaxZ: 200);

        // Act — should clamp to col=0, row=0
        var cells = grid.GetIntersectingCells(query).ToList();

        // Assert — clamped range is col=[0,0] x row=[0,0] = 1 cell
        Assert.Single(cells);
        Assert.Contains(new GridCell(0, 0), cells);
    }

    [Fact]
    public void GetIntersectingCells_QueryFullyOutsideGrid_ReturnsZeroCells()
    {
        // Arrange — query is entirely past the grid on both X and Z
        var grid = CreateGrid();
        var query = new BoundingBox(MinX: 6000, MinY: 0, MinZ: 6000, MaxX: 7000, MaxY: 100, MaxZ: 7000);

        // Act
        var cells = grid.GetIntersectingCells(query).ToList();

        // Assert
        Assert.Empty(cells);
    }

    [Fact]
    public void GetIntersectingCells_QueryOverlappingMultipleCells_ReturnsAllOverlapped()
    {
        // Arrange — query spans from cell (0,0) through (3,3)
        var grid = CreateGrid();
        var query = new BoundingBox(MinX: 0, MinY: 0, MinZ: 0, MaxX: 1999, MaxY: 100, MaxZ: 1999);

        // Act
        var cells = grid.GetIntersectingCells(query).ToList();

        // Assert — col=[0,3] x row=[0,3] = 16 cells
        Assert.Equal(16, cells.Count);
    }
}
