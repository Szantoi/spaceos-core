using FluentAssertions;
using Xunit;
using SpaceOS.Modules.Contracts.Shared;

namespace SpaceOS.Modules.Contracts.Tests.Shared;

public class SourceChannelTests
{
    [Fact]
    public void SourceChannel_Direct_IsZero()
    {
        ((int)SourceChannel.Direct).Should().Be(0);
    }

    [Fact]
    public void SourceChannel_FreeTier_IsOne()
    {
        ((int)SourceChannel.FreeTier).Should().Be(1);
    }

    [Fact]
    public void SourceChannel_Partner_IsTwo()
    {
        ((int)SourceChannel.Partner).Should().Be(2);
    }

    [Fact]
    public void SourceChannel_Api_IsThree()
    {
        ((int)SourceChannel.Api).Should().Be(3);
    }

    [Fact]
    public void SourceChannel_HasExactlyFourValues()
    {
        Enum.GetValues<SourceChannel>().Should().HaveCount(4);
    }
}
