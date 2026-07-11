---
id: MSG-BACKEND-190
from: conductor
to: backend
type: task
priority: high
status: UNREAD
model: sonnet
epic_id: EPIC-JT-EHS
estimated_nwt: 120
ref: MSG-BACKEND-189
created: 2026-07-08
content_hash: 5ea44b64a41d06ecf47be4f9112cae2f704e7c64843d939ef84d8f36fddd8142
---

# EHS Week 3: Infrastructure Layer (EF Core, Repositories, RLS, Migrations)

**Epic:** EPIC-JT-EHS (JoineryTech Munkavédelem/Safety Module)
**Pattern:** Proven Week 3 Infrastructure Layer (6 modules reference)
**Dependencies:** Week 2 Application Layer ✅ DONE (MSG-BACKEND-189, ~70 files, ~2630 LOC)

---

## Task Summary

Implement the **Infrastructure Layer** for the EHS module following the established Week 1-4 pattern:
- **EF Core DbContext** with DbConnectionInterceptor (RLS automatic tenant isolation)
- **Repository Implementations** (4 repositories)
- **Entity Type Configurations** (3 aggregates)
- **Database Migration** (initial schema)
- **Unit Tests** for repositories (Testcontainers PostgreSQL)

**Estimated NWT:** 120 (~4-6 hours)

---

## References

**Week 0 OpenAPI Spec:** `/opt/spaceos/spaceos-modules-ehs/docs/openapi.yaml`
- 23 endpoints, 3 aggregates

**Week 1 Domain Layer:** `/opt/spaceos/spaceos-modules-ehs/src/Domain/`
- 3 aggregates: Incident, RiskAssessment, TrainingRecord
- 11 domain events
- 34 unit tests ✅

**Week 2 Application Layer:** `/opt/spaceos/spaceos-modules-ehs/src/Application/` (MSG-BACKEND-189-DONE)
- 13 commands, 10 queries, 12 DTOs
- 4 repository contracts
- AutoMapper profile

**Pattern Reference:** Use identical structure from completed modules:
- `/opt/spaceos/spaceos-modules-crm/src/Infrastructure/`
- `/opt/spaceos/spaceos-modules-kontrolling/src/Infrastructure/`
- `/opt/spaceos/spaceos-modules-hr/src/Infrastructure/`
- `/opt/spaceos/spaceos-modules-maintenance/src/Infrastructure/`
- `/opt/spaceos/spaceos-modules-qa/src/Infrastructure/`
- `/opt/spaceos/spaceos-modules-dms/src/Infrastructure/`

---

## Scope: Infrastructure Layer Components

### 1. EF Core DbContext

**File:** `src/Infrastructure/Data/EhsDbContext.cs`

**DbSets (3):**
```csharp
public DbSet<Incident> Incidents => Set<Incident>();
public DbSet<RiskAssessment> RiskAssessments => Set<RiskAssessment>();
public DbSet<TrainingRecord> TrainingRecords => Set<TrainingRecord>();
```

**Configuration:**
- OnModelCreating: Apply all EntityTypeConfigurations
- No SaveChanges override (domain events handled by MediatR in Week 4)

**Dependencies:**
- Microsoft.EntityFrameworkCore 8.0.x
- Npgsql.EntityFrameworkCore.PostgreSQL 8.0.x

---

### 2. DbConnectionInterceptor (RLS Multi-Tenancy)

**File:** `src/Infrastructure/Data/TenantDbConnectionInterceptor.cs`

**Pattern:** Identical to other modules (HR, Maintenance, QA, DMS)

**Logic:**
```csharp
public override async ValueTask<InterceptionResult> ConnectionOpeningAsync(
    DbConnection connection,
    ConnectionEventData eventData,
    InterceptionResult result,
    CancellationToken ct = default)
{
    var tenantId = _httpContextAccessor.HttpContext?.User?.FindFirst("tenant_id")?.Value;
    if (!string.IsNullOrEmpty(tenantId))
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SET app.tenant_id = @tenantId";
        cmd.Parameters.Add(new NpgsqlParameter("tenantId", tenantId));
        await cmd.ExecuteNonQueryAsync(ct);
    }
    return result;
}
```

**Purpose:** Automatic RLS enforcement — all queries/commands filtered by `tenant_id`

---

### 3. Repository Implementations (4 classes)

