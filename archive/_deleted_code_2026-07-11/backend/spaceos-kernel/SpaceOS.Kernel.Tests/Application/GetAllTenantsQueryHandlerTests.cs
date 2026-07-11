// SpaceOS.Kernel.Tests/Application/GetAllTenantsQueryHandlerTests.cs
using Ardalis.Specification;
using Moq;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.Tenants.Queries;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Repositories;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

/// <summary>Unit tests for <see cref="GetAllTenantsQueryHandler"/>.</summary>
public sealed class GetAllTenantsQueryHandlerTests
{
    private readonly Mock<ITenantRepository> _repositoryMock = new();
    private readonly GetAllTenantsQueryHandler _handler;

    public GetAllTenantsQueryHandlerTests() =>
        _handler = new GetAllTenantsQueryHandler(_repositoryMock.Object);

    [Fact]
    public async Task Handle_WhenTenantsExist_ReturnsPagedResult()
    {
        // Arrange
        IReadOnlyList<Tenant> tenants = new List<Tenant>
        {
            Tenant.Create("ACME Corp"),
            Tenant.Create("Globex")
        }.AsReadOnly();

        _repositoryMock
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<Tenant>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        _repositoryMock
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<Tenant>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenants);

        // Act
        var result = await _handler.Handle(new GetAllTenantsQuery(), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Contains(result.Value.Items, d => d.Name == "ACME Corp");
    }

    [Fact]
    public async Task Handle_WhenNoTenantsExist_ReturnsEmptyPagedResult()
    {
        // Arrange
        IReadOnlyList<Tenant> emptyList = new List<Tenant>().AsReadOnly();

        _repositoryMock
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<Tenant>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _repositoryMock
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<Tenant>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyList);

        // Act
        var result = await _handler.Handle(new GetAllTenantsQuery(), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalCount);
    }

    [Fact]
    public async Task Handle_DefaultPaging_ReturnsPage1WithDefaultPageSize()
    {
        // Arrange
        IReadOnlyList<Tenant> tenants = new List<Tenant>
        {
            Tenant.Create("Tenant A"),
            Tenant.Create("Tenant B"),
            Tenant.Create("Tenant C")
        }.AsReadOnly();

        _repositoryMock
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<Tenant>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        _repositoryMock
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<Tenant>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenants);

        // Act
        var result = await _handler.Handle(new GetAllTenantsQuery(), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Page);
        Assert.Equal(20, result.Value.PageSize);
        Assert.Equal(3, result.Value.TotalCount);
    }

    [Fact]
    public async Task Handle_DefaultPaging_ReturnsFirstPage()
    {
        // Arrange — 3 items seeded, default page=1 / pageSize=20, all 3 returned
        IReadOnlyList<Tenant> tenants = new List<Tenant>
        {
            Tenant.Create("Alpha"),
            Tenant.Create("Beta"),
            Tenant.Create("Gamma")
        }.AsReadOnly();

        _repositoryMock
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<Tenant>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        _repositoryMock
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<Tenant>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenants);

        // Act
        var result = await _handler.Handle(new GetAllTenantsQuery(Page: 1, PageSize: 20), CancellationToken.None);

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
        IReadOnlyList<Tenant> pageSlice = new List<Tenant>
        {
            Tenant.Create("Alpha"),
            Tenant.Create("Beta")
        }.AsReadOnly();

        _repositoryMock
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<Tenant>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        _repositoryMock
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<Tenant>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageSlice);

        // Act
        var result = await _handler.Handle(new GetAllTenantsQuery(Page: 1, PageSize: 2), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Equal(3, result.Value.TotalCount);
        Assert.Equal(1, result.Value.Page);
        Assert.Equal(2, result.Value.PageSize);
    }
}
