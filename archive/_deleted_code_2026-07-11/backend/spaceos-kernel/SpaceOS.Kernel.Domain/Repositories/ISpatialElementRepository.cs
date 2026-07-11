using System;
using System.Threading;
using System.Threading.Tasks;
using SpaceOS.Kernel.Domain.Entities;

namespace SpaceOS.Kernel.Domain.Repositories;

/// <summary>
/// Persistence contract for <see cref="SpatialElement"/> entities.
/// </summary>
public interface ISpatialElementRepository
{
    /// <summary>Returns the <see cref="SpatialElement"/> with the given <paramref name="id"/>, or <see langword="null"/> if not found.</summary>
    /// <param name="id">The spatial element identifier to look up.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<SpatialElement?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Stages a new <see cref="SpatialElement"/> for insertion. Persist via <c>IUnitOfWork.SaveChangesAsync</c>.</summary>
    /// <param name="element">The spatial element to add.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task AddAsync(SpatialElement element, CancellationToken ct = default);
}
