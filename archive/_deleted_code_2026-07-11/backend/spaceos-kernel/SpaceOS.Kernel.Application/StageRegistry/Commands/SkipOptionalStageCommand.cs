// SpaceOS.Kernel.Application/StageRegistry/Commands/SkipOptionalStageCommand.cs
using System;
using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.StageRegistry.Commands;

/// <summary>Records that an optional stage was explicitly skipped on a <see cref="Domain.Entities.FlowEpic"/>.</summary>
/// <param name="FlowEpicId">The identifier of the flow epic.</param>
/// <param name="StageCode">The stage code of the optional stage to skip.</param>
public sealed record SkipOptionalStageCommand(Guid FlowEpicId, string StageCode) : IRequest<Result>;
