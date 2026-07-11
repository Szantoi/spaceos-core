using MediatR;
using Ardalis.Result;

namespace SpaceOS.Kernel.Application.FlowEpics.Commands;

/// <summary>
/// Command to advance a <see cref="SpaceOS.Kernel.Domain.Entities.FlowEpic"/> from the Discovery phase
/// to the Delivery (execution) phase.
/// </summary>
/// <param name="FlowEpicId">The identifier of the epic to start execution for.</param>
public record StartFlowEpicExecutionCommand(Guid FlowEpicId) : IRequest<Result>;
