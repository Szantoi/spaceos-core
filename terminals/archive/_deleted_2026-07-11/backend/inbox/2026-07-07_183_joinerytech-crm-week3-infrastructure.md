---
id: MSG-BACKEND-183
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
epic_id: EPIC-CUTTING-Q3
estimated_nwt: 60
created: 2026-07-07
processed: 2026-07-07
ref: ADR-054
content_hash: f9cc96cd47e529e66e090ff11e308d90793469c881c1faf192f7f790ac88fbd6
---

# JoineryTech Phase 1 Week 3: CRM Infrastructure Layer

## Context

**Week 1 Status:** ✅ COMPLETE (Domain layer)
- Domain layer: Lead, Opportunity aggregates with FSM
- MSG-BACKEND-146-DONE (2026-07-04)
- Build: 0 errors, 0 warnings

**Week 2 Status:** ✅ COMPLETE (Application layer)
- Application layer: 13 Commands, 6 Queries
- MSG-BACKEND-174-BLOCKED → Specification conflicts identified
- Actual implementation: COMPLETE (13 Command Handlers, 6 Query Handlers, 25 tests)
- Build: 0 errors, 0 warnings

**Week 4 Status:** ✅ COMPLETE (API layer)
- MSG-BACKEND-150-DONE (2026-07-04)
- 20 Minimal API endpoints

## Objective

Implement the **Infrastructure Layer** for the CRM (Customer Relationship Management) module following established patterns from DMS, HR, Maintenance, QA Week 3.

## Architecture Reference

**ADR-054: JoineryTech CRM Domain Model Design**
- **Two Aggregate Roots**: Lead, Opportunity
- **NO Customer aggregate** (Customer is separate module per ADR)
- **Owned Collections**: Activities (entity collection), Tasks (entity collection)
- **Value Objects**: ContactInfo, Money
- **FSM State Management**: Lead FSM (5 states), Opportunity FSM (6 states)

---

## Scope

### 1. DbContext

```csharp
// SpaceOS.Modules.CRM.Infrastructure/Persistence/CRMDbContext.cs
public class CRMDbContext : DbContext
{
    public DbSet<Lead> Leads { get; set; } = null!;
    public DbSet<Opportunity> Opportunities { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("crm");
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
```

**Schema**: `crm`
**Connection**: PostgreSQL via Npgsql
**Interceptor**: TenantDbConnectionInterceptor (session-based multi-tenancy)

### 2. Entity Type Configurations (2 aggregates)

**LeadEntityTypeConfiguration.cs**:
```csharp
public class LeadEntityTypeConfiguration : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        builder.ToTable("leads");

        // Primary key
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id)
            .HasConversion(id => id.Value, value => LeadId.From(value))
            .HasColumnName("id");

        // Multi-tenancy
        builder.Property(l => l.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.HasIndex(l => l.TenantId);

        // FSM Status
        builder.Property(l => l.Status)
            .HasConversion<string>()
            .HasColumnName("status")
            .HasMaxLength(20);

        // Value Object: ContactInfo (Owned)
        builder.OwnsOne(l => l.ContactInfo, contact => {
            contact.Property(c => c.Name).HasColumnName("contact_name").HasMaxLength(200);
            contact.OwnsOne(c => c.Email, email => {
                email.Property(e => e.Value).HasColumnName("contact_email").HasMaxLength(255);
            });
            contact.OwnsOne(c => c.Phone, phone => {
                phone.Property(p => p.Value).HasColumnName("contact_phone").HasMaxLength(50);
            });
            contact.Property(c => c.Company).HasColumnName("contact_company").HasMaxLength(200);
        });

        // Owned Collections: Activities (separate table)
        builder.OwnsMany(l => l.Activities, activities => {
            activities.ToTable("lead_activities");
            activities.WithOwner().HasForeignKey("lead_id");
            activities.Property<Guid>("id").HasColumnName("id");
            activities.HasKey("id");
            activities.Property(a => a.Type).HasColumnName("type").HasMaxLength(50);
            activities.Property(a => a.Timestamp).HasColumnName("timestamp");
            activities.Property(a => a.Description).HasColumnName("description").HasMaxLength(1000);
            activities.Property(a => a.CreatedBy)
                .HasConversion(id => id.Value, value => UserId.From(value))
                .HasColumnName("created_by");
        });

        // Owned Collections: Tasks (separate table)
        builder.OwnsMany(l => l.Tasks, tasks => {
            tasks.ToTable("lead_tasks");
            tasks.WithOwner().HasForeignKey("lead_id");
            tasks.Property<Guid>("id").HasColumnName("id");
            tasks.HasKey("id");
            tasks.Property(t => t.Title).HasColumnName("title").HasMaxLength(200);
            tasks.Property(t => t.DueDate).HasColumnName("due_date");
            tasks.Property(t => t.Completed).HasColumnName("completed");
            tasks.Property(t => t.Priority).HasColumnName("priority").HasMaxLength(20);
        });

        // Other properties
        builder.Property(l => l.Source)
            .HasConversion<string>()
            .HasColumnName("source")
            .HasMaxLength(50);

        builder.Property(l => l.AssignedTo)
            .HasConversion(id => id.Value, value => UserId.From(value))
            .HasColumnName("assigned_to");

        builder.Property(l => l.OpportunityRef).HasColumnName("opportunity_ref");
        builder.Property(l => l.CreatedAt).HasColumnName("created_at");
        builder.Property(l => l.UpdatedAt).HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(l => l.Status);
        builder.HasIndex(l => l.AssignedTo);
    }
}
```

