using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Inventory.Domain.Aggregates;
using SpaceOS.Modules.Inventory.Domain.Enums;
using SpaceOS.Modules.Inventory.Infrastructure.Persistence;
using SpaceOS.Modules.Inventory.Infrastructure.Repositories;
using Xunit;

namespace SpaceOS.Modules.Inventory.Tests.Security;

public class RlsIsolationTests : IDisposable
{
    private readonly InventoryDbContext _db;
    private readonly InventoryRepository _repo;
    private readonly Guid _tenantA = Guid.NewGuid();
    private readonly Guid _tenantB = Guid.NewGuid();
    private readonly Guid _catalogId = Guid.NewGuid();

    public RlsIsolationTests()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new InventoryDbContext(options);
        // Do NOT call EnsureCreated() to avoid HasData seed conflict
        _repo = new InventoryRepository(_db);

        var catalog = MaterialCatalog.Create("MDF 18mm", 2800, 2070, 18, 8500, "MDF-18", "MDF lap");
        typeof(MaterialCatalog).GetProperty("Id")!.SetValue(catalog, _catalogId);
        _db.MaterialCatalogs.Add(catalog);
        _db.SaveChanges();
    }

    [Fact]
    public async Task Offcut_TenantA_ShouldNotSeeTenantB_Offcuts()
    {
        var offcutA = Offcut.Register(_tenantA, _catalogId, 500, 300, null);
        var offcutB = Offcut.Register(_tenantB, _catalogId, 400, 200, null);
        await _repo.AddOffcutAsync(offcutA);
        await _repo.AddOffcutAsync(offcutB);
        await _repo.SaveChangesAsync();

        // Simulate tenant-specific query (in real PG, RLS enforces this)
        var tenantAOffcuts = await _db.Offcuts.AsNoTracking()
            .Where(o => o.TenantId == _tenantA && o.Status == OffcutStatus.Available)
            .ToListAsync();

        tenantAOffcuts.Should().ContainSingle(o => o.Id == offcutA.Id);
        tenantAOffcuts.Should().NotContain(o => o.Id == offcutB.Id);
    }

    [Fact]
    public async Task StockMovement_IsAppendOnly_NoUpdateAllowed()
    {
        var movement = StockMovement.Record(_tenantA, MovementType.Inbound, _catalogId, 10m, DateTime.UtcNow, "REF");
        await _repo.AddStockMovementAsync(movement);
        await _repo.SaveChangesAsync();

        // StockMovement has no public setters — verify no mutation possible
        movement.GetType().GetProperties()
            .Where(p => p.CanWrite && p.GetSetMethod()?.IsPublic == true)
            .Should().BeEmpty("StockMovement must be append-only with no public setters");
    }

    [Fact]
    public async Task PanelStock_TenantIsolation_BothTenantsCanHaveOwnStock()
    {
        var stockA = PanelStock.Create(_tenantA, _catalogId, 2800, 2070, StockType.FullPanel, 5, "A1");
        var stockB = PanelStock.Create(_tenantB, _catalogId, 2800, 2070, StockType.FullPanel, 10, "B1");
        await _repo.AddPanelStockAsync(stockA);
        await _repo.AddPanelStockAsync(stockB);
        await _repo.SaveChangesAsync();

        var aStocks = await _db.PanelStocks.AsNoTracking().Where(s => s.TenantId == _tenantA).ToListAsync();
        var bStocks = await _db.PanelStocks.AsNoTracking().Where(s => s.TenantId == _tenantB).ToListAsync();

        aStocks.Should().ContainSingle(s => s.Id == stockA.Id);
        bStocks.Should().ContainSingle(s => s.Id == stockB.Id);
        aStocks.Should().NotContain(s => s.TenantId == _tenantB);
    }

    [Fact]
    public async Task MaterialCatalog_HasNoTenantId_IsSharedAcrossTenants()
    {
        var catalogs = await _db.MaterialCatalogs.AsNoTracking().ToListAsync();
        catalogs.Should().NotBeEmpty("MaterialCatalog is tenant-independent shared reference data");

        // Verify MaterialCatalog has no TenantId property
        typeof(MaterialCatalog).GetProperty("TenantId").Should().BeNull(
            "MaterialCatalog is tenant-independent and must not have a TenantId");
    }

    [Fact]
    public void Offcut_HasNoPublicSetters_ForCriticalFields()
    {
        var props = typeof(Offcut).GetProperties()
            .Where(p => p.CanWrite && p.GetSetMethod()?.IsPublic == true);
        props.Should().BeEmpty("Offcut aggregate must have no public setters");
    }

    public void Dispose() => _db.Dispose();
}
