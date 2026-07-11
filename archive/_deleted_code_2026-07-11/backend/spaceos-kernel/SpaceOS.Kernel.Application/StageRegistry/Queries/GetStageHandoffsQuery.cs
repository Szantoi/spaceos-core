// SpaceOS.Kernel.Application/StageRegistry/Queries/GetStageHandoffsQuery.cs
using System;
using System.Collections.Generic;
using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.StageRegistry.Queries;

/// <summary>Returns the handoff history for a given flow epic.</summary>
/// <param name="FlowEpicId">The identifier of the flow epic whose handoffs to return.</param>
public sealed record GetStageHandoffsQuery(Guid FlowEpicId)
    : IRequest<Result<IReadOnlyList<StageHandoffDto>>>;
