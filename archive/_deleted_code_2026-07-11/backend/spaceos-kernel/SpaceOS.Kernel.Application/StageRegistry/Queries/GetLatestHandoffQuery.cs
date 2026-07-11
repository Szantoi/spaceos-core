// SpaceOS.Kernel.Application/StageRegistry/Queries/GetLatestHandoffQuery.cs
using System;
using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.StageRegistry.Queries;

/// <summary>Returns the most recent <see cref="Domain.Entities.StageHandoff"/> for a given (FlowEpic, source, target) triple.</summary>
/// <param name="FlowEpicId">The identifier of the flow epic.</param>
/// <param name="SourceStageCode">The source stage code.</param>
/// <param name="TargetStageCode">The target stage code.</param>
public sealed record GetLatestHandoffQuery(Guid FlowEpicId, string SourceStageCode, string TargetStageCode)
    : IRequest<Result<StageHandoffDto>>;
