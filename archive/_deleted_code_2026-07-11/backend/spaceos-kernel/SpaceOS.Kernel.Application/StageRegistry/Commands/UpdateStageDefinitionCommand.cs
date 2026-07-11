// SpaceOS.Kernel.Application/StageRegistry/Commands/UpdateStageDefinitionCommand.cs
using System;
using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.StageRegistry.Commands;

/// <summary>Updates the module endpoint of an existing <see cref="Domain.Entities.StageDefinition"/>.</summary>
/// <param name="Id">The identifier of the stage definition to update.</param>
/// <param name="ModuleEndpoint">The new loopback URL (SEC-01: port 5000-5099).</param>
public sealed record UpdateStageDefinitionCommand(Guid Id, string ModuleEndpoint) : IRequest<Result>;
