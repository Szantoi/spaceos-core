using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Repositories;

/// <summary>
/// Persistence contract for <see cref="BvhNode"/> entities.
/// Provides tree navigation without eager-loading child collections (BE-P3A-03).
/// </summary>
public interface IBvhRepository
{
    /// <summary>Returns the root <see cref="BvhNode"/> for the given physical space, or <see langword="null"/> if the tree is empty.</summary>
    /// <param name="physicalSpaceId">The physical space whose root node to retrieve.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<BvhNode?> GetRootAsync(Guid physicalSpaceId, CancellationToken ct = default);

    /// <summary>Returns the direct children of the given parent node.</summary>
    /// <param name="parentId">The parent node identifier.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<IReadOnlyList<BvhNode>> GetChildrenAsync(Guid parentId, CancellationToken ct = default);

    /// <summary>Stages a new <see cref="BvhNode"/> for insertion. Persist via <c>IUnitOfWork.SaveChangesAsync</c>.</summary>
    /// <param name="node">The BVH node to add.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task AddAsync(BvhNode node, CancellationToken ct = default);

    /// <summary>
    /// Returns the element identifiers of all leaf nodes whose bounding boxes intersect the given query box
    /// within the specified physical space.
    /// </summary>
    /// <param name="physicalSpaceId">The physical space to query within.</param>
    /// <param name="query">The bounding box to test for intersection.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<IReadOnlyList<Guid>> GetIntersectingLeafElementIdsAsync(Guid physicalSpaceId, BoundingBox query, CancellationToken ct = default);
}
