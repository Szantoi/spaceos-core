using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Exceptions;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Services;

/// <summary>
/// Domain service that performs async recursive BVH tree traversal for spatial intersection queries.
/// Includes depth guard (SEC-P3A-07) and cycle detection (SEC-P3A-03).
/// </summary>
public sealed class BvhQueryService
{
    private readonly IBvhRepository _bvhRepo;
    private const int MaxDepth = 32;

    /// <summary>
    /// Initialises a new <see cref="BvhQueryService"/> with the given BVH repository.
    /// </summary>
    /// <param name="bvhRepo">The repository for loading BVH nodes.</param>
    public BvhQueryService(IBvhRepository bvhRepo)
    {
        _bvhRepo = bvhRepo;
    }

    /// <summary>
    /// Returns the element identifiers of all BVH leaf nodes whose bounding boxes intersect the given query box.
    /// </summary>
    /// <param name="physicalSpaceId">The physical space to query within.</param>
    /// <param name="query">The bounding box to test for intersection.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A read-only list of element identifiers from intersecting leaf nodes.</returns>
    /// <exception cref="DomainException">Thrown when the tree exceeds maximum depth or a cycle is detected.</exception>
    public async Task<IReadOnlyList<Guid>> QueryIntersectingAsync(
        Guid physicalSpaceId, BoundingBox query, CancellationToken ct)
    {
        var results = new List<Guid>();
        var root = await _bvhRepo.GetRootAsync(physicalSpaceId, ct).ConfigureAwait(false);
        if (root is null) return results;
        await TraverseBvhAsync(root, query, results, new HashSet<Guid>(), 0, ct).ConfigureAwait(false);
        return results;
    }

    /// <summary>
    /// Recursively traverses the BVH tree, collecting element identifiers from intersecting leaf nodes.
    /// </summary>
    private async Task TraverseBvhAsync(
        BvhNode node,
        BoundingBox query,
        List<Guid> results,
        HashSet<Guid> visited,
        int depth,
        CancellationToken ct)
    {
        if (depth > MaxDepth)
            throw new DomainException($"BVH max depth ({MaxDepth}) exceeded.");

        if (!visited.Add(node.Id))
            throw new DomainException($"BVH cycle detected at node {node.Id}.");

        if (!node.BoundingBox.Intersects(query)) return;

        if (node.IsLeaf)
        {
            if (node.ElementId.HasValue) results.Add(node.ElementId.Value);
            return;
        }

        var children = await _bvhRepo.GetChildrenAsync(node.Id, ct).ConfigureAwait(false);
        foreach (var child in children)
        {
            await TraverseBvhAsync(child, query, results, visited, depth + 1, ct).ConfigureAwait(false);
        }
    }
}
