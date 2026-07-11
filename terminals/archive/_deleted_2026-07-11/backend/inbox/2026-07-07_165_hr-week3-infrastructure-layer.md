---
completed: 2026-07-07
processed: 2026-07-07
id: MSG-BACKEND-165
from: conductor
to: backend
type: task
priority: high
status: COMPLETED
model: sonnet
ref: MSG-CONDUCTOR-100
epic_id: EPIC-JOINERYTECH-MIGRATION
checkpoint_id: CP-JOINERYTECH-WEEK3-INFRA
estimated_nwt: 120
expected_nwt: 40
created: 2026-07-07
content_hash: f77cc2eca695d482c92b063024db03d8a054cd1b9c0174304c5e053d317cab3d
---

# HR Week 3 — Infrastructure Layer (EF Core + RLS)

## 🎯 Mission

**Implementáld az Infrastructure Layer-t a HR modulhoz:**
- EF Core DbContext konfigurációval
- Repository implementációkkal
- RLS (Row-Level Security) multi-tenancy támogatással
- Database migrations-ökkel

**Pattern Reuse:** DMS Week 3 Infrastructure mintákat követve (2-param Repository, RLS, StronglyTypedId)

---

## Context — DMS Week 3 Pattern Established ✅

**DMS Week 3 eredmények:**
- ✅ 2-param Repository pattern (id, ct) — NO explicit tenantId
- ✅ RLS implementation (TenantDbConnectionInterceptor + migrations)
- ✅ StronglyTypedId EF Core conversion
- ✅ Build: 0 errors, 0 warnings
- ⏱️ Duration: ~2 hours (vs ~1h expected)

**HR Week 3 Scope:**
- **2 aggregates:** Employee (+ sub-aggregates: Skills, Absence)
- **Pattern reuse:** DMS-ből established patterns
- **Expected:** 120 NWT (~4h) → 40 NWT (~1.3h) = **67% gyorsabb**

**Te vagy a második (HR)** — Pattern reuse validation!

---

## Scope — HR Week 3 Infrastructure Layer

### 1. EF Core DbContext (HRDbContext)

**Lokáció:** `spaceos-modules-hr/Infrastructure/Persistence/HRDbContext.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.HR.Domain.Employees;

namespace SpaceOS.Modules.HR.Infrastructure.Persistence;

public class HRDbContext : DbContext
{
    public HRDbContext(DbContextOptions<HRDbContext> options) : base(options) { }

    public DbSet<Employee> Employees { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("hr");

        modelBuilder.ApplyConfiguration(new EmployeeEntityTypeConfiguration());
    }
}
```

**Connection string konfig:** `appsettings.json` vagy DI extension

### 2. Entity Type Configurations

**Lokáció:** `spaceos-modules-hr/Infrastructure/Persistence/Configurations/`

**EmployeeEntityTypeConfiguration.cs:**
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.HR.Domain.Employees;

namespace SpaceOS.Modules.HR.Infrastructure.Persistence.Configurations;

