using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Inventory.Domain.Aggregates;
using SpaceOS.Modules.Inventory.Domain.Enums;
using SpaceOS.Modules.Inventory.Domain.Interfaces;
using SpaceOS.Modules.Inventory.Infrastructure.Persistence;

namespace SpaceOS.Modules.Inventory.Infrastructure.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly InventoryDbContext _db;

    public InventoryRepository(InventoryDbContext db)
    {
        _db = db;
    }

    // Normalize: strip spaces so "MDF18mm" matches DB value "MDF 18mm".
    // Comparison is done in-memory (after ToList) to avoid EF/Npgsql translation
    // differences between InMemory and PostgreSQL providers.
    private static string Normalize(string t) => t.Replace(" ", "");

    private async Task<List<MaterialCatalog>> LoadAllCatalogsAsync(CancellationToken ct) =>
        await _db.MaterialCatalogs.AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task<MaterialCatalog?> GetMaterialCatalogByTypeAsync(string materialType, CancellationToken ct = default)
    {
        var norm = Normalize(materialType);
        var all = await LoadAllCatalogsAsync(ct).ConfigureAwait(false);
        return all.FirstOrDefault(m => Normalize(m.MaterialType) == norm);
    }

    public async Task<IReadOnlyList<MaterialCatalog>> GetAllMaterialCatalogsAsync(CancellationToken ct = default)
        => await LoadAllCatalogsAsync(ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<PanelStock>> GetStockByMaterialTypeAsync(string materialType, CancellationToken ct = default)
    {
        var catalog = await GetMaterialCatalogByTypeAsync(materialType, ct).ConfigureAwait(false);
        if (catalog is null) return Array.Empty<PanelStock>();
        return await _db.PanelStocks.AsNoTracking()
            .Where(s => s.MaterialCatalogId == catalog.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task AddPanelStockAsync(PanelStock stock, CancellationToken ct = default)
        => await _db.PanelStocks.AddAsync(stock, ct).ConfigureAwait(false);

    public async Task<PanelStock?> GetPanelStockByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.PanelStocks.FindAsync(new object[] { id }, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Offcut>> GetAvailableOffcutsByMaterialTypeAsync(string materialType, CancellationToken ct = default)
    {
        var catalog = await GetMaterialCatalogByTypeAsync(materialType, ct).ConfigureAwait(false);
        if (catalog is null) return Array.Empty<Offcut>();
        return await _db.Offcuts.AsNoTracking()
            .Where(o => o.MaterialCatalogId == catalog.Id && o.Status == OffcutStatus.Available)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task AddOffcutAsync(Offcut offcut, CancellationToken ct = default)
        => await _db.Offcuts.AddAsync(offcut, ct).ConfigureAwait(false);

    public async Task<Offcut?> GetOffcutByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Offcuts.FindAsync(new object[] { id }, ct).ConfigureAwait(false);

    public async Task AddStockMovementAsync(StockMovement movement, CancellationToken ct = default)
        => await _db.StockMovements.AddAsync(movement, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<StockMovement>> GetMovementsByMaterialTypeAndDateRangeAsync(
        string materialType, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var catalog = await GetMaterialCatalogByTypeAsync(materialType, ct).ConfigureAwait(false);
        if (catalog is null) return Array.Empty<StockMovement>();
        return await _db.StockMovements.AsNoTracking()
            .Where(m => m.MaterialCatalogId == catalog.Id && m.OccurredAt >= from && m.OccurredAt <= to)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task AddOffcutReservationAsync(OffcutReservation reservation, CancellationToken ct = default)
        => await _db.OffcutReservations.AddAsync(reservation, ct).ConfigureAwait(false);

    public async Task<OffcutReservation?> GetOffcutReservationByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.OffcutReservations.FindAsync(new object[] { id }, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<OffcutReservation>> GetReservationsByOffcutIdAsync(Guid offcutId, CancellationToken ct = default)
        => await _db.OffcutReservations.AsNoTracking()
            .Where(r => r.OffcutId == offcutId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task<(IReadOnlyList<Offcut> Items, int Total)> GetOffcutPagedAsync(
        string? status, string? materialCode, decimal? minVolumeM3, DateTime? createdAfter,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Offcuts.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<OffcutStatus>(status, ignoreCase: true, out var parsedStatus))
            query = query.Where(o => o.Status == parsedStatus);

        if (!string.IsNullOrWhiteSpace(materialCode))
            query = query.Where(o => o.MaterialCode == materialCode);

        if (minVolumeM3.HasValue)
            query = query.Where(o => o.VolumeM3 >= minVolumeM3.Value);

        if (createdAfter.HasValue)
            query = query.Where(o => o.CreatedAt >= createdAfter.Value);

        var total = await query.CountAsync(ct).ConfigureAwait(false);
        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return (items, total);
    }

    public async Task<IReadOnlyList<Offcut>> GetAllOffcutsAsync(CancellationToken ct = default)
        => await _db.Offcuts.AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Offcut>> GetOffcutsByOriginSheetIdAsync(Guid tenantId, Guid originSheetId, CancellationToken ct = default)
        => await _db.Offcuts.AsNoTracking()
            .Where(o => o.TenantId == tenantId && o.OriginCuttingSheetId == originSheetId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task<OffcutBatch?> GetOffcutBatchAsync(Guid tenantId, string sourceType, Guid sourceId, CancellationToken ct = default)
        => await _db.OffcutBatches.AsNoTracking()
            .FirstOrDefaultAsync(b => b.TenantId == tenantId && b.SourceType == sourceType && b.SourceId == sourceId, ct)
            .ConfigureAwait(false);

    public async Task AddOffcutBatchAsync(OffcutBatch batch, CancellationToken ct = default)
        => await _db.OffcutBatches.AddAsync(batch, ct).ConfigureAwait(false);

    public async Task AddReorderOutboxAsync(InventoryReorderOutbox outbox, CancellationToken ct = default)
        => await _db.InventoryReorderOutboxes.AddAsync(outbox, ct).ConfigureAwait(false);

    public async Task<int> GetTotalStockQuantityAsync(Guid tenantId, Guid materialCatalogId, CancellationToken ct = default)
        => await _db.PanelStocks.AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.MaterialCatalogId == materialCatalogId)
            .SumAsync(s => s.Quantity, ct)
            .ConfigureAwait(false);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct).ConfigureAwait(false);
}
