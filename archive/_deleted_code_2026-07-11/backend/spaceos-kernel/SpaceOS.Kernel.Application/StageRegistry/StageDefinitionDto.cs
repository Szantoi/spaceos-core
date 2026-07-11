// SpaceOS.Kernel.Application/StageRegistry/StageDefinitionDto.cs
using System;

namespace SpaceOS.Kernel.Application.StageRegistry;

/// <summary>Read model for a <see cref="Domain.Entities.StageDefinition"/>.</summary>
/// <param name="Id">Unique identifier.</param>
/// <param name="TenantId">Owning tenant identifier.</param>
/// <param name="StageCode">Immutable stage code.</param>
/// <param name="DisplayName">Human-readable display name.</param>
/// <param name="ModuleEndpoint">Loopback URL of the Stage Module.</param>
/// <param name="IsActive">Whether this definition is currently active.</param>
/// <param name="CreatedAt">UTC creation timestamp.</param>
/// <param name="UpdatedAt">UTC last-modified timestamp.</param>
public sealed record StageDefinitionDto(
    Guid Id,
    Guid TenantId,
    string StageCode,
    string DisplayName,
    string ModuleEndpoint,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
