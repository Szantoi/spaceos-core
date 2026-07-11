---
processed: 2026-07-07
id: MSG-BACKEND-167
from: conductor
to: backend
type: task
priority: high
status: COMPLETED
model: sonnet
ref: MSG-CONDUCTOR-104
created: 2026-07-07
completed: 2026-07-07
epic_id: EPIC-JOINERYTECH-MIGRATION
checkpoint_id: CP-JOINERYTECH-WEEK3-INFRA
estimated_nwt: 120
expected_nwt: 40
actual_nwt: 40
content_hash: 19c1c426d552a24041264a59c48cac78939f5108b8de4ff8240c80fae2e13278
---

# QA Week 3 Infrastructure Layer (EF Core 8 + RLS) — FINAL MODULE

## 🎯 GOAL RE-ANCHORING

**Epic:** EPIC-JOINERYTECH-MIGRATION (Week 3 Infrastructure Layer — 4/4 modules — FINAL!)
**Progress:** 75% complete (DMS ✅, HR ✅, Maintenance ✅, QA 🔜)
**Your Task:** Implement QA module Infrastructure Layer following DMS Week 3 patterns — **Pattern Mastery**

**Pattern Mastery:** 4th iteration — expect smoothest implementation yet!

---

## Executive Summary

Implement the **QA module Infrastructure Layer** (EF Core 8 + PostgreSQL RLS) following the DMS Week 3 pattern. This is the **FINAL module** of Week 3 Infrastructure cascade!

**Scope:**
- 2 aggregates: **QACheckpoint**, **Inspection**
- Owned collections: **CheckpointCriteria** (QACheckpoint), **Defect** (Inspection)
- Repository pattern: **Hybrid** (2-param for point lookups, 3-param for range queries)
- RLS multi-tenancy: PostgreSQL session variables + policies
- Testcontainers integration tests

**Expected acceleration:** 120 NWT → **40 NWT** (67% faster with pattern mastery)

---

## Context from Previous Modules ✅

### DMS Week 3 ✅ — Pattern Establishment
- 2-param Repository signature
- RLS SQL function pattern
- TenantDbConnectionInterceptor
- StronglyTypedId EF Core conversion
- Testcontainers integration

### HR Week 3 ✅ — Hybrid Pattern Discovery
- Hybrid Repository (2-param + 3-param)
- Complex owned entities (PersonalData with nested Address)
- Owned collections (Skills) with RLS
- 5 integration tests passing

### Maintenance Week 3 ✅ — Pattern Reuse
- Hybrid repository validated
- Owned collections (MaintenancePlan, WorkOrderPart)
- 4-table RLS (2 aggregates + 2 owned collections)
- Build: 0 errors, 0 warnings 🏆

**All patterns proven — QA Week 3 is pattern mastery iteration!**

---

## Pattern Library Summary

### ✅ Pattern 1: Hybrid Repository (HR + Maintenance validated)
```csharp
public interface IQACheckpointRepository
{
    // 2-param for point lookups (RLS sufficient)
    Task<QACheckpoint?> GetByIdAsync(QACheckpointId id, CancellationToken ct = default);

    // 3-param for range queries (explicit tenant scoping)
    Task<IEnumerable<QACheckpoint>> GetByProductIdAsync(TenantId tenantId, ProductId productId, CancellationToken ct = default);
    Task<IEnumerable<QACheckpoint>> GetActiveCheckpointsAsync(TenantId tenantId, CancellationToken ct = default);
}
```

### ✅ Pattern 2: Owned Collections (HR + Maintenance validated)
```csharp
builder.OwnsMany(q => q.Criteria, criteria =>
{
    criteria.ToTable("qa_checkpoint_criteria", "qa");
    criteria.WithOwner().HasForeignKey("QACheckpointId");
    criteria.Property<Guid>("Id");
    criteria.HasKey("Id");
    criteria.Property(c => c.Name).HasMaxLength(200).IsRequired();
    criteria.Property(c => c.ExpectedValue).HasMaxLength(500);
    criteria.Property(c => c.Priority).HasConversion<string>().HasMaxLength(50);
});
```

