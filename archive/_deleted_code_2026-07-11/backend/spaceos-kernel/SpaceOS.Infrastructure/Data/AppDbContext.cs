using Microsoft.EntityFrameworkCore;
using SpaceOS.Infrastructure.Internal;
using SpaceOS.Kernel.Domain.Aggregates;
using SpaceOS.Kernel.Domain.AuditLog;
using SpaceOS.Kernel.Domain.Auth;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Federation;
using SpaceOS.Kernel.Domain.Outbox;
using SpaceOS.Kernel.Domain.Snapshots;
using SpaceOS.Kernel.Domain.Sync;
using SpaceOS.Kernel.Domain.UserProfiles;
namespace SpaceOS.Infrastructure.Data;

/// <summary>
/// EF Core database context for SpaceOS.
/// Global query filters enforce per-tenant data sovereignty on all tenant-scoped aggregates.
/// <list type="bullet">
///   <item><description>
///     <see cref="ITenantResolver.TryResolve"/> returns <c>null</c> → filter bypass (background jobs, migrations).
///   </description></item>
///   <item><description>
///     Returns <see cref="SpaceOS.Infrastructure.Auth.ClaimsTenantResolver.DenyWebRequestSentinel"/> →
///     filter produces empty results (web request with no valid <c>tid</c> claim).
///   </description></item>
///   <item><description>
///     Returns a real <see cref="SpaceOS.Kernel.Domain.ValueObjects.TenantId"/> → filter scopes to that tenant.
///   </description></item>
/// </list>
/// </summary>
/// <remarks>
/// <see cref="AuditEvent"/> is intentionally excluded from this context.
/// All audit-log writes flow through <see cref="SpaceOS.Infrastructure.Persistence.AuditDbContext"/>
/// which uses the <c>spaceos_audit_writer</c> PostgreSQL role with row-level security enforced.
/// </remarks>
public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ITenantResolver tenantResolver) : DbContext(options)
{
    private readonly ITenantResolver _tenantResolver = tenantResolver;

    // Evaluated per-query by EF Core's filter machinery. Returns null when Admin bypass is active.
    private Guid? CurrentTenantGuid => _tenantResolver.TryResolve()?.Value;

    /// <summary>Gets the <see cref="Facility"/> entities.</summary>
    public DbSet<Facility> Facilities => Set<Facility>();

    /// <summary>Gets the <see cref="Tenant"/> entities.</summary>
    public DbSet<Tenant> Tenants => Set<Tenant>();

    /// <summary>Gets the <see cref="WorkStation"/> entities.</summary>
    public DbSet<WorkStation> WorkStations => Set<WorkStation>();

    /// <summary>Gets the <see cref="FlowEpic"/> entities.</summary>
    public DbSet<FlowEpic> FlowEpics => Set<FlowEpic>();

    /// <summary>Gets the <see cref="SpaceLayer"/> entities.</summary>
    public DbSet<SpaceLayer> SpaceLayers => Set<SpaceLayer>();

    /// <summary>Gets the <see cref="AggregateSnapshot"/> entities.</summary>
    public DbSet<AggregateSnapshot> AggregateSnapshots => Set<AggregateSnapshot>();

    /// <summary>Gets the <see cref="OutboxMessage"/> entities.</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <summary>Gets the <see cref="UserProfile"/> entities used for GDPR pseudonymization.</summary>
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    /// <summary>Gets the <see cref="NodeManifest"/> entities for federation node registration.</summary>
    public DbSet<NodeManifest> NodeManifests => Set<NodeManifest>();

    /// <summary>Gets the <see cref="SyncSignal"/> entities for offline state synchronisation.</summary>
    public DbSet<SyncSignal> SyncSignals => Set<SyncSignal>();

    /// <summary>Gets the <see cref="RefreshToken"/> entities for opaque refresh token management.</summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <summary>Gets the <see cref="PhysicalSpace"/> aggregate roots.</summary>
    public DbSet<PhysicalSpace> PhysicalSpaces => Set<PhysicalSpace>();

    /// <summary>Gets the <see cref="BvhNode"/> entities for spatial BVH trees.</summary>
    public DbSet<BvhNode> BvhNodes => Set<BvhNode>();

    /// <summary>Gets the <see cref="SpatialElement"/> entities for spatial elements.</summary>
    public DbSet<SpatialElement> SpatialElements => Set<SpatialElement>();

    /// <summary>Gets the <see cref="SpatialTaskLink"/> entities for task-element links.</summary>
    public DbSet<SpatialTaskLink> SpatialTaskLinks => Set<SpatialTaskLink>();

    /// <summary>Gets the <see cref="TenantHandshakeAllowlist"/> entities for B2B cross-tenant authorization.</summary>
    public DbSet<TenantHandshakeAllowlist> TenantHandshakeAllowlists => Set<TenantHandshakeAllowlist>();

    /// <summary>Gets the <see cref="StageDefinition"/> entities for the Stage Registry.</summary>
    public DbSet<StageDefinition> StageDefinitions => Set<StageDefinition>();

    /// <summary>Gets the <see cref="StageChainTemplate"/> entities for the Stage Registry.</summary>
    public DbSet<StageChainTemplate> StageChainTemplates => Set<StageChainTemplate>();

    /// <summary>Gets the <see cref="StageChainStep"/> entities for the Stage Registry.</summary>
    public DbSet<StageChainStep> StageChainSteps => Set<StageChainStep>();

    /// <summary>Gets the <see cref="StageHandoff"/> entities for the Stage Registry.</summary>
    public DbSet<StageHandoff> StageHandoffs => Set<StageHandoff>();

    /// <summary>Gets the <see cref="ModuleSubscription"/> entities for cross-module event routing.</summary>
    public DbSet<ModuleSubscription> ModuleSubscriptions => Set<ModuleSubscription>();

    /// <summary>Gets the <see cref="InternalAccessAuditEntry"/> rows for the internal actor directory audit log.</summary>
    public DbSet<InternalAccessAuditEntry> InternalAccessAuditLog => Set<InternalAccessAuditEntry>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Override column types for PostgreSQL array columns.
        // Configurations use a portable JSON converter so SQLite unit/integration tests pass.
        // When running against PostgreSQL (production + API tests), remove the JSON converter
        // and restore the native varchar(32)[] array type so Npgsql handles the mapping.
        if (Database.IsNpgsql())
        {
            modelBuilder.Entity<Tenant>()
                .Property<List<string>>("_enabledModules")
                .HasConversion((Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter?)null)
                .HasColumnType("varchar(32)[]")
                .HasDefaultValueSql("'{}'");

            modelBuilder.Entity<TenantHandshakeAllowlist>()
                .Property<List<string>>("_allowedTradeTypes")
                .HasConversion((Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter?)null)
                .HasColumnType("varchar(32)[]");

            // StageHandoff.PayloadJson stored as JSONB on PostgreSQL for efficient querying.
            // Portable string type used by SQLite in tests.
            modelBuilder.Entity<StageHandoff>()
                .Property(x => x.PayloadJson)
                .HasColumnType("jsonb");
        }

        // AuditEvent is owned exclusively by AuditDbContext.
        // Ignore must be called after ApplyConfigurationsFromAssembly because the assembly
        // scan picks up AuditEventConfiguration — calling Ignore afterward removes the
        // entity type from this context's model, ensuring no AuditEvents table is created
        // or queried through AppDbContext.
        modelBuilder.Ignore<AuditEvent>();

        // Tenant data sovereignty — filter bypassed when resolver returns null (Admin path).
        // When CurrentTenantGuid is null, the SQL comparison f.TenantId = NULL is never matched
        // by EF Core — instead it generates: WHERE @p IS NULL OR f.TenantId = @p.
        // This avoids calling .Value on a null Nullable<Guid>.
        // IsArchived = false is always enforced — archived entities are invisible to all callers.
        modelBuilder.Entity<Tenant>()
            .HasQueryFilter(t => !t.IsArchived);
        modelBuilder.Entity<Facility>()
            .HasQueryFilter(f => !f.IsArchived && (CurrentTenantGuid == null || f.TenantId == CurrentTenantGuid));
        modelBuilder.Entity<WorkStation>()
            .HasQueryFilter(w => !w.IsArchived && (CurrentTenantGuid == null || w.TenantId == CurrentTenantGuid));
        modelBuilder.Entity<SpaceLayer>()
            .HasQueryFilter(sl => !sl.IsArchived && (CurrentTenantGuid == null || sl.TenantId == CurrentTenantGuid));
        modelBuilder.Entity<FlowEpic>()
            .HasQueryFilter(fe => !fe.IsArchived && (CurrentTenantGuid == null || fe.TenantId == CurrentTenantGuid));

        modelBuilder.Entity<PhysicalSpace>()
            .HasQueryFilter(ps => CurrentTenantGuid == null || ps.TenantId == CurrentTenantGuid);
        modelBuilder.Entity<SpatialElement>()
            .HasQueryFilter(se => !se.IsArchived && (CurrentTenantGuid == null || se.TenantId == CurrentTenantGuid));
        modelBuilder.Entity<BvhNode>()
            .HasQueryFilter(bn => CurrentTenantGuid == null || bn.TenantId == CurrentTenantGuid);
        modelBuilder.Entity<SpatialTaskLink>()
            .HasQueryFilter(stl => CurrentTenantGuid == null || stl.TenantId == CurrentTenantGuid);

        // Stage Registry — tenant-scoped filters
        modelBuilder.Entity<StageDefinition>()
            .HasQueryFilter(sd => CurrentTenantGuid == null || sd.TenantId == CurrentTenantGuid);
        modelBuilder.Entity<StageChainTemplate>()
            .HasQueryFilter(sct => CurrentTenantGuid == null || sct.TenantId == CurrentTenantGuid);
        modelBuilder.Entity<StageChainStep>()
            .HasQueryFilter(scs => CurrentTenantGuid == null || scs.TenantId == CurrentTenantGuid);
        modelBuilder.Entity<StageHandoff>()
            .HasQueryFilter(sh => CurrentTenantGuid == null || sh.TenantId == CurrentTenantGuid);

        base.OnModelCreating(modelBuilder);
    }
}
