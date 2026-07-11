using Ardalis.Specification;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Repositories;

/// <summary>
/// Persistence contract for the <see cref="SpaceLayer"/> aggregate root.
/// </summary>
/// <remarks>
/// All list/filter operations must be added as <c>ListAsync(ISpecification&lt;SpaceLayer&gt;, CancellationToken)</c>
/// overloads using Ardalis.Specification — never raw LINQ in application handlers.
/// </remarks>
public interface ISpaceLayerRepository
{
    /// <summary>Returns the <see cref="SpaceLayer"/> with the given <paramref name="id"/>, or <see langword="null"/> if not found.</summary>
    Task<SpaceLayer?> GetByIdAsync(SpaceLayerId id, CancellationToken ct = default);

    /// <summary>Stages a new <see cref="SpaceLayer"/> for insertion. Persist via <c>IUnitOfWork.SaveChangesAsync</c>.</summary>
    Task AddAsync(SpaceLayer layer, CancellationToken ct = default);

    /// <summary>Marks the <see cref="SpaceLayer"/> as modified so EF Core will persist changes on the next SaveChangesAsync call.</summary>
    Task UpdateAsync(SpaceLayer spaceLayer, CancellationToken ct = default);

    /// <summary>Returns all <see cref="SpaceLayer"/> instances matching the given specification.</summary>
    Task<IReadOnlyList<SpaceLayer>> ListAsync(ISpecification<SpaceLayer> specification, CancellationToken ct = default);

    /// <summary>Returns the total count of <see cref="SpaceLayer"/> instances matching the given specification.</summary>
    /// <param name="specification">The filter specification (must not apply Skip/Take).</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<int> CountAsync(ISpecification<SpaceLayer> specification, CancellationToken ct = default);
}
