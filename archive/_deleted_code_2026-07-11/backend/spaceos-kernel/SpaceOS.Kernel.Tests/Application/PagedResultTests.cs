// SpaceOS.Kernel.Tests/Application/PagedResultTests.cs
using SpaceOS.Kernel.Application.Common;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

/// <summary>Unit tests for <see cref="PagedResult{T}"/> computed properties.</summary>
public sealed class PagedResultTests
{
    [Fact]
    public void TotalPages_ExactDivision_ReturnsCorrectCount()
    {
        // Arrange
        var result = new PagedList<int>(Array.Empty<int>(), Page: 1, PageSize: 10, TotalCount: 20);

        // Act & Assert
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public void TotalPages_WithRemainder_RoundsUp()
    {
        // Arrange
        var result = new PagedList<int>(Array.Empty<int>(), Page: 1, PageSize: 10, TotalCount: 21);

        // Act & Assert
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public void TotalPages_ZeroItems_ReturnsZero()
    {
        // Arrange
        var result = new PagedList<int>(Array.Empty<int>(), Page: 1, PageSize: 20, TotalCount: 0);

        // Act & Assert
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public void PagedResult_ExposesAllProperties()
    {
        // Arrange
        IReadOnlyList<string> items = new List<string> { "a", "b" }.AsReadOnly();
        var result = new PagedList<string>(items, Page: 2, PageSize: 5, TotalCount: 12);

        // Assert
        Assert.Equal(items, result.Items);
        Assert.Equal(2, result.Page);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(12, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
    }
}
