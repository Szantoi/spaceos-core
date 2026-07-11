// SpaceOS.Kernel.Tests/Application/GetWorkStationsByFacilityQueryHandlerTests.cs
using Ardalis.Specification;
using Moq;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.WorkStations.Queries;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

/// <summary>Unit tests for <see cref="GetWorkStationsByFacilityQueryHandler"/>.</summary>
public sealed class GetWorkStationsByFacilityQueryHandlerTests
{
    private readonly Mock<IWorkStationRepository> _workStationRepositoryMock = new();
    private readonly GetWorkStationsByFacilityQueryHandler _handler;

    public GetWorkStationsByFacilityQueryHandlerTests() =>
        _handler = new GetWorkStationsByFacilityQueryHandler(_workStationRepositoryMock.Object);

    [Fact]
    public async Task Handle_WithValidFacilityId_ReturnsPagedResultWithWorkStations()
    {
        // Arrange
        var facilityId = FacilityId.New();
        IReadOnlyList<WorkStation> workStations = new List<WorkStation>
        {
            WorkStation.Create("WS-01", "Standard", facilityId, TenantId.New()),
            WorkStation.Create("WS-02", "DeepWork", facilityId, TenantId.New())
        }.AsReadOnly();

        _workStationRepositoryMock
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<WorkStation>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        _workStationRepositoryMock
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<WorkStation>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(workStations);

        // Act
        var result = await _handler.Handle(new GetWorkStationsByFacilityQuery(facilityId.Value), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Contains(result.Value.Items, ws => ws.Name == "WS-01");
        Assert.Contains(result.Value.Items, ws => ws.Name == "WS-02");
    }

    [Fact]
    public async Task Handle_WithEmptyFacility_ReturnsEmptyPagedResult()
    {
        // Arrange
        var facilityId = FacilityId.New();
        IReadOnlyList<WorkStation> emptyList = new List<WorkStation>().AsReadOnly();

        _workStationRepositoryMock
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<WorkStation>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _workStationRepositoryMock
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<WorkStation>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyList);

        // Act
        var result = await _handler.Handle(new GetWorkStationsByFacilityQuery(facilityId.Value), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalCount);
    }

    [Fact]
    public async Task Handle_DefaultPaging_ReturnsFirstPage()
    {
        // Arrange — 3 workstations, default page=1 / pageSize=20, all 3 returned
        var facilityId = FacilityId.New();
        IReadOnlyList<WorkStation> workStations = new List<WorkStation>
        {
            WorkStation.Create("WS-01", "Standard", facilityId, TenantId.New()),
            WorkStation.Create("WS-02", "DeepWork", facilityId, TenantId.New()),
            WorkStation.Create("WS-03", "Standard", facilityId, TenantId.New())
        }.AsReadOnly();

        _workStationRepositoryMock
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<WorkStation>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        _workStationRepositoryMock
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<WorkStation>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(workStations);

        // Act
        var result = await _handler.Handle(
            new GetWorkStationsByFacilityQuery(facilityId.Value, Page: 1, PageSize: 20),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Items.Count);
        Assert.Equal(3, result.Value.TotalCount);
        Assert.Equal(1, result.Value.Page);
        Assert.Equal(20, result.Value.PageSize);
    }

    [Fact]
    public async Task Handle_PageSize2_Page1_ReturnsCorrectSlice()
    {
        // Arrange — repository returns only the page-1 slice of 2 items; totalCount=3
        var facilityId = FacilityId.New();
        IReadOnlyList<WorkStation> pageSlice = new List<WorkStation>
        {
            WorkStation.Create("WS-01", "Standard", facilityId, TenantId.New()),
            WorkStation.Create("WS-02", "DeepWork", facilityId, TenantId.New())
        }.AsReadOnly();

        _workStationRepositoryMock
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<WorkStation>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        _workStationRepositoryMock
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<WorkStation>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageSlice);

        // Act
        var result = await _handler.Handle(
            new GetWorkStationsByFacilityQuery(facilityId.Value, Page: 1, PageSize: 2),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Equal(3, result.Value.TotalCount);
        Assert.Equal(1, result.Value.Page);
        Assert.Equal(2, result.Value.PageSize);
    }
}