public class EmployeeEntityTypeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees", "hr");

        builder.HasKey(e => e.Id);

        // StronglyTypedId conversion (DMS pattern)
        builder.Property(e => e.Id)
            .HasConversion(
                id => id.Value,
                value => new EmployeeId(value)
            );

        // TenantId index for RLS performance (DMS pattern)
        builder.Property(e => e.TenantId).IsRequired();
        builder.HasIndex(e => e.TenantId);

        // Owned entities: PersonalInfo, ContactInfo
        builder.OwnsOne(e => e.PersonalInfo, personalInfo =>
        {
            personalInfo.Property(p => p.FirstName).HasMaxLength(100).IsRequired();
            personalInfo.Property(p => p.LastName).HasMaxLength(100).IsRequired();
            personalInfo.Property(p => p.DateOfBirth).IsRequired();
            personalInfo.Property(p => p.TaxNumber).HasMaxLength(20);
        });

        builder.OwnsOne(e => e.ContactInfo, contactInfo =>
        {
            contactInfo.Property(c => c.Email).HasMaxLength(200).IsRequired();
            contactInfo.Property(c => c.Phone).HasMaxLength(20);
            contactInfo.Property(c => c.Address).HasMaxLength(500);
        });

        // Owned collections: Skills, Absence
        builder.OwnsMany(e => e.Skills, skills =>
        {
            skills.ToTable("employee_skills", "hr");
            skills.WithOwner().HasForeignKey("EmployeeId");
            skills.Property<Guid>("Id");
            skills.HasKey("Id");
            skills.Property(s => s.Name).HasMaxLength(100).IsRequired();
            skills.Property(s => s.Level).HasConversion<string>().HasMaxLength(50);
        });

        builder.OwnsMany(e => e.Absences, absences =>
        {
            absences.ToTable("employee_absences", "hr");
            absences.WithOwner().HasForeignKey("EmployeeId");
            absences.Property<Guid>("Id");
            absences.HasKey("Id");
            absences.Property(a => a.Type).HasConversion<string>().HasMaxLength(50);
            absences.Property(a => a.StartDate).IsRequired();
            absences.Property(a => a.EndDate).IsRequired();
            absences.Property(a => a.Reason).HasMaxLength(500);
        });

        // EmployeeStatus enum
        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        // Timestamps
        builder.Property(e => e.HiredAt).IsRequired();
        builder.Property(e => e.TerminatedAt);
    }
}
```

### 3. Repository Implementation

**Lokáció:** `spaceos-modules-hr/Infrastructure/Persistence/Repositories/`

**EmployeeRepository.cs:**
```csharp
using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.HR.Application.Contracts;
using SpaceOS.Modules.HR.Domain.Employees;

namespace SpaceOS.Modules.HR.Infrastructure.Persistence.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly HRDbContext _context;

    public EmployeeRepository(HRDbContext context)
    {
        _context = context;
    }

    // ⚠️ FONTOS: DMS 2-param pattern!
    // TenantId NEM kell a method signature-ben — RLS automatikusan szűr!

    public async Task<Employee?> GetByIdAsync(EmployeeId id, CancellationToken ct = default)
    {
        return await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task<IEnumerable<Employee>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Employees.ToListAsync(ct);
    }

    public async Task<IEnumerable<Employee>> GetByStatusAsync(EmployeeStatus status, CancellationToken ct = default)
    {
        return await _context.Employees
            .Where(e => e.Status == status)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Employee>> GetByDepartmentAsync(string department, CancellationToken ct = default)
    {
        return await _context.Employees
            .Where(e => e.Department == department)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Employee employee, CancellationToken ct = default)
    {
        await _context.Employees.AddAsync(employee, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Employee employee, CancellationToken ct = default)
    {
        _context.Employees.Update(employee);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(EmployeeId id, CancellationToken ct = default)
    {
        var employee = await GetByIdAsync(id, ct);
        if (employee != null)
        {
            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync(ct);
        }
    }
}
```

### 4. Database Migrations

**Lokáció:** `spaceos-modules-hr/Infrastructure/Persistence/Migrations/`

**Migration 1: InitialCreate**

```bash
cd spaceos-modules-hr
dotnet ef migrations add InitialCreate --context HRDbContext --output-dir Infrastructure/Persistence/Migrations
```

**Migration 2: EnableRLS**

```bash
dotnet ef migrations add EnableRLS --context HRDbContext --output-dir Infrastructure/Persistence/Migrations
```

**RLS Migration Up() metódusban (DMS pattern):**
```csharp
migrationBuilder.Sql(@"
    -- Tenant context setter function
    CREATE OR REPLACE FUNCTION hr.set_tenant_context(p_tenant_id UUID)
    RETURNS VOID AS $$
    BEGIN
        PERFORM set_config('app.tenant_id', p_tenant_id::text, false);
    END;
    $$ LANGUAGE plpgsql;

    -- RLS Policy: employees
    ALTER TABLE hr.employees ENABLE ROW LEVEL SECURITY;
    CREATE POLICY tenant_isolation_employees ON hr.employees
        USING (tenant_id = current_setting('app.tenant_id')::uuid);

    -- RLS Policy: employee_skills
    ALTER TABLE hr.employee_skills ENABLE ROW LEVEL SECURITY;
    CREATE POLICY tenant_isolation_employee_skills ON hr.employee_skills
        USING (employee_id IN (
            SELECT id FROM hr.employees WHERE tenant_id = current_setting('app.tenant_id')::uuid
        ));

    -- RLS Policy: employee_absences
    ALTER TABLE hr.employee_absences ENABLE ROW LEVEL SECURITY;
    CREATE POLICY tenant_isolation_employee_absences ON hr.employee_absences
        USING (employee_id IN (
            SELECT id FROM hr.employees WHERE tenant_id = current_setting('app.tenant_id')::uuid
        ));
");
```

### 5. TenantDbConnectionInterceptor (DMS pattern reuse)

**Lokáció:** `spaceos-modules-hr/Infrastructure/Persistence/TenantDbConnectionInterceptor.cs`

```csharp
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace SpaceOS.Modules.HR.Infrastructure.Persistence;

public class TenantDbConnectionInterceptor : DbConnectionInterceptor
{
    private readonly ITenantContext _tenantContext;

    public TenantDbConnectionInterceptor(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public override InterceptionResult ConnectionOpening(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId != Guid.Empty)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT hr.set_tenant_context('{tenantId}')";
            command.ExecuteNonQuery();
        }

        return base.ConnectionOpening(connection, eventData, result);
    }

    public override async ValueTask<InterceptionResult> ConnectionOpeningAsync(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result,
        CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId != Guid.Empty)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT hr.set_tenant_context('{tenantId}')";
            await command.ExecuteNonQueryAsync(ct);
        }

        return await base.ConnectionOpeningAsync(connection, eventData, result, ct);
    }
}
```

**ITenantContext interface:** Ha még nincs, hozd létre `Application/Contracts/ITenantContext.cs`-ben (DMS pattern).

### 6. DI Registration Extension (DMS pattern)

**Lokáció:** `spaceos-modules-hr/Infrastructure/DependencyInjection.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SpaceOS.Modules.HR.Application.Contracts;
using SpaceOS.Modules.HR.Infrastructure.Persistence;
using SpaceOS.Modules.HR.Infrastructure.Persistence.Repositories;

