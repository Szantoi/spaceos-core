// SpaceOS.Kernel.Application/Spaces/Commands/DTOs/LinkTaskToElementCommand.cs

using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Domain.Enums;

namespace SpaceOS.Kernel.Application.Spaces.Commands;

/// <summary>
/// Command to link a FlowTask to a <see cref="SpaceOS.Kernel.Domain.Entities.SpatialElement"/>
/// with a specific <see cref="WorkPhase"/>.
/// SEC-P3A-02: cross-tenant check is enforced at handler level.
/// </summary>
/// <param name="FlowTaskId">The FlowTask identifier to link.</param>
/// <param name="SpatialElementId">The SpatialElement identifier to link to.</param>
/// <param name="WorkPhase">The manufacturing or installation phase.</param>
public sealed record LinkTaskToElementCommand(
    Guid FlowTaskId,
    Guid SpatialElementId,
    WorkPhase WorkPhase) : IRequest<Result>;
