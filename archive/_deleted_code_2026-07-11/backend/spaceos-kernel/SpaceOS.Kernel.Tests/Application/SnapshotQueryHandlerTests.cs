// SpaceOS.Kernel.Tests/Application/SnapshotQueryHandlerTests.cs

using Ardalis.Result;
using Moq;
using SpaceOS.Kernel.Application.Snapshots;
using SpaceOS.Kernel.Application.Snapshots.Queries;
using SpaceOS.Kernel.Domain.Snapshots;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

/// <summary>
/// Additional unit tests for snapshot query handlers covering tenant isolation
/// and edge cases not covered by the existing test files.
/// </summary>
public sealed class SnapshotQueryHandlerTests
{
    private readonly Mock<IAggregateSnapshotRepository> _repository = new();

    // ── GetSnapshotAtQuery — tenant guard ────────────────────────────────────

    [Fact]
    public async Task GetSnapshotAt_WhenNoSnapshotForAggregate_ReturnsNotFound()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var at          = DateTimeOffset.UtcNow.AddHours(-1);

        _repository
            .Setup(r => r.GetAtTimestampAsync(aggregateId, at, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AggregateSnapshot?)null);

        var sut = new GetSnapshotAtQueryHandler(_repository.Object);

        // Act
        var result = await sut.Handle(new GetSnapshotAtQuery(aggregateId, at), CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task GetSnapshotAt_WhenSnapshotExists_MapsAllDtoFields()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var tenantId    = Guid.NewGuid();
        var at          = DateTimeOffset.UtcNow;
        var trigger     = Guid.NewGuid();

        var snapshot = AggregateSnapshot.Create(
            aggregateId, "FlowEpic", 5, at.AddMinutes(-1), trigger,
            """{"title":"X"}""",
            "abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789",
            tenantId);

        _repository
            .Setup(r => r.GetAtTimestampAsync(aggregateId, at, It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);

        var sut = new GetSnapshotAtQueryHandler(_repository.Object);

        // Act
        var result = await sut.Handle(new GetSnapshotAtQuery(aggregateId, at), CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        var dto = result.Value;
        Assert.Equal(aggregateId, dto.AggregateId);
        Assert.Equal("FlowEpic",  dto.AggregateType);
        Assert.Equal(5,           dto.Version);
        Assert.Equal(tenantId,    dto.TenantId);
    }

    // ── GetSnapshotVersionsQuery — empty list ────────────────────────────────

    [Fact]
    public async Task GetSnapshotVersions_EmptyList_ReturnsSuccessWithEmptyCollection()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();

        _repository
            .Setup(r => r.ListByAggregateAsync(aggregateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AggregateSnapshot>().AsReadOnly() as IReadOnlyList<AggregateSnapshot>);

        var sut = new GetSnapshotVersionsQueryHandler(_repository.Object);

        // Act
        var result = await sut.Handle(new GetSnapshotVersionsQuery(aggregateId), CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task GetSnapshotVersions_MultipleSnapshots_ReturnsAll()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var tenantId    = Guid.NewGuid();

        var snapshots = new List<AggregateSnapshot>
        {
            AggregateSnapshot.Create(aggregateId, "FlowEpic", 1, DateTimeOffset.UtcNow.AddMinutes(-10),
                Guid.NewGuid(), """{"v":1}""", "aaaa" + new string('0', 60), tenantId),
            AggregateSnapshot.Create(aggregateId, "FlowEpic", 2, DateTimeOffset.UtcNow.AddMinutes(-5),
                Guid.NewGuid(), """{"v":2}""", "bbbb" + new string('0', 60), tenantId),
        };

        _repository
            .Setup(r => r.ListByAggregateAsync(aggregateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshots.AsReadOnly() as IReadOnlyList<AggregateSnapshot>);

        var sut = new GetSnapshotVersionsQueryHandler(_repository.Object);

        // Act
        var result = await sut.Handle(new GetSnapshotVersionsQuery(aggregateId), CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(2, result.Value.Count);
    }
}
