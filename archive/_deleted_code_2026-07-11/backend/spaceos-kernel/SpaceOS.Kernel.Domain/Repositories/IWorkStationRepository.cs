using Ardalis.Specification;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Repositories;

/// <summary>
/// Persistence contract for the <see cref="WorkStation"/> aggregate root.
/// </summary>
public interface IWorkStationRepository
{
    /// <summary>Returns the <see cref="WorkStation"/> with the given <paramref name="id"/>, or <see langword="null"/> if not found.</summary>
    /// <param name="id">The workstation identifier to look up.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<WorkStation?> GetByIdAsync(WorkStationId id, CancellationToken ct = default);

    /// <summary>Returns <see langword="true"/> when a workstation with the given name already exists in the specified facility.</summary>
    /// <param name="facilityId">The facility scope to check within.</param>
    /// <param name="name">The workstation name to check for uniqueness.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<bool> ExistsByNameAsync(FacilityId facilityId, string name, CancellationToken ct = default);

    /// <summary>Stages a new <see cref="WorkStation"/> for insertion. Persist via <c>IUnitOfWork.SaveChangesAsync</c>.</summary>
    /// <param name="workStation">The workstation to add.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task AddAsync(WorkStation workStation, CancellationToken ct = default);

    /// <summary>Marks an existing <see cref="WorkStation"/> as modified. Persist via <c>IUnitOfWork.SaveChangesAsync</c>.</summary>
    /// <param name="workStation">The workstation to update.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task UpdateAsync(WorkStation workStation, CancellationToken ct = default);

    /// <summary>Returns all workstations matching the given specification.</summary>
    /// <param name="specification">The query specification.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <remarks>
    /// All list/filter queries must be expressed as an <c>ISpecification&lt;WorkStation&gt;</c> —
    /// never raw LINQ inside application handlers.
    /// </remarks>
    Task<IReadOnlyList<WorkStation>> ListAsync(ISpecification<WorkStation> specification, CancellationToken ct = default);

    /// <summary>Returns the total count of <see cref="WorkStation"/> instances matching the given specification.</summary>
    /// <param name="specification">The filter specification (must not apply Skip/Take).</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<int> CountAsync(ISpecification<WorkStation> specification, CancellationToken ct = default);
}
