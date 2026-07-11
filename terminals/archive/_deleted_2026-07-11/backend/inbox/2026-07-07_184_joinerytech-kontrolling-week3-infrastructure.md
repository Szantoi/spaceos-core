---
id: MSG-BACKEND-184
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
epic_id: EPIC-CUTTING-Q3
estimated_nwt: 60
created: 2026-07-07
ref: ADR-055
content_hash: 172d09039950b69e3bad2af6a7b0a65e695b4950e8061a981fb6176e244c9a78
completed: 2026-07-08
---

# JoineryTech Phase 1 Week 3: Kontrolling Infrastructure Layer

## Context

**Week 1 Status:** ✅ COMPLETE (Domain layer)
- Domain layer: Cost, Overhead aggregates (CALCULATED pattern per ADR-055)
- MSG-BACKEND-143-DONE (2026-07-04)
- Build: 0 errors, 0 warnings

**Week 2 Status:** ✅ COMPLETE (Application layer)
- Application layer: 5 Query Handlers, 3 Command Handlers
- MSG-BACKEND-175-BLOCKED → Already complete (115 tests PASS)
- Calculated layer approach validated
- Build: 0 errors, 0 warnings

**Week 4 Status:** ✅ COMPLETE (API layer)
- MSG-BACKEND-144-DONE (2026-07-04)
- 8 Minimal API endpoints

## Objective

Implement the **Infrastructure Layer** for the Kontrolling (Cost Management) module following established patterns from DMS, HR, Maintenance, QA Week 3.

## Architecture Reference

**ADR-055: JoineryTech Kontrolling Domain Model Design**

### KEY PRINCIPLE: CALCULATED LAYER APPROACH

**⚠️ CRITICAL ARCHITECTURAL DECISION:**
- **NO STORED EVM STATE** — ProjectCostCalculation is NOT stored in database
- **Calculations on-demand** — EAC, Variance, Margin computed when queried
- **Only Config + Adjustments stored** — OverheadConfig and CostAdjustment aggregates

**Two Aggregate Roots (STORED)**:
1. **OverheadConfig** — Tenant-level overhead configuration (rate, allocation method)
2. **CostAdjustment** — Manual corrections for calculation gaps

**One Calculated Model (NOT STORED)**:
- **ProjectCostCalculation** — Derived on-demand from Production, HR, Finance, Logistics modules

---

## Scope

### 1. DbContext

```csharp
// SpaceOS.Modules.Kontrolling.Infrastructure/Persistence/KontrollingDbContext.cs
public class KontrollingDbContext : DbContext
{
    public DbSet<OverheadConfig> OverheadConfigs { get; set; } = null!;
    public DbSet<CostAdjustment> CostAdjustments { get; set; } = null!;

    // NOTE: ProjectCostCalculation is NOT a DbSet - it's calculated!

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("kontrolling");
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
```

**Schema**: `kontrolling`
**Connection**: PostgreSQL via Npgsql
**Interceptor**: TenantDbConnectionInterceptor (session-based multi-tenancy)

### 2. Entity Type Configurations (2 aggregates)

**OverheadConfigEntityTypeConfiguration.cs**:
```csharp
public class OverheadConfigEntityTypeConfiguration : IEntityTypeConfiguration<OverheadConfig>
{
    public void Configure(EntityTypeBuilder<OverheadConfig> builder)
    {
        builder.ToTable("overhead_configs");

        // Primary key
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id)
            .HasConversion(id => id.Value, value => OverheadConfigId.From(value))
            .HasColumnName("id");

        // Multi-tenancy (one config per tenant)
        builder.Property(o => o.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.HasIndex(o => o.TenantId).IsUnique(); // ONE config per tenant

        // Overhead Rate (0.0 - 1.0, e.g., 0.15 = 15%)
        builder.Property(o => o.OverheadRate)
            .HasColumnName("overhead_rate")
            .HasColumnType("decimal(5,4)"); // 0.1500 precision

        // Allocation Method (enum)
        builder.Property(o => o.AllocationMethod)
            .HasConversion<string>()
            .HasColumnName("allocation_method")
            .HasMaxLength(50);

        // Owned Collection: OverheadRules (category exclusions, custom rates)
        builder.OwnsMany(o => o.OverheadRules, rules => {
            rules.ToTable("overhead_rules");
            rules.WithOwner().HasForeignKey("overhead_config_id");
            rules.Property<Guid>("id").HasColumnName("id");
            rules.HasKey("id");

            rules.Property(r => r.CostCategory)
                .HasConversion<string>()
                .HasColumnName("cost_category")
                .HasMaxLength(50);

            rules.Property(r => r.Exclude).HasColumnName("exclude");
            rules.Property(r => r.CustomRate).HasColumnName("custom_rate").HasColumnType("decimal(5,4)");
        });

        // Audit fields
        builder.Property(o => o.CreatedBy)
            .HasConversion(id => id.Value, value => UserId.From(value))
            .HasColumnName("created_by");

        builder.Property(o => o.CreatedAt).HasColumnName("created_at");
        builder.Property(o => o.UpdatedAt).HasColumnName("updated_at");
    }
}
```

