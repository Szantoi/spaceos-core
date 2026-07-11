using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Inventory.Domain.Aggregates;
using SpaceOS.Modules.Inventory.Infrastructure.Persistence.Configurations;

namespace SpaceOS.Modules.Inventory.Infrastructure.Persistence;

/// <summary>
/// Dedicated DbContext for background worker operations.
/// Does NOT register <c>TenantSessionInterceptor</c> — connects via the
/// <c>spaceos_inventory_worker</c> role (ADR-024: BYPASSRLS) so the worker
/// can process expired reservations across all tenants without GUC contamination.
/// </summary>
public sealed class InventoryWorkerDbContext : DbContext
{
    public InventoryWorkerDbContext(DbContextOptions<InventoryWorkerDbContext> options)
        : base(options) { }

    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<ReservationItem> ReservationItems => Set<ReservationItem>();
    public DbSet<InventoryReorderOutbox> InventoryReorderOutboxes => Set<InventoryReorderOutbox>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.ApplyConfiguration(new ReservationConfiguration());
        mb.ApplyConfiguration(new ReservationItemConfiguration());
        mb.ApplyConfiguration(new InventoryReorderOutboxConfiguration());
    }
}