### ✅ Pattern 3: RLS SQL Function (DMS established)
```sql
CREATE OR REPLACE FUNCTION qa.set_tenant_context(p_tenant_id UUID)
RETURNS VOID AS $$
BEGIN
    PERFORM set_config('app.tenant_id', p_tenant_id::text, false);
END;
$$ LANGUAGE plpgsql;
```

### ✅ Pattern 4: TenantDbConnectionInterceptor (All modules validated)
```csharp
public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
{
    var tenantId = _tenantContext.TenantId;
    if (tenantId != Guid.Empty)
    {
        connection.ExecuteNonQuery($"SELECT qa.set_tenant_context('{tenantId}')");
    }
    return base.ConnectionOpening(connection, eventData, result);
}
```

### ✅ Pattern 5: StronglyTypedId Conversion (All modules validated)
```csharp
builder.Property(q => q.Id)
    .HasConversion(
        id => id.Value,
        value => new QACheckpointId(value)
    );
```

### ✅ Pattern 6: Testcontainers (All modules validated)
```csharp
public class IntegrationTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("qa_test")
        .Build();

    public async Task InitializeAsync() => await _container.StartAsync();
    public async Task DisposeAsync() => await _container.DisposeAsync();
}
```

---

## Domain Recap: QA Module

**From Week 1 (Domain Layer):**

### QACheckpoint Aggregate
- **Root Entity:** `QACheckpoint(QACheckpointId, TenantId, ProductId, CheckpointName, CheckpointType, ...)`
- **FSM States:** `Draft`, `Active`, `Suspended`, `Retired`
- **Owned Collection:** `CheckpointCriteria` (separate table `qa_checkpoint_criteria`)
  - Properties: `CriteriaId`, `Name`, `Description`, `ExpectedValue`, `Priority`, `MeasurementType`

### Inspection Aggregate
- **Root Entity:** `Inspection(InspectionId, TenantId, QACheckpointId, ProductId, InspectedBy, ...)`
- **FSM States:** `Scheduled`, `InProgress`, `Passed`, `Failed`, `RequiresReview`
- **Owned Collection:** `Defects` (separate table `inspection_defects`)
  - Properties: `DefectId`, `DefectType`, `Severity`, `Description`, `Location`, `PhotoUrl`

---

## Task Specification

### 1. DbContext & Entity Type Configurations

**QADbContext.cs** — Central EF Core context with schema "qa"
- Manages `QACheckpoint` and `Inspection` DbSets
- Applies Fluent API configurations

**QACheckpointEntityTypeConfiguration.cs:**
- StronglyTypedId: `QACheckpointId`, `ProductId`, `TenantId`
- TenantId index for RLS performance
- Enum conversions: `CheckpointType` (string), `CheckpointStatus` (string)
- **Owned collection:** `CheckpointCriteria` → separate table `qa_checkpoint_criteria`
  ```csharp
  builder.OwnsMany(q => q.Criteria, criteria =>
  {
      criteria.ToTable("qa_checkpoint_criteria", "qa");
      criteria.WithOwner().HasForeignKey("QACheckpointId");
      criteria.Property<Guid>("Id");
      criteria.HasKey("Id");
      criteria.Property(c => c.Name).HasMaxLength(200).IsRequired();
      criteria.Property(c => c.Description).HasMaxLength(1000);
      criteria.Property(c => c.ExpectedValue).HasMaxLength(500);
      criteria.Property(c => c.Priority).HasConversion<string>().HasMaxLength(50);
      criteria.Property(c => c.MeasurementType).HasConversion<string>().HasMaxLength(50);
  });
  ```

**InspectionEntityTypeConfiguration.cs:**
- StronglyTypedId: `InspectionId`, `QACheckpointId`, `ProductId`, `TenantId`
- TenantId index for RLS
- Enum conversions: `InspectionStatus` (string)
- **Owned collection:** `Defects` → separate table `inspection_defects`
  ```csharp
  builder.OwnsMany(i => i.Defects, defects =>
  {
      defects.ToTable("inspection_defects", "qa");
      defects.WithOwner().HasForeignKey("InspectionId");
      defects.Property<Guid>("Id");
      defects.HasKey("Id");
      defects.Property(d => d.DefectType).HasConversion<string>().HasMaxLength(50).IsRequired();
      defects.Property(d => d.Severity).HasConversion<string>().HasMaxLength(50).IsRequired();
      defects.Property(d => d.Description).HasMaxLength(1000);
      defects.Property(d => d.Location).HasMaxLength(200);
      defects.Property(d => d.PhotoUrl).HasMaxLength(500);
  });
  ```

