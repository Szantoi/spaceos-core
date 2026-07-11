// SpaceOS.Kernel.Application/WorkStations/Queries/GetWorkStationsByFacilityQueryHandler.cs
using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.Specifications;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.WorkStations.Queries;

/// <summary>
/// Handles <see cref="GetWorkStationsByFacilityQuery"/>: returns a paged list of WorkStations for the given facility.
/// </summary>
public sealed class GetWorkStationsByFacilityQueryHandler
    : IRequestHandler<GetWorkStationsByFacilityQuery, Result<PagedList<WorkStationDto>>>
{
    private readonly IWorkStationRepository _repository;

    /// <summary>Initialises a new <see cref="GetWorkStationsByFacilityQueryHandler"/>.</summary>
    /// <param name="repository">Repository for workstation queries.</param>
    public GetWorkStationsByFacilityQueryHandler(IWorkStationRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    /// <summary>Executes the query and returns a paged result of workstation DTOs.</summary>
    public async Task<Result<PagedList<WorkStationDto>>> Handle(
        GetWorkStationsByFacilityQuery request,
        CancellationToken ct)
    {
        var facilityId = FacilityId.From(request.FacilityId);

        var countSpec = new WorkStationsByFacilityIdSpec(facilityId);
        var totalCount = await _repository.CountAsync(countSpec, ct).ConfigureAwait(false);

        var pagedSpec = new WorkStationsByFacilityPagedSpec(facilityId, request.Page, request.PageSize);
        var workStations = await _repository.ListAsync(pagedSpec, ct).ConfigureAwait(false);

        var items = workStations
            .Select(ws => new WorkStationDto(
                ws.Id.Value,
                ws.Name.Value,
                ws.Type.Value,
                ws.FacilityId.Value,
                ws.Status))
            .ToList()
            .AsReadOnly();

        return Result.Success(new PagedList<WorkStationDto>(items, request.Page, request.PageSize, totalCount));
    }
}
