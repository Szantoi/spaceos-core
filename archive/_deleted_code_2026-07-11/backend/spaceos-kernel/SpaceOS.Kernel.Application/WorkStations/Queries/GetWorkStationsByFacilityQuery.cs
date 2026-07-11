// SpaceOS.Kernel.Application/WorkStations/Queries/GetWorkStationsByFacilityQuery.cs
using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;

namespace SpaceOS.Kernel.Application.WorkStations.Queries;

/// <summary>Returns a single page of WorkStations belonging to the specified facility.</summary>
/// <param name="FacilityId">The identifier of the facility to filter by.</param>
/// <param name="Page">1-based page number. Defaults to 1.</param>
/// <param name="PageSize">Maximum items per page. Defaults to 20, maximum 100.</param>
public sealed record GetWorkStationsByFacilityQuery(Guid FacilityId, int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedList<WorkStationDto>>>;
