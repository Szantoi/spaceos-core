// SpaceOS.Kernel.Application/StageRegistry/Commands/RegisterStageDefinitionCommand.cs
using System;
using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.StageRegistry.Commands;

/// <summary>Registers a new <see cref="Domain.Entities.StageDefinition"/> for a tenant.</summary>
/// <param name="TenantId">The identifier of the owning tenant.</param>
/// <param name="StageCode">The immutable lowercase stage code.</param>
/// <param name="DisplayName">Human-readable display name.</param>
/// <param name="ModuleEndpoint">Loopback URL of the Stage Module (SEC-01: port 5000-5099).</param>
public sealed record RegisterStageDefinitionCommand(
    Guid TenantId,
    string StageCode,
    string DisplayName,
    string ModuleEndpoint) : IRequest<Result<Guid>>;
