// SpaceOS.Modules.Abstractions/WorkflowPhase.cs
namespace SpaceOS.Modules.Abstractions;

/// <summary>
/// Represents the workflow phase of a flow node at the module boundary.
/// This is the canonical definition shared across all SpaceOS modules.
/// </summary>
public enum WorkflowPhase
{
    /// <summary>Discovery phase — requirements gathering and initial scoping.</summary>
    Discovery = 1,

    /// <summary>Delivery phase — active production or execution.</summary>
    Delivery = 2,

    /// <summary>Closed / done — the unit of work is complete.</summary>
    ClosedDone = 3,
}
