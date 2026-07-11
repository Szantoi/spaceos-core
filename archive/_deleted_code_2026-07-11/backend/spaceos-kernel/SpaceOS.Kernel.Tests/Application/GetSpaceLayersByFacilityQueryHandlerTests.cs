// SpaceOS.Kernel.Tests/Application/GetSpaceLayersByFacilityQueryHandlerTests.cs
using Ardalis.Specification;
using Moq;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.SpaceLayers.Queries;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

/// <summary>Unit tests for <see cref="GetSpaceLayersByFacilityQueryHandler"/>.</summary>
public sealed class GetSpaceLayersByFacilityQueryHandlerTests
{
    private readonly Mock<ISpaceLayerRepository> _repositoryMock = new();
    private readonly GetSpaceLayersByFacilityQueryHandler _handler;

    public GetSpaceLayersByFacilityQueryHandlerTests() =>
        _handler = new GetSpaceLayersByFacilityQueryHandler(_repositoryMock.Object);

    [Fact]
    public async Task Handle_WhenLayersExist_ReturnsPagedResult()
    {
        // Arrange
        var facilityId = FacilityId.New();
        IReadOnlyList<SpaceLayer> layers = new List<SpaceLayer>
        {
            SpaceLayer.CreateLocalLayer("{}", facilityId, TradeType.Joinery, TenantId.New()),
            SpaceLayer.CreateLocalLayer("{\"x\":1}", facilityId, TradeType.Joinery, TenantId.New())
        }.AsReadOnly();

        _repositoryMock
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<SpaceLayer>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        _repositoryMock
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<SpaceLayer>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(layers);

        // Act
        var result = await _handler.Handle(new GetSpaceLayersByFacilityQuery(facilityId.Value), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Equal(2, result.Value.TotalCount);
    }

    [Fact]
    public async Task Handle_WhenNoLayersExist_ReturnsEmptyPagedResult()
    {
        // Arrange
        IReadOnlyList<SpaceLayer> emptyList = new List<SpaceLayer>().AsReadOnly();

        _repositoryMock
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<SpaceLayer>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _repositoryMock
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<SpaceLayer>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyList);

        // Act
        var result = await _handler.Handle(new GetSpaceLayersByFacilityQuery(Guid.NewGuid()), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalCount);
    }

    [Fact]
    public async Task Handle_DefaultPaging_ReturnsFirstPage()
    {
        // Arrange — 3 layers, default page=1 / pageSize=20, all 3 returned
        var facilityId = FacilityId.New();
        IReadOnlyList<SpaceLayer> layers = new List<SpaceLayer>
        {
            SpaceLayer.CreateLocalLayer("{}", facilityId, TradeType.Joinery, TenantId.New()),
            SpaceLayer.CreateLocalLayer("{\"x\":1}", facilityId, TradeType.Electrical, TenantId.New()),
            SpaceLayer.CreateLocalLayer("{\"y\":2}", facilityId, TradeType.Plumbing, TenantId.New())
        }.AsReadOnly();

        _repositoryMock
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<SpaceLayer>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        _repositoryMock
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<SpaceLayer>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(layers);

        // Act
        var result = await _handler.Handle(
            new GetSpaceLayersByFacilityQuery(facilityId.Value, Page: 1, PageSize: 20),
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
        IReadOnlyList<SpaceLayer> pageSlice = new List<SpaceLayer>
        {
            SpaceLayer.CreateLocalLayer("{}", facilityId, TradeType.Joinery, TenantId.New()),
            SpaceLayer.CreateLocalLayer("{\"x\":1}", facilityId, TradeType.Electrical, TenantId.New())
        }.AsReadOnly();

        _repositoryMock
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<SpaceLayer>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        _repositoryMock
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<SpaceLayer>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageSlice);

        // Act
        var result = await _handler.Handle(
            new GetSpaceLayersByFacilityQuery(facilityId.Value, Page: 1, PageSize: 2),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Equal(3, result.Value.TotalCount);
        Assert.Equal(1, result.Value.Page);
        Assert.Equal(2, result.Value.PageSize);
    }
}
