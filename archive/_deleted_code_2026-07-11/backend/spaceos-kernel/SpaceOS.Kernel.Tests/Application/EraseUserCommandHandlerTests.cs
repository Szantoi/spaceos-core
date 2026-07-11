// SpaceOS.Kernel.Tests/Application/EraseUserCommandHandlerTests.cs

using Ardalis.Result;
using Moq;
using SpaceOS.Kernel.Application.UserProfiles.Commands;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.UserProfiles;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

public sealed class EraseUserCommandHandlerTests
{
    private static readonly Guid TestTenantId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private const string ExternalUserId = "auth0|user-to-erase";

    private readonly Mock<IUserProfileRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly EraseUserCommandHandler _handler;

    public EraseUserCommandHandlerTests() =>
        _handler = new EraseUserCommandHandler(_repository.Object, _unitOfWork.Object);

    // -------------------------------------------------------------------------
    // Handle_ExistingProfile_ErasesAndReturnsSuccess
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_ExistingProfile_ErasesAndReturnsSuccess()
    {
        // Arrange
        var profile = UserProfile.Create(ExternalUserId, TestTenantId);
        _repository
            .Setup(r => r.GetByExternalUserIdAsync(ExternalUserId, TestTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var command = new EraseUserCommand(ExternalUserId, TestTenantId);

        // Act
        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.True(profile.IsErased);
        Assert.Equal("[ERASED]", profile.ExternalUserId);
    }

    // -------------------------------------------------------------------------
    // Handle_ExistingProfile_CallsUpdateAndSave
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_ExistingProfile_CallsUpdateAndSave()
    {
        // Arrange
        var profile = UserProfile.Create(ExternalUserId, TestTenantId);
        _repository
            .Setup(r => r.GetByExternalUserIdAsync(ExternalUserId, TestTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var command = new EraseUserCommand(ExternalUserId, TestTenantId);

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _repository.Verify(r => r.UpdateAsync(profile, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // -------------------------------------------------------------------------
    // Handle_ProfileNotFound_ReturnsNotFound
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_ProfileNotFound_ReturnsNotFound()
    {
        // Arrange
        _repository
            .Setup(r => r.GetByExternalUserIdAsync(ExternalUserId, TestTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        var command = new EraseUserCommand(ExternalUserId, TestTenantId);

        // Act
        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
