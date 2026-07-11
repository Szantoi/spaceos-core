using FluentAssertions;
using SpaceOS.Modules.Sales.Domain.Common;
using SpaceOS.Modules.Sales.Infrastructure.Outbox;
using SpaceOS.Modules.Sales.Infrastructure.Security;
using SpaceOS.Modules.Sales.Tests.Helpers;
using Xunit;

namespace SpaceOS.Modules.Sales.Tests.Security;

public class OutboxWorkerTests
{
    private readonly FakeClock _clock = new();

    private static OutboxMessage BuildPending(FakeClock clock) =>
        OutboxMessage.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            "QuoteConversionRequested", "{}", "idempotency-key-1", clock);

    [Fact]
    public void MarkInFlight_SetsStatusToInFlight()
    {
        var msg = BuildPending(_clock);

        msg.MarkInFlight(_clock);

        msg.Status.Should().Be("InFlight");
        msg.AttemptCount.Should().Be(1);
    }

    [Fact]
    public void MarkCompleted_SetsStatusToCompleted()
    {
        var msg = BuildPending(_clock);
        msg.MarkInFlight(_clock);

        msg.MarkCompleted(_clock);

        msg.Status.Should().Be("Completed");
        msg.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public void RecordFailure_BelowMax_SetsPending()
    {
        var msg = BuildPending(_clock);
        msg.MarkInFlight(_clock); // AttemptCount = 1

        msg.RecordFailure("System.Net.Http.HttpRequestException", _clock, maxAttempts: 3);

        msg.Status.Should().Be("Pending");
        msg.LastError.Should().Be("System.Net.Http.HttpRequestException");
    }

    [Fact]
    public void RecordFailure_AtMaxAttempts_SetsFailed()
    {
        var msg = BuildPending(_clock);
        // Reach AttemptCount = maxAttempts by marking in-flight that many times
        for (var i = 0; i < 3; i++) msg.MarkInFlight(_clock);

        msg.RecordFailure("System.TimeoutException", _clock, maxAttempts: 3);

        msg.Status.Should().Be("Failed");
    }

    [Fact]
    public void WorkerTenantContext_HasCorrectTenantId()
    {
        var expected = Guid.NewGuid();
        var ctx = new WorkerTenantContext(expected);

        ctx.TenantId.Should().Be(expected);
        ctx.ActorSub.Should().Be("worker:integration");
    }

    [Fact]
    public void OutboxMessage_Create_HasPendingStatus()
    {
        var msg = BuildPending(_clock);

        msg.Status.Should().Be("Pending");
        msg.AttemptCount.Should().Be(0);
        msg.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public void OutboxMessage_MarkInFlight_IncreasesAttemptCount()
    {
        var msg = BuildPending(_clock);

        msg.MarkInFlight(_clock);
        msg.MarkInFlight(_clock);

        msg.AttemptCount.Should().Be(2);
    }
}
