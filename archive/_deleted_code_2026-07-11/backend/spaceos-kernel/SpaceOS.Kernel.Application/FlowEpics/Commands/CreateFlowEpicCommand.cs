using MediatR;
using Ardalis.Result;

namespace SpaceOS.Kernel.Application.FlowEpics.Commands;

/// <summary>
/// Command to create a new <see cref="SpaceOS.Kernel.Domain.Entities.FlowEpic"/> in the Discovery phase.
/// Returns the new epic's <see cref="Guid"/> identifier on success.
/// </summary>
/// <param name="Title">A non-empty title describing the scope of the epic.</param>
/// <param name="TargetFacilityId">The identifier of the facility targeted by this epic.</param>
/// <param name="TenantId">The identifier of the tenant that owns this epic.</param>
public record CreateFlowEpicCommand(string Title, Guid TargetFacilityId, Guid TenantId) : IRequest<Result<Guid>>;
