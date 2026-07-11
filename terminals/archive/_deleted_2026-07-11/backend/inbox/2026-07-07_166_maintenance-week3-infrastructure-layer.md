---
processed: 2026-07-07
id: MSG-BACKEND-166
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: MSG-CONDUCTOR-103
created: 2026-07-07
epic_id: EPIC-JOINERYTECH-MIGRATION
checkpoint_id: CP-JOINERYTECH-WEEK3-INFRA
estimated_nwt: 150
expected_nwt: 50
---

# Maintenance Week 3 Infrastructure Layer (EF Core 8 + RLS)

## 🎯 GOAL RE-ANCHORING

**Epic:** EPIC-JOINERYTECH-MIGRATION (Week 3 Infrastructure Layer — 3/4 modules)
**Progress:** 50% complete (DMS ✅, HR ✅, Maintenance 🔜, QA ⏸️)
**Your Task:** Implement Maintenance module Infrastructure Layer following DMS Week 3 patterns

**Pattern Reuse:** DMS Week 3 established all patterns — reuse for 67% acceleration!

---

## Executive Summary

Implement the **Maintenance module Infrastructure Layer** (EF Core 8 + PostgreSQL RLS) following the DMS Week 3 pattern.

**Scope:**
- 2 aggregates: **Asset**, **WorkOrder**
- Owned collections: **MaintenanceLog** (Asset), **SparePart** (WorkOrder)
- Repository pattern: **Hybrid** (2-param for point lookups, 3-param for range queries like HR)
- RLS multi-tenancy: PostgreSQL session variables + policies
- Testcontainers integration tests

**Expected acceleration:** 150 NWT → **50 NWT** (67% faster with DMS pattern reuse)

---

## Context from DMS Week 3 ✅

**MSG-BACKEND-163-DONE delivered the following patterns:**

### ✅ Pattern 1: 2-Param Repository Signature
```csharp
public interface IDocumentCategoryRepository
{
    Task<DocumentCategory?> GetByIdAsync(DocumentCategoryId id, CancellationToken ct = default);
    Task<IEnumerable<DocumentCategory>> GetAllAsync(CancellationToken ct = default);
    // NO explicit TenantId parameter — RLS handles filtering!
}
```

### ✅ Pattern 2: RLS PostgreSQL Function
```sql
CREATE OR REPLACE FUNCTION maintenance.set_tenant_context(p_tenant_id UUID)
RETURNS VOID AS $$
BEGIN
    PERFORM set_config('app.tenant_id', p_tenant_id::text, false);
END;
$$ LANGUAGE plpgsql;
```

### ✅ Pattern 3: TenantDbConnectionInterceptor
```csharp
public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
{
    var tenantId = _tenantContext.TenantId;
    if (tenantId != Guid.Empty)
    {
        connection.ExecuteNonQuery($"SELECT maintenance.set_tenant_context('{tenantId}')");
    }
    return base.ConnectionOpening(connection, eventData, result);
}
```

### ✅ Pattern 4: StronglyTypedId EF Core Conversion
```csharp
builder.Property(d => d.Id)
    .HasConversion(
        id => id.Value,
        value => new AssetId(value)
    );
```

### ✅ Pattern 5: Testcontainers Integration
```csharp
public class IntegrationTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("maintenance_test")
        .Build();

    public async Task InitializeAsync() => await _container.StartAsync();
    public async Task DisposeAsync() => await _container.DisposeAsync();
}
```

---

## Context from HR Week 3 ✅

**MSG-BACKEND-165-DONE discovered Hybrid Repository Pattern:**

### ✅ Pattern 6: Hybrid Repository (2-param + 3-param)
```csharp
public interface IEmployeeRepository
{
    // 2-param for point lookups (RLS sufficient)
    Task<Employee?> GetByIdAsync(EmployeeId id, CancellationToken ct = default);

    // 3-param for broad queries (explicit tenant scoping safer)
    Task<Employee?> GetByEmailAsync(TenantId tenantId, string email, CancellationToken ct = default);
    Task<IEnumerable<Employee>> GetActiveByDepartmentAsync(TenantId tenantId, Department department, CancellationToken ct = default);
}
```

**Rationale:**
- Point lookups (by ID): RLS isolation at DB level ✅
- Range queries (by email, department, skill): Explicit tenant filtering safer ✅
- FK-based queries: Implicitly scoped via parent entity's tenant ✅

### ✅ Pattern 7: Owned Collections (OwnsMany)
```csharp
builder.OwnsMany(e => e.Skills, skills =>
{
    skills.ToTable("employee_skills", "hr");
    skills.WithOwner().HasForeignKey("EmployeeId");
    skills.Property<Guid>("Id");
    skills.HasKey("Id");
    skills.Property(s => s.Key).HasConversion<string>().HasMaxLength(50);
    skills.Property(s => s.Level).HasConversion<string>().HasMaxLength(50);
});
```

**RLS on owned collections:**
```sql
-- Owned collection inherits parent's RLS via FK
CREATE POLICY tenant_isolation_employee_skills ON hr.employee_skills
    USING (EXISTS (
        SELECT 1 FROM hr.employees WHERE id = employee_skills."EmployeeId" AND tenant_id = current_setting('app.tenant_id')::uuid
    ));
```

