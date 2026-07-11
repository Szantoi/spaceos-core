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

public class AssignWorkStationToFacilityCommandHandlerTests
{
    private readonly Mock<IWorkStationRepository> _workStationRepositoryMock;
    private readonly Mock<IFacilityRepository> _facilityRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcherMock;
    private readonly AssignWorkStationToFacilityCommandHandler _handler;

    public AssignWorkStationToFacilityCommandHandlerTests()
    {
        _workStationRepositoryMock = new Mock<IWorkStationRepository>();
        _facilityRepositoryMock = new Mock<IFacilityRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _domainEventDispatcherMock = new Mock<IDomainEventDispatcher>();
        _handler = new AssignWorkStationToFacilityCommandHandler(
            _workStationRepositoryMock.Object,
            _facilityRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var originalFacilityId = FacilityId.New();
        var newFacilityId = FacilityId.New();
        var workStation = WorkStation.Create("WS-01", "Assembly", originalFacilityId, TenantId.New());
        workStation.PopDomainEvents(); // clear creation event
        var newFacility = Facility.Create("New Facility", TenantId.New());

        var command = new AssignWorkStationToFacilityCommand(workStation.Id.Value, newFacilityId.Value);

        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<WorkStationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(workStation);
        _facilityRepositoryMock.Setup(r => r.GetByIdAsync(newFacilityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newFacility);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(newFacilityId, workStation.FacilityId);
        _workStationRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<WorkStation>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _domainEventDispatcherMock.Verify(d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenWorkStationNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var command = new AssignWorkStationToFacilityCommand(Guid.NewGuid(), Guid.NewGuid());

        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<WorkStationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkStation?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_WhenFacilityNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var workStation = WorkStation.Create("WS-01", "Assembly", FacilityId.New(), TenantId.New());
        var command = new AssignWorkStationToFacilityCommand(workStation.Id.Value, Guid.NewGuid());

        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<WorkStationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(workStation);
        _facilityRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<FacilityId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Facility?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }
}
