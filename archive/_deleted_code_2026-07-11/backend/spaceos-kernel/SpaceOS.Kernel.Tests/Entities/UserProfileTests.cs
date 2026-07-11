// SpaceOS.Kernel.Tests/Entities/UserProfileTests.cs

using SpaceOS.Kernel.Domain.UserProfiles;
using Xunit;

namespace SpaceOS.Kernel.Tests.Entities;

public sealed class UserProfileTests
{
    private static readonly Guid TestTenantId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private const string ExternalUserId = "auth0|test-user";

    // -------------------------------------------------------------------------
    // Create_ValidArguments_SetsPropertiesCorrectly
    // -------------------------------------------------------------------------

    [Fact]
    public void Create_ValidArguments_SetsPropertiesCorrectly()
    {
        var profile = UserProfile.Create(ExternalUserId, TestTenantId);

        Assert.NotEqual(Guid.Empty, profile.Id);
        Assert.Equal(ExternalUserId, profile.ExternalUserId);
        Assert.Equal(TestTenantId, profile.TenantId);
        Assert.False(profile.IsErased);
    }

    // -------------------------------------------------------------------------
    // Create_EmptyExternalUserId_ThrowsArgumentException
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyExternalUserId_ThrowsArgumentException(string empty)
    {
        Assert.Throws<ArgumentException>(() => UserProfile.Create(empty, TestTenantId));
    }

    // -------------------------------------------------------------------------
    // Erase_SetsIsErasedAndRedactsExternalUserId
    // -------------------------------------------------------------------------

    [Fact]
    public void Erase_SetsIsErasedAndRedactsExternalUserId()
    {
        var profile = UserProfile.Create(ExternalUserId, TestTenantId);

        profile.Erase();

        Assert.True(profile.IsErased);
        Assert.Equal("[ERASED]", profile.ExternalUserId);
    }

    // -------------------------------------------------------------------------
    // Erase_PreservesPseudonymId
    // -------------------------------------------------------------------------

    [Fact]
    public void Erase_PreservesPseudonymId()
    {
        var profile = UserProfile.Create(ExternalUserId, TestTenantId);
        var idBefore = profile.Id;

        profile.Erase();

        Assert.Equal(idBefore, profile.Id);
    }

    // -------------------------------------------------------------------------
    // Erase_CalledTwice_IsIdempotent
    // -------------------------------------------------------------------------

    [Fact]
    public void Erase_CalledTwice_IsIdempotent()
    {
        var profile = UserProfile.Create(ExternalUserId, TestTenantId);

        profile.Erase();
        profile.Erase(); // second call must not throw

        Assert.True(profile.IsErased);
        Assert.Equal("[ERASED]", profile.ExternalUserId);
    }
}
