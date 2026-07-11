// SpaceOS.Kernel.Tests/Application/GetFacilitiesByTenantQueryHandlerTests.cs
using Ardalis.Specification;
using Moq;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.Facilities.Queries;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

/// <summary>Unit tests for <see cref="GetFacilitiesByTenantQueryHandler"/>.</summary>
public sealed class GetFacilitiesByTenantQueryHandlerTests
{
    private readonly Mock<IFacilityRepository> _repositoryMock = new();
    private readonly GetFacilitiesByTenantQueryHandler _handler;

    public GetFacilitiesByTenantQueryHandlerTests() =>
        _handler = new GetFacilitiesByTenantQueryHandler(_repositoryMock.Object);

    [Fact]
    public async Task Handle_WhenFacilitiesExist_ReturnsPagedResult()
    {
        // Arrange
        var tenantId = TenantId.New();
        IReadOnlyList<Facility> facilities = new List<Facility>
        {
            Facility.Create("HQ", tenantId),
            Facility.Create("Branch", tenantId)
        }.AsReadOnly();

        _repositoryMock
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<Facility>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        _repositoryMock
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<Facility>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(facilities);

        // Act
        var result = await _handler.Handle(new GetFacilitiesByTenantQuery(tenantId.Value), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Equal(2, result.Value.TotalCount);
    }

    [Fact]
    public async Task Handle_WhenNoFacilitiesExist_ReturnsEmptyPagedResult()
    {
        // Arrange
        IReadOnlyList<Facility> emptyList = new List<Facility>().AsReadOnly();

        _repositoryMock
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<Facility>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _repositoryMock
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<Facility>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyList);

        // Act
        var result = await _handler.Handle(new GetFacilitiesByTenantQuery(Guid.NewGuid()), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalCount);
    }

    [Fact]
    public async Task Handle_DefaultPaging_ReturnsFirstPage()
    {
        // Arrange — 3 facilities, default page=1 / pageSize=20, all 3 returned
        var tenantId = TenantId.New();
        IReadOnlyList<Facility> facilities = new List<Facility>
        {
            Facility.Create("HQ", tenantId),
            Facility.Create("Branch A", tenantId),
            Facility.Create("Branch B", tenantId)
        }.AsReadOnly();

        _repositoryMock
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<Facility>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        _repositoryMock
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<Facility>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(facilities);

        // Act
        var result = await _handler.Handle(
            new GetFacilitiesByTenantQuery(tenantId.Value, Page: 1, PageSize: 20),
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
        var tenantId = TenantId.New();
        IReadOnlyList<Facility> pageSlice = new List<Facility>
        {
            Facility.Create("HQ", tenantId),
            Facility.Create("Branch A", tenantId)
        }.AsReadOnly();

        _repositoryMock
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<Facility>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        _repositoryMock
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<Facility>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageSlice);

        // Act
        var result = await _handler.Handle(
            new GetFacilitiesByTenantQuery(tenantId.Value, Page: 1, PageSize: 2),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Equal(3, result.Value.TotalCount);
        Assert.Equal(1, result.Value.Page);
        Assert.Equal(2, result.Value.PageSize);
    }
}
