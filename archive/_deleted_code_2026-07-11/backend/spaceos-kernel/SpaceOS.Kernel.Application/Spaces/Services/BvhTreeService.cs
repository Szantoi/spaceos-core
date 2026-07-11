// SpaceOS.Kernel.Application/Spaces/Services/BvhTreeService.cs

using Ardalis.Result;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Auth;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.Events;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.Services;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.Spaces.Services;

/// <summary>
/// Application-level BVH tree service that manages element insertion and intersection queries.
/// Creates root BvhNode if none exists; otherwise creates leaf nodes parented to root (Phase 3A simplification).
/// Raises <see cref="SpatialCollisionDetectedEvent"/> when intersecting elements are found during insertion.
/// </summary>
internal sealed class BvhTreeService : IBvhTreeService
{
    private readonly IPhysicalSpaceRepository _spaceRepo;
    private readonly IBvhRepository           _bvhRepo;
    private readonly ISpatialElementRepository _elementRepo;
    private readonly IUnitOfWork              _unitOfWork;
    private readonly IDomainEventDispatcher   _dispatcher;
    private readonly BvhQueryService          _queryService;
    private readonly ITenantResolver          _tenantResolver;

    /// <summary>Initialises a new <see cref="BvhTreeService"/>.</summary>
    public BvhTreeService(
        IPhysicalSpaceRepository spaceRepo,
        IBvhRepository bvhRepo,
        ISpatialElementRepository elementRepo,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher dispatcher,
        BvhQueryService queryService,
        ITenantResolver tenantResolver)
    {
        ArgumentNullException.ThrowIfNull(spaceRepo);
        ArgumentNullException.ThrowIfNull(bvhRepo);
        ArgumentNullException.ThrowIfNull(elementRepo);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(queryService);
        ArgumentNullException.ThrowIfNull(tenantResolver);
        _spaceRepo      = spaceRepo;
        _bvhRepo        = bvhRepo;
        _elementRepo    = elementRepo;
        _unitOfWork     = unitOfWork;
        _dispatcher     = dispatcher;
        _queryService   = queryService;
        _tenantResolver = tenantResolver;
    }

    /// <inheritdoc/>
    public async Task<Result<Guid>> InsertElementAsync(
        Guid physicalSpaceId, BoundingBox elementBox,
        Guid flowEpicId, TradeType tradeType, string elementType,
        CancellationToken ct)
    {
        var tenantId = _tenantResolver.TryResolve();
        if (tenantId is null)
            return Result.Unauthorized();

        var space = await _spaceRepo.GetByIdAsync(physicalSpaceId, ct).ConfigureAwait(false);
        if (space is null)
            return Result.NotFound($"PhysicalSpace not found: {physicalSpaceId}");

        // Ensure root node exists; create one if the tree is empty.
        var root = await _bvhRepo.GetRootAsync(physicalSpaceId, ct).ConfigureAwait(false);
        if (root is null)
        {
            root = BvhNode.CreateRoot(
                tenantId.Value.Value, physicalSpaceId,
                new BoundingBox(0, 0, 0, space.Dimensions.WidthMm, space.Dimensions.HeightMm, space.Dimensions.DepthMm));
            await _bvhRepo.AddAsync(root, ct).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        // Create spatial element first to get the element ID for the leaf node.
        var element = SpatialElement.Create(
            tenantId.Value.Value, Guid.Empty, flowEpicId, tradeType, elementType);

        // Create leaf node parented to root.
        var leafNode = BvhNode.CreateLeaf(
            tenantId.Value.Value, physicalSpaceId, root.Id, elementBox, element.Id);

        // Wire element to its leaf node via reflection-free approach:
        // We created element with Guid.Empty for BvhLeafId, so recreate with correct ID.
        element = SpatialElement.Create(
            tenantId.Value.Value, leafNode.Id, flowEpicId, tradeType, elementType);

        // Re-assign the leaf's ElementId to match the element's actual ID.
        leafNode = BvhNode.CreateLeaf(
            tenantId.Value.Value, physicalSpaceId, root.Id, elementBox, element.Id);

        await _bvhRepo.AddAsync(leafNode, ct).ConfigureAwait(false);
        await _elementRepo.AddAsync(element, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        // Check for spatial collisions and raise events.
        var intersecting = await _queryService.QueryIntersectingAsync(physicalSpaceId, elementBox, ct)
            .ConfigureAwait(false);

        // Raise element registered event.
        var registeredEvent = new SpatialElementRegisteredEvent(
            element.Id, physicalSpaceId, flowEpicId, tradeType.ToString(), DateTimeOffset.UtcNow);

        var allEvents = new List<Domain.Primitives.IDomainEvent> { registeredEvent };

        foreach (var otherId in intersecting)
        {
            if (otherId != element.Id)
            {
                allEvents.Add(new SpatialCollisionDetectedEvent(
                    element.Id, otherId, elementBox, DateTimeOffset.UtcNow));
            }
        }

        await _dispatcher.DispatchAsync(allEvents, ct).ConfigureAwait(false);

        return Result.Success(element.Id);
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<Guid>>> QueryIntersectingAsync(
        Guid physicalSpaceId, BoundingBox query, CancellationToken ct)
    {
        var spaceExists = await _spaceRepo.ExistsAsync(physicalSpaceId, ct).ConfigureAwait(false);
        if (!spaceExists)
            return Result.NotFound($"PhysicalSpace not found: {physicalSpaceId}");

        var results = await _queryService.QueryIntersectingAsync(physicalSpaceId, query, ct)
            .ConfigureAwait(false);

        return Result.Success(results);
    }
}