**Files:**
- `src/Infrastructure/Repositories/IncidentRepository.cs`
- `src/Infrastructure/Repositories/RiskAssessmentRepository.cs`
- `src/Infrastructure/Repositories/TrainingRecordRepository.cs`
- `src/Infrastructure/Notifications/EhsNotificationService.cs` (stub implementation)

**IncidentRepository Implementation:**
```csharp
public class IncidentRepository : IIncidentRepository
{
    private readonly EhsDbContext _context;

    public IncidentRepository(EhsDbContext context)
    {
        _context = context;
    }

    public async Task<Incident?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        return await _context.Incidents
            .Include(i => i.Investigation)
            .Include(i => i.CorrectiveActions)
            .Include(i => i.Witnesses)
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId, ct);
    }

    public async Task<List<Incident>> ListAsync(IncidentFilter filter, Guid tenantId, CancellationToken ct = default)
    {
        var query = _context.Incidents.AsQueryable();

        query = query.Where(i => i.TenantId == tenantId);

        if (filter.Type.HasValue)
            query = query.Where(i => i.Type == filter.Type.Value);

        if (filter.Status.HasValue)
            query = query.Where(i => i.Status == filter.Status.Value);

        if (filter.OccurredAtRange != null)
        {
            if (filter.OccurredAtRange.From.HasValue)
                query = query.Where(i => i.OccurredAt >= filter.OccurredAtRange.From.Value);
            if (filter.OccurredAtRange.To.HasValue)
                query = query.Where(i => i.OccurredAt <= filter.OccurredAtRange.To.Value);
        }

        if (filter.MinSeverity.HasValue)
            query = query.Where(i => i.Severity >= filter.MinSeverity.Value);

        return await query
            .OrderByDescending(i => i.OccurredAt)
            .ToListAsync(ct);
    }

    public async Task<IncidentSummaryDto> GetSummaryAsync(Guid tenantId, CancellationToken ct = default)
    {
        // Aggregation query returning IncidentSummaryDto
        // Group by Type, Severity, Status
    }

    public async Task<IncidentTrendsDto> GetTrendsAsync(int monthsBack, Guid tenantId, CancellationToken ct = default)
    {
        // Monthly aggregation for last N months
    }

    public async Task AddAsync(Incident incident, CancellationToken ct = default)
    {
        await _context.Incidents.AddAsync(incident, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Incident incident, CancellationToken ct = default)
    {
        _context.Incidents.Update(incident);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        return await _context.Incidents.AnyAsync(i => i.Id == id && i.TenantId == tenantId, ct);
    }
}
```

**RiskAssessmentRepository Implementation:**
```csharp
public class RiskAssessmentRepository : IRiskAssessmentRepository
{
    // Similar structure to IncidentRepository
    // 7 methods: GetById, List, GetRiskMatrix, GetRiskMatrixSummary, Add, Update, Exists
    // Include: Controls (owned entity)
}
```

**TrainingRecordRepository Implementation:**
```csharp
public class TrainingRecordRepository : ITrainingRecordRepository
{
    // Similar structure
    // 6 methods: GetById, List, GetExpiring, GetExpiringTrainings, Add, Exists
    // No owned entities (simple aggregate)
}
```

**EhsNotificationService (Stub):**
```csharp
public class EhsNotificationService : IEhsNotificationService
{
    public Task NotifyIncidentReportedAsync(Guid incidentId, CancellationToken ct = default)
    {
        // TODO: Week 4 — Email/SMS notification integration
        return Task.CompletedTask;
    }

    public Task NotifyHighRiskAssessmentAsync(Guid riskAssessmentId, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task NotifyTrainingExpiringAsync(Guid trainingRecordId, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}
```

---

### 4. Entity Type Configurations (3 files)

**Files:**
- `src/Infrastructure/Data/Configurations/IncidentConfiguration.cs`
- `src/Infrastructure/Data/Configurations/RiskAssessmentConfiguration.cs`
- `src/Infrastructure/Data/Configurations/TrainingRecordConfiguration.cs`