---

## Domain Recap: Maintenance Module

**From Week 1 (MSG-BACKEND-153-DONE):**

### Asset Aggregate
- **Root Entity:** `Asset(AssetId, TenantId, Name, AssetType, SerialNumber, Location, ...)`
- **FSM States:** `Operational`, `UnderMaintenance`, `OutOfService`, `Retired`
- **Owned Collection:** `MaintenanceLog` (separate table `asset_maintenance_logs`)
  - Properties: `LogId`, `Date`, `Description`, `PerformedBy`, `Cost`

### WorkOrder Aggregate
- **Root Entity:** `WorkOrder(WorkOrderId, TenantId, AssetId, Title, Description, ...)`
- **FSM States:** `Scheduled`, `InProgress`, `Completed`, `Cancelled`
- **Owned Collection:** `SpareParts` (separate table `work_order_spare_parts`)
  - Properties: `PartId`, `PartNumber`, `Quantity`, `UnitCost`

---

## Task Specification

### 1. DbContext & Entity Type Configurations

**MaintenanceDbContext.cs** — Central EF Core context with schema "maintenance"
- Manages `Asset` and `WorkOrder` DbSets
- Applies Fluent API configurations

**AssetEntityTypeConfiguration.cs:**
- StronglyTypedId: `AssetId`, `TenantId`
- TenantId index for RLS performance
- Enum conversions: `AssetType` (string), `AssetStatus` (string)
- **Owned collection:** `MaintenanceLog` → separate table `asset_maintenance_logs`
  ```csharp
  builder.OwnsMany(a => a.MaintenanceLogs, logs =>
  {
      logs.ToTable("asset_maintenance_logs", "maintenance");
      logs.WithOwner().HasForeignKey("AssetId");
      logs.Property<Guid>("Id");
      logs.HasKey("Id");
      logs.Property(l => l.Date).IsRequired();
      logs.Property(l => l.Description).HasMaxLength(500);
      logs.Property(l => l.PerformedBy).HasMaxLength(100);
      logs.Property(l => l.Cost).HasColumnType("decimal(10,2)");
  });
  ```

**WorkOrderEntityTypeConfiguration.cs:**
- StronglyTypedId: `WorkOrderId`, `AssetId` (FK), `TenantId`
- TenantId index for RLS
- Enum conversions: `WorkOrderStatus` (string)
- **Owned collection:** `SpareParts` → separate table `work_order_spare_parts`
  ```csharp
  builder.OwnsMany(w => w.SpareParts, parts =>
  {
      parts.ToTable("work_order_spare_parts", "maintenance");
      parts.WithOwner().HasForeignKey("WorkOrderId");
      parts.Property<Guid>("Id");
      parts.HasKey("Id");
      parts.Property(p => p.PartNumber).HasMaxLength(50).IsRequired();
      parts.Property(p => p.Quantity).IsRequired();
      parts.Property(p => p.UnitCost).HasColumnType("decimal(10,2)");
  });
  ```

### 2. Repository Implementations

**AssetRepository.cs** — Hybrid repository pattern
- `GetByIdAsync(AssetId, CancellationToken)` — 2-param, RLS handles isolation
- `GetBySerialNumberAsync(TenantId, string, CancellationToken)` — 3-param, explicit tenant scoping
- `GetByLocationAsync(TenantId, string, CancellationToken)` — 3-param
- `GetByStatusAsync(TenantId, AssetStatus, CancellationToken)` — 3-param
- `AddAsync`, `UpdateAsync` — CRUD operations

**WorkOrderRepository.cs** — Hybrid pattern
- `GetByIdAsync(WorkOrderId, CancellationToken)` — 2-param, RLS isolation
- `GetByAssetAsync(AssetId, CancellationToken)` — 2-param, implicit isolation via FK
- `GetByStatusAsync(TenantId, WorkOrderStatus, CancellationToken)` — 3-param
- `GetScheduledAsync(TenantId, DateOnly, CancellationToken)` — 3-param, range query
- `AddAsync`, `UpdateAsync` — CRUD operations

### 3. Multi-Tenancy & RLS

**TenantDbConnectionInterceptor.cs** — PostgreSQL session context setter
- Reuse DMS Week 3 pattern (change namespace to `maintenance`)
- Intercepts both sync and async connection opening
- Calls `maintenance.set_tenant_context('{tenantId}')` function

**DependencyInjection.cs** — Service registration
- MaintenanceDbContext with Npgsql and interceptor
- AssetRepository, WorkOrderRepository scoped registration
- TenantDbConnectionInterceptor scoped registration

### 4. Database Migrations

**20260707_001_InitialCreate.cs:**
- Creates "maintenance" schema
- `assets` table (~15 columns)
- `work_orders` table (~15 columns)
- `asset_maintenance_logs` table (owned collection)
- `work_order_spare_parts` table (owned collection)
- All indexes: TenantId, AssetId, WorkOrderId

