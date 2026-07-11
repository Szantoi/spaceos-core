using Ardalis.Result;
using Moq;
using SpaceOS.Kernel.Application.FlowEpics.Queries;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

public class GetFlowEpicByIdQueryHandlerTests
{
    private readonly Mock<IFlowEpicRepository> _flowEpicRepositoryMock;
    private readonly GetFlowEpicByIdQueryHandler _handler;

    public GetFlowEpicByIdQueryHandlerTests()
    {
        _flowEpicRepositoryMock = new Mock<IFlowEpicRepository>();
        _handler = new GetFlowEpicByIdQueryHandler(_flowEpicRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenEpicExists_ShouldReturnSuccessWithCorrectDto()
    {
        // Arrange
        var facilityId = FacilityId.New();
        var epic = FlowEpic.Create("Logistics Integration", facilityId, TenantId.New());
        var query = new GetFlowEpicByIdQuery(epic.Id.Value);

        _flowEpicRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<FlowEpicId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(epic);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(epic.Id.Value, result.Value.Id);
        Assert.Equal("Logistics Integration", result.Value.Title);
        Assert.Equal(facilityId.Value, result.Value.TargetFacilityId);
        Assert.Equal(WorkflowPhase.Discovery, result.Value.Phase);
        Assert.False(result.Value.IsDelegated);
    }

    [Fact]
    public async Task Handle_WhenEpicDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var query = new GetFlowEpicByIdQuery(Guid.NewGuid());

        _flowEpicRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<FlowEpicId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FlowEpic?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }
}
