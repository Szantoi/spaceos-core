// SpaceOS.Kernel.Application/StageRegistry/Commands/DeactivateStageDefinitionCommand.cs
using System;
using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.StageRegistry.Commands;

/// <summary>Deactivates a <see cref="Domain.Entities.StageDefinition"/>, excluding it from active chain lookups.</summary>
/// <param name="Id">The identifier of the stage definition to deactivate.</param>
public sealed record DeactivateStageDefinitionCommand(Guid Id) : IRequest<Result>;
