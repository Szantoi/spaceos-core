// SpaceOS.Modules.FlowManagement/Domain/FlowTask.cs
using SpaceOS.Modules.Abstractions;
using SpaceOS.Modules.Abstractions.Sync;

namespace SpaceOS.Modules.FlowManagement.Domain;

/// <summary>
/// Represents an individual work item within the FlowManagement module.
/// <see cref="EpicKernelId"/> is a UUID-only reference to a Kernel <c>FlowEpic</c> —
/// no foreign key is maintained across the module boundary.
/// </summary>
public sealed class FlowTask : IFlowNode, ISyncable
{
    /// <summary>Gets the unique identifier of this task.</summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the UUID of the Kernel <c>FlowEpic</c> this task is associated with.
    /// This is a UUID-only link — no foreign key constraint is enforced across the module boundary.
    /// </summary>
    public Guid EpicKernelId { get; private set; }

    /// <summary>Gets the optional milestone this task is grouped under.</summary>
    public Guid? MilestoneId { get; private set; }

    /// <summary>Gets the display name of the task.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets an optional description of the task.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the optional user assigned to this task.</summary>
    public Guid? AssigneeId { get; private set; }

    /// <summary>Gets the current status of the task (e.g. "Open", "Completed").</summary>
    public string Status { get; private set; } = "Open";

    /// <summary>Gets the optional due date of the task.</summary>
    public DateTimeOffset? DueDate { get; private set; }

    /// <summary>Gets the tenant that owns this task.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Gets the current workflow phase of this task.</summary>
    public WorkflowPhase Phase { get; private set; } = WorkflowPhase.Discovery;

    /// <summary>
    /// Gets a value indicating whether this task has been successfully synchronised
    /// with the Kernel at least once.
    /// </summary>
    public bool IsSyncedToKernel { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp of the most recent successful sync, or <c>null</c> if the
    /// task has never been synchronised.
    /// </summary>
    public DateTimeOffset? LastSyncAt { get; private set; }

    private FlowTask() { }

    /// <summary>
    /// Creates a new <see cref="FlowTask"/> linked to the given Kernel epic.
    /// </summary>
    /// <param name="epicKernelId">The UUID of the Kernel FlowEpic. UUID-only — no FK.</param>
    /// <param name="name">The display name of the task. Must not be null or whitespace.</param>
    /// <param name="tenantId">The tenant that owns this task.</param>
    /// <param name="milestoneId">An optional milestone to group this task under.</param>
    /// <returns>A new <see cref="FlowTask"/> instance with status "Open" and phase Discovery.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or whitespace.</exception>
    public static FlowTask Create(Guid epicKernelId, string name, Guid tenantId, Guid? milestoneId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Task name cannot be empty.", nameof(name));

        return new FlowTask
        {
            Id           = Guid.NewGuid(),
            EpicKernelId = epicKernelId,
            Name         = name,
            TenantId     = tenantId,
            MilestoneId  = milestoneId,
            Status       = "Open",
            Phase        = WorkflowPhase.Discovery,
        };
    }

    /// <summary>
    /// Transitions this task to the "Completed" status and the <see cref="WorkflowPhase.ClosedDone"/> phase.
    /// </summary>
    public void Complete()
    {
        Status = "Completed";
        Phase  = WorkflowPhase.ClosedDone;
    }

    /// <summary>
    /// Transitions this task back to the "Open" status and the <see cref="WorkflowPhase.Discovery"/> phase.
    /// Can only be called when the current status is "Completed".
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the task is not in "Completed" status.</exception>
    public void Reopen()
    {
        if (Status != "Completed")
            throw new InvalidOperationException($"Cannot reopen a task that is not completed. Current status: {Status}.");

        Status = "Open";
        Phase  = WorkflowPhase.Discovery;
    }

    /// <summary>
    /// Assigns this task to the specified user.
    /// </summary>
    /// <param name="assigneeId">The identifier of the user to assign this task to.</param>
    public void Assign(Guid assigneeId) => AssigneeId = assigneeId;

    /// <summary>
    /// Records a successful synchronisation with the Kernel.
    /// </summary>
    /// <param name="syncedAt">The UTC timestamp of the sync event.</param>
    public void MarkSynced(DateTimeOffset syncedAt)
    {
        IsSyncedToKernel = true;
        LastSyncAt       = syncedAt;
    }
}
