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

public class UpdateSpaceLayerIntentDataCommandHandlerTests
{
    private readonly Mock<ISpaceLayerRepository>  _spaceLayerRepositoryMock;
    private readonly Mock<IUnitOfWork>             _unitOfWorkMock;
    private readonly Mock<IDomainEventDispatcher>  _domainEventDispatcherMock;
    private readonly UpdateSpaceLayerIntentDataCommandHandler _handler;

    public UpdateSpaceLayerIntentDataCommandHandlerTests()
    {
        _spaceLayerRepositoryMock  = new Mock<ISpaceLayerRepository>();
        _unitOfWorkMock            = new Mock<IUnitOfWork>();
        _domainEventDispatcherMock = new Mock<IDomainEventDispatcher>();
        _handler = new UpdateSpaceLayerIntentDataCommandHandler(
            _spaceLayerRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var layer = SpaceLayer.CreateLocalLayer("{}", FacilityId.New(), TradeType.Joinery, TenantId.New());
        var command = new UpdateSpaceLayerIntentDataCommand(layer.Id.Value, "{\"updated\":true}");

        _spaceLayerRepositoryMock.Setup(r => r.GetByIdAsync(layer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(layer);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("{\"updated\":true}", layer.IntentDataJson);
        _spaceLayerRepositoryMock.Verify(r => r.UpdateAsync(layer, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _domainEventDispatcherMock.Verify(d => d.DispatchAsync(
            It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSpaceLayerNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var command = new UpdateSpaceLayerIntentDataCommand(Guid.NewGuid(), "{\"data\":1}");

        _spaceLayerRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<SpaceLayerId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SpaceLayer?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_WhenLayerIsExternal_ShouldReturnError()
    {
        // Arrange
        var layer = SpaceLayer.CreateExternalLayer("https://external.node", FacilityId.New(), TradeType.Joinery, TenantId.New());
        var command = new UpdateSpaceLayerIntentDataCommand(layer.Id.Value, "{\"data\":1}");

        _spaceLayerRepositoryMock.Setup(r => r.GetByIdAsync(layer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(layer);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Error, result.Status);
    }
}
