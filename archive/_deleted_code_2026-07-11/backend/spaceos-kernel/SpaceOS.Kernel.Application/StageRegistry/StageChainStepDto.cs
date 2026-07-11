// SpaceOS.Kernel.Application/StageRegistry/StageChainStepDto.cs
using System;

namespace SpaceOS.Kernel.Application.StageRegistry;

/// <summary>Read model for a <see cref="Domain.Entities.StageChainStep"/>.</summary>
/// <param name="Id">Unique identifier.</param>
/// <param name="StageDefinitionId">Referenced stage definition identifier.</param>
/// <param name="StageCode">Denormalised stage code.</param>
/// <param name="SortOrder">1-based sort order within the chain.</param>
/// <param name="IsOptional">Whether this step may be skipped.</param>
public sealed record StageChainStepDto(
    Guid Id,
    Guid StageDefinitionId,
    string StageCode,
    int SortOrder,
    bool IsOptional);
