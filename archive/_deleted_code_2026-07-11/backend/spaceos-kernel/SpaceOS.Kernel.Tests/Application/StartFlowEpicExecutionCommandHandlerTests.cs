using Moq;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.FlowEpics.Commands;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;
using Ardalis.Result;
using SpaceOS.Kernel.Domain.Enums;

namespace SpaceOS.Kernel.Tests.Application;

public class StartFlowEpicExecutionCommandHandlerTests
{
    private readonly Mock<IFlowEpicRepository> _flowEpicRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcherMock;
    private readonly StartFlowEpicExecutionCommandHandler _handler;

    public StartFlowEpicExecutionCommandHandlerTests()
    {
        _flowEpicRepositoryMock = new Mock<IFlowEpicRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _domainEventDispatcherMock = new Mock<IDomainEventDispatcher>();
        _handler = new StartFlowEpicExecutionCommandHandler(
            _flowEpicRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidId_ShouldTransitionToDelivery()
    {
        // Arrange
        var epicId = FlowEpicId.New();
        var facilityId = FacilityId.New();
        var epic = FlowEpic.Create("Test Epic", facilityId, TenantId.New());
        var command = new StartFlowEpicExecutionCommand(epicId.Value);

        _flowEpicRepositoryMock.Setup(r => r.GetByIdAsync(epicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(epic);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(WorkflowPhase.Delivery, epic.Phase);
        _unitOfWorkMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _domainEventDispatcherMock.Verify(d => d.DispatchAsync(
            It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEpicNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var epicId = FlowEpicId.New();
        var command = new StartFlowEpicExecutionCommand(epicId.Value);

        _flowEpicRepositoryMock.Setup(r => r.GetByIdAsync(epicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FlowEpic?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_WhenEpicAlreadyInDelivery_ShouldReturnError()
    {
        // Arrange
        var epic = FlowEpic.Create("Test Epic", FacilityId.New(), TenantId.New());
        epic.StartExecution(); // put into Delivery phase
        var command = new StartFlowEpicExecutionCommand(epic.Id.Value);

        _flowEpicRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<FlowEpicId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(epic);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Error, result.Status);
    }
}
