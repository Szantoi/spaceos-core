using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.WorkStations.Queries;

/// <summary>Query to retrieve a single workstation by its unique identifier.</summary>
/// <param name="WorkStationId">The identifier of the workstation to retrieve.</param>
public record GetWorkStationByIdQuery(Guid WorkStationId) : IRequest<Result<WorkStationDto>>;
