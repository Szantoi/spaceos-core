// SpaceOS.Kernel.Tests/Domain/OutboxStatusEnumTests.cs

using SpaceOS.Kernel.Domain.Enums;
using Xunit;

namespace SpaceOS.Kernel.Tests.Domain;

/// <summary>Tests verifying the <see cref="OutboxStatus"/> enum contract.</summary>
public sealed class OutboxStatusEnumTests
{
    [Theory]
    [InlineData(OutboxStatus.Pending,    "Pending")]
    [InlineData(OutboxStatus.Processing, "Processing")]
    [InlineData(OutboxStatus.Processed,  "Processed")]
    [InlineData(OutboxStatus.Dead,       "Dead")]
    public void OutboxStatus_ToString_ReturnsExpectedName(OutboxStatus status, string expectedName)
    {
        Assert.Equal(expectedName, status.ToString());
    }

    [Fact]
    public void OutboxStatus_PendingValue_IsZero()
    {
        Assert.Equal(0, (int)OutboxStatus.Pending);
    }

    [Fact]
    public void OutboxStatus_DeadValue_IsThree()
    {
        Assert.Equal(3, (int)OutboxStatus.Dead);
    }
}
