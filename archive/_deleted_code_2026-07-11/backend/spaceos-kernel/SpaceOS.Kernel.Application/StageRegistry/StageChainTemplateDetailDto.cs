// SpaceOS.Kernel.Application/StageRegistry/StageChainTemplateDetailDto.cs
using System;
using System.Collections.Generic;

namespace SpaceOS.Kernel.Application.StageRegistry;

/// <summary>Detailed read model for a <see cref="Domain.Entities.StageChainTemplate"/> including its ordered steps.</summary>
/// <param name="Id">Unique identifier.</param>
/// <param name="TenantId">Owning tenant identifier.</param>
/// <param name="Name">Unique name within the tenant.</param>
/// <param name="IsDefault">Whether this is the default chain for the tenant.</param>
/// <param name="Steps">Ordered steps of this chain template.</param>
/// <param name="CreatedAt">UTC creation timestamp.</param>
/// <param name="UpdatedAt">UTC last-modified timestamp.</param>
public sealed record StageChainTemplateDetailDto(
    Guid Id,
    Guid TenantId,
    string Name,
    bool IsDefault,
    IReadOnlyList<StageChainStepDto> Steps,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
