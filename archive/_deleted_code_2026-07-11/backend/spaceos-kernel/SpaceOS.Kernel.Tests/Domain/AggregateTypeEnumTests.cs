// SpaceOS.Kernel.Tests/Domain/AggregateTypeEnumTests.cs

using SpaceOS.Kernel.Domain.Enums;
using Xunit;

namespace SpaceOS.Kernel.Tests.Domain;

/// <summary>
/// Tests verifying the <see cref="AggregateType"/> enum contract.
/// The 'Other' value must NOT exist — aggregate type enumeration must be exhaustive.
/// </summary>
public sealed class AggregateTypeEnumTests
{
    [Fact]
    public void AggregateType_DoesNotContain_OtherValue()
    {
        var names = Enum.GetNames<AggregateType>();
        Assert.DoesNotContain("Other", names);
    }

    [Fact]
    public void AggregateType_ContainsFlowEpic()
    {
        Assert.True(Enum.IsDefined(typeof(AggregateType), AggregateType.FlowEpic));
    }

    [Fact]
    public void AggregateType_ContainsFlowMilestone()
    {
        Assert.True(Enum.IsDefined(typeof(AggregateType), AggregateType.FlowMilestone));
    }

    [Fact]
    public void AggregateType_ContainsB2BHandshake()
    {
        Assert.True(Enum.IsDefined(typeof(AggregateType), AggregateType.B2BHandshake));
    }

    [Fact]
    public void AggregateType_ContainsSpaceLayer()
    {
        Assert.True(Enum.IsDefined(typeof(AggregateType), AggregateType.SpaceLayer));
    }

    [Theory]
    [InlineData(AggregateType.FlowEpic,     "FlowEpic")]
    [InlineData(AggregateType.FlowMilestone, "FlowMilestone")]
    [InlineData(AggregateType.B2BHandshake,  "B2BHandshake")]
    [InlineData(AggregateType.SpaceLayer,    "SpaceLayer")]
    public void AggregateType_ToString_ReturnsExpectedName(AggregateType type, string expectedName)
    {
        Assert.Equal(expectedName, type.ToString());
    }
}