**CostAdjustmentEntityTypeConfiguration.cs**:
```csharp
public class CostAdjustmentEntityTypeConfiguration : IEntityTypeConfiguration<CostAdjustment>
{
    public void Configure(EntityTypeBuilder<CostAdjustment> builder)
    {
        builder.ToTable("cost_adjustments");

        // Primary key
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasConversion(id => id.Value, value => CostAdjustmentId.From(value))
            .HasColumnName("id");

        // Multi-tenancy
        builder.Property(c => c.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.HasIndex(c => c.TenantId);

        // Scope (Project-specific or Portfolio-wide)
        builder.Property(c => c.Scope)
            .HasConversion<string>()
            .HasColumnName("scope")
            .HasMaxLength(20);

        builder.Property(c => c.ProjectId).HasColumnName("project_id"); // Nullable (only for Scope=Project)

        // Cost Category
        builder.Property(c => c.Category)
            .HasConversion<string>()
            .HasColumnName("category")
            .HasMaxLength(50);

        // Value Objects: Planned/Actual Adjustments (Money)
        builder.OwnsOne(c => c.PlannedAdjustment, money => {
            money.Property(m => m.Amount).HasColumnName("planned_adjustment_amount").HasColumnType("decimal(18,2)");
            money.Property(m => m.Currency)
                .HasConversion<string>()
                .HasColumnName("planned_adjustment_currency")
                .HasMaxLength(3);
        });

        builder.OwnsOne(c => c.ActualAdjustment, money => {
            money.Property(m => m.Amount).HasColumnName("actual_adjustment_amount").HasColumnType("decimal(18,2)");
            money.Property(m => m.Currency)
                .HasConversion<string>()
                .HasColumnName("actual_adjustment_currency")
                .HasMaxLength(3);
        });

        // Reason (mandatory)
        builder.Property(c => c.Reason).HasColumnName("reason").HasMaxLength(500).IsRequired();

        // Audit fields
        builder.Property(c => c.CreatedBy)
            .HasConversion(id => id.Value, value => UserId.From(value))
            .HasColumnName("created_by");

        builder.Property(c => c.CreatedAt).HasColumnName("created_at");

        // Indexes
        builder.HasIndex(c => c.ProjectId);
        builder.HasIndex(c => c.Category);
        builder.HasIndex(c => c.Scope);
    }
}
```

### 3. Repository Implementations (Hybrid Pattern)

**OverheadConfigRepository.cs**:
```csharp
public class OverheadConfigRepository : IOverheadConfigRepository
{
    private readonly KontrollingDbContext _context;

    // 2-param methods (RLS isolation at DB level)
    public async Task<OverheadConfig?> GetByTenantAsync(Guid tenantId, CancellationToken ct)
    {
        // NOTE: Only ONE config per tenant (unique constraint)
        return await _context.OverheadConfigs
            .FirstOrDefaultAsync(o => o.TenantId == tenantId, ct);
    }

    public async Task AddAsync(OverheadConfig config, CancellationToken ct)
    {
        await _context.OverheadConfigs.AddAsync(config, ct);
    }

    public void Update(OverheadConfig config)
    {
        _context.OverheadConfigs.Update(config);
    }

    public IUnitOfWork UnitOfWork => _context;
}
```

**CostAdjustmentRepository.cs** (Hybrid pattern):
```csharp
public class CostAdjustmentRepository : ICostAdjustmentRepository
{
    private readonly KontrollingDbContext _context;

    // 2-param methods (RLS isolation)
    public async Task<CostAdjustment?> GetByIdAsync(CostAdjustmentId id, CancellationToken ct)
    {
        return await _context.CostAdjustments
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    // 3-param methods (explicit tenant scoping for range queries)
    public async Task<IReadOnlyList<CostAdjustment>> GetByProjectAsync(
        Guid tenantId, Guid projectId, CancellationToken ct)
    {
        return await _context.CostAdjustments
            .Where(c => c.TenantId == tenantId
                && c.Scope == AdjustmentScope.Project
                && c.ProjectId == projectId)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<CostAdjustment>> GetByCategoryAsync(
        Guid tenantId, CostCategory category, CancellationToken ct)
    {
        return await _context.CostAdjustments
            .Where(c => c.TenantId == tenantId && c.Category == category)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<CostAdjustment>> GetPortfolioAdjustmentsAsync(
        Guid tenantId, CancellationToken ct)
    {
        return await _context.CostAdjustments
            .Where(c => c.TenantId == tenantId && c.Scope == AdjustmentScope.Portfolio)
            .ToListAsync(ct);
    }

    public async Task AddAsync(CostAdjustment adjustment, CancellationToken ct)
    {
        await _context.CostAdjustments.AddAsync(adjustment, ct);
    }

    public void Remove(CostAdjustment adjustment)
    {
        _context.CostAdjustments.Remove(adjustment);
    }

    public IUnitOfWork UnitOfWork => _context;
}
```

