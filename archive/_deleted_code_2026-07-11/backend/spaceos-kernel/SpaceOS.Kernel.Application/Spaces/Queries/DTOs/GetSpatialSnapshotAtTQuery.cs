// SpaceOS.Kernel.Application/Spaces/Queries/DTOs/GetSpatialSnapshotAtTQuery.cs

using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;

namespace SpaceOS.Kernel.Application.Spaces.Queries;

/// <summary>
/// Query to retrieve a paginated spatial snapshot at a given point in time.
/// Returns the FSM state of each spatial element as it was at <paramref name="At"/>.
/// </summary>
/// <param name="PhysicalSpaceId">The physical space to query.</param>
/// <param name="At">The point in time to snapshot.</param>
/// <param name="Page">The 1-based page number (default 1).</param>
/// <param name="PageSize">The number of items per page (default 50).</param>
public sealed record GetSpatialSnapshotAtTQuery(
    Guid PhysicalSpaceId,
    DateTimeOffset At,
    int Page = 1,
    int PageSize = 50) : IRequest<Result<PagedList<SpatialContractDto>>>;
