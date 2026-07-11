using Ardalis.Result;
using Moq;
using SpaceOS.Kernel.Application.WorkStations;
using SpaceOS.Kernel.Application.WorkStations.Queries;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

public class GetWorkStationByIdQueryHandlerTests
{
    private readonly Mock<IWorkStationRepository> _workStationRepositoryMock;
    private readonly GetWorkStationByIdQueryHandler _handler;

    public GetWorkStationByIdQueryHandlerTests()
    {
        _workStationRepositoryMock = new Mock<IWorkStationRepository>();
        _handler = new GetWorkStationByIdQueryHandler(_workStationRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenWorkStationExists_ShouldReturnSuccessWithCorrectDto()
    {
        // Arrange
        var facilityId = FacilityId.New();
        var workStation = WorkStation.Create("TestStation", "TypeA", facilityId, TenantId.New());
        var query = new GetWorkStationByIdQuery(workStation.Id.Value);

        _workStationRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<WorkStationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(workStation);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(workStation.Id.Value, result.Value.Id);
        Assert.Equal("TestStation", result.Value.Name);
        Assert.Equal("TypeA", result.Value.Type);
        Assert.Equal(facilityId.Value, result.Value.FacilityId);
        Assert.Equal(workStation.Status, result.Value.Status);
    }

    [Fact]
    public async Task Handle_WhenWorkStationDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var query = new GetWorkStationByIdQuery(Guid.NewGuid());

        _workStationRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<WorkStationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkStation?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }
}
