// SpaceOS.Kernel.Domain/UserProfiles/IUserProfileRepository.cs

namespace SpaceOS.Kernel.Domain.UserProfiles;

/// <summary>
/// Repository interface for <see cref="UserProfile"/> aggregates.
/// Implementations live in the Infrastructure layer.
/// </summary>
public interface IUserProfileRepository
{
    /// <summary>
    /// Returns the <see cref="UserProfile"/> that matches the given external user identifier
    /// within the specified tenant, or <c>null</c> if no such profile exists.
    /// </summary>
    /// <param name="externalUserId">The JWT <c>sub</c> claim to look up.</param>
    /// <param name="tenantId">The owning tenant.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<UserProfile?> GetByExternalUserIdAsync(string externalUserId, Guid tenantId, CancellationToken ct = default);

    /// <summary>
    /// Returns the <see cref="UserProfile"/> with the given pseudonym GUID, or <c>null</c> if not found.
    /// </summary>
    /// <param name="id">The pseudonym GUID to look up.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<UserProfile?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Adds a new <see cref="UserProfile"/> to the persistence store.
    /// The caller must invoke <see cref="SpaceOS.Kernel.Domain.Repositories.IUnitOfWork.SaveChangesAsync"/> to commit.
    /// </summary>
    /// <param name="profile">The profile to add.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task AddAsync(UserProfile profile, CancellationToken ct = default);

    /// <summary>
    /// Marks the given <see cref="UserProfile"/> as modified so EF Core will persist changes on the next save.
    /// The caller must invoke <see cref="SpaceOS.Kernel.Domain.Repositories.IUnitOfWork.SaveChangesAsync"/> to commit.
    /// </summary>
    /// <param name="profile">The profile to update.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task UpdateAsync(UserProfile profile, CancellationToken ct = default);
}
