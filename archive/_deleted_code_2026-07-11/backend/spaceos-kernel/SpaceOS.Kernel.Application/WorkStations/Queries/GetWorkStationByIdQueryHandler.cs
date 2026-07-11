using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.WorkStations.Queries;

/// <summary>
/// Handles <see cref="GetWorkStationByIdQuery"/>: retrieves a workstation by identifier and
/// projects the result to a <see cref="WorkStationDto"/>.
/// </summary>
public class GetWorkStationByIdQueryHandler : IRequestHandler<GetWorkStationByIdQuery, Result<WorkStationDto>>
{
    private readonly IWorkStationRepository _repository;

    /// <summary>
    /// Initialises a new <see cref="GetWorkStationByIdQueryHandler"/>.
    /// </summary>
    /// <param name="repository">Repository for workstation queries.</param>
    public GetWorkStationByIdQueryHandler(IWorkStationRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    /// <inheritdoc/>
    public async Task<Result<WorkStationDto>> Handle(GetWorkStationByIdQuery request, CancellationToken ct)
    {
        var ws = await _repository
            .GetByIdAsync(WorkStationId.From(request.WorkStationId), ct)
            .ConfigureAwait(false);

        if (ws is null)
        {
            return Result.NotFound();
        }

        return Result.Success(new WorkStationDto(
            ws.Id.Value,
            ws.Name.Value,
            ws.Type.Value,
            ws.FacilityId.Value,
            ws.Status));
    }
}
