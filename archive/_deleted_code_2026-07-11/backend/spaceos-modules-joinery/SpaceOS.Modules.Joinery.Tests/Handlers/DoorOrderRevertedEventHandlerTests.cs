using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SpaceOS.Modules.Joinery.Application.Common;
using SpaceOS.Modules.Joinery.Domain.Aggregates;
using SpaceOS.Modules.Joinery.Domain.Entities;
using SpaceOS.Modules.Joinery.Domain.Enums;
using SpaceOS.Modules.Joinery.Domain.Events;
using SpaceOS.Modules.Joinery.Domain.ValueObjects;
using SpaceOS.Modules.Joinery.Infrastructure.EventHandlers;
using SpaceOS.Modules.Joinery.Infrastructure.Persistence;

namespace SpaceOS.Modules.Joinery.Tests.Handlers;

public class DoorOrderRevertedEventHandlerTests : IDisposable
{
    private readonly JoineryDbContext _db;
    private readonly DoorOrderRevertedEventHandler _sut;
    private readonly string _tempDir;

    private static readonly Guid TenantId = Guid.NewGuid();

    public DoorOrderRevertedEventHandlerTests()
    {
        var opts = new DbContextOptionsBuilder<JoineryDbContext>()
            .UseInMemoryDatabase($"revert-event-tests-{Guid.NewGuid()}")
            .Options;
        _db = new JoineryDbContext(opts);

        _tempDir = Path.Combine(Path.GetTempPath(), $"joinery_revert_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        _sut = new DoorOrderRevertedEventHandler(
            _db,
            NullLogger<DoorOrderRevertedEventHandler>.Instance);
    }

    public void Dispose()
    {
        _db.Dispose();
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static DoorOrder MakeCalculatedOrder(Guid tenantId)
    {
        var order = DoorOrder.Create(tenantId, "PRJ-REVERT", "Revert Event", Guid.NewGuid()).Value;
        var dims = DoorDimensions.Create(900m, 850m, 2100m, 2050m, 200m, 180m).Value;
        order.AddItem(DoorItem.Create(order.Id, "R01", 1, DoorType.FAF_T, OpeningDirection.Left, dims));
        order.Submit();
        order.MarkCalculating();
        order.MarkCalculated();
        return order;
    }

    private static CuttingListSnapshot MakeSnapshot(Guid orderId, Guid itemId)
    {
        var lines = new List<CuttingListLine>
        {
            new("Frame", "Frame", 900m, 100m, 902m, 102m, "MDF", 18m, 1, 1)
        };
        return CuttingListSnapshot.Create(
            TenantId, orderId, itemId,
            "template-v1", 1,
            900m, 2100m,
            null,
            DateTimeOffset.UtcNow,
            lines);
    }

    private async Task<string> CreatePdfFile(string content = "fake-pdf-content")
    {
        var path = Path.Combine(_tempDir, $"test_{Guid.NewGuid():N}.pdf");
        await File.WriteAllTextAsync(path, content);
        return path;
    }

    private DomainEventNotification<DoorOrderReverted> MakeNotification(Guid orderId) =>
        new(new DoorOrderReverted(orderId, TenantId));

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_NoSnapshotsNoCache_CompletesWithoutError()
    {
        var orderId = Guid.NewGuid();

        var act = () => _sut.Handle(MakeNotification(orderId), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_WithLatestSnapshot_DemotesSnapshotToNotLatest()
    {
        var order = MakeCalculatedOrder(TenantId);
        _db.DoorOrders.Add(order);
        var snapshot = MakeSnapshot(order.Id, order.Items[0].Id);
        _db.CuttingListSnapshots.Add(snapshot);
        await _db.SaveChangesAsync();

        snapshot.IsLatest.Should().BeTrue();

        await _sut.Handle(MakeNotification(order.Id), CancellationToken.None);

        var reloaded = await _db.CuttingListSnapshots.FindAsync(snapshot.Id);
        reloaded!.IsLatest.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithPdfCache_RemovesCacheRecord()
    {
        var order = MakeCalculatedOrder(TenantId);
        _db.DoorOrders.Add(order);
        var snapshot = MakeSnapshot(order.Id, order.Items[0].Id);
        _db.CuttingListSnapshots.Add(snapshot);

        var filePath = await CreatePdfFile();
        var cache = ProductionSheetCache.Create(TenantId, snapshot.Id, filePath, "abc123", DateTimeOffset.UtcNow);
        _db.ProductionSheetCaches.Add(cache);
        await _db.SaveChangesAsync();

        await _sut.Handle(MakeNotification(order.Id), CancellationToken.None);

        var remaining = await _db.ProductionSheetCaches.CountAsync();
        remaining.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithPdfCache_DeletesPdfFileFromDisk()
    {
        var order = MakeCalculatedOrder(TenantId);
        _db.DoorOrders.Add(order);
        var snapshot = MakeSnapshot(order.Id, order.Items[0].Id);
        _db.CuttingListSnapshots.Add(snapshot);

        var filePath = await CreatePdfFile();
        var cache = ProductionSheetCache.Create(TenantId, snapshot.Id, filePath, "abc123", DateTimeOffset.UtcNow);
        _db.ProductionSheetCaches.Add(cache);
        await _db.SaveChangesAsync();

        await _sut.Handle(MakeNotification(order.Id), CancellationToken.None);

        File.Exists(filePath).Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithCacheButMissingFile_RemovesCacheRecordWithoutThrowing()
    {
        var order = MakeCalculatedOrder(TenantId);
        _db.DoorOrders.Add(order);
        var snapshot = MakeSnapshot(order.Id, order.Items[0].Id);
        _db.CuttingListSnapshots.Add(snapshot);

        // FilePath points to a non-existent file
        var nonExistentPath = Path.Combine(_tempDir, "already_deleted.pdf");
        var cache = ProductionSheetCache.Create(TenantId, snapshot.Id, nonExistentPath, "abc123", DateTimeOffset.UtcNow);
        _db.ProductionSheetCaches.Add(cache);
        await _db.SaveChangesAsync();

        var act = () => _sut.Handle(MakeNotification(order.Id), CancellationToken.None);
        await act.Should().NotThrowAsync();

        var remaining = await _db.ProductionSheetCaches.CountAsync();
        remaining.Should().Be(0);
    }

    [Fact]
    public async Task Handle_SnapshotsOfDifferentOrder_AreNotAffected()
    {
        var order1 = MakeCalculatedOrder(TenantId);
        var order2 = MakeCalculatedOrder(TenantId);
        _db.DoorOrders.Add(order1);
        _db.DoorOrders.Add(order2);

        var snapshot1 = MakeSnapshot(order1.Id, order1.Items[0].Id);
        var snapshot2 = MakeSnapshot(order2.Id, order2.Items[0].Id);
        _db.CuttingListSnapshots.Add(snapshot1);
        _db.CuttingListSnapshots.Add(snapshot2);
        await _db.SaveChangesAsync();

        // Only revert order1
        await _sut.Handle(MakeNotification(order1.Id), CancellationToken.None);

        var reloaded2 = await _db.CuttingListSnapshots.FindAsync(snapshot2.Id);
        reloaded2!.IsLatest.Should().BeTrue("order2 snapshot should not be affected");
    }
}
