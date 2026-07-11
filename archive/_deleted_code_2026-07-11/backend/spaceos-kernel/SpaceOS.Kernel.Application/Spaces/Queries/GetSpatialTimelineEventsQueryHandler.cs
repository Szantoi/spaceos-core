// SpaceOS.Kernel.Application/Spaces/Queries/GetSpatialTimelineEventsQueryHandler.cs

using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Domain.Auth;
using SpaceOS.Kernel.Domain.Repositories;

namespace SpaceOS.Kernel.Application.Spaces.Queries;

/// <summary>
/// Handles <see cref="GetSpatialTimelineEventsQuery"/>: returns all timeline events
/// for a physical space in chronological order.
/// Uses raw SQL via <see cref="ISpatialQueryRepository"/> (spec §5.4).
/// </summary>
internal sealed class GetSpatialTimelineEventsQueryHandler
    : IRequestHandler<GetSpatialTimelineEventsQuery, Result<List<SpatialTimelineEventDto>>>
{
    private readonly ISpatialQueryRepository _queryRepo;
    private readonly ITenantResolver         _tenantResolver;

    /// <summary>Initialises a new <see cref="GetSpatialTimelineEventsQueryHandler"/>.</summary>
    /// <param name="queryRepo">The spatial query repository for raw SQL execution.</param>
    /// <param name="tenantResolver">Resolves the current tenant from the ambient context.</param>
    public GetSpatialTimelineEventsQueryHandler(
        ISpatialQueryRepository queryRepo,
        ITenantResolver tenantResolver)
    {
        ArgumentNullException.ThrowIfNull(queryRepo);
        ArgumentNullException.ThrowIfNull(tenantResolver);
        _queryRepo      = queryRepo;
        _tenantResolver = tenantResolver;
    }

    /// <inheritdoc/>
    public async Task<Result<List<SpatialTimelineEventDto>>> Handle(
        GetSpatialTimelineEventsQuery request,
        CancellationToken ct)
    {
        var tenantId = _tenantResolver.TryResolve();
        if (tenantId is null)
            return Result.Unauthorized();

        var rows = await _queryRepo.GetTimelineEventsAsync(
            request.PhysicalSpaceId,
            tenantId.Value.Value,
            ct).ConfigureAwait(false);

        var items = rows
            .Select(r => new SpatialTimelineEventDto(
                r.OccurredAt,
                r.ElementId,
                r.TradeType,
                r.FromState,
                r.ToState))
            .ToList();

        return Result.Success(items);
    }
}
