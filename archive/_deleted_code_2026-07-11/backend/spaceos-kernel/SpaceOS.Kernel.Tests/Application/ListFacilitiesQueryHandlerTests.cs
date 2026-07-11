// SpaceOS.Kernel.Tests/Application/ListFacilitiesQueryHandlerTests.cs
using Ardalis.Specification;
using Moq;
using SpaceOS.Kernel.Application.Tools.Queries;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Repositories;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

/// <summary>Tests for <see cref="ListFacilitiesQueryHandler"/>.</summary>
public sealed class ListFacilitiesQueryHandlerTests
{
    private readonly Mock<IFacilityRepository> _repository = new();
    private readonly ListFacilitiesQueryHandler _handler;

    public ListFacilitiesQueryHandlerTests()
    {
        _handler = new ListFacilitiesQueryHandler(_repository.Object);
    }

    [Fact]
    public async Task Handle_NoFacilities_ReturnsEmptyPagedList()
    {
        // Arrange
        _repository
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<Facility>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _repository
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<Facility>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var query = new ListFacilitiesQuery(Guid.NewGuid(), 1, 20);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalCount);
    }

    [Fact]
    public async Task Handle_PageSizeExceedsMax_ClampsToFifty()
    {
        // Arrange
        _repository
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<Facility>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _repository
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<Facility>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var query = new ListFacilitiesQuery(Guid.NewGuid(), 1, 100);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(50, result.Value.PageSize);
    }
}
