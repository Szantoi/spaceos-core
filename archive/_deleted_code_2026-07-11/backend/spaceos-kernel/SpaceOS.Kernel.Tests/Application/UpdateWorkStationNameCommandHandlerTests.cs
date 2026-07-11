using Moq;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.WorkStations.Commands;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;
using Ardalis.Result;

namespace SpaceOS.Kernel.Tests.Application;

public class UpdateWorkStationNameCommandHandlerTests
{
    private readonly Mock<IWorkStationRepository> _workStationRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcherMock;
    private readonly UpdateWorkStationNameCommandHandler _handler;

    public UpdateWorkStationNameCommandHandlerTests()
    {
        _workStationRepositoryMock = new Mock<IWorkStationRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _domainEventDispatcherMock = new Mock<IDomainEventDispatcher>();
        _handler = new UpdateWorkStationNameCommandHandler(
            _workStationRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var workStation = WorkStation.Create("Old Name", "Assembly", FacilityId.New(), TenantId.New());
        workStation.PopDomainEvents(); // clear creation event
        var command = new UpdateWorkStationNameCommand(workStation.Id.Value, "New Name");

        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<WorkStationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(workStation);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("New Name", workStation.Name.Value);
        _workStationRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<WorkStation>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _domainEventDispatcherMock.Verify(d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenWorkStationNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var command = new UpdateWorkStationNameCommand(Guid.NewGuid(), "New Name");

        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<WorkStationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkStation?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }
}
