using SpaceOS.Modules.Inventory.Domain.Aggregates;

namespace SpaceOS.Modules.Inventory.Domain.Interfaces;

public interface IInventoryRepository
{
    // MaterialCatalog
    Task<MaterialCatalog?> GetMaterialCatalogByTypeAsync(string materialType, CancellationToken ct = default);
    Task<IReadOnlyList<MaterialCatalog>> GetAllMaterialCatalogsAsync(CancellationToken ct = default);

    // PanelStock
    Task<IReadOnlyList<PanelStock>> GetStockByMaterialTypeAsync(string materialType, CancellationToken ct = default);
    Task AddPanelStockAsync(PanelStock stock, CancellationToken ct = default);
    Task<PanelStock?> GetPanelStockByIdAsync(Guid id, CancellationToken ct = default);

    // Offcut
    Task<IReadOnlyList<Offcut>> GetAvailableOffcutsByMaterialTypeAsync(string materialType, CancellationToken ct = default);
    Task AddOffcutAsync(Offcut offcut, CancellationToken ct = default);
    Task<Offcut?> GetOffcutByIdAsync(Guid id, CancellationToken ct = default);

    // OffcutReservation
    Task AddOffcutReservationAsync(OffcutReservation reservation, CancellationToken ct = default);
    Task<OffcutReservation?> GetOffcutReservationByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<OffcutReservation>> GetReservationsByOffcutIdAsync(Guid offcutId, CancellationToken ct = default);

    // Offcut queries (v2)
    Task<(IReadOnlyList<Offcut> Items, int Total)> GetOffcutPagedAsync(
        string? status, string? materialCode, decimal? minVolumeM3, DateTime? createdAfter,
        int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<Offcut>> GetAllOffcutsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Offcut>> GetOffcutsByOriginSheetIdAsync(Guid tenantId, Guid originSheetId, CancellationToken ct = default);

    // OffcutBatch (idempotency)
    Task<OffcutBatch?> GetOffcutBatchAsync(Guid tenantId, string sourceType, Guid sourceId, CancellationToken ct = default);
    Task AddOffcutBatchAsync(OffcutBatch batch, CancellationToken ct = default);

    // StockMovement
    Task AddStockMovementAsync(StockMovement movement, CancellationToken ct = default);
    Task<IReadOnlyList<StockMovement>> GetMovementsByMaterialTypeAndDateRangeAsync(
        string materialType, DateTime from, DateTime to, CancellationToken ct = default);

    // ReorderOutbox
    Task AddReorderOutboxAsync(InventoryReorderOutbox outbox, CancellationToken ct = default);

    // Stock aggregation
    Task<int> GetTotalStockQuantityAsync(Guid tenantId, Guid materialCatalogId, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
