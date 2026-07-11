// SpaceOS.Kernel.Application/StageRegistry/StageChainTemplateDto.cs
using System;

namespace SpaceOS.Kernel.Application.StageRegistry;

/// <summary>Summary read model for a <see cref="Domain.Entities.StageChainTemplate"/>.</summary>
/// <param name="Id">Unique identifier.</param>
/// <param name="TenantId">Owning tenant identifier.</param>
/// <param name="Name">Unique name within the tenant.</param>
/// <param name="IsDefault">Whether this is the default chain for the tenant.</param>
/// <param name="StepCount">Number of steps in this chain.</param>
/// <param name="CreatedAt">UTC creation timestamp.</param>
/// <param name="UpdatedAt">UTC last-modified timestamp.</param>
public sealed record StageChainTemplateDto(
    Guid Id,
    Guid TenantId,
    string Name,
    bool IsDefault,
    int StepCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
