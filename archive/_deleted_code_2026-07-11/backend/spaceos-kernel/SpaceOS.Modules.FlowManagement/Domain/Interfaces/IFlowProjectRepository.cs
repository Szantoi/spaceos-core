// SpaceOS.Modules.FlowManagement/Domain/Interfaces/IFlowProjectRepository.cs
namespace SpaceOS.Modules.FlowManagement.Domain.Interfaces;

/// <summary>
/// Repository contract for <see cref="FlowProject"/> persistence.
/// </summary>
public interface IFlowProjectRepository
{
    /// <summary>
    /// Retrieves a <see cref="FlowProject"/> by its identifier, or <c>null</c> if not found.
    /// </summary>
    /// <param name="id">The unique identifier of the project.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching project, or <c>null</c>.</returns>
    Task<FlowProject?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Persists a new <see cref="FlowProject"/> to the store.
    /// </summary>
    /// <param name="project">The project to add.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddAsync(FlowProject project, CancellationToken ct);

    /// <summary>
    /// Persists changes to an existing <see cref="FlowProject"/>.
    /// </summary>
    /// <param name="project">The project to update.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateAsync(FlowProject project, CancellationToken ct);

    /// <summary>
    /// Returns all projects optionally scoped to the given program.
    /// Pass <c>null</c> to retrieve projects not assigned to any program.
    /// </summary>
    /// <param name="programId">
    /// The identifier of the program to filter by, or <c>null</c> for unassigned projects.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of projects matching the filter.</returns>
    Task<IReadOnlyList<FlowProject>> ListByProgramAsync(Guid? programId, CancellationToken ct);
}
