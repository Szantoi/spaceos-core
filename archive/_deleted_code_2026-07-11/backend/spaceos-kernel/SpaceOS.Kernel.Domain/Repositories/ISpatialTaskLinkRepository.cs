using System.Threading;
using System.Threading.Tasks;
using SpaceOS.Kernel.Domain.Entities;

namespace SpaceOS.Kernel.Domain.Repositories;

/// <summary>
/// Persistence contract for <see cref="SpatialTaskLink"/> entities.
/// </summary>
public interface ISpatialTaskLinkRepository
{
    /// <summary>Stages a new <see cref="SpatialTaskLink"/> for insertion. Persist via <c>IUnitOfWork.SaveChangesAsync</c>.</summary>
    /// <param name="link">The spatial task link to add.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task AddAsync(SpatialTaskLink link, CancellationToken ct = default);
}
