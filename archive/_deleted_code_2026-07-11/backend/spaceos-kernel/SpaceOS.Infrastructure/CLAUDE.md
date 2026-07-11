# CLAUDE.md — SpaceOS.Infrastructure

> Infrastructure layer rules. Read root CLAUDE.md first for global rules.
> This layer implements persistence. EF Core lives here and nowhere else.

---

## PURPOSE

Implement the repository interfaces defined in Domain.
Infrastructure knows Application and Domain — they do not know Infrastructure.

---

## DATABASE

**Target:** PostgreSQL (production)
**Test:** SQLite (in-memory — for unit/integration tests only)

Never write SQLite-specific SQL or EF Core config. Use portable EF Core constructs only.

---

## REPOSITORY IMPLEMENTATION TEMPLATE

```csharp
// SpaceOS.Infrastructure/Repositories/TenantRepository.cs

/// <summary>EF Core implementation of <see cref="ITenantRepository"/>.</summary>
internal sealed class TenantRepository : ITenantRepository
{
    private readonly AppDbContext _context;

    public TenantRepository(AppDbContext context) => _context = context;

    // ✅ AsNoTracking on every read-only method
    public async Task<Tenant?> GetByIdAsync(TenantId id, CancellationToken ct) =>
        await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, ct)
            .ConfigureAwait(false);

    // ✅ WithSpecification on every list query
    public async Task<IReadOnlyList<Tenant>> ListAsync(
        ISpecification<Tenant> spec, CancellationToken ct) =>
        await _context.Tenants
            .AsNoTracking()
            .WithSpecification(spec)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    // ✅ No SaveChangesAsync here — IUnitOfWork.SaveChangesAsync is called in the handler
    public async Task AddAsync(Tenant tenant, CancellationToken ct)
    {
        await _context.Tenants.AddAsync(tenant, ct).ConfigureAwait(false);
    }

    public Task UpdateAsync(Tenant tenant, CancellationToken ct)
    {
        _context.Tenants.Update(tenant);
        return Task.CompletedTask;
    }
}
```

---

## EF CORE CONFIGURATION RULES

```csharp
// SpaceOS.Infrastructure/Configurations/TenantConfiguration.cs

/// <summary>EF Core Fluent API configuration for <see cref="Tenant"/>.</summary>
internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(t => t.Id);

        // ✅ Value Object as owned entity — map to column, not to separate table
        builder.Property(t => t.Id)
            .HasConversion(id => id.Value, value => TenantId.From(value));

        builder.OwnsOne(t => t.Name, name =>
        {
            name.Property(n => n.Value)
                .HasColumnName("Name")
                .HasMaxLength(100)
                .IsRequired();
        });

        builder.Ignore(t => t.DomainEvents); // never persist domain events
    }
}
```

**Rules:**
- Every VO → `HasConversion` or `OwnsOne` — never a raw primitive property
- `builder.Ignore(t => t.DomainEvents)` on every aggregate configuration
- No data annotations on domain entities — Fluent API only
- One `IEntityTypeConfiguration<T>` file per aggregate
- `SaveChangesAsync` is **never** called inside a repository — it is called via `IUnitOfWork.SaveChangesAsync()` in the handler, after persist

---

## MIGRATION RULES

- Never auto-migrate at startup (`Database.Migrate()` in production code is forbidden)
- Migrations are explicit CLI operations: `dotnet ef migrations add <Name>`
- Migration names follow `PascalCase` descriptive naming: `AddWorkStationStatusColumn`
- Every migration must be reviewed before applying to a shared environment
- **Breaking schema change → stop and confirm** before generating migration

---

## DBCONTEXT RULES

```csharp
// SpaceOS.Infrastructure/Data/AppDbContext.cs

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Core aggregates
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Facility> Facilities => Set<Facility>();
    public DbSet<WorkStation> WorkStations => Set<WorkStation>();
    public DbSet<SpaceLayer> SpaceLayers => Set<SpaceLayer>();
    public DbSet<FlowEpic> FlowEpics => Set<FlowEpic>();

    // Sprint C aggregates
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<AggregateSnapshot> AggregateSnapshots => Set<AggregateSnapshot>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<NodeManifest> NodeManifests => Set<NodeManifest>();
    public DbSet<SyncSignal> SyncSignals => Set<SyncSignal>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
```

`ApplyConfigurationsFromAssembly` — all configurations auto-registered. Never call `modelBuilder.Entity<T>()` inline.

---

## DEPENDENCY INJECTION REGISTRATION

```csharp
// SpaceOS.Infrastructure/DependencyInjection.cs

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        // Core repositories
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IFacilityRepository, FacilityRepository>();
        services.AddScoped<IWorkStationRepository, WorkStationRepository>();
        services.AddScoped<ISpaceLayerRepository, SpaceLayerRepository>();
        services.AddScoped<IFlowEpicRepository, FlowEpicRepository>();

        // Sprint C repositories
        services.AddScoped<IAuditEventRepository, AuditEventRepository>();
        services.AddScoped<INodeManifestRepository, NodeManifestRepository>();
        services.AddScoped<ISyncSignalRepository, SyncSignalRepository>();

        return services;
    }
}
```

---

## TENANT RESOLUTION (Auth layer)

Two components work together — they **must return the same UUID** per request:

| Component | Location | Source | Purpose |
|---|---|---|---|
| `ClaimsTenantResolver` | `Auth/ClaimsTenantResolver.cs` | `tid` → `spaceos_tenants` → `groups` | EF global query filter (`HasQueryFilter`) |
| `TenantSessionInterceptor` | `Persistence/TenantSessionInterceptor.cs` | `tid` → `spaceos_tenants` → `tenant_id` | PostgreSQL `app.current_tenant_id` session var |

**Priority order — `TenantSessionInterceptor` (MSG-067 fix):**
1. `tid` flat claim (DB tenant UUID — **matches ClaimsTenantResolver**)
2. `spaceos_tenants` JSON array (Keycloak UUID — fallback when no `tid`)
3. `tenant_id` legacy flat claim
4. Sentinel UUID `00000000-0000-0000-0000-000000000001` — **never throw**

**Priority order — `ClaimsTenantResolver`:**
1. `tid` flat claim (DB tenant UUID)
2. `spaceos_tenants` JSON array
3. `groups` path claim (`/spaceos-{uuid}` or bare UUID)
4. `null` — **never throw**

**Rules:**
- `ClaimsTenantResolver` uses `JsonDocument.Parse` — never `JsonSerializer.Deserialize<TenantClaimDto>` (required fields break partial JSON)
- Both parsers catch all exceptions and fall through gracefully
- `TenantSessionInterceptor` registered only for PostgreSQL (never SQLite/tests)
- Sentinel UUID `00000000-0000-0000-0000-000000000001` → admin/background full read access

---

## ANTI-PATTERNS

```csharp
// ❌ AsNoTracking on write operation
_context.Tenants.AsNoTracking().Update(tenant); // tracking required for updates

// ❌ Raw list query without specification
await _context.Tenants.ToListAsync(ct); // always use WithSpecification

// ❌ Data annotations on domain entity
[MaxLength(100)]
public string Name { get; set; } // Fluent API only

// ❌ Auto-migration at startup
app.Services.GetRequiredService<AppDbContext>().Database.Migrate();

// ❌ DbContext reference outside Infrastructure
using SpaceOS.Infrastructure.Persistence; // never in Application or Domain

// ❌ SQLite-specific constructs
options.UseSqlite(...); // only in test setup, never in production config
```
