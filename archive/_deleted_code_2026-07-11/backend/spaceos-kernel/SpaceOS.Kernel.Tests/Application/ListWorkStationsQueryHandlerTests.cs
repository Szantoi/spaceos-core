// SpaceOS.Kernel.Tests/Application/ListWorkStationsQueryHandlerTests.cs
using Ardalis.Specification;
using Moq;
using SpaceOS.Kernel.Application.Tools.Queries;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Repositories;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

/// <summary>Tests for <see cref="ListWorkStationsQueryHandler"/>.</summary>
public sealed class ListWorkStationsQueryHandlerTests
{
    private readonly Mock<IWorkStationRepository> _repository = new();
    private readonly ListWorkStationsQueryHandler _handler;

    public ListWorkStationsQueryHandlerTests()
    {
        _handler = new ListWorkStationsQueryHandler(_repository.Object);
    }

    [Fact]
    public async Task Handle_NoWorkStations_ReturnsEmptyPagedList()
    {
        // Arrange
        _repository
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<WorkStation>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _repository
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<WorkStation>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var query = new ListWorkStationsQuery(Guid.NewGuid(), 1, 20);

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
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<WorkStation>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _repository
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<WorkStation>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var query = new ListWorkStationsQuery(Guid.NewGuid(), 1, 200);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(50, result.Value.PageSize);
    }
}
