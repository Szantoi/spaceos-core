// SpaceOS.Kernel.Application/UserProfiles/IPseudonymizer.cs

namespace SpaceOS.Kernel.Application.UserProfiles;

/// <summary>
/// Resolves or creates a stable pseudonym GUID for a given external user identity.
/// Used by <see cref="SpaceOS.Kernel.Application.AuditLog.AuditEventDispatcher"/> to store
/// a GDPR-erasable reference in the audit log instead of the raw JWT <c>sub</c> claim.
/// </summary>
public interface IPseudonymizer
{
    /// <summary>
    /// Returns the pseudonym GUID for the given external user identifier within the specified tenant.
    /// If no <see cref="SpaceOS.Kernel.Domain.UserProfiles.UserProfile"/> exists yet, one is created and persisted.
    /// </summary>
    /// <param name="externalUserId">The JWT <c>sub</c> claim of the authenticated user.</param>
    /// <param name="tenantId">The tenant the user belongs to.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The stable pseudonym <see cref="Guid"/> to store in the audit log.</returns>
    Task<Guid> GetOrCreatePseudonymAsync(string externalUserId, Guid tenantId, CancellationToken ct = default);
}
