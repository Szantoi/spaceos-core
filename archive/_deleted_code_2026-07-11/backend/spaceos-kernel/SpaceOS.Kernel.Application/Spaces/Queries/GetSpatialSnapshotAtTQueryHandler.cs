// SpaceOS.Kernel.Application/Spaces/Queries/GetSpatialSnapshotAtTQueryHandler.cs

using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Auth;
using SpaceOS.Kernel.Domain.Repositories;

namespace SpaceOS.Kernel.Application.Spaces.Queries;

/// <summary>
/// Handles <see cref="GetSpatialSnapshotAtTQuery"/>: returns a paginated snapshot of spatial elements
/// with their FSM state at a given point in time.
/// Uses raw SQL via <see cref="ISpatialQueryRepository"/> (spec §5.4).
/// </summary>
internal sealed class GetSpatialSnapshotAtTQueryHandler
    : IRequestHandler<GetSpatialSnapshotAtTQuery, Result<PagedList<SpatialContractDto>>>
{
    private readonly ISpatialQueryRepository _queryRepo;
    private readonly ITenantResolver         _tenantResolver;

    /// <summary>Initialises a new <see cref="GetSpatialSnapshotAtTQueryHandler"/>.</summary>
    /// <param name="queryRepo">The spatial query repository for raw SQL execution.</param>
    /// <param name="tenantResolver">Resolves the current tenant from the ambient context.</param>
    public GetSpatialSnapshotAtTQueryHandler(
        ISpatialQueryRepository queryRepo,
        ITenantResolver tenantResolver)
    {
        ArgumentNullException.ThrowIfNull(queryRepo);
        ArgumentNullException.ThrowIfNull(tenantResolver);
        _queryRepo      = queryRepo;
        _tenantResolver = tenantResolver;
    }

    /// <inheritdoc/>
    public async Task<Result<PagedList<SpatialContractDto>>> Handle(
        GetSpatialSnapshotAtTQuery request,
        CancellationToken ct)
    {
        var tenantId = _tenantResolver.TryResolve();
        if (tenantId is null)
            return Result.Unauthorized();

        var offset = (request.Page - 1) * request.PageSize;

        var (rows, totalCount) = await _queryRepo.GetSnapshotAtAsync(
            request.PhysicalSpaceId,
            tenantId.Value.Value,
            request.At,
            request.PageSize,
            offset,
            ct).ConfigureAwait(false);

        var items = rows
            .Select(r => new SpatialContractDto(
                r.ElementId,
                r.MinX, r.MinY, r.MinZ,
                r.MaxX, r.MaxY, r.MaxZ,
                r.TradeType,
                r.FsmStateAtT,
                r.ReachedAt))
            .ToList()
            .AsReadOnly();

        return Result.Success(new PagedList<SpatialContractDto>(
            items, request.Page, request.PageSize, totalCount));
    }
}
