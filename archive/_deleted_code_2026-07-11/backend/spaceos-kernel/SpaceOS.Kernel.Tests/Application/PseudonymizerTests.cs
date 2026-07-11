// SpaceOS.Kernel.Tests/Application/PseudonymizerTests.cs

using Moq;
using SpaceOS.Kernel.Application.UserProfiles;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.UserProfiles;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

public sealed class PseudonymizerTests
{
    private static readonly Guid TestTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const string ExternalUserId = "auth0|user-abc";

    private readonly Mock<IUserProfileRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Pseudonymizer _pseudonymizer;

    public PseudonymizerTests() =>
        _pseudonymizer = new Pseudonymizer(_repository.Object, _unitOfWork.Object);

    // -------------------------------------------------------------------------
    // GetOrCreatePseudonymAsync_ExistingProfile_ReturnsCachedId
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetOrCreatePseudonymAsync_ExistingProfile_ReturnsCachedId()
    {
        // Arrange
        var existingProfile = UserProfile.Create(ExternalUserId, TestTenantId);
        _repository
            .Setup(r => r.GetByExternalUserIdAsync(ExternalUserId, TestTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);

        // Act
        var result = await _pseudonymizer.GetOrCreatePseudonymAsync(
            ExternalUserId, TestTenantId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(existingProfile.Id, result);
        _repository.Verify(r => r.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // -------------------------------------------------------------------------
    // GetOrCreatePseudonymAsync_NewProfile_CreatesAndPersists
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetOrCreatePseudonymAsync_NewProfile_CreatesAndPersists()
    {
        // Arrange
        _repository
            .Setup(r => r.GetByExternalUserIdAsync(ExternalUserId, TestTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        // Act
        var result = await _pseudonymizer.GetOrCreatePseudonymAsync(
            ExternalUserId, TestTenantId, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotEqual(Guid.Empty, result);
        _repository.Verify(r => r.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // -------------------------------------------------------------------------
    // GetOrCreatePseudonymAsync_EmptyExternalUserId_ThrowsArgumentException
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetOrCreatePseudonymAsync_EmptyExternalUserId_ThrowsArgumentException(string emptyId)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _pseudonymizer.GetOrCreatePseudonymAsync(emptyId, TestTenantId, TestContext.Current.CancellationToken));
    }
}
