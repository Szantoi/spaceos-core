using Moq;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Exceptions;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.Services;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.Domain.Spatial;

/// <summary>
/// Unit tests for <see cref="BvhQueryService.QueryIntersectingAsync"/> — cycle detection,
/// depth guard, hit, and miss scenarios
/// (DoD: BvhQueryService cycle guard + depth guard + hit/miss tests).
/// </summary>
public sealed class BvhQueryServiceTests
{
    private readonly Mock<IBvhRepository> _bvhRepoMock = new();
    private readonly BvhQueryService _service;

    /// <summary>
    /// Bounding box that covers the entire test space for intersection hits.
    /// </summary>
    private static readonly BoundingBox UniversalBox = new(0, 0, 0, 10000, 10000, 10000);

    /// <summary>
    /// A bounding box far outside any test node — guarantees intersection miss.
    /// </summary>
    private static readonly BoundingBox FarAwayBox = new(90000, 90000, 90000, 99000, 99000, 99000);

    public BvhQueryServiceTests()
    {
        _service = new BvhQueryService(_bvhRepoMock.Object);
    }

    [Fact]
    public async Task QueryIntersectingAsync_CycleDetection_ThrowsDomainException()
    {
        // Arrange — A→B→A cycle
        var spaceId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var nodeAId = Guid.NewGuid();
        var nodeBId = Guid.NewGuid();

        var nodeA = BvhNode.CreateRoot(tenantId, spaceId, UniversalBox);
        // We need to set the Id to a known value — use reflection since Id has private setter
        SetId(nodeA, nodeAId);

        var nodeB = BvhNode.CreateLeaf(tenantId, spaceId, nodeAId, UniversalBox, Guid.NewGuid());
        SetId(nodeB, nodeBId);

        // Make nodeA appear as non-leaf by returning nodeB as its child
        // Then nodeB's children return nodeA → cycle
        _bvhRepoMock
            .Setup(r => r.GetRootAsync(spaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(nodeA);

        // nodeA is a root (not a leaf), so GetChildrenAsync is called
        _bvhRepoMock
            .Setup(r => r.GetChildrenAsync(nodeAId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { nodeB });

        // nodeB needs to be non-leaf to trigger further traversal; create a non-leaf node B
        var nodeBNonLeaf = BvhNode.CreateRoot(tenantId, spaceId, UniversalBox);
        SetId(nodeBNonLeaf, nodeBId);

        // Re-setup root to return nodeA, children of A returns nodeBNonLeaf, children of B returns nodeA
        _bvhRepoMock
            .Setup(r => r.GetChildrenAsync(nodeAId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { nodeBNonLeaf });

        _bvhRepoMock
            .Setup(r => r.GetChildrenAsync(nodeBId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { nodeA });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _service.QueryIntersectingAsync(spaceId, UniversalBox, CancellationToken.None));

        Assert.Contains("BVH cycle detected", ex.Message);
    }

    [Fact]
    public async Task QueryIntersectingAsync_DepthGuard_ThrowsDomainExceptionAtDepth33()
    {
        // Arrange — mock GetChildrenAsync to always return a single child, creating infinite depth
        var spaceId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var root = BvhNode.CreateRoot(tenantId, spaceId, UniversalBox);
        _bvhRepoMock
            .Setup(r => r.GetRootAsync(spaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(root);

        // Every call to GetChildrenAsync returns a new non-leaf node with a unique ID
        _bvhRepoMock
            .Setup(r => r.GetChildrenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, CancellationToken _) =>
            {
                var child = BvhNode.CreateRoot(tenantId, spaceId, UniversalBox);
                return new[] { child };
            });

        // Act & Assert — depth 33 exceeds MaxDepth=32
        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _service.QueryIntersectingAsync(spaceId, UniversalBox, CancellationToken.None));

        Assert.Contains("BVH max depth", ex.Message);
    }

    [Fact]
    public async Task QueryIntersectingAsync_IntersectingLeafNode_ReturnsElementId()
    {
        // Arrange — root with one leaf child whose AABB intersects the query
        var spaceId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var elementId = Guid.NewGuid();

        var root = BvhNode.CreateRoot(tenantId, spaceId, new BoundingBox(0, 0, 0, 1000, 1000, 1000));
        var rootId = root.Id;

        var leaf = BvhNode.CreateLeaf(tenantId, spaceId, rootId, new BoundingBox(0, 0, 0, 500, 500, 500), elementId);

        _bvhRepoMock
            .Setup(r => r.GetRootAsync(spaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(root);
        _bvhRepoMock
            .Setup(r => r.GetChildrenAsync(rootId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { leaf });

        var query = new BoundingBox(0, 0, 0, 100, 100, 100);

        // Act
        var results = await _service.QueryIntersectingAsync(spaceId, query, CancellationToken.None);

        // Assert
        Assert.Single(results);
        Assert.Equal(elementId, results[0]);
    }

    [Fact]
    public async Task QueryIntersectingAsync_NonIntersectingBoundingBox_ReturnsEmpty()
    {
        // Arrange — root and leaf are at origin, query is far away
        var spaceId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var root = BvhNode.CreateRoot(tenantId, spaceId, new BoundingBox(0, 0, 0, 100, 100, 100));

        _bvhRepoMock
            .Setup(r => r.GetRootAsync(spaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(root);

        // Act — query far away from root AABB
        var results = await _service.QueryIntersectingAsync(spaceId, FarAwayBox, CancellationToken.None);

        // Assert — root doesn't intersect, so no traversal, empty result
        Assert.Empty(results);
        _bvhRepoMock.Verify(r => r.GetChildrenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Helper to set the Id property of a <see cref="TenantScopedEntity"/> via reflection,
    /// since Id has a protected setter.
    /// </summary>
    private static void SetId(BvhNode node, Guid id)
    {
        var prop = typeof(SpaceOS.Kernel.Domain.Common.TenantScopedEntity)
            .GetProperty(nameof(BvhNode.Id))!;
        prop.SetValue(node, id);
    }
}