### 2. Repository Implementations

**QACheckpointRepository.cs** — Hybrid repository pattern
- `GetByIdAsync(QACheckpointId, CancellationToken)` — 2-param, RLS handles isolation
- `GetByProductIdAsync(TenantId, ProductId, CancellationToken)` — 3-param, explicit tenant scoping
- `GetActiveCheckpointsAsync(TenantId, CancellationToken)` — 3-param
- `GetByTypeAsync(TenantId, CheckpointType, CancellationToken)` — 3-param
- `AddAsync`, `UpdateAsync` — CRUD operations

**InspectionRepository.cs** — Hybrid pattern
- `GetByIdAsync(InspectionId, CancellationToken)` — 2-param, RLS isolation
- `GetByCheckpointAsync(QACheckpointId, CancellationToken)` — 2-param, implicit isolation via FK
- `GetByStatusAsync(TenantId, InspectionStatus, CancellationToken)` — 3-param
- `GetPendingInspectionsAsync(TenantId, DateOnly, CancellationToken)` — 3-param, range query
- `AddAsync`, `UpdateAsync` — CRUD operations

### 3. Multi-Tenancy & RLS

**TenantDbConnectionInterceptor.cs** — PostgreSQL session context setter
- Reuse DMS/HR/Maintenance pattern (change namespace to `qa`)
- Intercepts both sync and async connection opening
- Calls `qa.set_tenant_context('{tenantId}')` function

**DependencyInjection.cs** — Service registration
- QADbContext with Npgsql and interceptor
- QACheckpointRepository, InspectionRepository scoped registration
- TenantDbConnectionInterceptor scoped registration

### 4. Database Migrations

**20260707_001_InitialCreate.cs:**
- Creates "qa" schema
- `qa_checkpoints` table (~12 columns)
- `inspections` table (~15 columns)
- `qa_checkpoint_criteria` table (owned collection)
- `inspection_defects` table (owned collection)
- All indexes: TenantId, QACheckpointId, InspectionId, ProductId

**20260707_002_EnableRLS.cs:** (OPTIONAL for Week 3 - can defer to Week 4)
- Creates PostgreSQL function: `qa.set_tenant_context(p_tenant_id UUID)`
- Enables RLS on `qa_checkpoints` table with 4 policies (SELECT, INSERT, UPDATE, DELETE)
- Enables RLS on `inspections` table with 4 policies
- Enables RLS on `qa_checkpoint_criteria` via parent FK filtering
- Enables RLS on `inspection_defects` via parent FK filtering
- All policies use: `current_setting('app.tenant_id')::uuid`

**NOTE:** If time is tight, **defer RLS migration to Week 4**. Focus on getting the DbContext + Repository + Tests working first!

### 5. Integration Tests

**IntegrationTestFixture.cs:**
- PostgreSQL 16 Alpine container lifecycle
- Applies migrations automatically
- Xunit collection fixture

**BasicRepositoryTests.cs** — Core scenarios (5 tests)
1. `QACheckpointRepository_CanCreateAndRetrieveCheckpoint` — CRUD validation
2. `QACheckpointRepository_CanUpdateCheckpointWithCriteria` — Owned collection addition
3. `InspectionRepository_CanCreateAndRetrieveInspection` — Inspection CRUD
4. `InspectionRepository_CanTransitionInspectionState` — FSM state transition (Scheduled → InProgress → Passed)
5. `MultiTenant_CheckpointsFromDifferentTenants` — Multi-tenant isolation verification

---

## Deliverables Checklist

