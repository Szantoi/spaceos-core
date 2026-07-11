// SpaceOS.Kernel.Application/StageRegistry/Commands/AssignChainCommand.cs
using System;
using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.StageRegistry.Commands;

/// <summary>Assigns a <see cref="Domain.Entities.StageChainTemplate"/> to a <see cref="Domain.Entities.FlowEpic"/> and sets its initial stage.</summary>
/// <param name="FlowEpicId">The identifier of the flow epic to assign the chain to.</param>
/// <param name="ChainTemplateId">The identifier of the chain template to follow.</param>
/// <param name="FirstStageCode">The stage code of the first step in the chain.</param>
public sealed record AssignChainCommand(
    Guid FlowEpicId,
    Guid ChainTemplateId,
    string FirstStageCode) : IRequest<Result>;
