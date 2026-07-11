namespace SpaceOS.Kernel.Domain.ValueObjects;

/// <summary>
/// Axis-aligned bounding box defined by minimum and maximum coordinates in millimetres.
/// Used for spatial collision detection in the BVH tree.
/// </summary>
public sealed record BoundingBox(
    int MinX, int MinY, int MinZ,
    int MaxX, int MaxY, int MaxZ)
{
    /// <summary>
    /// Determines whether this bounding box intersects with another bounding box
    /// using the separating axis theorem across all six axes.
    /// </summary>
    /// <param name="other">The other bounding box to test against.</param>
    /// <returns><see langword="true"/> if the boxes overlap on all three axes; otherwise <see langword="false"/>.</returns>
    public bool Intersects(BoundingBox other) =>
        MinX <= other.MaxX && MaxX >= other.MinX &&
        MinY <= other.MaxY && MaxY >= other.MinY &&
        MinZ <= other.MaxZ && MaxZ >= other.MinZ;
}
