using FluentAssertions;
using SpaceOS.Modules.Joinery.Domain.Entities;

namespace SpaceOS.Modules.Joinery.Tests.Outbox;

public class JoineryOutboxEntryTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private static JoineryOutboxEntry MakeEntry() =>
        JoineryOutboxEntry.Create(TenantId, "DoorItemCalculationRequested", "{}", Now);

    [Fact]
    public void Create_WithValidArgs_ReturnsNewEntry()
    {
        var entry = MakeEntry();

        entry.TenantId.Should().Be(TenantId);
        entry.EventType.Should().Be("DoorItemCalculationRequested");
        entry.PayloadJson.Should().Be("{}");
        entry.ProcessedAt.Should().BeNull();
        entry.FailedAt.Should().BeNull();
        entry.RetryCount.Should().Be(0);
    }

    [Fact]
    public void Create_WithEmptyEventType_Throws()
    {
        var act = () => JoineryOutboxEntry.Create(TenantId, "", "{}", Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyPayload_Throws()
    {
        var act = () => JoineryOutboxEntry.Create(TenantId, "SomeEvent", "", Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MarkProcessed_SetsProcessedAt()
    {
        var entry = MakeEntry();
        var processedAt = DateTimeOffset.UtcNow;

        entry.MarkProcessed(processedAt);

        entry.ProcessedAt.Should().Be(processedAt);
        entry.FailedAt.Should().BeNull();
    }

    [Fact]
    public void IncrementRetry_BelowMax_IncrementsCountOnly()
    {
        var entry = MakeEntry();

        entry.IncrementRetry("transient error", Now);

        entry.RetryCount.Should().Be(1);
        entry.FailedAt.Should().BeNull();
        entry.Error.Should().BeNull();
    }

    [Fact]
    public void IncrementRetry_AtMaxRetry_SetsFailedAt()
    {
        var entry = MakeEntry();
        entry.IncrementRetry("error 1", Now);
        entry.IncrementRetry("error 2", Now);

        entry.IncrementRetry("final error", Now);

        entry.RetryCount.Should().Be(3);
        entry.FailedAt.Should().NotBeNull();
        entry.Error.Should().Be("final error");
    }

    [Fact]
    public void IncrementRetry_TruncatesLongErrors()
    {
        var entry = MakeEntry();
        entry.IncrementRetry("e1", Now);
        entry.IncrementRetry("e2", Now);
        var longError = new string('x', 3000);

        entry.IncrementRetry(longError, Now);

        entry.Error.Should().HaveLength(2000);
    }
}
