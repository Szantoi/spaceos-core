using SpaceOS.Kernel.Domain.Exceptions;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.ValueObjects;

public class WorkStationTypeTests
{
    [Theory]
    [InlineData("Assembly")]
    [InlineData("  CNC  ")]
    public void From_WithValidType_ShouldCreateWorkStationType(string value)
    {
        var type = WorkStationType.From(value);
        Assert.Equal(value.Trim(), type.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void From_WithNullOrWhiteSpace_ShouldThrowDomainException(string value)
    {
        Assert.Throws<DomainException>(() => WorkStationType.From(value));
    }

    [Fact]
    public void From_WithTypeTooLong_ShouldThrowDomainException()
    {
        var longType = new string('x', 51);
        Assert.Throws<DomainException>(() => WorkStationType.From(longType));
    }
}
