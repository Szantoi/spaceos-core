using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.Domain.Spatial;

/// <summary>
/// Unit tests for <see cref="BoundingBox.Intersects"/> — 8 cases covering hit/miss on each axis,
/// full containment, and adjacent-touching edge case (DoD: BoundingBox.Intersects 8 cases).
/// </summary>
public sealed class BoundingBoxTests
{
    // -------------------------------------------------------------------------
    // X-axis tests
    // -------------------------------------------------------------------------

    [Fact]
    public void Intersects_PartialOverlapOnXAxis_ReturnsTrue()
    {
        // Arrange — boxes overlap on X (5..15 ∩ 10..20), fully overlap on Y and Z
        var a = new BoundingBox(MinX: 5, MinY: 0, MinZ: 0, MaxX: 15, MaxY: 10, MaxZ: 10);
        var b = new BoundingBox(MinX: 10, MinY: 0, MinZ: 0, MaxX: 20, MaxY: 10, MaxZ: 10);

        // Act & Assert
        Assert.True(a.Intersects(b));
        Assert.True(b.Intersects(a));
    }

    [Fact]
    public void Intersects_GapOnXAxis_ReturnsFalse()
    {
        // Arrange — A ends at X=10, B starts at X=15 → gap on X
        var a = new BoundingBox(MinX: 0, MinY: 0, MinZ: 0, MaxX: 10, MaxY: 10, MaxZ: 10);
        var b = new BoundingBox(MinX: 15, MinY: 0, MinZ: 0, MaxX: 25, MaxY: 10, MaxZ: 10);

        // Act & Assert
        Assert.False(a.Intersects(b));
        Assert.False(b.Intersects(a));
    }

    // -------------------------------------------------------------------------
    // Y-axis tests
    // -------------------------------------------------------------------------

    [Fact]
    public void Intersects_PartialOverlapOnYAxis_ReturnsTrue()
    {
        // Arrange — boxes overlap on Y (5..15 ∩ 10..20), fully overlap on X and Z
        var a = new BoundingBox(MinX: 0, MinY: 5, MinZ: 0, MaxX: 10, MaxY: 15, MaxZ: 10);
        var b = new BoundingBox(MinX: 0, MinY: 10, MinZ: 0, MaxX: 10, MaxY: 20, MaxZ: 10);

        // Act & Assert
        Assert.True(a.Intersects(b));
        Assert.True(b.Intersects(a));
    }

    [Fact]
    public void Intersects_GapOnYAxis_ReturnsFalse()
    {
        // Arrange — A ends at Y=10, B starts at Y=15 → gap on Y
        var a = new BoundingBox(MinX: 0, MinY: 0, MinZ: 0, MaxX: 10, MaxY: 10, MaxZ: 10);
        var b = new BoundingBox(MinX: 0, MinY: 15, MinZ: 0, MaxX: 10, MaxY: 25, MaxZ: 10);

        // Act & Assert
        Assert.False(a.Intersects(b));
        Assert.False(b.Intersects(a));
    }

    // -------------------------------------------------------------------------
    // Z-axis tests
    // -------------------------------------------------------------------------

    [Fact]
    public void Intersects_PartialOverlapOnZAxis_ReturnsTrue()
    {
        // Arrange — boxes overlap on Z (5..15 ∩ 10..20), fully overlap on X and Y
        var a = new BoundingBox(MinX: 0, MinY: 0, MinZ: 5, MaxX: 10, MaxY: 10, MaxZ: 15);
        var b = new BoundingBox(MinX: 0, MinY: 0, MinZ: 10, MaxX: 10, MaxY: 10, MaxZ: 20);

        // Act & Assert
        Assert.True(a.Intersects(b));
        Assert.True(b.Intersects(a));
    }

    [Fact]
    public void Intersects_GapOnZAxis_ReturnsFalse()
    {
        // Arrange — A ends at Z=10, B starts at Z=15 → gap on Z
        var a = new BoundingBox(MinX: 0, MinY: 0, MinZ: 0, MaxX: 10, MaxY: 10, MaxZ: 10);
        var b = new BoundingBox(MinX: 0, MinY: 0, MinZ: 15, MaxX: 10, MaxY: 10, MaxZ: 25);

        // Act & Assert
        Assert.False(a.Intersects(b));
        Assert.False(b.Intersects(a));
    }

    // -------------------------------------------------------------------------
    // Full containment and edge cases
    // -------------------------------------------------------------------------

    [Fact]
    public void Intersects_FullContainment_ReturnsTrue()
    {
        // Arrange — inner box is fully contained within outer box
        var outer = new BoundingBox(MinX: 0, MinY: 0, MinZ: 0, MaxX: 100, MaxY: 100, MaxZ: 100);
        var inner = new BoundingBox(MinX: 10, MinY: 10, MinZ: 10, MaxX: 50, MaxY: 50, MaxZ: 50);

        // Act & Assert
        Assert.True(outer.Intersects(inner));
        Assert.True(inner.Intersects(outer));
    }

    [Fact]
    public void Intersects_AdjacentTouchingNotOverlapping_ReturnsTrue()
    {
        // Arrange — A.MaxX == B.MinX (touching face) — the Intersects impl uses <=/>= so touching counts
        var a = new BoundingBox(MinX: 0, MinY: 0, MinZ: 0, MaxX: 10, MaxY: 10, MaxZ: 10);
        var b = new BoundingBox(MinX: 10, MinY: 0, MinZ: 0, MaxX: 20, MaxY: 10, MaxZ: 10);

        // Act & Assert — touching faces satisfy MinX <= other.MaxX && MaxX >= other.MinX
        Assert.True(a.Intersects(b));
        Assert.True(b.Intersects(a));
    }

    [Fact]
    public void Intersects_IdenticalBoxes_ReturnsTrue()
    {
        // Arrange — two identical bounding boxes
        var a = new BoundingBox(MinX: 5, MinY: 5, MinZ: 5, MaxX: 15, MaxY: 15, MaxZ: 15);
        var b = new BoundingBox(MinX: 5, MinY: 5, MinZ: 5, MaxX: 15, MaxY: 15, MaxZ: 15);

        // Act & Assert
        Assert.True(a.Intersects(b));
        Assert.True(b.Intersects(a));
    }

    [Fact]
    public void Intersects_SinglePointOverlap_ReturnsTrue()
    {
        // Arrange — boxes share exactly one corner point (10,10,10)
        var a = new BoundingBox(MinX: 0, MinY: 0, MinZ: 0, MaxX: 10, MaxY: 10, MaxZ: 10);
        var b = new BoundingBox(MinX: 10, MinY: 10, MinZ: 10, MaxX: 20, MaxY: 20, MaxZ: 20);

        // Act & Assert — single point overlap satisfies <= on all axes
        Assert.True(a.Intersects(b));
        Assert.True(b.Intersects(a));
    }
}