namespace SpaceOS.Modules.HR.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddHRInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // DbContext with interceptor (DMS pattern)
        services.AddDbContext<HRDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("HRDatabase");
            options.UseNpgsql(connectionString);
            options.AddInterceptors(sp.GetRequiredService<TenantDbConnectionInterceptor>());
        });

        // Repositories
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();

        // Interceptor
        services.AddScoped<TenantDbConnectionInterceptor>();

        return services;
    }
}
```

### 7. Integration Tests (Testcontainers - DMS pattern)

**Lokáció:** `spaceos-modules-hr/tests/Infrastructure.Tests/Persistence/`

**EmployeeRepositoryTests.cs:**
```csharp
using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.HR.Domain.Employees;
using SpaceOS.Modules.HR.Infrastructure.Persistence;
using SpaceOS.Modules.HR.Infrastructure.Persistence.Repositories;
using Testcontainers.PostgreSql;
using Xunit;

namespace SpaceOS.Modules.HR.Infrastructure.Tests.Persistence;

public class EmployeeRepositoryTests : IAsyncLifetime
{
    private PostgreSqlContainer _postgresContainer;
    private HRDbContext _context;
    private EmployeeRepository _repository;

    public async Task InitializeAsync()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .Build();

        await _postgresContainer.StartAsync();

        var options = new DbContextOptionsBuilder<HRDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;

        _context = new HRDbContext(options);
        await _context.Database.MigrateAsync();

        _repository = new EmployeeRepository(_context);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistEmployee()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var employee = Employee.Create(
            tenantId,
            new PersonalInfo("John", "Doe", DateOnly.FromDateTime(DateTime.Now.AddYears(-30)), "TAX123"),
            new ContactInfo("john.doe@example.com", "+1234567890", "123 Main St"),
            "Engineering",
            Position.Developer
        );

        // Act
        await _repository.AddAsync(employee);

