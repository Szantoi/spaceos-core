using Moq;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.FlowEpics.Commands;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;
using Ardalis.Result;

namespace SpaceOS.Kernel.Tests.Application;

public class CreateFlowEpicCommandHandlerTests
{
    private readonly Mock<IFlowEpicRepository> _flowEpicRepositoryMock;
    private readonly Mock<IFacilityRepository> _facilityRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcherMock;
    private readonly CreateFlowEpicCommandHandler _handler;

    public CreateFlowEpicCommandHandlerTests()
    {
        _flowEpicRepositoryMock = new Mock<IFlowEpicRepository>();
        _facilityRepositoryMock = new Mock<IFacilityRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _domainEventDispatcherMock = new Mock<IDomainEventDispatcher>();
        _handler = new CreateFlowEpicCommandHandler(
            _flowEpicRepositoryMock.Object,
            _facilityRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldCreateEpicInDiscoveryPhase()
    {
        // Arrange
        var facilityId = FacilityId.New();
        var command = new CreateFlowEpicCommand("Kitchen Manufacturing", facilityId.Value, Guid.NewGuid());

        var facility = Facility.Create("Test Facility", TenantId.New());
        _facilityRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<FacilityId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(facility);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _flowEpicRepositoryMock.Verify(r => r.AddAsync(
            It.Is<FlowEpic>(e => e.Title == command.Title && e.Phase == WorkflowPhase.Discovery),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _domainEventDispatcherMock.Verify(d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenFacilityNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var facilityId = FacilityId.New();
        var command = new CreateFlowEpicCommand("Test Epic", facilityId.Value, Guid.NewGuid());

        _facilityRepositoryMock.Setup(r => r.GetByIdAsync(facilityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Facility?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }
}