**IncidentConfiguration:**
```csharp
public class IncidentConfiguration : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        builder.ToTable("incidents");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(i => i.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(i => i.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(i => i.Severity)
            .HasColumnName("severity")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(i => i.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(i => i.Location)
            .HasColumnName("location")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(i => i.Description)
            .HasColumnName("description")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(i => i.ReportedBy)
            .HasColumnName("reported_by")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(i => i.OccurredAt)
            .HasColumnName("occurred_at")
            .IsRequired();

        builder.Property(i => i.ReportedAt)
            .HasColumnName("reported_at")
            .IsRequired();

        // Owned entity: Investigation (0-1)
        builder.OwnsOne(i => i.Investigation, investigation =>
        {
            investigation.ToTable("incident_investigations");
            investigation.WithOwner().HasForeignKey("incident_id");
            investigation.Property<Guid>("incident_id").HasColumnName("incident_id");

            investigation.Property(inv => inv.InvestigatedBy)
                .HasColumnName("investigated_by")
                .HasMaxLength(100)
                .IsRequired();

            investigation.Property(inv => inv.InvestigationNotes)
                .HasColumnName("investigation_notes")
                .HasMaxLength(4000);

            investigation.Property(inv => inv.RootCause)
                .HasColumnName("root_cause")
                .HasMaxLength(1000);

            investigation.Property(inv => inv.InvestigatedAt)
                .HasColumnName("investigated_at")
                .IsRequired();
        });

        // Owned entity collection: CorrectiveActions (0-n)
        builder.OwnsMany(i => i.CorrectiveActions, actions =>
        {
            actions.ToTable("incident_corrective_actions");
            actions.WithOwner().HasForeignKey("incident_id");
            actions.Property<Guid>("incident_id").HasColumnName("incident_id");
            actions.HasKey("incident_id", "ActionDescription"); // Composite key

            actions.Property(a => a.ActionDescription)
                .HasColumnName("action_description")
                .HasMaxLength(1000)
                .IsRequired();

            actions.Property(a => a.ResponsiblePerson)
                .HasColumnName("responsible_person")
                .HasMaxLength(100)
                .IsRequired();

            actions.Property(a => a.DueDate)
                .HasColumnName("due_date")
                .IsRequired();

            actions.Property(a => a.CompletedAt)
                .HasColumnName("completed_at");
        });

        // Owned entity collection: Witnesses (0-n)
        builder.OwnsMany(i => i.Witnesses, witnesses =>
        {
            witnesses.ToTable("incident_witnesses");
            witnesses.WithOwner().HasForeignKey("incident_id");
            witnesses.Property<Guid>("incident_id").HasColumnName("incident_id");
            witnesses.HasKey("incident_id", "WitnessName"); // Composite key

            witnesses.Property(w => w.WitnessName)
                .HasColumnName("witness_name")
                .HasMaxLength(100)
                .IsRequired();

            witnesses.Property(w => w.WitnessStatement)
                .HasColumnName("witness_statement")
                .HasMaxLength(2000)
                .IsRequired();

            witnesses.Property(w => w.StatementDate)
                .HasColumnName("statement_date")
                .IsRequired();
        });

        // Indexes
        builder.HasIndex(i => i.TenantId).HasDatabaseName("ix_incidents_tenant_id");
        builder.HasIndex(i => i.Status).HasDatabaseName("ix_incidents_status");
        builder.HasIndex(i => i.OccurredAt).HasDatabaseName("ix_incidents_occurred_at");
    }
}
```

**RiskAssessmentConfiguration:**
```csharp
public class RiskAssessmentConfiguration : IEntityTypeConfiguration<RiskAssessment>
{
    public void Configure(EntityTypeBuilder<RiskAssessment> builder)
    {
        builder.ToTable("risk_assessments");
        builder.HasKey(r => r.Id);

        // Properties: Id, TenantId, Location, Activity, Hazards, Severity, Likelihood, RiskScore, RiskLevel, Status, ReviewDueDate, AssessedBy, AssessedAt

        // Owned entity collection: Controls (0-n)
        builder.OwnsMany(r => r.Controls, controls =>
        {
            controls.ToTable("risk_assessment_controls");
            controls.WithOwner().HasForeignKey("risk_assessment_id");
            controls.Property<Guid>("risk_assessment_id").HasColumnName("risk_assessment_id");
            controls.HasKey("risk_assessment_id", "ControlMeasure"); // Composite key

            controls.Property(c => c.ControlMeasure)
                .HasColumnName("control_measure")
                .HasMaxLength(500)
                .IsRequired();

            controls.Property(c => c.ResponsiblePerson)
                .HasColumnName("responsible_person")
                .HasMaxLength(100)
                .IsRequired();

            controls.Property(c => c.ImplementedAt)
                .HasColumnName("implemented_at")
                .IsRequired();
        });

        // Indexes
        builder.HasIndex(r => r.TenantId).HasDatabaseName("ix_risk_assessments_tenant_id");
        builder.HasIndex(r => r.RiskLevel).HasDatabaseName("ix_risk_assessments_risk_level");
        builder.HasIndex(r => r.Status).HasDatabaseName("ix_risk_assessments_status");
    }
}
```

