// SpaceOS.Modules.FlowManagement/Domain/FlowProject.cs
using SpaceOS.Modules.Abstractions;

namespace SpaceOS.Modules.FlowManagement.Domain;

/// <summary>
/// Represents a project within a <see cref="FlowProgram"/>.
/// Projects group related <see cref="FlowMilestone"/> and <see cref="FlowTask"/> instances.
/// </summary>
public sealed class FlowProject : IFlowNode
{
    /// <summary>Gets the unique identifier of this project.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the display name of the project.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the optional program this project belongs to.</summary>
    public Guid? ProgramId { get; private set; }

    /// <summary>Gets an optional description of the project.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the optional planned start date of the project.</summary>
    public DateTimeOffset? StartDate { get; private set; }

    /// <summary>Gets the optional planned end date of the project.</summary>
    public DateTimeOffset? EndDate { get; private set; }

    /// <summary>Gets the tenant that owns this project.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Gets the current workflow phase of this project.</summary>
    public WorkflowPhase Phase { get; private set; } = WorkflowPhase.Discovery;

    private FlowProject() { }

    /// <summary>
    /// Creates a new <see cref="FlowProject"/> with the given name and tenant.
    /// </summary>
    /// <param name="name">The display name of the project. Must not be null or whitespace.</param>
    /// <param name="tenantId">The tenant that owns this project.</param>
    /// <param name="programId">An optional program to assign this project to.</param>
    /// <returns>A new <see cref="FlowProject"/> instance with phase Discovery.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or whitespace.</exception>
    public static FlowProject Create(string name, Guid tenantId, Guid? programId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Project name cannot be empty.", nameof(name));

        return new FlowProject
        {
            Id        = Guid.NewGuid(),
            Name      = name,
            TenantId  = tenantId,
            ProgramId = programId,
            Phase     = WorkflowPhase.Discovery,
        };
    }

    /// <summary>
    /// Updates the planned start and end dates of this project.
    /// </summary>
    /// <param name="start">The new start date, or <c>null</c> to clear it.</param>
    /// <param name="end">The new end date, or <c>null</c> to clear it.</param>
    public void UpdateDates(DateTimeOffset? start, DateTimeOffset? end)
    {
        StartDate = start;
        EndDate   = end;
    }
}
