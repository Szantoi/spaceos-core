// SpaceOS.Kernel.Tests/Domain/DomainEventTests.cs

using SpaceOS.Kernel.Domain.Events;
using SpaceOS.Kernel.Domain.Primitives;
using Xunit;

namespace SpaceOS.Kernel.Tests.Domain;

/// <summary>
/// Tests verifying the new domain events introduced in Sprint D Phase 3B:
/// <see cref="AggregateSnapshotCreatedEvent"/> and <see cref="OutboxEntryDeadEvent"/>.
/// </summary>
public sealed class DomainEventTests
{
    // ── AggregateSnapshotCreatedEvent ─────────────────────────────────────────

    [Fact]
    public void AggregateSnapshotCreatedEvent_ImplementsIDomainEvent()
    {
        var evt = new AggregateSnapshotCreatedEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "FlowEpic", 1, "cafecafe" + new string('0', 56),
            DateTimeOffset.UtcNow);

        Assert.IsAssignableFrom<IDomainEvent>(evt);
    }

    [Fact]
    public void AggregateSnapshotCreatedEvent_SnapshotHash_IsPreserved()
    {
        const string hash = "abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789";
        var evt = new AggregateSnapshotCreatedEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "FlowEpic", 1, hash, DateTimeOffset.UtcNow);

        Assert.Equal(hash, evt.SnapshotHash);
    }

    [Fact]
    public void AggregateSnapshotCreatedEvent_AggregateType_IsPreserved()
    {
        var evt = new AggregateSnapshotCreatedEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "FlowMilestone", 2, "aaaa" + new string('b', 60),
            DateTimeOffset.UtcNow);

        Assert.Equal("FlowMilestone", evt.AggregateType);
    }

    // ── OutboxEntryDeadEvent ──────────────────────────────────────────────────

    [Fact]
    public void OutboxEntryDeadEvent_ImplementsIDomainEvent()
    {
        var evt = new OutboxEntryDeadEvent(
            Guid.NewGuid(), Guid.NewGuid(), "FlowEpicClosedDone", 5, DateTimeOffset.UtcNow);

        Assert.IsAssignableFrom<IDomainEvent>(evt);
    }

    [Fact]
    public void OutboxEntryDeadEvent_RetryCount_IsPreserved()
    {
        var evt = new OutboxEntryDeadEvent(
            Guid.NewGuid(), Guid.NewGuid(), "FlowEpicClosedDone", 7, DateTimeOffset.UtcNow);

        Assert.Equal(7, evt.RetryCount);
    }

    [Fact]
    public void OutboxEntryDeadEvent_EventType_IsPreserved()
    {
        var evt = new OutboxEntryDeadEvent(
            Guid.NewGuid(), Guid.NewGuid(), "SomeEventType", 3, DateTimeOffset.UtcNow);

        Assert.Equal("SomeEventType", evt.EventType);
    }
}