**TrainingRecordConfiguration:**
```csharp
public class TrainingRecordConfiguration : IEntityTypeConfiguration<TrainingRecord>
{
    public void Configure(EntityTypeBuilder<TrainingRecord> builder)
    {
        builder.ToTable("training_records");
        builder.HasKey(t => t.Id);

        // Properties: Id, TenantId, EmployeeId, EmployeeName, TrainingType, TrainingProvider, CompletionDate, ExpiresAt, CertificateNumber, Notes, TrainingStatus (computed)

        // Indexes
        builder.HasIndex(t => t.TenantId).HasDatabaseName("ix_training_records_tenant_id");
        builder.HasIndex(t => t.EmployeeId).HasDatabaseName("ix_training_records_employee_id");
        builder.HasIndex(t => t.ExpiresAt).HasDatabaseName("ix_training_records_expires_at");
    }
}
```

---

### 5. Database Migration

**File:** `src/Infrastructure/Migrations/YYYYMMDDHHMMSS_InitialEhsSchema.cs`

**Command:**
```bash
cd /opt/spaceos/spaceos-modules-ehs/src/Infrastructure
dotnet ef migrations add InitialEhsSchema --context EhsDbContext
```

**Tables Created (6 total):**
1. `incidents` — main table
2. `incident_investigations` — owned entity (1-1)
3. `incident_corrective_actions` — owned entity collection (1-n)
4. `incident_witnesses` — owned entity collection (1-n)
5. `risk_assessments` — main table
6. `risk_assessment_controls` — owned entity collection (1-n)
7. `training_records` — main table

**RLS Policies (PostgreSQL):**
- Applied manually in Week 4 or via migration SQL
- Pattern: `CREATE POLICY tenant_isolation ON incidents USING (tenant_id = current_setting('app.tenant_id')::uuid);`

---

### 6. Unit Tests (Testcontainers)

**File:** `tests/Infrastructure.Tests/Repositories/IncidentRepositoryTests.cs` (and 2 more)

**Pattern:** Identical to other modules (HR, Maintenance, QA, DMS)

**Setup:**
```csharp
public class IncidentRepositoryTests : IAsyncLifetime
{
    private PostgreSqlContainer _postgresContainer;
    private EhsDbContext _dbContext;
    private IncidentRepository _repository;

    public async Task InitializeAsync()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .Build();

        await _postgresContainer.StartAsync();

        var options = new DbContextOptionsBuilder<EhsDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;

        _dbContext = new EhsDbContext(options);
        await _dbContext.Database.MigrateAsync();

        _repository = new IncidentRepository(_dbContext);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistIncident()
    {
        // Arrange
        var incident = Incident.Create(...);

        // Act
        await _repository.AddAsync(incident);

        // Assert
        var retrieved = await _repository.GetByIdAsync(incident.Id, incident.TenantId);
        Assert.NotNull(retrieved);
        Assert.Equal(incident.Id, retrieved.Id);
    }

    [Fact]
    public async Task ListAsync_WithFilter_ShouldReturnFilteredResults()
    {
        // Test filtering by Type, Status, Severity, DateRange
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }
}
```

**Test Coverage:**
- Repository CRUD operations (Add, GetById, List, Update)
- Filter logic (IncidentFilter, RiskAssessmentFilter, TrainingRecordFilter)
- Aggregation queries (Summary, Trends, Matrix)
- Owned entity persistence (Investigation, CorrectiveActions, Witnesses, Controls)

**Estimated Test Count:** ~30-40 tests across 3 repository test classes

---

## Infrastructure Layer Structure

```
SpaceOS.Modules.EHS.Infrastructure/
  Data/
    EhsDbContext.cs
    TenantDbConnectionInterceptor.cs
    Configurations/
      IncidentConfiguration.cs
      RiskAssessmentConfiguration.cs
      TrainingRecordConfiguration.cs
  Repositories/
    IncidentRepository.cs
    RiskAssessmentRepository.cs
    TrainingRecordRepository.cs
  Notifications/
    EhsNotificationService.cs (stub)
  Migrations/
    YYYYMMDDHHMMSS_InitialEhsSchema.cs
    YYYYMMDDHHMMSS_InitialEhsSchema.Designer.cs
    EhsDbContextModelSnapshot.cs
  SpaceOS.Modules.EHS.Infrastructure.csproj

tests/
  SpaceOS.Modules.EHS.Infrastructure.Tests/
    Repositories/
      IncidentRepositoryTests.cs
      RiskAssessmentRepositoryTests.cs
      TrainingRecordRepositoryTests.cs
    SpaceOS.Modules.EHS.Infrastructure.Tests.csproj
```

