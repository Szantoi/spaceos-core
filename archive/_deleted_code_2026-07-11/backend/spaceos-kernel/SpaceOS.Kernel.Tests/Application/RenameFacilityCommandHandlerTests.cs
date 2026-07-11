using Moq;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.Facilities.Commands;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;
using Ardalis.Result;

namespace SpaceOS.Kernel.Tests.Application;

public class RenameFacilityCommandHandlerTests
{
    private readonly Mock<IFacilityRepository> _facilityRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcherMock;
    private readonly RenameFacilityCommandHandler _handler;

    public RenameFacilityCommandHandlerTests()
    {
        _facilityRepositoryMock = new Mock<IFacilityRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _domainEventDispatcherMock = new Mock<IDomainEventDispatcher>();
        _handler = new RenameFacilityCommandHandler(
            _facilityRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var facility = Facility.Create("Old Name", TenantId.New());
        facility.PopDomainEvents(); // clear creation event
        var command = new RenameFacilityCommand(facility.Id.Value, "New Name");

        _facilityRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<FacilityId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(facility);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("New Name", facility.Name.Value);
        _facilityRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Facility>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _domainEventDispatcherMock.Verify(d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenFacilityNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var command = new RenameFacilityCommand(Guid.NewGuid(), "New Name");

        _facilityRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<FacilityId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Facility?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }
}
