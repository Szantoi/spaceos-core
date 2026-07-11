// SpaceOS.Kernel.Tests/Application/GetSnapshotAtQueryHandlerTests.cs

using Ardalis.Result;
using Moq;
using SpaceOS.Kernel.Application.Snapshots.Queries;
using SpaceOS.Kernel.Domain.Snapshots;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

/// <summary>Unit tests for <see cref="GetSnapshotAtQueryHandler"/>.</summary>
public sealed class GetSnapshotAtQueryHandlerTests
{
    private readonly Mock<IAggregateSnapshotRepository> _repository = new();
    private readonly GetSnapshotAtQueryHandler _sut;

    public GetSnapshotAtQueryHandlerTests() =>
        _sut = new GetSnapshotAtQueryHandler(_repository.Object);

    // ── Snapshot exists ───────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_SnapshotExists_ReturnsSuccessWithDto()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var at          = DateTimeOffset.UtcNow;
        var snapshot    = BuildSnapshot(aggregateId);

        _repository
            .Setup(r => r.GetAtTimestampAsync(aggregateId, at, It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);

        var query = new GetSnapshotAtQuery(aggregateId, at);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(snapshot.Id,            result.Value.Id);
        Assert.Equal(snapshot.AggregateId,   result.Value.AggregateId);
        Assert.Equal(snapshot.AggregateType, result.Value.AggregateType);
        Assert.Equal(snapshot.Version,       result.Value.Version);
        Assert.Equal(snapshot.SnapshotAt,    result.Value.SnapshotAt);
        Assert.Equal(snapshot.StateJson,     result.Value.StateJson);
        Assert.Equal(snapshot.SnapshotHash,  result.Value.SnapshotHash);
        Assert.Equal(snapshot.TenantId,      result.Value.TenantId);
    }

    // ── Not found ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_SnapshotNotFound_ReturnsNotFound()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var at          = DateTimeOffset.UtcNow;

        _repository
            .Setup(r => r.GetAtTimestampAsync(aggregateId, at, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AggregateSnapshot?)null);

        var query = new GetSnapshotAtQuery(aggregateId, at);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static AggregateSnapshot BuildSnapshot(Guid aggregateId) =>
        AggregateSnapshot.Create(
            aggregateId:   aggregateId,
            aggregateType: "FlowEpic",
            version:       1,
            snapshotAt:    DateTimeOffset.UtcNow.AddMinutes(-5),
            triggerEventId: Guid.NewGuid(),
            stateJson:     """{"id":"test"}""",
            snapshotHash:  "abcdefabcdefabcdefabcdefabcdefabcdefabcdefabcdefabcdefabcdefabcd",
            tenantId:      Guid.NewGuid());
}
