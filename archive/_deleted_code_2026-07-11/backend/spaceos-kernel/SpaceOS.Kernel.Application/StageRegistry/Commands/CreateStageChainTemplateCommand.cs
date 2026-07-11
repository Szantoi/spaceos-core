// SpaceOS.Kernel.Application/StageRegistry/Commands/CreateStageChainTemplateCommand.cs
using System;
using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.StageRegistry.Commands;

/// <summary>Creates a new <see cref="Domain.Entities.StageChainTemplate"/> for a tenant.</summary>
/// <param name="TenantId">The identifier of the owning tenant.</param>
/// <param name="Name">The unique name for the chain template within the tenant.</param>
/// <param name="IsDefault">Whether this should be the default chain for the tenant.</param>
public sealed record CreateStageChainTemplateCommand(
    Guid TenantId,
    string Name,
    bool IsDefault = false) : IRequest<Result<Guid>>;