**OpportunityEntityTypeConfiguration.cs**:
```csharp
public class OpportunityEntityTypeConfiguration : IEntityTypeConfiguration<Opportunity>
{
    public void Configure(EntityTypeBuilder<Opportunity> builder)
    {
        builder.ToTable("opportunities");

        // Primary key
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id)
            .HasConversion(id => id.Value, value => OpportunityId.From(value))
            .HasColumnName("id");

        // Multi-tenancy
        builder.Property(o => o.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.HasIndex(o => o.TenantId);

        // FSM Status
        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasColumnName("status")
            .HasMaxLength(20);

        // Value Object: ContactInfo (Owned)
        builder.OwnsOne(o => o.ContactInfo, contact => {
            contact.Property(c => c.Name).HasColumnName("contact_name").HasMaxLength(200);
            contact.OwnsOne(c => c.Email, email => {
                email.Property(e => e.Value).HasColumnName("contact_email").HasMaxLength(255);
            });
            contact.OwnsOne(c => c.Phone, phone => {
                phone.Property(p => p.Value).HasColumnName("contact_phone").HasMaxLength(50);
            });
            contact.Property(c => c.Company).HasColumnName("contact_company").HasMaxLength(200);
        });

        // Value Object: Money (EstimatedValue)
        builder.OwnsOne(o => o.EstimatedValue, money => {
            money.Property(m => m.Amount).HasColumnName("estimated_value_amount").HasColumnType("decimal(18,2)");
            money.Property(m => m.Currency)
                .HasConversion<string>()
                .HasColumnName("estimated_value_currency")
                .HasMaxLength(3);
        });

        // Owned Collections: Activities
        builder.OwnsMany(o => o.Activities, activities => {
            activities.ToTable("opportunity_activities");
            activities.WithOwner().HasForeignKey("opportunity_id");
            activities.Property<Guid>("id").HasColumnName("id");
            activities.HasKey("id");
            activities.Property(a => a.Type).HasColumnName("type").HasMaxLength(50);
            activities.Property(a => a.Timestamp).HasColumnName("timestamp");
            activities.Property(a => a.Description).HasColumnName("description").HasMaxLength(1000);
            activities.Property(a => a.CreatedBy)
                .HasConversion(id => id.Value, value => UserId.From(value))
                .HasColumnName("created_by");
        });

        // Owned Collections: Tasks
        builder.OwnsMany(o => o.Tasks, tasks => {
            tasks.ToTable("opportunity_tasks");
            tasks.WithOwner().HasForeignKey("opportunity_id");
            tasks.Property<Guid>("id").HasColumnName("id");
            tasks.HasKey("id");
            tasks.Property(t => t.Title).HasColumnName("title").HasMaxLength(200);
            tasks.Property(t => t.DueDate).HasColumnName("due_date");
            tasks.Property(t => t.Completed).HasColumnName("completed");
            tasks.Property(t => t.Priority).HasColumnName("priority").HasMaxLength(20);
        });

        // Other properties
        builder.Property(o => o.Probability).HasColumnName("probability").HasColumnType("decimal(5,2)");
        builder.Property(o => o.ExpectedCloseDate).HasColumnName("expected_close_date");
        builder.Property(o => o.AssignedTo)
            .HasConversion(id => id.Value, value => UserId.From(value))
            .HasColumnName("assigned_to");

        builder.Property(o => o.LeadRef).HasColumnName("lead_ref");
        builder.Property(o => o.QuoteRef).HasColumnName("quote_ref");
        builder.Property(o => o.B2BPartnerRef).HasColumnName("b2b_partner_ref");

        builder.Property(o => o.CreatedAt).HasColumnName("created_at");
        builder.Property(o => o.UpdatedAt).HasColumnName("updated_at");
        builder.Property(o => o.ClosedAt).HasColumnName("closed_at");

        // Indexes
        builder.HasIndex(o => o.Status);
        builder.HasIndex(o => o.AssignedTo);
        builder.HasIndex(o => o.ExpectedCloseDate);
    }
}
```

