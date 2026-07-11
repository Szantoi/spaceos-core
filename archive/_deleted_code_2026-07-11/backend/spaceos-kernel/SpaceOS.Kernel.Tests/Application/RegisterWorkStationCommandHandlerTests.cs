using Moq;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.WorkStations;
using SpaceOS.Kernel.Application.WorkStations.Commands;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;
using Ardalis.Result;

namespace SpaceOS.Kernel.Tests.Application;

public class RegisterWorkStationCommandHandlerTests
{
    private readonly Mock<IWorkStationRepository> _workStationRepositoryMock;
    private readonly Mock<IFacilityRepository> _facilityRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcherMock;
    private readonly RegisterWorkStationCommandHandler _handler;

    public RegisterWorkStationCommandHandlerTests()
    {
        _workStationRepositoryMock = new Mock<IWorkStationRepository>();
        _facilityRepositoryMock = new Mock<IFacilityRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _domainEventDispatcherMock = new Mock<IDomainEventDispatcher>();
        _handler = new RegisterWorkStationCommandHandler(
            _workStationRepositoryMock.Object,
            _facilityRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var facilityId = FacilityId.New();
        var command = new RegisterWorkStationCommand("WS-1", "Desk", facilityId.Value, Guid.NewGuid());
        
        _facilityRepositoryMock.Setup(r => r.GetByIdAsync(facilityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Facility.Create("Test Facility", TenantId.New()));
        _workStationRepositoryMock.Setup(r => r.ExistsByNameAsync(It.IsAny<FacilityId>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        _workStationRepositoryMock.Verify(r => r.AddAsync(It.IsAny<WorkStation>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _domainEventDispatcherMock.Verify(d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenFacilityNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var facilityId = FacilityId.New();
        var command = new RegisterWorkStationCommand("WS-1", "Desk", facilityId.Value, Guid.NewGuid());

        _facilityRepositoryMock.Setup(r => r.GetByIdAsync(facilityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Facility?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_WhenNameAlreadyExistsInFacility_ShouldReturnError()
    {
        // Arrange
        var facility = Facility.Create("Factory A", TenantId.New());
        var command = new RegisterWorkStationCommand("WS-01", "Assembly", facility.Id.Value, Guid.NewGuid());

        _facilityRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<FacilityId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(facility);
        _workStationRepositoryMock.Setup(r => r.ExistsByNameAsync(It.IsAny<FacilityId>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Error, result.Status);
    }
}
