using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Inventory.Application.Commands.ApproveOffcutReservation;
using SpaceOS.Modules.Inventory.Application.Commands.ReserveOffcut;
using SpaceOS.Modules.Inventory.Application.Commands.UseOffcutInJob;
using SpaceOS.Modules.Inventory.Application.Events;
using SpaceOS.Modules.Inventory.Application.Queries.GetOffcutList;
using SpaceOS.Modules.Inventory.Application.Queries.GetOffcutStatsSummary;
using SpaceOS.Modules.Inventory.Domain.Aggregates;
using SpaceOS.Modules.Inventory.Domain.Enums;
using SpaceOS.Modules.Inventory.Infrastructure.Persistence;
using SpaceOS.Modules.Inventory.Infrastructure.Repositories;
using Xunit;

namespace SpaceOS.Modules.Inventory.Tests.Application;

/// <summary>
/// End-to-end lifecycle validation: full domain stack (real handlers + real in-memory DB),
/// no HTTP mocking. Validates the complete Offcut Phase 1 flow end-to-end.
/// </summary>
public class OffcutLifecycleE2ETests : IDisposable
{
    private static readonly Guid TenantId  = Guid.NewGuid();
    private static readonly Guid CatalogId = Guid.NewGuid();
    private static readonly Guid JobId     = Guid.NewGuid();

    private readonly InventoryDbContext _db;
    private readonly InventoryRepository _repo;

    private readonly CuttingJobCompletedEventHandler _eventHandler;
    private readonly ReserveOffcutCommandHandler     _reserveHandler;
    private readonly ApproveOffcutReservationCommandHandler _approveHandler;
    private readonly UseOffcutInJobCommandHandler    _useHandler;
    private readonly GetOffcutListQueryHandler       _listHandler;
    private readonly GetOffcutStatsSummaryQueryHandler _statsHandler;

    public OffcutLifecycleE2ETests()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase($"E2E-{Guid.NewGuid()}")
            .Options;
        _db   = new InventoryDbContext(options);
        _repo = new InventoryRepository(_db);

        // Seed catalog entry
        var catalog = MaterialCatalog.Create("MDF 18mm", 2800, 2070, 18, 8500, "MDF-18", "MDF lap");
        typeof(MaterialCatalog).GetProperty("Id")!.SetValue(catalog, CatalogId);
        _db.MaterialCatalogs.Add(catalog);
        _db.SaveChanges();

