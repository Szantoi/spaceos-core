using Moq;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.WorkStations;
using SpaceOS.Kernel.Application.WorkStations.Commands;

using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;
using Ardalis.Result;

namespace SpaceOS.Kernel.Tests.Application;

public class UpdateWorkStationStatusCommandHandlerTests
{
    private readonly Mock<IWorkStationRepository> _workStationRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcherMock;
    private readonly UpdateWorkStationStatusCommandHandler _handler;

    public UpdateWorkStationStatusCommandHandlerTests()
    {
        _workStationRepositoryMock = new Mock<IWorkStationRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _domainEventDispatcherMock = new Mock<IDomainEventDispatcher>();
        _handler = new UpdateWorkStationStatusCommandHandler(
            _workStationRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var workStationId = WorkStationId.New();
        var command = new UpdateWorkStationStatusCommand(workStationId.Value, WorkStationStatus.Active);
        var workStation = WorkStation.Create("WS-1", "Desk", FacilityId.New(), TenantId.New());
        
        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(workStationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workStation);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _workStationRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<WorkStation>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _domainEventDispatcherMock.Verify(r => r.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenWorkStationNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var workStationId = WorkStationId.New();
        var command = new UpdateWorkStationStatusCommand(workStationId.Value, WorkStationStatus.Active);

        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(workStationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkStation?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_WhenStatusUnchanged_ShouldSucceedWithoutDispatchingEvents()
    {
        // Arrange
        var workStation = WorkStation.Create("WS-1", "Assembly", FacilityId.New(), TenantId.New());
        workStation.PopDomainEvents(); // clear creation event — handler gets a clean workstation
        // WorkStation alapértelmezett státusza: Available
        var command = new UpdateWorkStationStatusCommand(workStation.Id.Value, WorkStationStatus.Available);

        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<WorkStationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(workStation);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _domainEventDispatcherMock.Verify(d => d.DispatchAsync(
            It.Is<IEnumerable<IDomainEvent>>(events => !events.Any()),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
