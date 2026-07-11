// SpaceOS.Kernel.Tests/Application/GetFlowEpicsByFacilityQueryHandlerTests.cs
using Ardalis.Specification;
using Moq;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.FlowEpics.Queries;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

/// <summary>Unit tests for <see cref="GetFlowEpicsByFacilityQueryHandler"/>.</summary>
public sealed class GetFlowEpicsByFacilityQueryHandlerTests
{
    private readonly Mock<IFlowEpicRepository> _repositoryMock = new();
    private readonly GetFlowEpicsByFacilityQueryHandler _handler;

    public GetFlowEpicsByFacilityQueryHandlerTests() =>
        _handler = new GetFlowEpicsByFacilityQueryHandler(_repositoryMock.Object);

    [Fact]
    public async Task Handle_WhenEpicsExist_ReturnsPagedResult()
    {
        // Arrange
        var facilityId = FacilityId.New();
        IReadOnlyList<FlowEpic> epics = new List<FlowEpic>
        {
            FlowEpic.Create("Epic One", facilityId, TenantId.New()),
            FlowEpic.Create("Epic Two", facilityId, TenantId.New())
        }.AsReadOnly();

        _repositoryMock
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<FlowEpic>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        _repositoryMock
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<FlowEpic>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(epics);

        // Act
        var result = await _handler.Handle(new GetFlowEpicsByFacilityQuery(facilityId.Value), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Equal(2, result.Value.TotalCount);
    }

    [Fact]
    public async Task Handle_WhenNoEpicsExist_ReturnsEmptyPagedResult()
    {
        // Arrange
        IReadOnlyList<FlowEpic> emptyList = new List<FlowEpic>().AsReadOnly();

        _repositoryMock
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<FlowEpic>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _repositoryMock
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<FlowEpic>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyList);

        // Act
        var result = await _handler.Handle(new GetFlowEpicsByFacilityQuery(Guid.NewGuid()), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalCount);
    }

    [Fact]
    public async Task Handle_DefaultPaging_ReturnsFirstPage()
    {
        // Arrange — 3 epics, default page=1 / pageSize=20, all 3 returned
        var facilityId = FacilityId.New();
        IReadOnlyList<FlowEpic> epics = new List<FlowEpic>
        {
            FlowEpic.Create("Epic One", facilityId, TenantId.New()),
            FlowEpic.Create("Epic Two", facilityId, TenantId.New()),
            FlowEpic.Create("Epic Three", facilityId, TenantId.New())
        }.AsReadOnly();

        _repositoryMock
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<FlowEpic>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        _repositoryMock
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<FlowEpic>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(epics);

        // Act
        var result = await _handler.Handle(
            new GetFlowEpicsByFacilityQuery(facilityId.Value, Page: 1, PageSize: 20),
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
        IReadOnlyList<FlowEpic> pageSlice = new List<FlowEpic>
        {
            FlowEpic.Create("Epic One", facilityId, TenantId.New()),
            FlowEpic.Create("Epic Two", facilityId, TenantId.New())
        }.AsReadOnly();

        _repositoryMock
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<FlowEpic>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        _repositoryMock
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<FlowEpic>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageSlice);

        // Act
        var result = await _handler.Handle(
            new GetFlowEpicsByFacilityQuery(facilityId.Value, Page: 1, PageSize: 2),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Equal(3, result.Value.TotalCount);
        Assert.Equal(1, result.Value.Page);
        Assert.Equal(2, result.Value.PageSize);
    }
}
