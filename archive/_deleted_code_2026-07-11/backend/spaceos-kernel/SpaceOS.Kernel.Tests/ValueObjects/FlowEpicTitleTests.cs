using SpaceOS.Kernel.Domain.Exceptions;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.ValueObjects;

public class FlowEpicTitleTests
{
    [Theory]
    [InlineData("Kitchen Manufacturing")]
    [InlineData("  trimmed title  ")]
    public void From_WithValidTitle_ShouldCreateFlowEpicTitle(string value)
    {
        var title = FlowEpicTitle.From(value);
        Assert.Equal(value.Trim(), title.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void From_WithNullOrWhiteSpace_ShouldThrowDomainException(string value)
    {
        Assert.Throws<DomainException>(() => FlowEpicTitle.From(value));
    }

    [Fact]
    public void From_WithTitleTooLong_ShouldThrowDomainException()
    {
        var longTitle = new string('x', 201);
        Assert.Throws<DomainException>(() => FlowEpicTitle.From(longTitle));
    }
}