---

## Implementation Guidelines

### NuGet Packages

**Infrastructure Project:**
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.10" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.10" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.10" />
<PackageReference Include="Microsoft.AspNetCore.Http.Abstractions" Version="2.2.0" />
```

**Test Project:**
```xml
<PackageReference Include="xUnit" Version="2.9.2" />
<PackageReference Include="Testcontainers.PostgreSql" Version="3.10.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.10" />
```

### DbContext Registration (Week 4)

```csharp
// Program.cs (API Layer)
services.AddDbContext<EhsDbContext>(options =>
{
    options.UseNpgsql(configuration.GetConnectionString("EhsDb"));
    options.AddInterceptors(new TenantDbConnectionInterceptor(httpContextAccessor));
});

services.AddScoped<IIncidentRepository, IncidentRepository>();
services.AddScoped<IRiskAssessmentRepository, RiskAssessmentRepository>();
services.AddScoped<ITrainingRecordRepository, TrainingRecordRepository>();
services.AddScoped<IEhsNotificationService, EhsNotificationService>();
```

---

## Acceptance Criteria

**Quality Gates:**
1. ✅ **EhsDbContext created** with 3 DbSets
2. ✅ **TenantDbConnectionInterceptor** implemented (RLS enforcement)
3. ✅ **4 repository implementations** (Incident, RiskAssessment, TrainingRecord, Notification)
4. ✅ **3 EntityTypeConfigurations** applied
5. ✅ **Initial migration generated** (6 tables + indexes)
6. ✅ **Testcontainers repository tests** (~30-40 tests passing)
7. ✅ **Zero compilation errors, zero warnings**
8. ✅ **Pattern consistency with other 6 modules verified**

**File Counts (estimated):**
- DbContext + Interceptor: 2 files
- Repositories: 4 files
- EntityTypeConfigurations: 3 files
- Migrations: 3 files (migration + designer + snapshot)
- Test classes: 3 files
- Project files: 2 files (.csproj)
- **Total:** ~17 files, ~1500-2000 lines

**Testing:**
- ✅ Testcontainers PostgreSQL integration tests
- ✅ Repository CRUD operations verified
- ✅ Filter logic tested
- ✅ Aggregation queries tested
- ✅ Owned entity persistence verified

---

## Success Criteria

**DONE when:**
1. ✅ EhsDbContext + DbConnectionInterceptor implemented
2. ✅ 4 repository implementations complete
3. ✅ 3 EntityTypeConfigurations applied
4. ✅ Initial migration generated and verified
5. ✅ ~30-40 repository tests passing (Testcontainers)
6. ✅ `dotnet build` → 0 errors, 0 warnings
7. ✅ `dotnet test` → 30-40 tests GREEN
8. ✅ Pattern consistency verified against 6 reference modules

**DONE Outbox Checklist:**
- [ ] Summary: "EHS Week 3 Infrastructure Layer complete — EhsDbContext, 4 repositories, 3 configurations, 6 tables migrated, 30-40 tests GREEN"
- [ ] File count: ~17 files
- [ ] Lines written: ~1500-2000 lines
- [ ] Build verification: `dotnet build` + `dotnet test` output
- [ ] Migration verification: `dotnet ef migrations list` output
- [ ] Pattern consistency: verified against CRM/Kontrolling/HR/Maintenance/QA/DMS

---

## Next Steps (Week 4)

**Week 4: API Layer** (scheduled after Week 3 DONE)
- Minimal API endpoints (23 operations)
- MediatR pipeline integration
- OpenAPI documentation generation
- Full integration tests (Testcontainers + HTTP client)

---

**Priority:** High (EHS Week 3/4 critical path for EPIC-JT-EHS completion)
**Model:** Sonnet (proven Week 3 Infrastructure Layer model across 6 modules)
**Estimated NWT:** 120 (~4-6 hours, proven pattern)

📊 **Generated by Conductor** — EHS Week 3 Infrastructure Layer Dispatch

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
