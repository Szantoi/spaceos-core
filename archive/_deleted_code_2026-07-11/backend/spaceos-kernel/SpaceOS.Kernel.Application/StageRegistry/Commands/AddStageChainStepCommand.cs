// SpaceOS.Kernel.Application/StageRegistry/Commands/AddStageChainStepCommand.cs
using System;
using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.StageRegistry.Commands;

/// <summary>Adds a <see cref="Domain.Entities.StageChainStep"/> to an existing <see cref="Domain.Entities.StageChainTemplate"/>.</summary>
/// <param name="ChainTemplateId">The identifier of the chain template to modify.</param>
/// <param name="StageDefinitionId">The identifier of the stage definition to add.</param>
/// <param name="SortOrder">The 1-based sort order within the chain (must be unique).</param>
/// <param name="IsOptional">Whether this step may be skipped (SEC-03).</param>
public sealed record AddStageChainStepCommand(
    Guid ChainTemplateId,
    Guid StageDefinitionId,
    int SortOrder,
    bool IsOptional = false) : IRequest<Result>;
