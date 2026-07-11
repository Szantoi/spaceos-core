using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Joinery.Domain.Aggregates;
using SpaceOS.Modules.Joinery.Domain.Core;
using SpaceOS.Modules.Joinery.Domain.Entities;
using SpaceOS.Modules.Joinery.Domain.Rules;
using Anyaglista = SpaceOS.Modules.Joinery.Domain.Core.Anyaglista;

namespace SpaceOS.Modules.Joinery.Infrastructure.Persistence;

public class JoineryDbContext : DbContext
{
    public JoineryDbContext(DbContextOptions<JoineryDbContext> options) : base(options) { }

    public DbSet<DoorOrder> DoorOrders => Set<DoorOrder>();
    public DbSet<DoorItem> DoorItems => Set<DoorItem>();
    public DbSet<DoorOrderConvertedLine> DoorOrderConvertedLines => Set<DoorOrderConvertedLine>();
    public DbSet<DoorTypeRule> DoorTypeRules => Set<DoorTypeRule>();
    public DbSet<PartDimensionRule> PartDimensionRules => Set<PartDimensionRule>();
    public DbSet<ProcessTaskTemplate> ProcessTaskTemplates => Set<ProcessTaskTemplate>();
    public DbSet<GlobalConstant> GlobalConstants => Set<GlobalConstant>();
    public DbSet<CuttingListSnapshot> CuttingListSnapshots => Set<CuttingListSnapshot>();
    public DbSet<ProductionSheetCache> ProductionSheetCaches => Set<ProductionSheetCache>();
    public DbSet<JoineryOutboxEntry> JoineryOutboxEntries => Set<JoineryOutboxEntry>();
    public DbSet<Gyartasilap> Gyartasilaps => Set<Gyartasilap>();
    public DbSet<GyartasilapBatch> GyartasilapBatches => Set<GyartasilapBatch>();
    public DbSet<Anyaglista> Anyaglistak => Set<Anyaglista>();
    public DbSet<ProductTemplate> ProductTemplates => Set<ProductTemplate>();
    public DbSet<ProductConfiguration> ProductConfigurations => Set<ProductConfiguration>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.HasDefaultSchema("spaceos_joinery");
        mb.ApplyConfigurationsFromAssembly(typeof(JoineryDbContext).Assembly);
    }
}