### 4. Multi-Tenancy Infrastructure

**TenantDbConnectionInterceptor.cs** (same as DMS/CRM/Maintenance/QA pattern):
```csharp
public class TenantDbConnectionInterceptor : DbConnectionInterceptor
{
    private readonly ITenantContext _tenantContext;

    public override async ValueTask<InterceptionResult> ConnectionOpeningAsync(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result,
        CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId != Guid.Empty)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT kontrolling.set_tenant_context('{tenantId}')";
            await cmd.ExecuteNonQueryAsync(ct);
        }

        return await base.ConnectionOpeningAsync(connection, eventData, result, ct);
    }
}
```

**ITenantContext.cs** (tenant abstraction):
```csharp
public interface ITenantContext
{
    Guid TenantId { get; }
}
```

### 5. Dependency Injection

```csharp
// SpaceOS.Modules.Kontrolling.Infrastructure/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddKontrollingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<KontrollingDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Kontrolling"));
            options.AddInterceptors(new TenantDbConnectionInterceptor());
        });

        services.AddScoped<IOverheadConfigRepository, OverheadConfigRepository>();
        services.AddScoped<ICostAdjustmentRepository, CostAdjustmentRepository>();

        return services;
    }
}
```

### 6. Database Migrations

**Manual Migration**: `20260707_InitialCreate.cs`

Creates 3 tables:
1. `kontrolling.overhead_configs` (OverheadConfig aggregate root)
2. `kontrolling.overhead_rules` (owned collection)
3. `kontrolling.cost_adjustments` (CostAdjustment aggregate root)

**⚠️ IMPORTANT:** NO table for ProjectCostCalculation — it's calculated on-demand!

**Schema**: `kontrolling`

**Migration Commands**:
```bash
dotnet ef migrations add InitialCreate --project src/Infrastructure
dotnet ef database update --project src/Infrastructure
```

### 7. Integration Tests (Testcontainers PostgreSQL 16)

**IntegrationTestFixture.cs** (Testcontainers lifecycle):
```csharp
public class IntegrationTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("kontrolling_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    public KontrollingDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<KontrollingDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;

        var context = new KontrollingDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public Task InitializeAsync() => _postgresContainer.StartAsync();
    public Task DisposeAsync() => _postgresContainer.DisposeAsync().AsTask();
}
```

**BasicRepositoryTests.cs** (5 core scenarios):
```csharp
public class BasicRepositoryTests : IClassFixture<IntegrationTestFixture>
{
    [Fact]
    public async Task OverheadConfigRepository_CanCreateAndRetrieveConfig()
    {
        // Tenant-level config CRUD validation
        // Verify unique constraint (one config per tenant)
    }

    [Fact]
    public async Task OverheadConfigRepository_CanUpdateConfigWithRules()
    {
        // Owned collection test (OverheadRules management)
        // Add/remove category exclusions, custom rates
    }

    [Fact]
    public async Task CostAdjustmentRepository_CanCreateAndRetrieveAdjustment()
    {
        // CostAdjustment CRUD validation
        // Verify Scope handling (Project vs Portfolio)
    }

    [Fact]
    public async Task CostAdjustmentRepository_CanFilterByCategory()
    {
        // Query by CostCategory (Material, Labor, Overhead, etc.)
    }

    [Fact]
    public async Task MultiTenant_ConfigsFromDifferentTenants()
    {
        // Multi-tenant isolation verification (RLS-protected lookups)
        // Ensure tenant A cannot access tenant B's config
    }
}
```

---

## Pattern Reuse Validation

**Patterns from Previous Modules**:
- ✅ **DMS Week 3**: TenantDbConnectionInterceptor, StronglyTypedId conversions, snake_case, PostgreSQL schema isolation
- ✅ **HR Week 3**: Hybrid repository pattern (2-param RLS + 3-param explicit tenant)
- ✅ **Maintenance Week 3**: Owned collection configuration (OwnsMany pattern)
- ✅ **CRM Week 3**: Multiple value objects (Money VO for adjustments)

