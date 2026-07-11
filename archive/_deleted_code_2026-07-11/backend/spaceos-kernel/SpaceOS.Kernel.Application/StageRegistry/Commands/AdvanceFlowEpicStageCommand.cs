// SpaceOS.Kernel.Application/StageRegistry/Commands/AdvanceFlowEpicStageCommand.cs
using System;
using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.StageRegistry.Commands;

/// <summary>
/// Advances a <see cref="Domain.Entities.FlowEpic"/> to the next stage in its assigned chain.
/// The handler validates the transition via <see cref="Domain.Services.IStageChainValidator"/> (SEC-03 / BE-01).
/// </summary>
/// <param name="FlowEpicId">The identifier of the flow epic to advance.</param>
/// <param name="TargetStageCode">The stage code of the desired next stage.</param>
public sealed record AdvanceFlowEpicStageCommand(Guid FlowEpicId, string TargetStageCode) : IRequest<Result>;
