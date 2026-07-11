// SpaceOS.Kernel.Domain/UserProfiles/UserProfile.cs

namespace SpaceOS.Kernel.Domain.UserProfiles;

/// <summary>
/// Maps a real external user identity (JWT <c>sub</c> claim) to a stable pseudonym GUID.
/// The audit log stores only the pseudonym, satisfying GDPR right-to-erasure:
/// deleting or erasing this record severs the link to real identity without destroying
/// the audit chain integrity.
/// </summary>
public sealed class UserProfile
{
    /// <summary>Gets the pseudonym GUID stored in the audit log in place of the real user identity.</summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the original JWT <c>sub</c> claim value, or <c>"[ERASED]"</c> after GDPR erasure.
    /// </summary>
    public string ExternalUserId { get; private set; } = string.Empty;

    /// <summary>Gets the identifier of the tenant this profile belongs to.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Gets the UTC timestamp at which this profile was created.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the external user identifier has been erased
    /// in response to a GDPR erasure request.
    /// </summary>
    public bool IsErased { get; private set; }

    /// <summary>Parameterless constructor reserved for EF Core materialisation.</summary>
    private UserProfile() { }

    private UserProfile(Guid id, string externalUserId, Guid tenantId, DateTimeOffset createdAt)
    {
        Id             = id;
        ExternalUserId = externalUserId;
        TenantId       = tenantId;
        CreatedAt      = createdAt;
        IsErased       = false;
    }

    /// <summary>
    /// Creates a new <see cref="UserProfile"/> mapping the given external user identity to a fresh pseudonym GUID.
    /// </summary>
    /// <param name="externalUserId">The JWT <c>sub</c> claim to pseudonymize. Must not be empty.</param>
    /// <param name="tenantId">The tenant that owns this user profile.</param>
    /// <returns>A new, unpersisted <see cref="UserProfile"/>.</returns>
    public static UserProfile Create(string externalUserId, Guid tenantId)
    {
        if (string.IsNullOrWhiteSpace(externalUserId))
            throw new ArgumentException("External user ID cannot be empty.", nameof(externalUserId));

        return new UserProfile(
            id:             Guid.NewGuid(),
            externalUserId: externalUserId,
            tenantId:       tenantId,
            createdAt:      DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Erases PII by replacing the external user identifier with the sentinel value <c>"[ERASED]"</c>.
    /// The pseudonym GUID (<see cref="Id"/>) is preserved so existing audit log references remain valid.
    /// Calling this method on an already-erased profile is a no-op.
    /// </summary>
    public void Erase()
    {
        if (IsErased)
            return;

        ExternalUserId = "[ERASED]";
        IsErased       = true;
    }
}
