// SpaceOS.Kernel.Tests/Application/RevokeTokenCommandHandlerTests.cs

using Ardalis.Result;
using Moq;
using SpaceOS.Kernel.Application.Auth;
using SpaceOS.Kernel.Application.Auth.Commands;
using SpaceOS.Kernel.Domain.Auth;
using SpaceOS.Kernel.Domain.Repositories;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

/// <summary>Unit tests for <see cref="RevokeTokenCommandHandler"/>.</summary>
public sealed class RevokeTokenCommandHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _rtRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly RevokeTokenCommandHandler _handler;

    public RevokeTokenCommandHandlerTests()
    {
        _handler = new RevokeTokenCommandHandler(_rtRepo.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_ActiveToken_RevokeAndReturnsSuccess()
    {
        // Arrange
        var rawToken = RefreshTokenHasher.GenerateOpaqueToken();
        var hash     = RefreshTokenHasher.HashToken(rawToken);
        var stored   = RefreshToken.Create(Guid.NewGuid(), hash, DateTimeOffset.UtcNow.AddHours(8));

        _rtRepo.Setup(r => r.GetByHashAsync(hash, It.IsAny<CancellationToken>()))
               .ReturnsAsync(stored);

        // Act
        var result = await _handler.Handle(new RevokeTokenCommand(rawToken), CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.True(stored.IsRevoked);
    }

    [Fact]
    public async Task Handle_ActiveToken_CommitsUnitOfWork()
    {
        // Arrange
        var rawToken = RefreshTokenHasher.GenerateOpaqueToken();
        var hash     = RefreshTokenHasher.HashToken(rawToken);
        var stored   = RefreshToken.Create(Guid.NewGuid(), hash, DateTimeOffset.UtcNow.AddHours(8));

        _rtRepo.Setup(r => r.GetByHashAsync(hash, It.IsAny<CancellationToken>()))
               .ReturnsAsync(stored);

        // Act
        await _handler.Handle(new RevokeTokenCommand(rawToken), CancellationToken.None);

        // Assert
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentToken_ReturnsSuccessWithoutError()
    {
        // Arrange — idempotent: non-existent token must still return 200 OK (BE-P15-11)
        var rawToken = RefreshTokenHasher.GenerateOpaqueToken();

        _rtRepo.Setup(r => r.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((RefreshToken?)null);

        // Act
        var result = await _handler.Handle(new RevokeTokenCommand(rawToken), CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
    }

    [Fact]
    public async Task Handle_AlreadyRevokedToken_ReturnsSuccessWithoutError()
    {
        // Arrange — idempotent: already-revoked token must still return 200 OK (BE-P15-11)
        var rawToken = RefreshTokenHasher.GenerateOpaqueToken();
        var hash     = RefreshTokenHasher.HashToken(rawToken);
        var stored   = RefreshToken.Create(Guid.NewGuid(), hash, DateTimeOffset.UtcNow.AddHours(8));
        stored.Revoke(); // pre-revoke

        _rtRepo.Setup(r => r.GetByHashAsync(hash, It.IsAny<CancellationToken>()))
               .ReturnsAsync(stored);

        // Act
        var result = await _handler.Handle(new RevokeTokenCommand(rawToken), CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
    }

    [Fact]
    public async Task Handle_NonExistentToken_CommitsUnitOfWork()
    {
        // Arrange
        var rawToken = RefreshTokenHasher.GenerateOpaqueToken();

        _rtRepo.Setup(r => r.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((RefreshToken?)null);

        // Act
        await _handler.Handle(new RevokeTokenCommand(rawToken), CancellationToken.None);

        // Assert — UoW is always committed (idempotent operation)
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