        // Assert
        var retrieved = await _repository.GetByIdAsync(employee.Id);
        Assert.NotNull(retrieved);
        Assert.Equal(employee.Id, retrieved.Id);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }
}
```

**RLS Tests:** Tesztelj multi-tenant isolation-t (DMS pattern).

---

## Acceptance Criteria

### Build Gate ✅

- [ ] `dotnet build` — **0 errors**
- [ ] Warnings: csak non-critical nullable reference warnings megengedettek

### Migration Gate ✅

- [ ] `dotnet ef migrations add InitialCreate` — sikeres
- [ ] `dotnet ef migrations add EnableRLS` — sikeres
- [ ] `dotnet ef database update` — sikeres (Testcontainers PostgreSQL)
- [ ] RLS policies active: `SELECT * FROM pg_policies WHERE schemaname = 'hr';`

### Test Gate ✅

- [ ] Integration tests PASS (Testcontainers)
- [ ] Repository CRUD operations work
- [ ] RLS tenant isolation verified
- [ ] Owned collections (Skills, Absences) persisted correctly

### Security Gate ✅

- [ ] RLS enabled on all tables (employees, employee_skills, employee_absences)
- [ ] TenantId indexed on employees table
- [ ] DbConnectionInterceptor registered in DI
- [ ] No SQL injection vectors

---

## Pattern Reuse from DMS Week 3

### ✅ FOLLOW These Patterns

1. **2-param Repository signature** (id, ct) — NO explicit tenantId
2. **RLS SQL function** per schema (`hr.set_tenant_context`)
3. **TenantDbConnectionInterceptor** with async support
4. **StronglyTypedId EF Core conversion** (HasConversion)
5. **Testcontainers integration tests** (PostgreSQL 16 Alpine)
6. **DI extension method** (AddHRInfrastructure)

### ⚠️ HR-Specific Considerations

**Owned Collections (Skills, Absences):**
- Use `OwnsMany()` for collections
- Create separate tables (`employee_skills`, `employee_absences`)
- RLS policy on owned tables (filter by parent EmployeeId)

**Owned Entities (PersonalInfo, ContactInfo):**
- Use `OwnsOne()` for value objects
- Inline columns in employees table (no separate table)

---

## Timeline Estimate

| Fase | NWT | Idő | Aktivitás |
|------|-----|-----|-----------|
| **DbContext + Configs** | 20 | 40 perc | HRDbContext + EmployeeEntityTypeConfiguration (owned entities!) |
| **Repository** | 10 | 20 perc | EmployeeRepository (DMS pattern reuse) |
| **Migrations** | 25 | 50 perc | InitialCreate + EnableRLS (3 tables: employees + skills + absences) |
| **DI Extension** | 5 | 10 perc | AddHRInfrastructure (copy from DMS) |
| **Integration Tests** | 15 | 30 perc | Testcontainers + owned collections tests |
| **Build + Debugging** | 10 | 20 perc | Error fixes, warnings cleanup |
| **TOTAL** | **85** | **~2.8h** | Actual estimate (with owned collections complexity) |
| **Expected (Pattern Reuse)** | **40** | **~1.3h** | **67% faster** 🚀 |

**Confidence:** HIGH (80%) — DMS pattern proven, owned collections documented.

**Pattern reuse:** DbContext structure, RLS SQL, Interceptor, DI → copy-paste ready!

---

## DONE Outbox Requirements

**Fájlnév:** `2026-07-07_165_hr-week3-infrastructure-layer-done.md`

```yaml
---
id: MSG-BACKEND-165-DONE
from: backend
to: conductor
type: done
status: UNREAD
priority: high
ref: MSG-BACKEND-165
created: YYYY-MM-DD
---

# HR Week 3 Infrastructure Layer — DONE

## Összefoglaló

**✅ Infrastructure Layer teljes mértékben implementálva** a HR modulhoz EF Core + RLS mintával.

### Implementált komponensek

**DbContext:**
- HRDbContext with schema "hr"
- DbSet: Employees
- EmployeeEntityTypeConfiguration applied

**Repository:**
- EmployeeRepository (2-param pattern from DMS)

**Migrations:**
- InitialCreate — 3 tables (employees, employee_skills, employee_absences) + indexes
- EnableRLS — RLS policies for multi-tenancy

**DI Extension:**
- AddHRInfrastructure registration
- TenantDbConnectionInterceptor

**Tests:**
- Testcontainers integration tests
- Owned collections verified

### Build eredmény

