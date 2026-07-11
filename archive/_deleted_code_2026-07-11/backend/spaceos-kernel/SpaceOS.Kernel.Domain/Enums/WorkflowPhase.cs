namespace SpaceOS.Kernel.Domain.Enums;

/// <summary>
/// Represents the lifecycle phase of a <see cref="SpaceOS.Kernel.Domain.Entities.FlowEpic"/>.
/// </summary>
public enum WorkflowPhase
{
    /// <summary>Initial phase where the epic is being scoped and delegated.</summary>
    Discovery = 1,

    /// <summary>Execution phase where the epic's work is actively being delivered.</summary>
    Delivery = 2,

    /// <summary>Terminal phase where the epic is closed with a verified proof document.</summary>
    ClosedDone = 3
}
