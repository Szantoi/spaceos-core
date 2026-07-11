// SpaceOS.Kernel.Tests/Application/GetTenantSummaryQueryHandlerTests.cs
using Ardalis.Specification;
using Moq;
using SpaceOS.Kernel.Application.Tools.Queries;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Repositories;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

/// <summary>Tests for <see cref="GetTenantSummaryQueryHandler"/>.</summary>
public sealed class GetTenantSummaryQueryHandlerTests
{
    private readonly Mock<IFlowEpicRepository>    _flowEpicRepo    = new();
    private readonly Mock<IWorkStationRepository> _workStationRepo = new();
    private readonly Mock<IFacilityRepository>    _facilityRepo    = new();
    private readonly GetTenantSummaryQueryHandler _handler;

    public GetTenantSummaryQueryHandlerTests()
    {
        _handler = new GetTenantSummaryQueryHandler(
            _flowEpicRepo.Object,
            _workStationRepo.Object,
            _facilityRepo.Object);
    }

    [Fact]
    public async Task Handle_NoData_ReturnsZeroCounts()
    {
        // Arrange
        _flowEpicRepo
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<FlowEpic>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _workStationRepo
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<WorkStation>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _facilityRepo
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<Facility>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetTenantSummaryQuery(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.FlowEpicCount);
        Assert.Equal(0, result.Value.ActiveWorkstationCount);
        Assert.Equal(0, result.Value.FacilityCount);
    }

    [Fact]
    public async Task Handle_WithData_ReturnsCorrectCounts()
    {
        // Arrange
        _flowEpicRepo
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<FlowEpic>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        _workStationRepo
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<WorkStation>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);
        _facilityRepo
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<Facility>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var query = new GetTenantSummaryQuery(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.FlowEpicCount);
        Assert.Equal(7, result.Value.ActiveWorkstationCount);
        Assert.Equal(2, result.Value.FacilityCount);
    }

    [Fact]
    public async Task Handle_CallsEachRepositoryOnce()
    {
        // Arrange
        _flowEpicRepo
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<FlowEpic>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _workStationRepo
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<WorkStation>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _facilityRepo
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<Facility>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetTenantSummaryQuery(Guid.NewGuid());

        // Act
        await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert — each repository queried exactly once
        _flowEpicRepo.Verify(r => r.CountAsync(It.IsAny<ISpecification<FlowEpic>>(), It.IsAny<CancellationToken>()), Times.Once);
        _workStationRepo.Verify(r => r.CountAsync(It.IsAny<ISpecification<WorkStation>>(), It.IsAny<CancellationToken>()), Times.Once);
        _facilityRepo.Verify(r => r.CountAsync(It.IsAny<ISpecification<Facility>>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
