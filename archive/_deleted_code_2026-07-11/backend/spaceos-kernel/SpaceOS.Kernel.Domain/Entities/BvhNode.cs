using System;
using SpaceOS.Kernel.Domain.Common;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Entities;

/// <summary>
/// Entity representing a node in a bounding volume hierarchy (BVH) tree.
/// No navigation to child nodes — children are loaded via <c>IBvhRepository.GetChildrenAsync()</c> (BE-P3A-03).
/// </summary>
public sealed class BvhNode : TenantScopedEntity
{
    /// <summary>Gets the identifier of the <see cref="SpaceOS.Kernel.Domain.Aggregates.PhysicalSpace"/> this node belongs to.</summary>
    public Guid PhysicalSpaceId { get; private set; }

    /// <summary>Gets the identifier of the parent node, or <see langword="null"/> if this is the root.</summary>
    public Guid? ParentId { get; private set; }

    /// <summary>Gets the axis-aligned bounding box of this node.</summary>
    public BoundingBox BoundingBox { get; private set; } = null!;

    /// <summary>Gets a value indicating whether this node is a leaf containing an element reference.</summary>
    public bool IsLeaf { get; private set; }

    /// <summary>Gets the identifier of the spatial element, or <see langword="null"/> if this is not a leaf.</summary>
    public Guid? ElementId { get; private set; }

    /// <summary>
    /// Required by EF Core for materialisation. Not for application use.
    /// </summary>
    private BvhNode() { }

    /// <summary>
    /// Creates a new root <see cref="BvhNode"/> for the given physical space.
    /// </summary>
    /// <param name="tenantId">The owning tenant identifier.</param>
    /// <param name="physicalSpaceId">The physical space this node belongs to.</param>
    /// <param name="boundingBox">The axis-aligned bounding box of this node.</param>
    /// <returns>A new root <see cref="BvhNode"/> instance.</returns>
    public static BvhNode CreateRoot(Guid tenantId, Guid physicalSpaceId, BoundingBox boundingBox)
    {
        return new BvhNode
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PhysicalSpaceId = physicalSpaceId,
            ParentId = null,
            BoundingBox = boundingBox,
            IsLeaf = false,
            ElementId = null
        };
    }

    /// <summary>
    /// Creates a new leaf <see cref="BvhNode"/> containing a spatial element reference.
    /// </summary>
    /// <param name="tenantId">The owning tenant identifier.</param>
    /// <param name="physicalSpaceId">The physical space this node belongs to.</param>
    /// <param name="parentId">The parent node identifier.</param>
    /// <param name="boundingBox">The axis-aligned bounding box of this leaf.</param>
    /// <param name="elementId">The spatial element referenced by this leaf.</param>
    /// <returns>A new leaf <see cref="BvhNode"/> instance.</returns>
    public static BvhNode CreateLeaf(Guid tenantId, Guid physicalSpaceId, Guid parentId, BoundingBox boundingBox, Guid elementId)
    {
        return new BvhNode
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PhysicalSpaceId = physicalSpaceId,
            ParentId = parentId,
            BoundingBox = boundingBox,
            IsLeaf = true,
            ElementId = elementId
        };
    }
}
