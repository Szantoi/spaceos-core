// SpaceOS.Kernel.Tests/Domain/AggregateSnapshotTests.cs

using SpaceOS.Kernel.Domain.Snapshots;
using Xunit;

namespace SpaceOS.Kernel.Tests.Domain;

/// <summary>Unit tests for <see cref="AggregateSnapshot"/> creation invariants.</summary>
public sealed class AggregateSnapshotTests
{
    private static readonly Guid SomeTenantId    = Guid.NewGuid();
    private static readonly Guid SomeAggregateId = Guid.NewGuid();

    // ── Create — basic ───────────────────────────────────────────────────────

    [Fact]
    public void Create_ValidArguments_AssignsNonEmptyId()
    {
        // Act
        var snapshot = BuildSnapshot(SomeAggregateId, """{"id":"x"}""");

        // Assert
        Assert.NotEqual(Guid.Empty, snapshot.Id);
    }

    [Fact]
    public void Create_ValidArguments_SetsAggregateId()
    {
        var snapshot = BuildSnapshot(SomeAggregateId, """{"id":"x"}""");
        Assert.Equal(SomeAggregateId, snapshot.AggregateId);
    }

    [Fact]
    public void Create_ValidArguments_SetsVersion()
    {
        var snapshot = BuildSnapshot(SomeAggregateId, """{"id":"x"}""", version: 3);
        Assert.Equal(3, snapshot.Version);
    }

    [Fact]
    public void Create_ValidArguments_SetsStateJson()
    {
        const string json = """{"title":"test","phase":"Discovery"}""";
        var snapshot = BuildSnapshot(SomeAggregateId, json);
        Assert.Equal(json, snapshot.StateJson);
    }

    [Fact]
    public void Create_ValidArguments_SetsSnapshotHash()
    {
        var snapshot = BuildSnapshot(SomeAggregateId, """{"id":"x"}""");
        Assert.NotEmpty(snapshot.SnapshotHash);
    }

    [Fact]
    public void Create_ValidArguments_SetsAggregateType()
    {
        var snapshot = BuildSnapshot(SomeAggregateId, """{"id":"x"}""");
        Assert.Equal("FlowEpic", snapshot.AggregateType);
    }

    [Fact]
    public void Create_ValidArguments_SetsTenantId()
    {
        var snapshot = BuildSnapshot(SomeAggregateId, """{"id":"x"}""");
        Assert.Equal(SomeTenantId, snapshot.TenantId);
    }

    // ── Validation ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyAggregateType_ThrowsArgumentException(string type)
    {
        Assert.ThrowsAny<ArgumentException>(() => AggregateSnapshot.Create(
            SomeAggregateId, type, 1, DateTimeOffset.UtcNow, Guid.NewGuid(),
            """{"id":"x"}""", "aaaa", SomeTenantId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyStateJson_ThrowsArgumentException(string json)
    {
        Assert.ThrowsAny<ArgumentException>(() => AggregateSnapshot.Create(
            SomeAggregateId, "FlowEpic", 1, DateTimeOffset.UtcNow, Guid.NewGuid(),
            json, "aaaa", SomeTenantId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptySnapshotHash_ThrowsArgumentException(string hash)
    {
        Assert.ThrowsAny<ArgumentException>(() => AggregateSnapshot.Create(
            SomeAggregateId, "FlowEpic", 1, DateTimeOffset.UtcNow, Guid.NewGuid(),
            """{"id":"x"}""", hash, SomeTenantId));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static AggregateSnapshot BuildSnapshot(
        Guid aggregateId,
        string stateJson,
        int version = 1)
        => AggregateSnapshot.Create(
            aggregateId:    aggregateId,
            aggregateType:  "FlowEpic",
            version:        version,
            snapshotAt:     DateTimeOffset.UtcNow,
            triggerEventId: Guid.NewGuid(),
            stateJson:      stateJson,
            snapshotHash:   "abcdefabcdefabcdefabcdefabcdefabcdefabcdefabcdefabcdefabcdefabcd",
            tenantId:       SomeTenantId);
}
