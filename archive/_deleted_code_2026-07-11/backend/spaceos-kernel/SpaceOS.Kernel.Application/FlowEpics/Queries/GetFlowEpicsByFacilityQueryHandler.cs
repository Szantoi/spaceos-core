// SpaceOS.Kernel.Application/FlowEpics/Queries/GetFlowEpicsByFacilityQueryHandler.cs
using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.Specifications;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.FlowEpics.Queries;

/// <summary>
/// Handles <see cref="GetFlowEpicsByFacilityQuery"/>: returns a paged list of FlowEpics targeting the given facility.
/// </summary>
public sealed class GetFlowEpicsByFacilityQueryHandler
    : IRequestHandler<GetFlowEpicsByFacilityQuery, Result<PagedList<FlowEpicDto>>>
{
    private readonly IFlowEpicRepository _flowEpicRepository;

    /// <summary>Initialises a new <see cref="GetFlowEpicsByFacilityQueryHandler"/>.</summary>
    /// <param name="flowEpicRepository">The flow epic repository.</param>
    public GetFlowEpicsByFacilityQueryHandler(IFlowEpicRepository flowEpicRepository)
    {
        ArgumentNullException.ThrowIfNull(flowEpicRepository);
        _flowEpicRepository = flowEpicRepository;
    }

    /// <summary>Executes the query and returns a paged result of flow epic DTOs.</summary>
    public async Task<Result<PagedList<FlowEpicDto>>> Handle(
        GetFlowEpicsByFacilityQuery request,
        CancellationToken ct)
    {
        var facilityId = FacilityId.From(request.FacilityId);

        var countSpec = new FlowEpicsByFacilityIdSpec(facilityId);
        var totalCount = await _flowEpicRepository.CountAsync(countSpec, ct).ConfigureAwait(false);

        var pagedSpec = new FlowEpicsByFacilityPagedSpec(facilityId, request.Page, request.PageSize);
        var epics = await _flowEpicRepository.ListAsync(pagedSpec, ct).ConfigureAwait(false);

        var items = epics
            .Select(e => new FlowEpicDto(
                e.Id.Value,
                e.Title.Value,
                e.TargetFacilityId.Value,
                e.Phase,
                e.Handshake is not null))
            .ToList()
            .AsReadOnly();

        return Result.Success(new PagedList<FlowEpicDto>(items, request.Page, request.PageSize, totalCount));
    }
}
