using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Inventory.Domain.Aggregates;
using SpaceOS.Modules.Inventory.Domain.Enums;
using SpaceOS.Modules.Inventory.Infrastructure.Persistence;
using SpaceOS.Modules.Inventory.Infrastructure.Repositories;
using Xunit;

namespace SpaceOS.Modules.Inventory.Tests.Infrastructure;

public class InventoryRepositoryTests : IDisposable
{
    private readonly InventoryDbContext _db;
    private readonly InventoryRepository _repo;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _catalogId = new Guid("10000000-0000-0000-0000-000000000001");

    public InventoryRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new InventoryDbContext(options);
        // Do NOT call EnsureCreated() — it seeds HasData with fixed GUIDs which would conflict
        _repo = new InventoryRepository(_db);

        // Seed a material catalog entry manually with test catalogId
        var catalog = MaterialCatalog.Create("MDF 18mm", 2800, 2070, 18, 8500, "MDF-18", "MDF lap 18mm");
        typeof(MaterialCatalog).GetProperty("Id")!.SetValue(catalog, _catalogId);
        _db.MaterialCatalogs.Add(catalog);
        _db.SaveChanges();
    }

    [Fact]
    public async Task GetMaterialCatalogByType_ExistingType_ShouldReturn()
    {
        var result = await _repo.GetMaterialCatalogByTypeAsync("MDF 18mm");
        result.Should().NotBeNull();
        result!.MaterialType.Should().Be("MDF 18mm");
    }

    [Fact]
    public async Task GetMaterialCatalogByType_NonExisting_ShouldReturnNull()
    {
        var result = await _repo.GetMaterialCatalogByTypeAsync("UNKNOWN");
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddPanelStock_ShouldPersist()
    {
        var stock = PanelStock.Create(_tenantId, _catalogId, 2800, 2070, StockType.FullPanel, 5, "A1");
        await _repo.AddPanelStockAsync(stock);
        await _repo.SaveChangesAsync();

        var found = await _repo.GetPanelStockByIdAsync(stock.Id);
        found.Should().NotBeNull();
        found!.Quantity.Should().Be(5);
    }

    [Fact]
    public async Task AddOffcut_ShouldPersist()
    {
        var offcut = Offcut.Register(_tenantId, _catalogId, 500, 300, null);
        await _repo.AddOffcutAsync(offcut);
        await _repo.SaveChangesAsync();

        var found = await _repo.GetOffcutByIdAsync(offcut.Id);
        found.Should().NotBeNull();
        found!.Status.Should().Be(OffcutStatus.Available);
    }

    [Fact]
    public async Task GetAvailableOffcuts_ShouldReturnOnlyAvailable()
    {
        var available = Offcut.Register(_tenantId, _catalogId, 500, 300, null);
        var waste = Offcut.Register(_tenantId, _catalogId, 200, 100, null);
        waste.MarkWaste();

        await _repo.AddOffcutAsync(available);
        await _repo.AddOffcutAsync(waste);
        await _repo.SaveChangesAsync();

        var result = await _repo.GetAvailableOffcutsByMaterialTypeAsync("MDF 18mm");
        result.Should().ContainSingle(o => o.Id == available.Id);
    }

    [Fact]
    public async Task AddStockMovement_ShouldPersist()
    {
        var movement = StockMovement.Record(_tenantId, MovementType.Inbound, _catalogId, 10m, DateTime.UtcNow, "REF-001");
        await _repo.AddStockMovementAsync(movement);
        await _repo.SaveChangesAsync();

        var from = DateTime.UtcNow.AddHours(-1);
        var to = DateTime.UtcNow.AddHours(1);
        var movements = await _repo.GetMovementsByMaterialTypeAndDateRangeAsync("MDF 18mm", from, to);
        movements.Should().ContainSingle(m => m.Id == movement.Id);
    }

    [Fact]
    public async Task GetStockByMaterialType_ShouldReturnMatchingStocks()
    {
        var stock = PanelStock.Create(_tenantId, _catalogId, 2800, 2070, StockType.FullPanel, 3, "B2");
        await _repo.AddPanelStockAsync(stock);
        await _repo.SaveChangesAsync();

        var result = await _repo.GetStockByMaterialTypeAsync("MDF 18mm");
        result.Should().Contain(s => s.Id == stock.Id);
    }

    [Fact]
    public async Task StockMovements_DateRangeFilter_ShouldWork()
    {
        var old = StockMovement.Record(_tenantId, MovementType.Consumption, _catalogId, 5m, DateTime.UtcNow.AddDays(-10), "OLD");
        var recent = StockMovement.Record(_tenantId, MovementType.Consumption, _catalogId, 3m, DateTime.UtcNow, "RECENT");
        await _repo.AddStockMovementAsync(old);
        await _repo.AddStockMovementAsync(recent);
        await _repo.SaveChangesAsync();

        var from = DateTime.UtcNow.AddDays(-1);
        var to = DateTime.UtcNow.AddDays(1);
        var result = await _repo.GetMovementsByMaterialTypeAndDateRangeAsync("MDF 18mm", from, to);
        result.Should().ContainSingle(m => m.Id == recent.Id);
        result.Should().NotContain(m => m.Id == old.Id);
    }

    [Fact]
    public async Task GetAllMaterialCatalogs_ShouldReturnAll()
    {
        var catalogs = await _repo.GetAllMaterialCatalogsAsync();
        catalogs.Should().NotBeEmpty();
    }

    // UI sends materialType without spaces (e.g. "MDF18mm") — repo must match "MDF 18mm"
    [Fact]
    public async Task GetMaterialCatalogByType_SpaceStripped_ShouldMatch()
    {
        var result = await _repo.GetMaterialCatalogByTypeAsync("MDF18mm");
        result.Should().NotBeNull();
        result!.MaterialType.Should().Be("MDF 18mm");
    }

    [Fact]
    public async Task GetStockByMaterialType_SpaceStripped_ShouldReturnSameAsWithSpaces()
    {
        var stock = PanelStock.Create(_tenantId, _catalogId, 2800, 2070, StockType.FullPanel, 7, "C3");
        await _repo.AddPanelStockAsync(stock);
        await _repo.SaveChangesAsync();

        var withSpaces    = await _repo.GetStockByMaterialTypeAsync("MDF 18mm");
        var withoutSpaces = await _repo.GetStockByMaterialTypeAsync("MDF18mm");

        withoutSpaces.Should().Contain(s => s.Id == stock.Id);
        withoutSpaces.Count.Should().Be(withSpaces.Count);
    }

    public void Dispose() => _db.Dispose();
}
