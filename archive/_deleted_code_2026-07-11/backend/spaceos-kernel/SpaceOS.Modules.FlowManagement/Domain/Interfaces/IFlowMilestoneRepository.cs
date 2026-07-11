// SpaceOS.Modules.FlowManagement/Domain/Interfaces/IFlowMilestoneRepository.cs
namespace SpaceOS.Modules.FlowManagement.Domain.Interfaces;

/// <summary>
/// Repository contract for <see cref="FlowMilestone"/> persistence.
/// </summary>
public interface IFlowMilestoneRepository
{
    /// <summary>
    /// Retrieves a <see cref="FlowMilestone"/> by its identifier, or <c>null</c> if not found.
    /// </summary>
    /// <param name="id">The unique identifier of the milestone.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching milestone, or <c>null</c>.</returns>
    Task<FlowMilestone?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Persists a new <see cref="FlowMilestone"/> to the store.
    /// </summary>
    /// <param name="milestone">The milestone to add.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddAsync(FlowMilestone milestone, CancellationToken ct);

    /// <summary>
    /// Persists changes to an existing <see cref="FlowMilestone"/>.
    /// </summary>
    /// <param name="milestone">The milestone to update.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateAsync(FlowMilestone milestone, CancellationToken ct);

    /// <summary>
    /// Returns all milestones belonging to the given project.
    /// </summary>
    /// <param name="projectId">The identifier of the project.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of milestones belonging to the project.</returns>
    Task<IReadOnlyList<FlowMilestone>> ListByProjectAsync(Guid projectId, CancellationToken ct);
}
