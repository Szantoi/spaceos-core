// SpaceOS.Kernel.Tests/Application/GetSnapshotVersionsQueryHandlerTests.cs

using Ardalis.Result;
using Moq;
using SpaceOS.Kernel.Application.Snapshots.Queries;
using SpaceOS.Kernel.Domain.Snapshots;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

/// <summary>Unit tests for <see cref="GetSnapshotVersionsQueryHandler"/>.</summary>
public sealed class GetSnapshotVersionsQueryHandlerTests
{
    private readonly Mock<IAggregateSnapshotRepository> _repository = new();
    private readonly GetSnapshotVersionsQueryHandler _sut;

    public GetSnapshotVersionsQueryHandlerTests() =>
        _sut = new GetSnapshotVersionsQueryHandler(_repository.Object);

    // ── Has versions ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_MultipleSnapshots_ReturnsDtoListInOrder()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var tenantId    = Guid.NewGuid();

        var snapshots = new List<AggregateSnapshot>
        {
            BuildSnapshot(aggregateId, tenantId, version: 1),
            BuildSnapshot(aggregateId, tenantId, version: 2),
            BuildSnapshot(aggregateId, tenantId, version: 3),
        }.AsReadOnly() as IReadOnlyList<AggregateSnapshot>;

        _repository
            .Setup(r => r.ListByAggregateAsync(aggregateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshots!);

        var query = new GetSnapshotVersionsQuery(aggregateId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(3, result.Value.Count);
        Assert.Equal(1, result.Value[0].Version);
        Assert.Equal(2, result.Value[1].Version);
        Assert.Equal(3, result.Value[2].Version);
    }

    [Fact]
    public async Task Handle_MultipleSnapshots_MapsAllDtoFieldsCorrectly()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var tenantId    = Guid.NewGuid();
        var snapshot    = BuildSnapshot(aggregateId, tenantId, version: 1);

        var snapshots = new List<AggregateSnapshot> { snapshot }.AsReadOnly() as IReadOnlyList<AggregateSnapshot>;

        _repository
            .Setup(r => r.ListByAggregateAsync(aggregateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshots!);

        var query = new GetSnapshotVersionsQuery(aggregateId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        var dto = result.Value[0];
        Assert.Equal(snapshot.Id,            dto.Id);
        Assert.Equal(snapshot.AggregateId,   dto.AggregateId);
        Assert.Equal(snapshot.AggregateType, dto.AggregateType);
        Assert.Equal(snapshot.Version,       dto.Version);
        Assert.Equal(snapshot.SnapshotAt,    dto.SnapshotAt);
        Assert.Equal(snapshot.StateJson,     dto.StateJson);
        Assert.Equal(snapshot.SnapshotHash,  dto.SnapshotHash);
        Assert.Equal(snapshot.TenantId,      dto.TenantId);
    }

    // ── Empty list ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_NoSnapshots_ReturnsEmptyList()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();

        _repository
            .Setup(r => r.ListByAggregateAsync(aggregateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AggregateSnapshot>().AsReadOnly() as IReadOnlyList<AggregateSnapshot>);

        var query = new GetSnapshotVersionsQuery(aggregateId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Empty(result.Value);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static AggregateSnapshot BuildSnapshot(Guid aggregateId, Guid tenantId, int version) =>
        AggregateSnapshot.Create(
            aggregateId:   aggregateId,
            aggregateType: "FlowEpic",
            version:       version,
            snapshotAt:    DateTimeOffset.UtcNow.AddMinutes(-version),
            triggerEventId: Guid.NewGuid(),
            stateJson:     """{"id":"test"}""",
            snapshotHash:  "abcdefabcdefabcdefabcdefabcdefabcdefabcdefabcdefabcdefabcdefabcd",
            tenantId:      tenantId);
}
