// SpaceOS.Kernel.Tests/Application/HeartbeatCommandHandlerTests.cs
using Ardalis.Result;
using Moq;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.Nodes.Commands.Heartbeat;
using SpaceOS.Kernel.Domain.Federation;
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

/// <summary>Unit tests for <see cref="HeartbeatCommandHandler"/>.</summary>
public sealed class HeartbeatCommandHandlerTests
{
    private readonly Mock<INodeManifestRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcher = new();
    private readonly HeartbeatCommandHandler _handler;

    private static readonly Guid ValidTenantId = Guid.NewGuid();

    public HeartbeatCommandHandlerTests()
    {
        _handler = new HeartbeatCommandHandler(
            _repository.Object,
            _unitOfWork.Object,
            _dispatcher.Object);
    }

    // --- Success path ---

    [Fact]
    public async Task Handle_ExistingManifest_ReturnsSuccess()
    {
        // Arrange
        var manifest = NodeManifest.Create(TenantId.From(ValidTenantId), "https://node.example.com");
        _repository
            .Setup(r => r.GetByTenantIdAsync(It.IsAny<TenantId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(manifest);

        var command = new HeartbeatCommand(ValidTenantId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ExistingManifest_CallsUpdateAsync_Once()
    {
        // Arrange
        var manifest = NodeManifest.Create(TenantId.From(ValidTenantId), "https://node.example.com");
        _repository
            .Setup(r => r.GetByTenantIdAsync(It.IsAny<TenantId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(manifest);

        var command = new HeartbeatCommand(ValidTenantId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _repository.Verify(r => r.UpdateAsync(It.IsAny<NodeManifest>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingManifest_DispatchesDomainEvents_Once()
    {
        // Arrange
        var manifest = NodeManifest.Create(TenantId.From(ValidTenantId), "https://node.example.com");
        _repository
            .Setup(r => r.GetByTenantIdAsync(It.IsAny<TenantId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(manifest);

        var command = new HeartbeatCommand(ValidTenantId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _dispatcher.Verify(
            d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingManifest_UpdatesLastHeartbeatAt()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;
        var manifest = NodeManifest.Create(TenantId.From(ValidTenantId), "https://node.example.com");
        _repository
            .Setup(r => r.GetByTenantIdAsync(It.IsAny<TenantId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(manifest);

        var command = new HeartbeatCommand(ValidTenantId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(manifest.LastHeartbeatAt);
        Assert.True(manifest.LastHeartbeatAt >= before);
    }

    // --- Not found path ---

    [Fact]
    public async Task Handle_NoManifestForTenant_ReturnsNotFound()
    {
        // Arrange
        _repository
            .Setup(r => r.GetByTenantIdAsync(It.IsAny<TenantId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((NodeManifest?)null);

        var command = new HeartbeatCommand(ValidTenantId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_NoManifestForTenant_NeverCallsUpdateAsync()
    {
        // Arrange
        _repository
            .Setup(r => r.GetByTenantIdAsync(It.IsAny<TenantId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((NodeManifest?)null);

        var command = new HeartbeatCommand(ValidTenantId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _repository.Verify(r => r.UpdateAsync(It.IsAny<NodeManifest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NoManifestForTenant_NeverDispatchesDomainEvents()
    {
        // Arrange
        _repository
            .Setup(r => r.GetByTenantIdAsync(It.IsAny<TenantId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((NodeManifest?)null);

        var command = new HeartbeatCommand(ValidTenantId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _dispatcher.Verify(
            d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
