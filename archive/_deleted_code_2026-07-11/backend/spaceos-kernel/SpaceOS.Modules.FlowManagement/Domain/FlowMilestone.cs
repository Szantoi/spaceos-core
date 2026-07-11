// SpaceOS.Modules.FlowManagement/Domain/FlowMilestone.cs
using SpaceOS.Modules.Abstractions;

namespace SpaceOS.Modules.FlowManagement.Domain;

/// <summary>
/// Represents a milestone within a <see cref="FlowProject"/>.
/// Milestones group <see cref="FlowTask"/> instances and mark significant delivery points.
/// </summary>
public sealed class FlowMilestone : IFlowNode
{
    /// <summary>Gets the unique identifier of this milestone.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the display name of the milestone.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the project this milestone belongs to.</summary>
    public Guid ProjectId { get; private set; }

    /// <summary>Gets the optional target completion date of the milestone.</summary>
    public DateTimeOffset? TargetDate { get; private set; }

    /// <summary>Gets the current status of the milestone (e.g. "Open", "Completed").</summary>
    public string Status { get; private set; } = "Open";

    /// <summary>Gets the tenant that owns this milestone.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Gets the current workflow phase of this milestone.</summary>
    public WorkflowPhase Phase { get; private set; } = WorkflowPhase.Discovery;

    private FlowMilestone() { }

    /// <summary>
    /// Creates a new <see cref="FlowMilestone"/> with the given name and project.
    /// </summary>
    /// <param name="name">The display name of the milestone. Must not be null or whitespace.</param>
    /// <param name="projectId">The project this milestone belongs to.</param>
    /// <param name="tenantId">The tenant that owns this milestone.</param>
    /// <returns>A new <see cref="FlowMilestone"/> instance with status "Open" and phase Discovery.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or whitespace.</exception>
    public static FlowMilestone Create(string name, Guid projectId, Guid tenantId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Milestone name cannot be empty.", nameof(name));

        return new FlowMilestone
        {
            Id        = Guid.NewGuid(),
            Name      = name,
            ProjectId = projectId,
            TenantId  = tenantId,
            Status    = "Open",
            Phase     = WorkflowPhase.Discovery,
        };
    }

    /// <summary>
    /// Transitions this milestone to the "Completed" status and the <see cref="WorkflowPhase.ClosedDone"/> phase.
    /// </summary>
    public void Complete()
    {
        Status = "Completed";
        Phase  = WorkflowPhase.ClosedDone;
    }

    /// <summary>
    /// Updates the target completion date of this milestone.
    /// </summary>
    /// <param name="date">The new target date, or <c>null</c> to clear it.</param>
    public void UpdateTargetDate(DateTimeOffset? date) => TargetDate = date;
}
