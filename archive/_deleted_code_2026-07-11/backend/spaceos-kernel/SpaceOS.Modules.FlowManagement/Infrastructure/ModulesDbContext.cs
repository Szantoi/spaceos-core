// SpaceOS.Modules.FlowManagement/Infrastructure/ModulesDbContext.cs
using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.FlowManagement.Domain;

namespace SpaceOS.Modules.FlowManagement.Infrastructure;

/// <summary>
/// EF Core database context for the FlowManagement module.
/// On PostgreSQL, all tables are placed in the <c>modules</c> schema.
/// On SQLite (development / tests), schema is not supported and is omitted.
/// </summary>
public sealed class ModulesDbContext : DbContext
{
    /// <summary>Gets the <see cref="FlowTask"/> entities.</summary>
    public DbSet<FlowTask> FlowTasks => Set<FlowTask>();

    /// <summary>Gets the <see cref="FlowMilestone"/> entities.</summary>
    public DbSet<FlowMilestone> FlowMilestones => Set<FlowMilestone>();

    /// <summary>Gets the <see cref="FlowProject"/> entities.</summary>
    public DbSet<FlowProject> FlowProjects => Set<FlowProject>();

    /// <summary>Gets the <see cref="FlowProgram"/> entities.</summary>
    public DbSet<FlowProgram> FlowPrograms => Set<FlowProgram>();

    /// <summary>Gets the <see cref="OfflineSyncQueueItem"/> entities.</summary>
    public DbSet<OfflineSyncQueueItem> OfflineSyncQueue => Set<OfflineSyncQueueItem>();

    /// <summary>
    /// Initialises a new instance of <see cref="ModulesDbContext"/> with the given options.
    /// </summary>
    /// <param name="options">The EF Core context options.</param>
    public ModulesDbContext(DbContextOptions<ModulesDbContext> options) : base(options) { }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // PostgreSQL: isolate module tables under the "modules" schema.
        // SQLite does not support schemas — omit to keep dev/test portable.
        // Use the provider name string to avoid a hard dependency on Npgsql extensions.
        if (Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
            modelBuilder.HasDefaultSchema("modules");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ModulesDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
