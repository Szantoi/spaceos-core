// SpaceOS.Kernel.Tests/StageRegistry/Handlers/AssignChainCommandHandlerTests.cs
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

/// <summary>Unit tests for <see cref="AssignChainCommandHandler"/>.</summary>
public sealed class AssignChainCommandHandlerTests
{
    private readonly Mock<IFlowEpicRepository> _epicRepository = new();
    private readonly Mock<IStageChainTemplateRepository> _chainRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcher = new();
    private readonly AssignChainCommandHandler _handler;

    private static readonly TenantId TestTenantId = TenantId.From(Guid.NewGuid());
    private static readonly FacilityId TestFacilityId = FacilityId.From(Guid.NewGuid());

    private static FlowEpic BuildEpic() =>
        FlowEpic.Create("Test Epic", TestFacilityId, TestTenantId);

    private static StageChainTemplate BuildTemplate() =>
        StageChainTemplate.Create(TestTenantId.Value, "Standard Chain");

    public AssignChainCommandHandlerTests()
    {
        _handler = new AssignChainCommandHandler(
            _epicRepository.Object,
            _chainRepository.Object,
            _unitOfWork.Object,
            _dispatcher.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccess()
    {
        // Arrange
        var epic = BuildEpic();
        var template = BuildTemplate();
        _epicRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<FlowEpicId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(epic);
        _chainRepository
            .Setup(r => r.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        var command = new AssignChainCommand(epic.Id.Value, template.Id, "review_step");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsUpdateAsync_Once()
    {
        // Arrange
        var epic = BuildEpic();
        var template = BuildTemplate();
        _epicRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<FlowEpicId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(epic);
        _chainRepository
            .Setup(r => r.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        var command = new AssignChainCommand(epic.Id.Value, template.Id, "review_step");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _epicRepository.Verify(r =>
            r.UpdateAsync(It.IsAny<FlowEpic>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSaveChangesAsync_Once()
    {
        // Arrange
        var epic = BuildEpic();
        var template = BuildTemplate();
        _epicRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<FlowEpicId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(epic);
        _chainRepository
            .Setup(r => r.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        var command = new AssignChainCommand(epic.Id.Value, template.Id, "review_step");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _unitOfWork.Verify(u =>
            u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_DispatchesDomainEvents_Once()
    {
        // Arrange
        var epic = BuildEpic();
        var template = BuildTemplate();
        _epicRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<FlowEpicId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(epic);
        _chainRepository
            .Setup(r => r.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        var command = new AssignChainCommand(epic.Id.Value, template.Id, "review_step");

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
        var command = new AssignChainCommand(Guid.NewGuid(), Guid.NewGuid(), "review_step");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_ChainNotFound_ReturnsNotFound()
    {
        // Arrange
        var epic = BuildEpic();
        _epicRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<FlowEpicId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(epic);
        _chainRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StageChainTemplate?)null);
        var command = new AssignChainCommand(epic.Id.Value, Guid.NewGuid(), "review_step");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }
}
