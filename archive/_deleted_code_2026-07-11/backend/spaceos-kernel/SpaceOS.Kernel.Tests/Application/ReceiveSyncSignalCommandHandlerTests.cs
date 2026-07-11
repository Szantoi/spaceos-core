// SpaceOS.Kernel.Tests/Application/ReceiveSyncSignalCommandHandlerTests.cs
using Ardalis.Result;
using Moq;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.Sync;
using SpaceOS.Kernel.Application.Sync.Commands.ReceiveSignal;
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.Sync;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

/// <summary>Unit tests for <see cref="ReceiveSyncSignalCommandHandler"/>.</summary>
public sealed class ReceiveSyncSignalCommandHandlerTests
{
    private readonly Mock<ISyncSignalRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ISyncSignalWriteLock> _writeLock = new();
    private readonly Mock<ITransactionManager> _transactionManager = new();
    private readonly Mock<ISyncSignalHasher> _hasher = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcher = new();
    private readonly ReceiveSyncSignalCommandHandler _handler;

    private static readonly Guid ValidTenantId = Guid.NewGuid();
    private static readonly Guid ValidEpicId = Guid.NewGuid();
    private const string GenesisHash = SyncConstants.GenesisHash;
    private const string ComputedHash = "abc123def456";

    public ReceiveSyncSignalCommandHandlerTests()
    {
        // Write lock returns a no-op disposable by default
        var lockHandle = new Mock<IAsyncDisposable>();
        lockHandle.Setup(h => h.DisposeAsync()).Returns(ValueTask.CompletedTask);
        _writeLock
            .Setup(l => l.AcquireAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lockHandle.Object);

        // Transaction returns a no-op disposable by default
        var txHandle = new Mock<IAsyncDisposable>();
        txHandle.Setup(h => h.DisposeAsync()).Returns(ValueTask.CompletedTask);
        _transactionManager
            .Setup(t => t.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(txHandle.Object);

        _handler = new ReceiveSyncSignalCommandHandler(
            _repository.Object,
            _unitOfWork.Object,
            _writeLock.Object,
            _transactionManager.Object,
            _hasher.Object,
            _dispatcher.Object);
    }

    private ReceiveSyncSignalCommand BuildCommand(Guid? clientSignalId = null) =>
        new ReceiveSyncSignalCommand(
            ValidTenantId,
            ValidEpicId,
            "InProgress",
            clientSignalId ?? Guid.NewGuid(),
            "{\"action\":\"start\"}");

    // --- Success path ---

    [Fact]
    public async Task Handle_NewSignal_ReturnsSuccess()
    {
        // Arrange
        _repository
            .Setup(r => r.GetByClientSignalIdAsync(It.IsAny<TenantId>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SyncSignal?)null);
        _repository
            .Setup(r => r.GetLastHashAsync(It.IsAny<TenantId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenesisHash);
        _hasher
            .Setup(h => h.ComputeHash(GenesisHash, It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .Returns(ComputedHash);

        var command = BuildCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_NewSignal_CallsAddAsyncAndCommit_Once()
    {
        // Arrange
        _repository
            .Setup(r => r.GetByClientSignalIdAsync(It.IsAny<TenantId>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SyncSignal?)null);
        _repository
            .Setup(r => r.GetLastHashAsync(It.IsAny<TenantId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenesisHash);
        _hasher
            .Setup(h => h.ComputeHash(GenesisHash, It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .Returns(ComputedHash);

        var command = BuildCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _repository.Verify(r => r.AddAsync(It.IsAny<SyncSignal>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transactionManager.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NewSignal_DispatchesDomainEvents_Once()
    {
        // Arrange
        _repository
            .Setup(r => r.GetByClientSignalIdAsync(It.IsAny<TenantId>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SyncSignal?)null);
        _repository
            .Setup(r => r.GetLastHashAsync(It.IsAny<TenantId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenesisHash);
        _hasher
            .Setup(h => h.ComputeHash(GenesisHash, It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .Returns(ComputedHash);

        var command = BuildCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _dispatcher.Verify(
            d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // --- Idempotency path ---

    [Fact]
    public async Task Handle_DuplicateClientSignalId_ReturnsSuccessWithoutReinserting()
    {
        // Arrange — signal with this clientSignalId already exists
        var clientSignalId = Guid.NewGuid();
        var existingSignal = SyncSignal.Create(
            FlowEpicId.From(ValidEpicId),
            TenantId.From(ValidTenantId),
            "InProgress",
            ComputedHash,
            GenesisHash,
            clientSignalId);

        _repository
            .Setup(r => r.GetByClientSignalIdAsync(It.IsAny<TenantId>(), clientSignalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSignal);

        var command = BuildCommand(clientSignalId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _repository.Verify(r => r.AddAsync(It.IsAny<SyncSignal>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DuplicateClientSignalId_NeverAcquiresWriteLock()
    {
        // Arrange — early-exit before the lock is acquired
        var clientSignalId = Guid.NewGuid();
        var existingSignal = SyncSignal.Create(
            FlowEpicId.From(ValidEpicId),
            TenantId.From(ValidTenantId),
            "InProgress",
            ComputedHash,
            GenesisHash,
            clientSignalId);

        _repository
            .Setup(r => r.GetByClientSignalIdAsync(It.IsAny<TenantId>(), clientSignalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSignal);

        var command = BuildCommand(clientSignalId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _writeLock.Verify(l => l.AcquireAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DuplicateClientSignalId_NeverDispatchesDomainEvents()
    {
        // Arrange
        var clientSignalId = Guid.NewGuid();
        var existingSignal = SyncSignal.Create(
            FlowEpicId.From(ValidEpicId),
            TenantId.From(ValidTenantId),
            "InProgress",
            ComputedHash,
            GenesisHash,
            clientSignalId);

        _repository
            .Setup(r => r.GetByClientSignalIdAsync(It.IsAny<TenantId>(), clientSignalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSignal);

        var command = BuildCommand(clientSignalId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _dispatcher.Verify(
            d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // --- Hash chain continuity ---

    [Fact]
    public async Task Handle_NewSignal_PassesPreviousHashToHasher()
    {
        // Arrange
        const string previousHash = "deadbeef1234";

        _repository
            .Setup(r => r.GetByClientSignalIdAsync(It.IsAny<TenantId>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SyncSignal?)null);
        _repository
            .Setup(r => r.GetLastHashAsync(It.IsAny<TenantId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(previousHash);
        _hasher
            .Setup(h => h.ComputeHash(previousHash, It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .Returns(ComputedHash)
            .Verifiable();

        var command = BuildCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert — hasher received the tail hash from the repository
        _hasher.Verify(h => h.ComputeHash(previousHash, It.IsAny<string>(), It.IsAny<DateTimeOffset>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NewSignal_AcquiresWriteLock_Once()
    {
        // Arrange
        _repository
            .Setup(r => r.GetByClientSignalIdAsync(It.IsAny<TenantId>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SyncSignal?)null);
        _repository
            .Setup(r => r.GetLastHashAsync(It.IsAny<TenantId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenesisHash);
        _hasher
            .Setup(h => h.ComputeHash(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .Returns(ComputedHash);

        var command = BuildCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _writeLock.Verify(l => l.AcquireAsync(ValidTenantId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
