using Ardalis.Specification;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Repositories;

/// <summary>
/// Repository for managing Tenant aggregates.
/// </summary>
public interface ITenantRepository
{
    /// <summary>
    /// Retrieves a Tenant by its unique identifier.
    /// </summary>
    Task<Tenant?> GetByIdAsync(TenantId id, CancellationToken ct = default);

    /// <summary>Returns all <see cref="Tenant"/> instances matching the given specification.</summary>
    Task<IReadOnlyList<Tenant>> ListAsync(ISpecification<Tenant> specification, CancellationToken ct = default);

    /// <summary>Stages a new <see cref="Tenant"/> for insertion.</summary>
    Task AddAsync(Tenant tenant, CancellationToken ct = default);

    /// <summary>Stages an updated <see cref="Tenant"/> for persistence. Persist via <c>IUnitOfWork.SaveChangesAsync</c>.</summary>
    /// <param name="tenant">The tenant to update.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task UpdateAsync(Tenant tenant, CancellationToken ct = default);

    /// <summary>Returns the total count of <see cref="Tenant"/> instances matching the given specification.</summary>
    /// <param name="specification">The filter specification (must not apply Skip/Take).</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<int> CountAsync(ISpecification<Tenant> specification, CancellationToken ct = default);

    /// <summary>
    /// Returns the first non-archived <see cref="Tenant"/> whose <c>EmailHash</c> matches
    /// <paramref name="emailHash"/>, or <c>null</c> if no match is found.
    /// </summary>
    /// <param name="emailHash">The SHA-256 hex-encoded email hash to look up.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<Tenant?> GetByEmailHashAsync(string emailHash, CancellationToken ct = default);
}
