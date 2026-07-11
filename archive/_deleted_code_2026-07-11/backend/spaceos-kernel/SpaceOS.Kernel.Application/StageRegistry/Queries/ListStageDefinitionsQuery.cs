// SpaceOS.Kernel.Application/StageRegistry/Queries/ListStageDefinitionsQuery.cs
using System.Collections.Generic;
using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.StageRegistry.Queries;

/// <summary>Returns all active <see cref="Domain.Entities.StageDefinition"/> records for the current tenant.</summary>
public sealed record ListStageDefinitionsQuery : IRequest<Result<IReadOnlyList<StageDefinitionDto>>>;
