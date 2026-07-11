// SpaceOS.Kernel.Domain/Federation/INodeManifestRepository.cs
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Federation;

/// <summary>
/// Persistence contract for the <see cref="NodeManifest"/> aggregate root.
/// </summary>
public interface INodeManifestRepository
{
    /// <summary>
    /// Returns the <see cref="NodeManifest"/> for the specified tenant, or <see langword="null"/>
    /// if no manifest has been registered.
    /// </summary>
    /// <param name="tenantId">The tenant whose manifest to retrieve.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<NodeManifest?> GetByTenantIdAsync(TenantId tenantId, CancellationToken ct = default);

    /// <summary>
    /// Stages a new <see cref="NodeManifest"/> for insertion.
    /// Changes are committed via <c>IUnitOfWork.SaveChangesAsync</c>.
    /// </summary>
    /// <param name="manifest">The manifest to add.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task AddAsync(NodeManifest manifest, CancellationToken ct = default);

    /// <summary>
    /// Marks an existing <see cref="NodeManifest"/> as modified.
    /// Changes are committed via <c>IUnitOfWork.SaveChangesAsync</c>.
    /// </summary>
    /// <param name="manifest">The manifest to update.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task UpdateAsync(NodeManifest manifest, CancellationToken ct = default);
}
