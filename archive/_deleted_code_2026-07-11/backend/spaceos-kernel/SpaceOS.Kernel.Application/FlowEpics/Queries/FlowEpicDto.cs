using SpaceOS.Kernel.Domain.Enums;

namespace SpaceOS.Kernel.Application.FlowEpics.Queries;

/// <summary>
/// Data transfer object representing a flow epic returned by application queries.
/// </summary>
/// <param name="Id">The unique identifier of the epic.</param>
/// <param name="Title">The title describing the scope of the epic.</param>
/// <param name="TargetFacilityId">The identifier of the facility targeted by the epic.</param>
/// <param name="Phase">The current workflow phase of the epic.</param>
/// <param name="IsDelegated">Whether the epic has been delegated to a guest tenant.</param>
public record FlowEpicDto(
    Guid Id,
    string Title,
    Guid TargetFacilityId,
    WorkflowPhase Phase,
    bool IsDelegated);
