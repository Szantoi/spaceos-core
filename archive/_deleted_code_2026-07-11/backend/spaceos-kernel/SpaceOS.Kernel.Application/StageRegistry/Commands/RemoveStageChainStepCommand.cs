// SpaceOS.Kernel.Application/StageRegistry/Commands/RemoveStageChainStepCommand.cs
using System;
using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.StageRegistry.Commands;

/// <summary>Removes a step identified by <see cref="StageCode"/> from a <see cref="Domain.Entities.StageChainTemplate"/>.</summary>
/// <param name="ChainTemplateId">The identifier of the chain template to modify.</param>
/// <param name="StageCode">The stage code of the step to remove.</param>
public sealed record RemoveStageChainStepCommand(Guid ChainTemplateId, string StageCode) : IRequest<Result>;
