// SpaceOS.Modules.FlowManagement/Domain/Interfaces/IFlowTaskRepository.cs
namespace SpaceOS.Modules.FlowManagement.Domain.Interfaces;

/// <summary>
/// Repository contract for <see cref="FlowTask"/> persistence.
/// </summary>
public interface IFlowTaskRepository
{
    /// <summary>
    /// Retrieves a <see cref="FlowTask"/> by its identifier, or <c>null</c> if not found.
    /// </summary>
    /// <param name="id">The unique identifier of the task.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching task, or <c>null</c>.</returns>
    Task<FlowTask?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Persists a new <see cref="FlowTask"/> to the store.
    /// </summary>
    /// <param name="task">The task to add.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddAsync(FlowTask task, CancellationToken ct);

    /// <summary>
    /// Persists changes to an existing <see cref="FlowTask"/>.
    /// </summary>
    /// <param name="task">The task to update.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateAsync(FlowTask task, CancellationToken ct);

    /// <summary>
    /// Returns all tasks associated with the given Kernel epic identifier.
    /// </summary>
    /// <param name="epicKernelId">The UUID of the Kernel FlowEpic.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of tasks belonging to the epic.</returns>
    Task<IReadOnlyList<FlowTask>> ListByEpicAsync(Guid epicKernelId, CancellationToken ct);
}
