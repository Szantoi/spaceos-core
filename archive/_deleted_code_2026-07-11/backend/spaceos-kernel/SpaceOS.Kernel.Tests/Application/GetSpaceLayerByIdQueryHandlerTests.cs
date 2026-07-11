using Ardalis.Result;
using Moq;
using SpaceOS.Kernel.Application.SpaceLayers;
using SpaceOS.Kernel.Application.SpaceLayers.Queries;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

public class GetSpaceLayerByIdQueryHandlerTests
{
    private readonly Mock<ISpaceLayerRepository> _spaceLayerRepositoryMock;
    private readonly GetSpaceLayerByIdQueryHandler _handler;

    public GetSpaceLayerByIdQueryHandlerTests()
    {
        _spaceLayerRepositoryMock = new Mock<ISpaceLayerRepository>();
        _handler = new GetSpaceLayerByIdQueryHandler(_spaceLayerRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenSpaceLayerExists_ShouldReturnSuccess()
    {
        // Arrange
        var facilityId = FacilityId.New();
        var layer = SpaceLayer.CreateLocalLayer("{}", facilityId, TradeType.Joinery, TenantId.New());
        var query = new GetSpaceLayerByIdQuery(layer.Id.Value);

        _spaceLayerRepositoryMock.Setup(r => r.GetByIdAsync(layer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(layer);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(layer.Id.Value, result.Value.Id);
        Assert.Equal(facilityId.Value, result.Value.FacilityId);
        Assert.Equal(TradeType.Joinery, result.Value.TradeType);
        Assert.False(result.Value.IsExternalNode);
        Assert.Equal("{}", result.Value.IntentDataJson);
    }

    [Fact]
    public async Task Handle_WhenSpaceLayerNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var query = new GetSpaceLayerByIdQuery(Guid.NewGuid());

        _spaceLayerRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<SpaceLayerId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SpaceLayer?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }
}