### Infrastructure Layer
- [ ] `src/Infrastructure/Persistence/QADbContext.cs`
- [ ] `src/Infrastructure/Persistence/Configurations/QACheckpointEntityTypeConfiguration.cs`
- [ ] `src/Infrastructure/Persistence/Configurations/InspectionEntityTypeConfiguration.cs`
- [ ] `src/Infrastructure/Persistence/Repositories/QACheckpointRepository.cs`
- [ ] `src/Infrastructure/Persistence/Repositories/InspectionRepository.cs`
- [ ] `src/Infrastructure/Persistence/ITenantContext.cs`
- [ ] `src/Infrastructure/Persistence/TenantDbConnectionInterceptor.cs`
- [ ] `src/Infrastructure/DependencyInjection.cs`

### Migrations
- [ ] `src/Infrastructure/Persistence/Migrations/20260707_001_InitialCreate.cs`
- [ ] `src/Infrastructure/Persistence/Migrations/20260707_002_EnableRLS.cs` (OPTIONAL - defer if needed)
- [ ] `src/Infrastructure/Persistence/Migrations/QADbContextModelSnapshot.cs`

### Project Files
- [ ] `src/SpaceOS.Modules.QA.csproj` (add NuGet packages)
- [ ] `tests/SpaceOS.Modules.QA.Tests.csproj` (add Testcontainers)

### Integration Tests
- [ ] `tests/Integration/IntegrationTestFixture.cs`
- [ ] `tests/Integration/BasicRepositoryTests.cs`

---

## Build & Test Verification

```bash
# Build src
cd /opt/spaceos/spaceos-modules-qa
dotnet build src/SpaceOS.Modules.QA.csproj

# Build tests
dotnet build tests/SpaceOS.Modules.QA.Tests.csproj

# Run integration tests (Testcontainers)
dotnet test tests/SpaceOS.Modules.QA.Tests.csproj

# Expected: 0 errors, 0 warnings, 5 tests passing
```

---

## Pattern Reuse Validation

| Pattern | Source | Expected Outcome |
|---------|--------|------------------|
| Hybrid Repository | HR Week 3 | ✅ 2-param + 3-param mix |
| RLS SQL Function | DMS Week 3 | ✅ Copy-paste with namespace change |
| DbConnectionInterceptor | DMS Week 3 | ✅ Exact copy with "qa" namespace |
| StronglyTypedId Conversion | DMS Week 3 | ✅ Same HasConversion pattern |
| Owned Collections | HR + Maintenance | ✅ OwnsMany() with separate tables |
| Testcontainers | All modules | ✅ PostgreSQL 16 Alpine fixture |

---

## Success Criteria

- [ ] **Build:** 0 errors, 0 warnings
- [ ] **Tests:** 5 integration tests passing
- [ ] **RLS:** Optional for Week 3 (defer to Week 4 if needed)
- [ ] **Owned Collections:** CheckpointCriteria, Defects in separate tables
- [ ] **Pattern Mastery:** Smoothest implementation yet (4th iteration)

---

## Expected Timeline

**Estimated:** 120 NWT (4 hours) → **Expected:** 40 NWT (~1.5 hours) = **67% faster** 🚀

**Rationale:**
- DMS established patterns ✅
- HR validated hybrid + owned collections ✅
- Maintenance validated 4-table RLS ✅
- QA is 4th iteration → **pattern mastery** → fastest yet
- Expected completion: ~14:00 CEST

---

## Strategic Priority: SPEED OVER RLS COMPLETENESS

**If implementation is taking longer than expected:**
1. **SKIP RLS migration** (defer to Week 4)
2. **Focus on:** DbContext + Repository + Tests
3. **Validate:** Build passes (0 errors) + basic tests work
4. **RLS can be added later** — infrastructure patterns are the goal!

**Week 3 goal:** Validate Infrastructure Layer patterns across 4 modules
**Week 4 goal:** Add RLS + API Layer

---

## Notes

- Reuse DMS/HR/Maintenance patterns for consistency
- Follow hybrid repository pattern (2-param + 3-param)
- RLS policies optional for Week 3 (can defer)
- Integration tests validate multi-tenancy isolation (if RLS implemented)

---

**This is the FINAL module of Week 3 Infrastructure cascade! 🚀**

After QA DONE → Week 3 COMPLETE → Celebrate! 🎉

🤖 Generated with Claude Code | Conductor Terminal
