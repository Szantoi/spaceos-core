using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.FlowEpics.Queries;

/// <summary>
/// Handles <see cref="GetFlowEpicByIdQuery"/>: retrieves a flow epic by identifier and
/// projects the result to a <see cref="FlowEpicDto"/>.
/// </summary>
public class GetFlowEpicByIdQueryHandler : IRequestHandler<GetFlowEpicByIdQuery, Result<FlowEpicDto>>
{
    private readonly IFlowEpicRepository _flowEpicRepository;

    /// <summary>
    /// Initialises a new <see cref="GetFlowEpicByIdQueryHandler"/>.
    /// </summary>
    /// <param name="flowEpicRepository">Repository for flow epic queries.</param>
    public GetFlowEpicByIdQueryHandler(IFlowEpicRepository flowEpicRepository)
    {
        ArgumentNullException.ThrowIfNull(flowEpicRepository);
        _flowEpicRepository = flowEpicRepository;
    }

    /// <inheritdoc/>
    public async Task<Result<FlowEpicDto>> Handle(GetFlowEpicByIdQuery request, CancellationToken ct)
    {
        var epic = await _flowEpicRepository
            .GetByIdAsync(FlowEpicId.From(request.FlowEpicId), ct)
            .ConfigureAwait(false);

        if (epic is null)
        {
            return Result.NotFound();
        }

        return Result.Success(new FlowEpicDto(
            epic.Id.Value,
            epic.Title.Value,
            epic.TargetFacilityId.Value,
            epic.Phase,
            epic.Handshake is not null));
    }
}
