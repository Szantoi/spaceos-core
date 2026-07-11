using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.FlowEpics.Queries;

/// <summary>
/// Query to retrieve a single flow epic by its unique identifier.
/// </summary>
/// <param name="FlowEpicId">The identifier of the epic to retrieve.</param>
public record GetFlowEpicByIdQuery(Guid FlowEpicId) : IRequest<Result<FlowEpicDto>>;
