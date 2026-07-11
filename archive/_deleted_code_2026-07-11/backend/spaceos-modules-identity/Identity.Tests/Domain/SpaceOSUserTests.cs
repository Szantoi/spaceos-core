// Identity.Tests/Domain/SpaceOSUserTests.cs

using Identity.Domain.Aggregates;
using Identity.Domain.DomainEvents;
using Identity.Domain.ValueObjects;
using Xunit;

namespace Identity.Tests.Domain;

public sealed class SpaceOSUserTests
{
    private static readonly Guid DefaultTenantId = Guid.NewGuid();
    private static readonly Email DefaultEmail = Email.From("test@example.com");
    private static readonly DisplayName DefaultName = DisplayName.From("Kovács", "János");

    private static SpaceOSUser CreateUser() =>
        SpaceOSUser.Create(DefaultTenantId, DefaultEmail, DefaultName);

    [Fact]
    public void Create_SetsKcSyncStatusToPending()
    {
        var user = CreateUser();

        Assert.Equal(KcSyncStatus.Pending, user.KcSyncStatus);
    }

    [Fact]
    public void Create_SetsKeycloakUserIdToNull()
    {
        var user = CreateUser();

        Assert.Null(user.KeycloakUserId);
    }

    [Fact]
    public void Create_RaisesUserCreatedEvent()
    {
        var user = CreateUser();

        var evt = Assert.Single(user.DomainEvents.OfType<UserCreatedEvent>());
        Assert.Equal(user.Id, evt.UserId);
        Assert.Equal(DefaultTenantId, evt.TenantId);
    }

    [Fact]
    public void Create_SetsStatusToActive()
    {
        var user = CreateUser();

        Assert.Equal(UserStatus.Active, user.Status);
    }

    [Fact]
    public void Disable_WhenActive_Succeeds()
    {
        var user = CreateUser();

        var (success, error) = user.Disable();

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal(UserStatus.Disabled, user.Status);
    }

    [Fact]
    public void Disable_WhenActive_RaisesUserDisabledEvent()
    {
        var user = CreateUser();
        user.ClearDomainEvents();

        user.Disable();

        Assert.Single(user.DomainEvents.OfType<UserDisabledEvent>());
    }

    [Fact]
    public void Disable_WhenAlreadyDisabled_ReturnsError()
    {
        var user = CreateUser();
        user.Disable();
        user.ClearDomainEvents();

        var (success, error) = user.Disable();

        Assert.False(success);
        Assert.Equal("already_disabled", error);
        Assert.Empty(user.DomainEvents);
    }

    [Fact]
    public void Enable_WhenDisabled_Succeeds()
    {
        var user = CreateUser();
        user.Disable();
        user.ClearDomainEvents();

        var (success, error) = user.Enable();

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal(UserStatus.Active, user.Status);
    }

    [Fact]
    public void Enable_WhenDisabled_RaisesUserEnabledEvent()
    {
        var user = CreateUser();
        user.Disable();
        user.ClearDomainEvents();

        user.Enable();

        Assert.Single(user.DomainEvents.OfType<UserEnabledEvent>());
    }

    [Fact]
    public void Enable_WhenAlreadyActive_ReturnsError()
    {
        var user = CreateUser();
        user.ClearDomainEvents();

        var (success, error) = user.Enable();

        Assert.False(success);
        Assert.Equal("already_active", error);
        Assert.Empty(user.DomainEvents);
    }

    [Fact]
    public void Email_Normalizes_ToLowercase()
    {
        var email = Email.From("Test.User@EXAMPLE.COM");

        Assert.Equal("test.user@example.com", email.Value);
    }

    [Fact]
    public void Email_InvalidFormat_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Email.From("not-an-email"));
        Assert.Throws<ArgumentException>(() => Email.From("@nodomain.com"));
        Assert.Throws<ArgumentException>(() => Email.From("noatsign.com"));
    }

    [Fact]
    public void Email_EmptyOrWhitespace_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Email.From(""));
        Assert.Throws<ArgumentException>(() => Email.From("   "));
    }

    [Fact]
    public void DisplayName_ExceedsMaxLength_ThrowsArgumentException()
    {
        var tooLong = new string('a', DisplayName.MaxLength + 1);

        Assert.Throws<ArgumentException>(() => DisplayName.From(tooLong, "Valid"));
        Assert.Throws<ArgumentException>(() => DisplayName.From("Valid", tooLong));
    }

    [Fact]
    public void DisplayName_Empty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => DisplayName.From("", "Last"));
        Assert.Throws<ArgumentException>(() => DisplayName.From("First", ""));
    }

    [Fact]
    public void SpaceOSUserId_Empty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => SpaceOSUserId.From(Guid.Empty));
    }

    [Fact]
    public void SpaceOSUserId_New_GeneratesUniqueIds()
    {
        var id1 = SpaceOSUserId.New();
        var id2 = SpaceOSUserId.New();

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void KeycloakUserId_EmptyString_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => KeycloakUserId.From(""));
        Assert.Throws<ArgumentException>(() => KeycloakUserId.From("   "));
    }

    [Fact]
    public void UpdateProfile_ChangesDisplayNameAndRaisesEvent()
    {
        var user = CreateUser();
        user.ClearDomainEvents();
        var newName = DisplayName.From("Nagy", "Péter");

        user.UpdateProfile(newName);

        Assert.Equal(newName, user.DisplayName);
        Assert.Single(user.DomainEvents.OfType<UserProfileUpdatedEvent>());
    }

    [Fact]
    public void MarkKcSyncFailed_SetsSyncStatusAndRaisesEvent()
    {
        var user = CreateUser();
        user.ClearDomainEvents();

        user.MarkKcSyncFailed("KC connection timeout");

        Assert.Equal(KcSyncStatus.Failed, user.KcSyncStatus);
        var evt = Assert.Single(user.DomainEvents.OfType<UserKcSyncFailedEvent>());
        Assert.Equal("KC connection timeout", evt.Reason);
    }

    [Fact]
    public void MarkKcSynced_SetsKcUserIdAndSyncStatus()
    {
        var user = CreateUser();
        var kcId = KeycloakUserId.From("kc-user-123");

        user.MarkKcSynced(kcId);

        Assert.Equal(kcId, user.KeycloakUserId);
        Assert.Equal(KcSyncStatus.Synced, user.KcSyncStatus);
    }
}
