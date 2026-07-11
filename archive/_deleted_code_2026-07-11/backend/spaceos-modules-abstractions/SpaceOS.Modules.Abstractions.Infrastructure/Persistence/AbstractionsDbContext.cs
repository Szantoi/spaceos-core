using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Abstractions.Domain.Aggregates;
using SpaceOS.Modules.Abstractions.Domain.Entities;

namespace SpaceOS.Modules.Abstractions.Infrastructure.Persistence;

public sealed class AbstractionsDbContext : DbContext
{
    public DbSet<ProductTemplate>    ProductTemplates    { get; set; } = null!;
    public DbSet<ComponentSlot>      ComponentSlots      { get; set; } = null!;
    public DbSet<SlotConnection>     SlotConnections     { get; set; } = null!;
    public DbSet<TemplateParameter>  TemplateParameters  { get; set; } = null!;
    public DbSet<GeometryAttachment> GeometryAttachments { get; set; } = null!;

    public AbstractionsDbContext(DbContextOptions<AbstractionsDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("spaceos_modules");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AbstractionsDbContext).Assembly);
    }
}
