using System;
using System.Threading;
using System.Threading.Tasks;
using SpaceOS.Kernel.Domain.Aggregates;

namespace SpaceOS.Kernel.Domain.Repositories;

/// <summary>
/// Persistence contract for the <see cref="PhysicalSpace"/> aggregate root.
/// </summary>
public interface IPhysicalSpaceRepository
{
    /// <summary>Returns the <see cref="PhysicalSpace"/> with the given <paramref name="id"/>, or <see langword="null"/> if not found.</summary>
    /// <param name="id">The physical space identifier to look up.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<PhysicalSpace?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns <see langword="true"/> when a physical space with the given <paramref name="id"/> exists.</summary>
    /// <param name="id">The physical space identifier to check.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);

    /// <summary>Stages a new <see cref="PhysicalSpace"/> for insertion. Persist via <c>IUnitOfWork.SaveChangesAsync</c>.</summary>
    /// <param name="space">The physical space to add.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task AddAsync(PhysicalSpace space, CancellationToken ct = default);
}
