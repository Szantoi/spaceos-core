using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.FlowEpics.Commands;

/// <summary>
/// Command to update the title of an existing <see cref="SpaceOS.Kernel.Domain.Entities.FlowEpic"/>.
/// </summary>
/// <param name="FlowEpicId">The identifier of the epic whose title will be updated.</param>
/// <param name="NewTitle">The new title to assign to the epic.</param>
public record UpdateFlowEpicTitleCommand(Guid FlowEpicId, string NewTitle) : IRequest<Result>;