**Kontrolling-Specific Pattern**: MINIMAL STORAGE (only config + adjustments stored, NOT calculated results)

---

## Architectural Validation Checklist

**ADR-055 Compliance:**
- [ ] ✅ ProjectCostCalculation is NOT a DbSet (calculated on-demand)
- [ ] ✅ OverheadConfig stores tenant-level configuration ONLY
- [ ] ✅ CostAdjustment stores manual corrections ONLY
- [ ] ✅ NO stored EAC, Variance, or Margin values in database
- [ ] ✅ Calculation logic resides in Application Layer (Query Handlers)

**Why This Matters:**
- **One source of truth** — Calculations derive from Production, HR, Finance modules
- **Immutable calculations** — Results are fresh, never stale
- **Minimal storage** — Only metadata stored, not computed results

---

## Quality Gates

1. **Build:** 0 errors, 0 warnings
2. **Tests:** Write integration tests for:
   - OverheadConfig CRUD + owned collection (rules)
   - CostAdjustment CRUD + scope filtering
   - Multi-tenant isolation (RLS)
   - Unique constraint validation (one config per tenant)
3. **Multi-Tenancy:** TenantDbConnectionInterceptor working
4. **Migrations:** Database schema created with 3 tables (NO ProjectCostCalculation table!)
5. **Pattern Validation:** Hybrid repository + calculated layer approach proven

---

## File Structure

```
SpaceOS.Modules.Kontrolling.Infrastructure/
├── Persistence/
│   ├── KontrollingDbContext.cs
│   ├── Configurations/
│   │   ├── OverheadConfigEntityTypeConfiguration.cs
│   │   └── CostAdjustmentEntityTypeConfiguration.cs
│   ├── Repositories/
│   │   ├── OverheadConfigRepository.cs
│   │   └── CostAdjustmentRepository.cs
│   ├── ITenantContext.cs
│   ├── TenantDbConnectionInterceptor.cs
│   └── Migrations/
│       ├── 20260707_InitialCreate.cs
│       └── KontrollingDbContextModelSnapshot.cs
├── DependencyInjection.cs
└── SpaceOS.Modules.Kontrolling.Infrastructure.csproj

tests/Integration/
├── IntegrationTestFixture.cs
└── BasicRepositoryTests.cs
```

---

## Estimated Effort

**60 NWT (~2 hours)**

Breakdown:
- DbContext + Entity Configurations: 15 NWT (30 min) — 2 aggregates, 1 owned collection, SIMPLER than CRM
- Repositories (hybrid pattern): 15 NWT (30 min) — 2 repositories
- Multi-tenancy (interceptor + tenant context): 5 NWT (10 min) — reuse from DMS/CRM
- Migrations (manual): 10 NWT (20 min) — 3 tables (NOT 6 like CRM)
- Integration Tests: 15 NWT (30 min) — 5 scenarios + unique constraint validation

---

## Acceptance Criteria

- [ ] KontrollingDbContext with OverheadConfig + CostAdjustment DbSets, schema: "kontrolling"
- [ ] 2 Entity Type Configurations (OverheadConfig with owned rules, CostAdjustment with Money VOs)
- [ ] 2 Repositories implementing hybrid 2-param + 3-param pattern
- [ ] TenantDbConnectionInterceptor + DI extension
- [ ] Database migrations (manual InitialCreate + ModelSnapshot)
- [ ] **NO ProjectCostCalculation table** (calculated layer validated)
- [ ] Testcontainers PostgreSQL 16 fixture + collection
- [ ] 5 integration test scenarios with unique constraint verification
- [ ] Build verification: **0 errors, 0 warnings**

---

## References

- Week 1 Domain: MSG-BACKEND-143-DONE (2026-07-04)
- Week 2 Application: MSG-BACKEND-175-BLOCKED (already complete, 115 tests PASS)
- Week 4 API: MSG-BACKEND-144-DONE (2026-07-04)
- Architecture: ADR-055 (JoineryTech Kontrolling Domain Model Design — CALCULATED LAYER)
- Pattern Source: DMS Week 3 (MSG-163-DONE), HR Week 3 (MSG-165-DONE), Maintenance Week 3 (MSG-166-DONE), QA Week 3 (MSG-167-DONE), CRM Week 3 (MSG-183)

---

**Priority:** HIGH — Week 3 Infrastructure remaining (2/2 modules)
**Blocker Status:** ✅ UNBLOCKED (ADR-055 validated, calculated layer approach confirmed)

🤖 Generated by Conductor

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
