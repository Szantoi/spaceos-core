using Ardalis.Specification;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Repositories;

/// <summary>
/// Persistence contract for the <see cref="Facility"/> aggregate root.
/// </summary>
public interface IFacilityRepository
{
    /// <summary>Returns the <see cref="Facility"/> with the given <paramref name="id"/>, or <see langword="null"/> if not found.</summary>
    /// <param name="id">The facility identifier to look up.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<Facility?> GetByIdAsync(FacilityId id, CancellationToken ct = default);

    /// <summary>Returns <see langword="true"/> when a facility with the given name already exists for the specified tenant.</summary>
    /// <param name="tenantId">The tenant scope to check within.</param>
    /// <param name="name">The facility name to check for uniqueness.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<bool> ExistsByNameAsync(TenantId tenantId, string name, CancellationToken ct = default);

    /// <summary>Stages a new <see cref="Facility"/> for insertion. Persist via <c>IUnitOfWork.SaveChangesAsync</c>.</summary>
    /// <param name="facility">The facility to add.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task AddAsync(Facility facility, CancellationToken ct = default);

    /// <summary>Stages an updated <see cref="Facility"/> for persistence. Persist via <c>IUnitOfWork.SaveChangesAsync</c>.</summary>
    /// <param name="facility">The facility to update.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task UpdateAsync(Facility facility, CancellationToken ct = default);

    /// <summary>Returns all <see cref="Facility"/> instances matching the given specification.</summary>
    Task<IReadOnlyList<Facility>> ListAsync(ISpecification<Facility> specification, CancellationToken ct = default);

    /// <summary>Returns the total count of <see cref="Facility"/> instances matching the given specification.</summary>
    /// <param name="specification">The filter specification (must not apply Skip/Take).</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<int> CountAsync(ISpecification<Facility> specification, CancellationToken ct = default);
}
