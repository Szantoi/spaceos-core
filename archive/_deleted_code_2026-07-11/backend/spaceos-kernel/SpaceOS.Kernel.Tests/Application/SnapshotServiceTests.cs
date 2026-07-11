// SpaceOS.Kernel.Tests/Application/SnapshotServiceTests.cs

using Moq;
using SpaceOS.Kernel.Application.Snapshots;
using SpaceOS.Kernel.Domain.Common;
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Snapshots;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

/// <summary>Unit tests for <see cref="SnapshotService"/>.</summary>
public sealed class SnapshotServiceTests
{
    private readonly Mock<IAggregateSnapshotRepository> _repository = new();
    private readonly SnapshotService _sut;

    private static readonly TenantId   SomeTenantId   = TenantId.From(Guid.NewGuid());
    private static readonly FacilityId SomeFacilityId = FacilityId.From(Guid.NewGuid());

    public SnapshotServiceTests()
    {
        _sut = new SnapshotService(_repository.Object);
    }

    // ── Version increment ────────────────────────────────────────────────────

    [Fact]
    public async Task TakeSnapshotAsync_NoExistingSnapshot_CreatesVersionOne()
    {
        // Arrange
        var epic = FlowEpic.Create("Epic", SomeFacilityId, SomeTenantId);
        AggregateSnapshot? captured = null;

        _repository.Setup(r => r.GetLatestAsync(epic.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AggregateSnapshot?)null);
        _repository.Setup(r => r.AddAsync(It.IsAny<AggregateSnapshot>(), It.IsAny<CancellationToken>()))
            .Callback<AggregateSnapshot, CancellationToken>((s, _) => captured = s)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.TakeSnapshotAsync(epic, AggregateType.FlowEpic, null, CancellationToken.None);

        // Assert
        Assert.NotNull(captured);
        Assert.Equal(1, captured!.Version);
    }

    [Fact]
    public async Task TakeSnapshotAsync_ExistingVersionTwo_CreatesVersionThree()
    {
        // Arrange
        var epic    = FlowEpic.Create("Epic", SomeFacilityId, SomeTenantId);
        var existing = AggregateSnapshot.Create(
            epic.Id.Value, "FlowEpic", 2, DateTimeOffset.UtcNow.AddMinutes(-1),
            Guid.NewGuid(), """{"v":2}""",
            "abcdabcdabcdabcdabcdabcdabcdabcdabcdabcdabcdabcdabcdabcdabcdabcd",
            SomeTenantId.Value);

        AggregateSnapshot? captured = null;

        _repository.Setup(r => r.GetLatestAsync(epic.Id.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _repository.Setup(r => r.AddAsync(It.IsAny<AggregateSnapshot>(), It.IsAny<CancellationToken>()))
            .Callback<AggregateSnapshot, CancellationToken>((s, _) => captured = s)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.TakeSnapshotAsync(epic, AggregateType.FlowEpic, null, CancellationToken.None);

        // Assert
        Assert.NotNull(captured);
        Assert.Equal(3, captured!.Version);
    }

    // ── Serialisation via ISnapshotable ──────────────────────────────────────

    [Fact]
    public async Task TakeSnapshotAsync_CallsAddAsync_WithNonEmptyStateJson()
    {
        // Arrange
        var epic = FlowEpic.Create("SnapshotMe", SomeFacilityId, SomeTenantId);
        AggregateSnapshot? captured = null;

        _repository.Setup(r => r.GetLatestAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AggregateSnapshot?)null);
        _repository.Setup(r => r.AddAsync(It.IsAny<AggregateSnapshot>(), It.IsAny<CancellationToken>()))
            .Callback<AggregateSnapshot, CancellationToken>((s, _) => captured = s)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.TakeSnapshotAsync(epic, AggregateType.FlowEpic, null, CancellationToken.None);

        // Assert: StateJson is not empty (proving private setters were captured via DTO)
        Assert.NotNull(captured);
        Assert.False(string.IsNullOrWhiteSpace(captured!.StateJson));
        Assert.NotEqual("{}", captured.StateJson.Trim());
    }

    [Fact]
    public async Task TakeSnapshotAsync_StateJson_ContainsTitleValue()
    {
        // Arrange
        var epic = FlowEpic.Create("CaptureMe", SomeFacilityId, SomeTenantId);
        AggregateSnapshot? captured = null;

        _repository.Setup(r => r.GetLatestAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AggregateSnapshot?)null);
        _repository.Setup(r => r.AddAsync(It.IsAny<AggregateSnapshot>(), It.IsAny<CancellationToken>()))
            .Callback<AggregateSnapshot, CancellationToken>((s, _) => captured = s)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.TakeSnapshotAsync(epic, AggregateType.FlowEpic, null, CancellationToken.None);

        // Assert
        Assert.Contains("CaptureMe", captured!.StateJson, StringComparison.Ordinal);
    }

    // ── Repository is called once ─────────────────────────────────────────────

    [Fact]
    public async Task TakeSnapshotAsync_CallsGetLatestAsync_Once()
    {
        var epic = FlowEpic.Create("Epic", SomeFacilityId, SomeTenantId);

        _repository.Setup(r => r.GetLatestAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AggregateSnapshot?)null);

        await _sut.TakeSnapshotAsync(epic, AggregateType.FlowEpic, null, CancellationToken.None);

        _repository.Verify(r => r.GetLatestAsync(epic.Id.Value, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TakeSnapshotAsync_CallsAddAsync_Once()
    {
        var epic = FlowEpic.Create("Epic", SomeFacilityId, SomeTenantId);

        _repository.Setup(r => r.GetLatestAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AggregateSnapshot?)null);

        await _sut.TakeSnapshotAsync(epic, AggregateType.FlowEpic, null, CancellationToken.None);

        _repository.Verify(r => r.AddAsync(It.IsAny<AggregateSnapshot>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