### 3. Repository Implementations (Hybrid Pattern)

**LeadRepository.cs** (2-param + 3-param methods):
```csharp
public class LeadRepository : ILeadRepository
{
    private readonly CRMDbContext _context;

    // 2-param methods (RLS isolation at DB level)
    public async Task<Lead?> GetByIdAsync(LeadId id, CancellationToken ct)
    {
        return await _context.Leads
            .FirstOrDefaultAsync(l => l.Id == id, ct);
    }

    // 3-param methods (explicit tenant scoping for range queries)
    public async Task<IReadOnlyList<Lead>> GetByStatusAsync(
        Guid tenantId, LeadStatus status, CancellationToken ct)
    {
        return await _context.Leads
            .Where(l => l.TenantId == tenantId && l.Status == status)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Lead>> GetByDateRangeAsync(
        Guid tenantId, DateTime startDate, DateTime endDate, CancellationToken ct)
    {
        return await _context.Leads
            .Where(l => l.TenantId == tenantId
                && l.CreatedAt >= startDate
                && l.CreatedAt <= endDate)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Lead>> GetAssignedToUserAsync(
        Guid tenantId, UserId userId, CancellationToken ct)
    {
        return await _context.Leads
            .Where(l => l.TenantId == tenantId && l.AssignedTo == userId)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Lead lead, CancellationToken ct)
    {
        await _context.Leads.AddAsync(lead, ct);
    }

    public IUnitOfWork UnitOfWork => _context;
}
```

**OpportunityRepository.cs** (similar hybrid pattern):
- GetByIdAsync(OpportunityId, CancellationToken) — 2-param
- GetByStatusAsync(Guid, OpportunityStatus, CancellationToken) — 3-param
- GetForecastAsync(Guid, DateTime, DateTime, CancellationToken) — 3-param (with probability filtering)
- GetOverdueTasksAsync(Guid, DateTime, CancellationToken) — 3-param

### 4. Multi-Tenancy Infrastructure

**TenantDbConnectionInterceptor.cs** (same as DMS/Maintenance/QA pattern):
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
            cmd.CommandText = $"SELECT crm.set_tenant_context('{tenantId}')";
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
// SpaceOS.Modules.CRM.Infrastructure/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddCRMInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<CRMDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("CRM"));
            options.AddInterceptors(new TenantDbConnectionInterceptor());
        });

        services.AddScoped<ILeadRepository, LeadRepository>();
        services.AddScoped<IOpportunityRepository, OpportunityRepository>();

        return services;
    }
}
```

### 6. Database Migrations

**Manual Migration**: `20260707_InitialCreate.cs`

Creates 6 tables:
1. `crm.leads` (Lead aggregate root)
2. `crm.opportunities` (Opportunity aggregate root)
3. `crm.lead_activities` (owned collection)
4. `crm.lead_tasks` (owned collection)
5. `crm.opportunity_activities` (owned collection)
6. `crm.opportunity_tasks` (owned collection)

**Schema**: `crm`

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
        .WithDatabase("crm_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    public CRMDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CRMDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;

        var context = new CRMDbContext(options);
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
    public async Task LeadRepository_CanCreateAndRetrieveLead()
    {
        // CRUD validation for Lead aggregate
    }

    [Fact]
    public async Task LeadRepository_CanUpdateLeadWithActivities()
    {
        // Owned collection test (Activities management)
    }

    [Fact]
    public async Task OpportunityRepository_CanCreateAndRetrieveOpportunity()
    {
        // CRUD validation for Opportunity aggregate
    }

    [Fact]
    public async Task OpportunityRepository_CanTransitionOpportunityState()
    {
        // FSM transitions (Draft→Proposal→Negotiation→Won)
    }

    [Fact]
    public async Task MultiTenant_LeadsFromDifferentTenants()
    {
        // Multi-tenant isolation verification (RLS-protected lookups)
    }
}
```