Build succeeded.
    X Warning(s) - NON-CRITICAL
    0 Error(s)

### Pattern Reuse Validation

✅ DMS 2-param Repository pattern followed
✅ RLS SQL function (`hr.set_tenant_context`) created
✅ TenantDbConnectionInterceptor reused
✅ StronglyTypedId conversion applied

**Fájlok:** ~12 files (DbContext, Config, Repository, Interceptor, DI, 2 Migrations, ITenantContext, Tests)

## Következő lépések javaslat

Maintenance Week 3 Infrastructure dispatch — Reuse DMS + HR patterns.
```

---

## Referenciák

| Dokumentum | Tartalom |
|---|---|
| `MSG-BACKEND-163-DONE` | DMS Week 3 Infrastructure completion report |
| `docs/knowledge/patterns/DATABASE_PATTERNS.md` | RLS SQL, Owned entities pattern |
| `docs/knowledge/deployment/KNOWN_GOTCHAS.md` | RLS GUC pattern |
| `MSG-CONDUCTOR-100` | Week 3 Infrastructure planning |

---

**Conductor note:** HR Week 3 második Infrastructure modul — validate DMS pattern reuse! Owned collections (Skills, Absences) add complexity.

🚀 Good luck! Pattern reuse validation starts here!

---

## Completion Report
*2026-07-07T07:28:26.336Z*

### Summary
HR Week 3 Infrastructure Layer — Complete. Implemented: HRDbContext with "hr" schema, EmployeeEntityTypeConfiguration + AbsenceEntityTypeConfiguration with complex owned entities/collections, TenantDbConnectionInterceptor for RLS session management, database migrations (InitialCreate + EnableRLS with RLS policies), EmployeeRepository + AbsenceRepository with hybrid 2-param + 3-param pattern, DependencyInjection extension, Testcontainers integration tests (5 core CRUD scenarios). Build: 0 errors, 0 warnings. RLS isolation verified. Ready for Application Layer (CQRS handlers).

### Implementation Details
Infrastructure Layer fully complete and tested. Pattern reuse from DMS Week 3 validated across DbContext configuration, RLS implementation, StronglyTypedId conversion, repository pattern, and dependency injection. Hybrid repository pattern discovered: 2-param methods (GetByIdAsync) rely on RLS at DB level; 3-param methods (GetByEmailAsync, GetActiveByDepartmentAsync) require explicit tenant parameter for safety. Complex owned entities (PersonalData with nested Address) properly configured. Integration tests cover CRUD operations, state transitions, multi-tenant isolation. All acceptance criteria met.

### Files Changed
- `src/Infrastructure/Persistence/HRDbContext.cs`
- `src/Infrastructure/Persistence/Configurations/EmployeeEntityTypeConfiguration.cs`
- `src/Infrastructure/Persistence/Configurations/AbsenceEntityTypeConfiguration.cs`
- `src/Infrastructure/Persistence/Repositories/EmployeeRepository.cs`
- `src/Infrastructure/Persistence/Repositories/AbsenceRepository.cs`
- `src/Infrastructure/Persistence/TenantDbConnectionInterceptor.cs`
- `src/Infrastructure/Persistence/ITenantContext.cs`
- `src/Infrastructure/DependencyInjection.cs`
- `src/Infrastructure/Persistence/Migrations/20260707_001_InitialCreate.cs`
- `src/Infrastructure/Persistence/Migrations/20260707_002_EnableRLS.cs`
- `src/Infrastructure/Persistence/Migrations/HRDbContextModelSnapshot.cs`
- `tests/Integration/IntegrationTestFixture.cs`
- `tests/Integration/BasicRepositoryTests.cs`
- `src/SpaceOS.Modules.HR.csproj`
- `tests/SpaceOS.Modules.HR.Tests.csproj`

### Next Steps
1. Application/CQRS handlers implementation (separate epic) — Commands: CreateEmployeeCommand, ApproveAbsenceCommand; Handlers with FluentValidation; DTOs. 2. API Layer — Minimal API endpoints (/employees GET/POST, /absences POST/APPROVE). 3. E2E testing — End-to-end request flow validation. 4. Integration with Kernel Identity service for user/tenant context injection.

