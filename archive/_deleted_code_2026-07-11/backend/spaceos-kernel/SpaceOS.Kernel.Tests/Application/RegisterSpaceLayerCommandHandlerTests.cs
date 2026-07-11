using Ardalis.Result;
using Moq;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.SpaceLayers.Commands;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

public class RegisterSpaceLayerCommandHandlerTests
{
    private readonly Mock<ISpaceLayerRepository> _spaceLayerRepositoryMock;
    private readonly Mock<IFacilityRepository> _facilityRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcherMock;
    private readonly RegisterSpaceLayerCommandHandler _handler;

    public RegisterSpaceLayerCommandHandlerTests()
    {
        _spaceLayerRepositoryMock = new Mock<ISpaceLayerRepository>();
        _facilityRepositoryMock = new Mock<IFacilityRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _domainEventDispatcherMock = new Mock<IDomainEventDispatcher>();
        _handler = new RegisterSpaceLayerCommandHandler(
            _spaceLayerRepositoryMock.Object,
            _facilityRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidLocalLayerRequest_ShouldReturnSuccess()
    {
        // Arrange
        var facility = Facility.Create("Test Facility", TenantId.New());
        var facilityId = facility.Id;
        var command = new RegisterSpaceLayerCommand(
            facilityId.Value,
            TradeType.Joinery,
            IsExternalNode: false,
            ExternalSourceUrl: null,
            IntentDataJson: "{}",
            TenantId: Guid.NewGuid());

        _facilityRepositoryMock.Setup(r => r.GetByIdAsync(facilityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(facility);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        _spaceLayerRepositoryMock.Verify(r => r.AddAsync(It.IsAny<SpaceLayer>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _domainEventDispatcherMock.Verify(d => d.DispatchAsync(
            It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidExternalLayerRequest_ShouldReturnSuccess()
    {
        // Arrange
        var facility = Facility.Create("Test Facility", TenantId.New());
        var facilityId = facility.Id;
        var command = new RegisterSpaceLayerCommand(
            facilityId.Value,
            TradeType.Joinery,
            IsExternalNode: true,
            ExternalSourceUrl: "https://external.node",
            IntentDataJson: null,
            TenantId: Guid.NewGuid());

        _facilityRepositoryMock.Setup(r => r.GetByIdAsync(facilityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(facility);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        _domainEventDispatcherMock.Verify(d => d.DispatchAsync(
            It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenFacilityNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var facilityId = FacilityId.New();
        var command = new RegisterSpaceLayerCommand(
            facilityId.Value,
            TradeType.Joinery,
            IsExternalNode: false,
            ExternalSourceUrl: null,
            IntentDataJson: "{}",
            TenantId: Guid.NewGuid());

        _facilityRepositoryMock.Setup(r => r.GetByIdAsync(facilityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Facility?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }
}
