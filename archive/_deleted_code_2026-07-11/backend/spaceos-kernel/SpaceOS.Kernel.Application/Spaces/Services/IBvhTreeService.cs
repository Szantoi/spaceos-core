// SpaceOS.Kernel.Application/Spaces/Services/IBvhTreeService.cs

using Ardalis.Result;
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.Spaces.Services;

/// <summary>
/// Internal service for managing the BVH tree within a physical space.
/// Not exposed as a public API endpoint (BE-P3A-02).
/// </summary>
public interface IBvhTreeService
{
    /// <summary>
    /// Inserts a <see cref="SpaceOS.Kernel.Domain.Entities.SpatialElement"/> into the BVH tree.
    /// Creates the root node if none exists, otherwise creates a leaf parented to root.
    /// Tree restructuring is managed internally.
    /// </summary>
    /// <param name="physicalSpaceId">The physical space to insert into.</param>
    /// <param name="elementBox">The bounding box of the element.</param>
    /// <param name="flowEpicId">The FlowEpic this element belongs to.</param>
    /// <param name="tradeType">The construction trade type.</param>
    /// <param name="elementType">The driver-specific element classification.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The new element identifier on success.</returns>
    Task<Result<Guid>> InsertElementAsync(
        Guid physicalSpaceId, BoundingBox elementBox,
        Guid flowEpicId, TradeType tradeType, string elementType,
        CancellationToken ct);

    /// <summary>
    /// Queries the BVH tree for elements whose bounding boxes intersect the given query box.
    /// Delegates to the domain <see cref="SpaceOS.Kernel.Domain.Services.BvhQueryService"/>.
    /// </summary>
    /// <param name="physicalSpaceId">The physical space to query within.</param>
    /// <param name="query">The bounding box to test for intersection.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A read-only list of intersecting element identifiers.</returns>
    Task<Result<IReadOnlyList<Guid>>> QueryIntersectingAsync(
        Guid physicalSpaceId, BoundingBox query, CancellationToken ct);
}
