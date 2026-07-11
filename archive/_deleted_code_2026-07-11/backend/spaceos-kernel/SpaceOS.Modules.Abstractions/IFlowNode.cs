// SpaceOS.Modules.Abstractions/IFlowNode.cs
namespace SpaceOS.Modules.Abstractions;

/// <summary>
/// Represents a node within a workflow flow — the cross-module contract for any
/// unit of work that participates in a SpaceOS flow.
/// </summary>
public interface IFlowNode
{
    /// <summary>Gets the unique identifier of this flow node.</summary>
    Guid Id { get; }

    /// <summary>Gets the human-readable name of this flow node.</summary>
    string Name { get; }

    /// <summary>Gets the current workflow phase of this flow node.</summary>
    WorkflowPhase Phase { get; }
}
