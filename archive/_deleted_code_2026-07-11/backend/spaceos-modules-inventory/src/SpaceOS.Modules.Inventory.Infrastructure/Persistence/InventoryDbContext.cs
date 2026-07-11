using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Inventory.Domain.Aggregates;
using SpaceOS.Modules.Inventory.Infrastructure.Persistence.Configurations;

namespace SpaceOS.Modules.Inventory.Infrastructure.Persistence;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    public DbSet<MaterialCatalog> MaterialCatalogs => Set<MaterialCatalog>();
    public DbSet<PanelStock> PanelStocks => Set<PanelStock>();
    public DbSet<Offcut> Offcuts => Set<Offcut>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<ReservationItem> ReservationItems => Set<ReservationItem>();
    public DbSet<OffcutReservation> OffcutReservations => Set<OffcutReservation>();
    public DbSet<OffcutBatch> OffcutBatches => Set<OffcutBatch>();
    public DbSet<InventoryInboundInbox> InventoryInboundInboxes => Set<InventoryInboundInbox>();
    public DbSet<InventoryReorderOutbox> InventoryReorderOutboxes => Set<InventoryReorderOutbox>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("spaceos_inventory");
        modelBuilder.ApplyConfiguration(new MaterialCatalogConfiguration());
        modelBuilder.ApplyConfiguration(new PanelStockConfiguration());
        modelBuilder.ApplyConfiguration(new OffcutConfiguration());
        modelBuilder.ApplyConfiguration(new StockMovementConfiguration());
        modelBuilder.ApplyConfiguration(new ReservationConfiguration());
        modelBuilder.ApplyConfiguration(new ReservationItemConfiguration());
        modelBuilder.ApplyConfiguration(new OffcutReservationConfiguration());
        modelBuilder.ApplyConfiguration(new OffcutBatchConfiguration());
        modelBuilder.ApplyConfiguration(new InventoryInboundInboxConfiguration());
        modelBuilder.ApplyConfiguration(new InventoryReorderOutboxConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
