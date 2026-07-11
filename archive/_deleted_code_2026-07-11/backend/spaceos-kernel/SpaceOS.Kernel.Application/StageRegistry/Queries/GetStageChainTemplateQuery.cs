// SpaceOS.Kernel.Application/StageRegistry/Queries/GetStageChainTemplateQuery.cs
using System;
using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.StageRegistry.Queries;

/// <summary>Returns a <see cref="Domain.Entities.StageChainTemplate"/> with its ordered steps by identifier.</summary>
/// <param name="Id">The identifier of the chain template to retrieve.</param>
public sealed record GetStageChainTemplateQuery(Guid Id)
    : IRequest<Result<StageChainTemplateDetailDto>>;
