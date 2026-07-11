// SpaceOS.Modules.FlowManagement/Domain/Interfaces/IFlowProgramRepository.cs
namespace SpaceOS.Modules.FlowManagement.Domain.Interfaces;

/// <summary>
/// Repository contract for <see cref="FlowProgram"/> persistence.
/// </summary>
public interface IFlowProgramRepository
{
    /// <summary>
    /// Retrieves a <see cref="FlowProgram"/> by its identifier, or <c>null</c> if not found.
    /// </summary>
    /// <param name="id">The unique identifier of the program.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching program, or <c>null</c>.</returns>
    Task<FlowProgram?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Persists a new <see cref="FlowProgram"/> to the store.
    /// </summary>
    /// <param name="program">The program to add.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddAsync(FlowProgram program, CancellationToken ct);

    /// <summary>
    /// Persists changes to an existing <see cref="FlowProgram"/>.
    /// </summary>
    /// <param name="program">The program to update.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateAsync(FlowProgram program, CancellationToken ct);

    /// <summary>
    /// Returns all programs owned by the given tenant.
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of programs belonging to the tenant.</returns>
    Task<IReadOnlyList<FlowProgram>> ListByTenantAsync(Guid tenantId, CancellationToken ct);
}