**20260707_002_EnableRLS.cs:**
- Creates PostgreSQL function: `maintenance.set_tenant_context(p_tenant_id UUID)`
- Enables RLS on `assets` table with 4 policies (SELECT, INSERT, UPDATE, DELETE)
- Enables RLS on `work_orders` table with 4 policies
- Enables RLS on `asset_maintenance_logs` via parent FK filtering
- Enables RLS on `work_order_spare_parts` via parent FK filtering
- All policies use: `current_setting('app.tenant_id')::uuid`

### 5. Integration Tests

**IntegrationTestFixture.cs:**
- PostgreSQL 16 Alpine container lifecycle
- Applies migrations automatically
- Xunit collection fixture

**BasicRepositoryTests.cs** — Core scenarios (5 tests)
1. `AssetRepository_CanCreateAndRetrieveAsset` — CRUD validation
2. `AssetRepository_CanUpdateAssetWithLog` — Owned collection addition
3. `WorkOrderRepository_CanCreateAndRetrieveWorkOrder` — WorkOrder CRUD
4. `WorkOrderRepository_CanTransitionWorkOrderState` — FSM state transition (Scheduled → InProgress)
5. `MultiTenant_AssetsFromDifferentTenants` — Multi-tenant isolation verification

---

## Deliverables Checklist

### Infrastructure Layer
- [ ] `src/Infrastructure/Persistence/MaintenanceDbContext.cs`
- [ ] `src/Infrastructure/Persistence/Configurations/AssetEntityTypeConfiguration.cs`
- [ ] `src/Infrastructure/Persistence/Configurations/WorkOrderEntityTypeConfiguration.cs`
- [ ] `src/Infrastructure/Persistence/Repositories/AssetRepository.cs`
- [ ] `src/Infrastructure/Persistence/Repositories/WorkOrderRepository.cs`
- [ ] `src/Infrastructure/Persistence/ITenantContext.cs`
- [ ] `src/Infrastructure/Persistence/TenantDbConnectionInterceptor.cs`
- [ ] `src/Infrastructure/DependencyInjection.cs`

### Migrations
- [ ] `src/Infrastructure/Persistence/Migrations/20260707_001_InitialCreate.cs`
- [ ] `src/Infrastructure/Persistence/Migrations/20260707_002_EnableRLS.cs`
- [ ] `src/Infrastructure/Persistence/Migrations/MaintenanceDbContextModelSnapshot.cs`

### Project Files
- [ ] `src/SpaceOS.Modules.Maintenance.csproj` (add NuGet packages)
- [ ] `tests/SpaceOS.Modules.Maintenance.Tests.csproj` (add Testcontainers)

### Integration Tests
- [ ] `tests/Integration/IntegrationTestFixture.cs`
- [ ] `tests/Integration/BasicRepositoryTests.cs`

---

## Build & Test Verification

```bash
# Build src
cd /opt/spaceos/spaceos-modules-maintenance
dotnet build src/SpaceOS.Modules.Maintenance.csproj

# Build tests
dotnet build tests/SpaceOS.Modules.Maintenance.Tests.csproj

# Run integration tests (Testcontainers)
dotnet test tests/SpaceOS.Modules.Maintenance.Tests.csproj

# Expected: 0 errors, 0 warnings, 5 tests passing
```

---

## Pattern Reuse Validation

| Pattern | Source | Expected Outcome |
|---------|--------|------------------|
| 2-param Repository | DMS Week 3 | ✅ Identical signature |
| Hybrid Repository | HR Week 3 | ✅ 2-param + 3-param mix |
| RLS SQL Function | DMS Week 3 | ✅ Copy-paste with namespace change |
| DbConnectionInterceptor | DMS Week 3 | ✅ Exact copy with "maintenance" namespace |
| StronglyTypedId Conversion | DMS Week 3 | ✅ Same HasConversion pattern |
| Owned Collections | HR Week 3 | ✅ OwnsMany() with separate tables |
| Testcontainers | DMS Week 3 | ✅ PostgreSQL 16 Alpine fixture |

---

## Success Criteria

- [ ] **Build:** 0 errors, 0 warnings
- [ ] **Tests:** 5 integration tests passing
- [ ] **RLS:** All tables have tenant isolation policies
- [ ] **Owned Collections:** MaintenanceLog, SpareParts in separate tables with RLS
- [ ] **Pattern Reuse:** DMS + HR patterns successfully applied

---

## Expected Timeline

**Estimated:** 150 NWT (5 hours) → **Expected:** 50 NWT (~1.5 hours) = **67% faster** 🚀

**Rationale:**
- DMS established patterns ✅
- HR validated owned collections ✅
- Maintenance reuses both → minimal learning curve
- Expected completion: ~12:00-12:30 CEST

---

## Notes

- Reuse DMS Week 3 patterns for consistency
- Follow HR Week 3 owned collection pattern for MaintenanceLog and SpareParts
- RLS policies on owned collections via parent FK filtering
- Integration tests validate multi-tenancy isolation

---

**Next Module:** QA Week 3 Infrastructure (after Maintenance DONE)

🤖 Generated with Claude Code | Conductor Terminal