---

## Pattern Reuse Validation

**Patterns from Previous Modules**:
- ✅ **DMS Week 3**: TenantDbConnectionInterceptor, StronglyTypedId conversions, snake_case, PostgreSQL schema isolation
- ✅ **HR Week 3**: Hybrid repository pattern (2-param RLS + 3-param explicit tenant)
- ✅ **Maintenance Week 3**: Owned collection configuration (OwnsMany pattern)
- ✅ **QA Week 3**: Nested owned types (if needed for complex value objects)

**New Pattern**: Multiple owned collections per aggregate (Lead: Activities + Tasks, Opportunity: Activities + Tasks)

---

## Quality Gates

1. **Build:** 0 errors, 0 warnings
2. **Tests:** Write integration tests for:
   - LeadRepository CRUD + owned collections
   - OpportunityRepository CRUD + FSM transitions
   - Multi-tenant isolation (RLS)
   - Owned collection persistence (Activities, Tasks)
3. **Multi-Tenancy:** TenantDbConnectionInterceptor working
4. **Migrations:** Database schema created with all 6 tables
5. **Pattern Validation:** Hybrid repository + owned collections proven

---

## File Structure

```
SpaceOS.Modules.CRM.Infrastructure/
├── Persistence/
│   ├── CRMDbContext.cs
│   ├── Configurations/
│   │   ├── LeadEntityTypeConfiguration.cs
│   │   └── OpportunityEntityTypeConfiguration.cs
│   ├── Repositories/
│   │   ├── LeadRepository.cs
│   │   └── OpportunityRepository.cs
│   ├── ITenantContext.cs
│   ├── TenantDbConnectionInterceptor.cs
│   └── Migrations/
│       ├── 20260707_InitialCreate.cs
│       └── CRMDbContextModelSnapshot.cs
├── DependencyInjection.cs
└── SpaceOS.Modules.CRM.Infrastructure.csproj

tests/Integration/
├── IntegrationTestFixture.cs
└── BasicRepositoryTests.cs
```

---

## Estimated Effort

**60 NWT (~2 hours)**

Breakdown:
- DbContext + Entity Configurations: 20 NWT (40 min) — 2 aggregates, 4 owned collections
- Repositories (hybrid pattern): 15 NWT (30 min) — 2 repositories
- Multi-tenancy (interceptor + tenant context): 5 NWT (10 min) — reuse from DMS
- Migrations (manual): 10 NWT (20 min) — 6 tables
- Integration Tests: 10 NWT (20 min) — 5 scenarios

---

## Acceptance Criteria

- [ ] CRMDbContext with Lead + Opportunity DbSets, schema: "crm"
- [ ] 2 Entity Type Configurations with owned collections (Activities, Tasks)
- [ ] 2 Repositories implementing hybrid 2-param + 3-param pattern
- [ ] TenantDbConnectionInterceptor + DI extension
- [ ] Database migrations (manual InitialCreate + ModelSnapshot)
- [ ] Testcontainers PostgreSQL 16 fixture + collection
- [ ] 5 integration test scenarios with descriptive assertions
- [ ] Build verification: **0 errors, 0 warnings**

---

## References

- Week 1 Domain: MSG-BACKEND-146-DONE (2026-07-04)
- Week 2 Application: Actual implementation COMPLETE (MSG-174-BLOCKED identified specification conflicts)
- Week 4 API: MSG-BACKEND-150-DONE (2026-07-04)
- Architecture: ADR-054 (JoineryTech CRM Domain Model Design)
- Pattern Source: DMS Week 3 (MSG-163-DONE), HR Week 3 (MSG-165-DONE), Maintenance Week 3 (MSG-166-DONE), QA Week 3 (MSG-167-DONE)

---

**Priority:** HIGH — Week 3 Infrastructure remaining (2/2 modules)
**Blocker Status:** ✅ UNBLOCKED (ADR-054 validated, specification conflicts resolved)

🤖 Generated by Conductor

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
