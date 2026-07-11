// SpaceOS.Kernel.Tests/Application/CloseFlowEpicCommandHandlerTests.cs

using Ardalis.Result;
using Moq;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.FlowEpics.Commands.CloseFlowEpic;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.Outbox;
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.Snapshots;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

/// <summary>Unit tests for <see cref="CloseFlowEpicCommandHandler"/>.</summary>
public class CloseFlowEpicCommandHandlerTests
{
    private const string ValidProofUrl  = "https://storage.example.com/proof/doc.pdf";
    private const string ValidProofHash = "a3f1b2c4d5e6f7a8b9c0d1e2f3a4b5c6d7e8f9a0b1c2d3e4f5e6f7a8b9c0d1e2";

    private readonly Mock<IFlowEpicRepository>          _flowEpicRepoMock   = new();
    private readonly Mock<IAggregateSnapshotRepository> _snapshotRepoMock   = new();
    private readonly Mock<IOutboxRepository>            _outboxRepoMock     = new();
    private readonly Mock<IUnitOfWork>                  _unitOfWorkMock     = new();
    private readonly Mock<IDomainEventDispatcher>       _dispatcherMock     = new();
    private readonly CloseFlowEpicCommandHandler        _handler;

    public CloseFlowEpicCommandHandlerTests()
    {
        _handler = new CloseFlowEpicCommandHandler(
            _flowEpicRepoMock.Object,
            _snapshotRepoMock.Object,
            _outboxRepoMock.Object,
            _unitOfWorkMock.Object,
            _dispatcherMock.Object);
    }

    private static FlowEpic CreateDeliveryEpic()
    {
        var epic = FlowEpic.Create("Test Epic", FacilityId.New(), TenantId.New());
        epic.StartExecution();
        return epic;
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_EpicInDeliveryPhase_ReturnsSuccess()
    {
        // Arrange
        var epic = CreateDeliveryEpic();
        var command = new CloseFlowEpicCommand(epic.Id.Value, ValidProofUrl, ValidProofHash);

        _flowEpicRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<FlowEpicId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(epic);
        _snapshotRepoMock
            .Setup(r => r.GetLatestAsync(epic.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AggregateSnapshot?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_EpicInDeliveryPhase_TransitionsToClosedDone()
    {
        // Arrange
        var epic = CreateDeliveryEpic();
        var command = new CloseFlowEpicCommand(epic.Id.Value, ValidProofUrl, ValidProofHash);

        _flowEpicRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<FlowEpicId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(epic);
        _snapshotRepoMock
            .Setup(r => r.GetLatestAsync(epic.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AggregateSnapshot?)null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(WorkflowPhase.ClosedDone, epic.Phase);
    }

    [Fact]
    public async Task Handle_EpicInDeliveryPhase_CallsSnapshotRepositoryAddAsync_Once()
    {
        // Arrange
        var epic = CreateDeliveryEpic();
        var command = new CloseFlowEpicCommand(epic.Id.Value, ValidProofUrl, ValidProofHash);

        _flowEpicRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<FlowEpicId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(epic);
        _snapshotRepoMock
            .Setup(r => r.GetLatestAsync(epic.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AggregateSnapshot?)null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _snapshotRepoMock.Verify(
            r => r.AddAsync(It.IsAny<AggregateSnapshot>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_EpicInDeliveryPhase_CallsOutboxRepositoryAddAsync_Once()
    {
        // Arrange
        var epic = CreateDeliveryEpic();
        var command = new CloseFlowEpicCommand(epic.Id.Value, ValidProofUrl, ValidProofHash);

        _flowEpicRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<FlowEpicId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(epic);
        _snapshotRepoMock
            .Setup(r => r.GetLatestAsync(epic.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AggregateSnapshot?)null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _outboxRepoMock.Verify(
            r => r.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_EpicInDeliveryPhase_CallsSaveChangesAsync_Once()
    {
        // Arrange
        var epic = CreateDeliveryEpic();
        var command = new CloseFlowEpicCommand(epic.Id.Value, ValidProofUrl, ValidProofHash);

        _flowEpicRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<FlowEpicId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(epic);
        _snapshotRepoMock
            .Setup(r => r.GetLatestAsync(epic.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AggregateSnapshot?)null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_EpicInDeliveryPhase_DispatchesDomainEvents_Once()
    {
        // Arrange
        var epic = CreateDeliveryEpic();
        var command = new CloseFlowEpicCommand(epic.Id.Value, ValidProofUrl, ValidProofHash);

        _flowEpicRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<FlowEpicId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(epic);
        _snapshotRepoMock
            .Setup(r => r.GetLatestAsync(epic.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AggregateSnapshot?)null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _dispatcherMock.Verify(
            d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_EpicInDeliveryPhase_SnapshotVersionIsOneWhenNoExistingSnapshot()
    {
        // Arrange
        var epic = CreateDeliveryEpic();
        var command = new CloseFlowEpicCommand(epic.Id.Value, ValidProofUrl, ValidProofHash);
        AggregateSnapshot? capturedSnapshot = null;

        _flowEpicRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<FlowEpicId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(epic);
        _snapshotRepoMock
            .Setup(r => r.GetLatestAsync(epic.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AggregateSnapshot?)null);
        _snapshotRepoMock
            .Setup(r => r.AddAsync(It.IsAny<AggregateSnapshot>(), It.IsAny<CancellationToken>()))
            .Callback<AggregateSnapshot, CancellationToken>((s, _) => capturedSnapshot = s)
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedSnapshot);
        Assert.Equal(1, capturedSnapshot!.Version);
    }

    // ── Not found ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_EpicNotFound_ReturnsNotFound()
    {
        // Arrange
        var command = new CloseFlowEpicCommand(Guid.NewGuid(), ValidProofUrl, ValidProofHash);

        _flowEpicRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<FlowEpicId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FlowEpic?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_EpicNotFound_NeverCallsSnapshotRepository()
    {
        // Arrange
        var command = new CloseFlowEpicCommand(Guid.NewGuid(), ValidProofUrl, ValidProofHash);

        _flowEpicRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<FlowEpicId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FlowEpic?)null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _snapshotRepoMock.Verify(
            r => r.AddAsync(It.IsAny<AggregateSnapshot>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── Wrong phase ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_EpicInDiscoveryPhase_ReturnsError()
    {
        // Arrange
        var epic = FlowEpic.Create("Discovery Epic", FacilityId.New(), TenantId.New());
        // Phase is Discovery — Close() will throw DomainException
        var command = new CloseFlowEpicCommand(epic.Id.Value, ValidProofUrl, ValidProofHash);

        _flowEpicRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<FlowEpicId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(epic);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
    }

    [Fact]
    public async Task Handle_EpicAlreadyClosedDone_ReturnsError()
    {
        // Arrange
        var epic = CreateDeliveryEpic();
        epic.Close(ValidProofUrl, ValidProofHash);
        epic.PopDomainEvents();
        // Phase is now ClosedDone

        var command = new CloseFlowEpicCommand(epic.Id.Value, ValidProofUrl, ValidProofHash);

        _flowEpicRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<FlowEpicId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(epic);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
    }
}