        _eventHandler   = new CuttingJobCompletedEventHandler(_repo);
        _reserveHandler = new ReserveOffcutCommandHandler(_repo);
        _approveHandler = new ApproveOffcutReservationCommandHandler(_repo);
        _useHandler     = new UseOffcutInJobCommandHandler(_repo);
        _listHandler    = new GetOffcutListQueryHandler(_repo);
        _statsHandler   = new GetOffcutStatsSummaryQueryHandler(_repo);
    }

    public void Dispose() => _db.Dispose();

    // ── Full lifecycle ────────────────────────────────────────────────────────

    [Fact]
    public async Task FullLifecycle_CuttingJobCompleted_To_Used_AllStepsPass()
    {
        // Step 1 — CuttingJobCompleted → offcut registered (Available)
        var evt = new CuttingJobCompletedEvent(
            JobId, CatalogId, "MDF18mm", 1000m, 800m, 18m, 0.15m, TenantId);
        await _eventHandler.Handle(evt, default);

        // Step 2 — GET list → 1 offcut, status Available
        var listResult = await _listHandler.Handle(
            new GetOffcutListQuery(null, null, null, null, 1, 20), default);
        listResult.IsSuccess.Should().BeTrue();
        listResult.Value.Total.Should().Be(1);
        var offcutItem = listResult.Value.Offcuts[0];
        offcutItem.Status.Should().Be("Available");
        var offcutId = offcutItem.Id;

        // Step 3 — Reserve → 201 equivalent: reservation created, offcut still Available
        var reserveResult = await _reserveHandler.Handle(
            new ReserveOffcutCommand(offcutId, JobId, TenantId), default);
        reserveResult.IsSuccess.Should().BeTrue();
        reserveResult.Value.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        var reservationId = reserveResult.Value.ReservationId;

        // Step 4 — Approve → reservation Approved, offcut status → Reserved
        var approveResult = await _approveHandler.Handle(
            new ApproveOffcutReservationCommand(reservationId), default);
        approveResult.IsSuccess.Should().BeTrue();
        approveResult.Value.Status.Should().Be("Approved");

        var offcut = await _repo.GetOffcutByIdAsync(offcutId);
        offcut!.Status.Should().Be(OffcutStatus.Reserved);

        // Step 5 — Use → offcut status → Used
        var useResult = await _useHandler.Handle(
            new UseOffcutInJobCommand(offcutId, JobId), default);
        useResult.IsSuccess.Should().BeTrue();
        useResult.Value.Status.Should().Be("Used");
        useResult.Value.UsedInJobId.Should().Be(JobId);
        useResult.Value.UsedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Step 6 — Stats → usedCount=1, no available volume left
        var statsResult = await _statsHandler.Handle(new GetOffcutStatsSummaryQuery(), default);
        statsResult.IsSuccess.Should().BeTrue();
        statsResult.Value.UsedCount.Should().Be(1);
        statsResult.Value.ReservedCount.Should().Be(0);
        statsResult.Value.TotalAvailableVolumeM3.Should().Be(0m);
    }

    // ── Event handler accuracy ────────────────────────────────────────────────

    [Fact]
    public async Task EventHandler_ComputesCorrectVolumeAndStubDimensions()
    {
        // total = 1000*800*18/1e9 = 0.0144 m³, waste 15% = 0.00216 m³
        var expectedWaste = Offcut.ComputeVolume(1000m, 800m, 18m) * 0.15m;

        await _eventHandler.Handle(
            new CuttingJobCompletedEvent(JobId, CatalogId, "MDF18mm", 1000m, 800m, 18m, 0.15m, TenantId),
            default);

        var offcut = _db.Offcuts.Single();
        offcut.WidthMm.Should().Be(500m);
        offcut.HeightMm.Should().Be(400m);
        offcut.ThicknessMm.Should().Be(18m);
        offcut.VolumeM3.Should().Be(expectedWaste);
        offcut.WeightKg.Should().Be(expectedWaste * 750m);
        offcut.MaterialCode.Should().Be("MDF18mm");
        offcut.CuttingJobId.Should().Be(JobId);
    }

    // ── Stats aggregation ─────────────────────────────────────────────────────

    [Fact]
    public async Task StatsQuery_MixedStatuses_CorrectAggregation()
    {
        var available = Offcut.Register(TenantId, CatalogId, "MDF18mm", 500, 400, 18, 0m, 0m, null, null);
        var reserved  = Offcut.Register(TenantId, CatalogId, "MDF18mm", 500, 400, 18, 0m, 0m, null, null);
        reserved.Reserve();
        var used = Offcut.Register(TenantId, CatalogId, "HDF3mm", 300, 200, 3, 0m, 0m, null, null);
        used.MarkUsed(JobId);
        var scrapped = Offcut.Register(TenantId, CatalogId, "MDF18mm", 200, 100, 18, 0m, 0m, null, null);
        scrapped.Scrap();

        _db.Offcuts.AddRange(available, reserved, used, scrapped);
        await _db.SaveChangesAsync();

        var stats = (await _statsHandler.Handle(new GetOffcutStatsSummaryQuery(), default)).Value;

        stats.ReservedCount.Should().Be(1);
        stats.UsedCount.Should().Be(1);
        stats.ScrappedCount.Should().Be(1);
        stats.TotalAvailableVolumeM3.Should().Be(available.VolumeM3);
        stats.AvailableByMaterial.Should().ContainKey("MDF18mm");
        stats.AvailableByMaterial["MDF18mm"].VolumeM3.Should().Be(available.VolumeM3);
    }
}
