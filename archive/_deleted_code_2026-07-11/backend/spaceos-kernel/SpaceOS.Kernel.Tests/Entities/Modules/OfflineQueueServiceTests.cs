// SpaceOS.Kernel.Tests/Entities/Modules/OfflineQueueServiceTests.cs
using SpaceOS.Modules.FlowManagement.Services;
using Xunit;

namespace SpaceOS.Kernel.Tests.Entities.Modules;

/// <summary>Unit tests for <see cref="OfflineQueueService"/> static helper methods.</summary>
public sealed class OfflineQueueServiceTests
{
    // --- GetBackoffDelay: exponential back-off formula min(2^retryCount, 60s) ---

    [Fact]
    public void GetBackoffDelay_RetryCount0_Returns1Second()
    {
        var delay = OfflineQueueService.GetBackoffDelay(0);

        Assert.Equal(TimeSpan.FromSeconds(1), delay);
    }

    [Fact]
    public void GetBackoffDelay_RetryCount1_Returns2Seconds()
    {
        var delay = OfflineQueueService.GetBackoffDelay(1);

        Assert.Equal(TimeSpan.FromSeconds(2), delay);
    }

    [Fact]
    public void GetBackoffDelay_RetryCount2_Returns4Seconds()
    {
        var delay = OfflineQueueService.GetBackoffDelay(2);

        Assert.Equal(TimeSpan.FromSeconds(4), delay);
    }

    [Fact]
    public void GetBackoffDelay_RetryCount6_Returns60Seconds_Cap()
    {
        // 2^6 = 64, capped at 60
        var delay = OfflineQueueService.GetBackoffDelay(6);

        Assert.Equal(TimeSpan.FromSeconds(60), delay);
    }

    [Fact]
    public void GetBackoffDelay_LargeRetryCount_NeverExceedsCap()
    {
        var delay = OfflineQueueService.GetBackoffDelay(100);

        Assert.Equal(TimeSpan.FromSeconds(60), delay);
    }

    [Theory]
    [InlineData(3, 8)]
    [InlineData(4, 16)]
    [InlineData(5, 32)]
    public void GetBackoffDelay_RetryCountN_Returns2ToThePowerNSeconds(int retryCount, int expectedSeconds)
    {
        var delay = OfflineQueueService.GetBackoffDelay(retryCount);

        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), delay);
    }
}
