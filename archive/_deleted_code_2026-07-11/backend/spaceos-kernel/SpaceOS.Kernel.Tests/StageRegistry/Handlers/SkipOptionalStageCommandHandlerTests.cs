// SpaceOS.Kernel.Tests/StageRegistry/Handlers/SkipOptionalStageCommandHandlerTests.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Result;
using Moq;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.StageRegistry.Commands;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.StageRegistry.Handlers;

/// <summary>Unit tests for <see cref="SkipOptionalStageCommandHandler"/>.</summary>
public sealed class SkipOptionalStageCommandHandlerTests
{
    private readonly Mock<IFlowEpicRepository> _epicRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcher = new();
    private readonly SkipOptionalStageCommandHandler _handler;

    private static readonly TenantId TestTenantId = TenantId.From(Guid.NewGuid());
    private static readonly FacilityId TestFacilityId = FacilityId.From(Guid.NewGuid());

    private static FlowEpic BuildEpic()
    {
        var epic = FlowEpic.Create("Test Epic", TestFacilityId, TestTenantId);
        epic.AssignChain(Guid.NewGuid(), "stage_a");
        epic.PopDomainEvents();
        return epic;
    }

    public SkipOptionalStageCommandHandlerTests()
    {
        _handler = new SkipOptionalStageCommandHandler(
            _epicRepository.Object,
            _unitOfWork.Object,
            _dispatcher.Object);
    }

    [Fact]
    public async Task Handle_ExistingEpic_ReturnsSuccess()
    {
        // Arrange
        var epic = BuildEpic();
        _epicRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<FlowEpicId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(epic);
        var command = new SkipOptionalStageCommand(epic.Id.Value, "stage_b");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ExistingEpic_CallsUpdateAsync_Once()
    {
        // Arrange
        var epic = BuildEpic();
        _epicRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<FlowEpicId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(epic);
        var command = new SkipOptionalStageCommand(epic.Id.Value, "stage_b");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _epicRepository.Verify(r =>
            r.UpdateAsync(It.IsAny<FlowEpic>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingEpic_CallsSaveChangesAsync_Once()
    {
        // Arrange
        var epic = BuildEpic();
        _epicRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<FlowEpicId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(epic);
        var command = new SkipOptionalStageCommand(epic.Id.Value, "stage_b");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _unitOfWork.Verify(u =>
            u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingEpic_DispatchesDomainEvents_Once()
    {
        // Arrange
        var epic = BuildEpic();
        _epicRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<FlowEpicId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(epic);
        var command = new SkipOptionalStageCommand(epic.Id.Value, "stage_b");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _dispatcher.Verify(d =>
            d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_EpicNotFound_ReturnsNotFound()
    {
        // Arrange
        _epicRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<FlowEpicId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FlowEpic?)null);
        var command = new SkipOptionalStageCommand(Guid.NewGuid(), "stage_b");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }
}
