// SpaceOS.Kernel.Tests/Application/ListFlowEpicsQueryHandlerTests.cs
using Ardalis.Specification;
using Moq;
using SpaceOS.Kernel.Application.Tools.Queries;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Repositories;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

/// <summary>Tests for <see cref="ListFlowEpicsQueryHandler"/>.</summary>
public sealed class ListFlowEpicsQueryHandlerTests
{
    private readonly Mock<IFlowEpicRepository> _repository = new();
    private readonly ListFlowEpicsQueryHandler _handler;

    public ListFlowEpicsQueryHandlerTests()
    {
        _handler = new ListFlowEpicsQueryHandler(_repository.Object);
    }

    [Fact]
    public async Task Handle_NoEpics_ReturnsEmptyPagedList()
    {
        // Arrange
        _repository
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<FlowEpic>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _repository
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<FlowEpic>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var query = new ListFlowEpicsQuery(Guid.NewGuid(), 1, 20);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalCount);
        Assert.Equal(1, result.Value.Page);
        Assert.Equal(20, result.Value.PageSize);
    }

    [Fact]
    public async Task Handle_PageSizeExceedsMax_ClampsToFifty()
    {
        // Arrange
        _repository
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<FlowEpic>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _repository
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<FlowEpic>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var query = new ListFlowEpicsQuery(Guid.NewGuid(), 1, 999);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(50, result.Value.PageSize);
    }

    [Fact]
    public async Task Handle_PageBelowOne_NormalisesToOne()
    {
        // Arrange
        _repository
            .Setup(r => r.CountAsync(It.IsAny<ISpecification<FlowEpic>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _repository
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<FlowEpic>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var query = new ListFlowEpicsQuery(Guid.NewGuid(), -5, 20);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Page);
    }
}
