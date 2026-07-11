// Identity.Domain/Aggregates/SpaceOSUser.cs

using Identity.Domain.DomainEvents;
using Identity.Domain.ValueObjects;

namespace Identity.Domain.Aggregates;

public sealed class SpaceOSUser
{
    private readonly List<object> _domainEvents = new();

    public SpaceOSUserId Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Email Email { get; private set; }
    public DisplayName DisplayName { get; private set; }
    public UserStatus Status { get; private set; }
    public KcSyncStatus KcSyncStatus { get; private set; }
    public KeycloakUserId? KeycloakUserId { get; private set; }
    public OperatorPin? OperatorPin { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyList<object> DomainEvents => _domainEvents.AsReadOnly();

    // Required by EF Core for materialization (ComplexProperty cannot bind to ctor params)
#pragma warning disable CS8618
    private SpaceOSUser() { }
#pragma warning restore CS8618

    private SpaceOSUser(
        SpaceOSUserId id,
        Guid tenantId,
        Email email,
        DisplayName displayName,
        DateTimeOffset createdAt)
    {
        Id = id;
        TenantId = tenantId;
        Email = email;
        DisplayName = displayName;
        Status = UserStatus.Active;
        KcSyncStatus = KcSyncStatus.Pending;
        KeycloakUserId = null;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public static SpaceOSUser Create(
        Guid tenantId,
        Email email,
        DisplayName displayName,
        DateTimeOffset? now = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));

        var timestamp = now ?? DateTimeOffset.UtcNow;
        var user = new SpaceOSUser(SpaceOSUserId.New(), tenantId, email, displayName, timestamp);

        user._domainEvents.Add(new UserCreatedEvent(user.Id, tenantId, email, displayName, timestamp));

        return user;
    }

    public (bool Success, string? Error) Disable(DateTimeOffset? now = null)
    {
        if (Status == UserStatus.Disabled)
            return (false, "already_disabled");

        Status = UserStatus.Disabled;
        UpdatedAt = now ?? DateTimeOffset.UtcNow;

        _domainEvents.Add(new UserDisabledEvent(Id, TenantId, UpdatedAt));

        return (true, null);
    }

    public (bool Success, string? Error) Enable(DateTimeOffset? now = null)
    {
        if (Status == UserStatus.Active)
            return (false, "already_active");

        Status = UserStatus.Active;
        UpdatedAt = now ?? DateTimeOffset.UtcNow;

        _domainEvents.Add(new UserEnabledEvent(Id, TenantId, UpdatedAt));

        return (true, null);
    }

    public void UpdateProfile(DisplayName newDisplayName, DateTimeOffset? now = null)
    {
        DisplayName = newDisplayName;
        UpdatedAt = now ?? DateTimeOffset.UtcNow;

        _domainEvents.Add(new UserProfileUpdatedEvent(Id, TenantId, newDisplayName, UpdatedAt));
    }

    public void RequestPasswordReset(DateTimeOffset? now = null)
    {
        var timestamp = now ?? DateTimeOffset.UtcNow;
        _domainEvents.Add(new PasswordResetRequestedEvent(Id, TenantId, timestamp));
    }

    public void MarkKcSyncFailed(string reason, DateTimeOffset? now = null)
    {
        KcSyncStatus = KcSyncStatus.Failed;
        UpdatedAt = now ?? DateTimeOffset.UtcNow;

        _domainEvents.Add(new UserKcSyncFailedEvent(Id, TenantId, reason, UpdatedAt));
    }

    public void MarkKcSynced(KeycloakUserId keycloakUserId, DateTimeOffset? now = null)
    {
        KeycloakUserId = keycloakUserId;
        KcSyncStatus = KcSyncStatus.Synced;
        UpdatedAt = now ?? DateTimeOffset.UtcNow;
    }

    public (bool Success, string? Error) SetOperatorPin(OperatorPin? pin, DateTimeOffset? now = null)
    {
        // Factory operators can have a 4-digit PIN for quick shopfloor authentication
        OperatorPin = pin;
        UpdatedAt = now ?? DateTimeOffset.UtcNow;

        return (true, null);
    }

    public (bool Success, string? Error) ClearOperatorPin(DateTimeOffset? now = null)
    {
        if (OperatorPin == null)
            return (false, "no_pin_set");

        OperatorPin = null;
        UpdatedAt = now ?? DateTimeOffset.UtcNow;

        return (true, null);
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}
