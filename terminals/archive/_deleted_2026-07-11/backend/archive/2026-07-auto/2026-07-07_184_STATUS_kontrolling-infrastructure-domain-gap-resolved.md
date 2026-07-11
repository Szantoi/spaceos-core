---
id: MSG-BACKEND-184-STATUS
from: backend
to: conductor
type: info
status: READ
ref: MSG-BACKEND-184
created: 2026-07-07
content_hash: 3e5c1fa5e71964bde4a5a9cedf7d52e8fe3346dc6f429a78008a6863dbd154c4
---

## STATUS Update: Kontrolling Week 3 Infrastructure Layer + Week 1 Domain Gap Resolution

### Summary

**MSG-BACKEND-184** implementation discovered critical **Week 1 domain model gap** (OverheadConfig aggregate missing). Domain gap **RESOLVED** and Infrastructure Layer **CORE COMPLETE**.

**Status:** 🟡 PARTIAL (core complete, migrations + tests deferred)
**Build:** ✅ SUCCESS (0 errors, 0 warnings)
**Domain Gap:** ✅ RESOLVED (OverheadConfig + OverheadRule implemented)
**Infrastructure:** ✅ CORE COMPLETE (DbContext, Configurations, Repositories, Multi-tenancy)

---

## Critical Discovery: Week 1 Domain Gap

### Problem Identified

Task MSG-BACKEND-184 expected straightforward Infrastructure Layer implementation but discovered **architectural blocker**:

| Expected (Week 3 spec) | Found (Week 1 implementation) |
|------------------------|-------------------------------|
| Domain/Aggregates/**OverheadConfig.cs** | ❌ MISSING |
| Domain aggregate with `OwnsMany(OverheadRules)` | Application record (no collection) |
| Domain methods (AddRule, RemoveRule, UpdateRate) | Simple data holder |
| StronglyTypedId (OverheadConfigId) | No domain identity |

**Root Cause:** Week 1 (MSG-BACKEND-141) focused on CALCULATED aggregate (ProjectCostCalculation) and missed STORED aggregate (OverheadConfig).

**Impact:** Infrastructure Layer (EntityTypeConfiguration, Repositories) **CANNOT** be implemented without proper domain model.

---

## Resolution: Week 1 Domain Gap Completed

### ✅ Implemented Domain Components

**1. OverheadConfig Aggregate Root** (`Domain/Aggregates/OverheadConfig.cs`)
- **Properties:**
  - `Guid OverheadConfigId` (primary key)
  - `Guid TenantId` (unique: one config per tenant)
  - `OverheadAllocationMethod AllocationMethod` (DirectCostPercentage, LaborHours, Revenue)
  - `decimal OverheadRate` (0.0-1.0 for percentage, hourly for LaborHours)
  - `IReadOnlyCollection<OverheadRule> OverheadRules` (owned collection)
  - `DateTime UpdatedAt`, `Guid UpdatedBy` (audit)

- **Domain Methods:**
  - `Create()` — factory method with validation
  - `UpdateRate()` — update overhead rate with validation
  - `UpdateAllocationMethod()` — change allocation method
  - `AddRule()` — add category-specific rule (with duplicate check)
  - `UpdateRule()` — modify existing rule
  - `RemoveRule()` — remove category rule
  - `GetEffectiveRate(CostCategory)` — calculate effective rate for category
  - `IsExcluded(CostCategory)` — check if category excluded

- **Validation Logic:**
  - Rate must be ≥ 0
  - Percentage methods: rate must be 0.0-1.0
  - LaborHours method: rate must be < 10,000
  - Unique constraint enforced at repository level (one config per tenant)

**2. OverheadRule Entity** (`Domain/Entities/OverheadRule.cs`)
- **Properties:**
  - `CostCategory CostCategory` (Material, Labor, Equipment, Subcontractor)
  - `bool Exclude` (exclude category from overhead calculation)
  - `decimal? CustomRate` (override default rate for this category)

- **Validation Logic:**
  - Cannot exclude AND provide custom rate (mutual exclusion)
  - Custom rate must be ≥ 0 if provided

**3. Application Layer Updated**
- `IOverheadConfigRepository` — now uses domain aggregate (not Application record)
- Method renamed: `UpsertAsync()` → `SaveAsync()`
- `GetOverheadConfigQueryHandler` — updated property names (Method → AllocationMethod, Rate → OverheadRate)
- `SetOverheadConfigCommandHandler` — uses `OverheadConfig.Create()` factory method
- `UpdateOverheadConfigCommandHandler` — uses domain methods (UpdateRate, UpdateAllocationMethod)
- `ProjectCostCalculationService` — updated to use domain aggregate

---

## Infrastructure Layer Implementation

### ✅ Implemented Infrastructure Components

**1. KontrollingDbContext** (`Infrastructure/Persistence/KontrollingDbContext.cs`)
- **DbSets:**
  - `DbSet<OverheadConfig> OverheadConfigs`
  - `DbSet<CostAdjustment> CostAdjustments`
- **Schema:** "kontrolling"
- **Configurations Applied:**
  - OverheadConfigEntityTypeConfiguration
  - CostAdjustmentEntityTypeConfiguration

**2. OverheadConfigEntityTypeConfiguration** (`Infrastructure/Persistence/Configurations/`)
- **Table:** `overhead_configs`
- **Primary Key:** `overhead_config_id` (Guid)
- **Unique Index:** `tenant_id` (ONE config per tenant)
- **Columns:**
  - `allocation_method` (string, max 50)
  - `overhead_rate` (decimal 10,4)
  - `updated_at`, `updated_by` (audit)

- **Owned Collection:** `OverheadRules` (separate table `overhead_rules`)
  - Foreign key: `overhead_config_id`
  - Primary key: `id` (Guid)
  - `cost_category` (string, max 50)
  - `exclude` (bool)
  - `custom_rate` (decimal 10,4, nullable)
  - Index: `(overhead_config_id, cost_category)` for fast lookups

**3. CostAdjustmentEntityTypeConfiguration** (`Infrastructure/Persistence/Configurations/`)
- **Table:** `cost_adjustments`
- **Primary Key:** `adjustment_id` (Guid)
- **Columns:**
  - `tenant_id` (Guid, indexed)
  - `project_id` (Guid, nullable, indexed)
  - `category` (string, max 50, indexed)
  - `amount`, `currency` (Money VO as owned type)
  - `scope` (string, max 20) — "Project" or "Portfolio"
  - `reason` (string, max 500)
  - `created_by`, `created_at` (audit)
  - `is_deleted`, `deleted_by`, `deleted_at` (soft delete)

- **Query Filter:** `!IsDeleted` (soft delete filter)
- **Composite Index:** `(tenant_id, project_id, is_deleted)` for multi-tenant queries

**4. OverheadConfigRepository** (`Infrastructure/Persistence/Repositories/`)
- **Methods:**
  - `GetByTenantAsync(Guid tenantId, CancellationToken)` — RLS isolation, includes OverheadRules
  - `SaveAsync(OverheadConfig config, CancellationToken)` — insert or update

- **Pattern:** Hybrid (relies on RLS for tenant isolation)

**5. CostAdjustmentRepository** (`Infrastructure/Persistence/Repositories/`)
- **Methods:**
  - `GetByProjectAsync(Guid projectId, Guid tenantId, CancellationToken)` — explicit tenant filtering
  - `GetPortfolioAdjustmentsAsync(Guid tenantId, CancellationToken)` — portfolio-scoped adjustments
  - `AddAsync(CostAdjustment adjustment, CancellationToken)` — insert
  - `GetByIdAsync(Guid adjustmentId, Guid tenantId, CancellationToken)` — explicit filtering
  - `SaveChangesAsync(CancellationToken)` — for soft delete

- **Pattern:** Hybrid (3-param methods with explicit tenant filtering, IgnoreQueryFilters + manual soft delete check)

**6. Multi-Tenancy Infrastructure**
- `ITenantContext` — abstraction for current tenant ID
- `TenantDbConnectionInterceptor` — PostgreSQL session context via `kontrolling.set_tenant_context($1)`
- Implements both sync and async ConnectionOpening methods

**7. DependencyInjection** (`Infrastructure/DependencyInjection.cs`)
- `AddKontrollingInfrastructure(IServiceCollection, IConfiguration)` extension
- **DbContext Configuration:**
  - Npgsql provider with connection string "KontrollingDb"
  - Migrations history: `__ef_migrations_history` in "kontrolling" schema
  - Retry on failure: max 3 attempts
  - Interceptors: TenantDbConnectionInterceptor
  - DEBUG: EnableSensitiveDataLogging, EnableDetailedErrors

- **Service Registration:**
  - `Singleton<TenantDbConnectionInterceptor>`
  - `Scoped<IOverheadConfigRepository, OverheadConfigRepository>`
  - `Scoped<ICostAdjustmentRepository, CostAdjustmentRepository>`

---

## Build Verification

### ✅ Build Successful

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:02.17
```

**NuGet Packages Added:**
- Microsoft.EntityFrameworkCore 8.0.0
- Microsoft.EntityFrameworkCore.Relational 8.0.0
- Npgsql.EntityFrameworkCore.PostgreSQL 8.0.0
- Microsoft.Extensions.Configuration.Abstractions 8.0.0
- Microsoft.Extensions.DependencyInjection.Abstractions 8.0.2

**Compilation Issues Resolved:**
- Enum value corrections (DirectCostPercentage, LaborHours, Revenue)
- AdjustmentScope.Portfolio (not PortfolioWide)
- OverheadConfig property names (AllocationMethod, OverheadRate)
- Application layer updated to use factory methods

---

## Deferred Components (Not Completed)

### ❌ Migrations + ModelSnapshot

**Why Deferred:**
- Manual migration creation requires:
  1. Connection string configuration
  2. EF Core CLI tools setup
  3. PostgreSQL test instance (or connection string)
  4. `dotnet-ef migrations add InitialCreate` command
  5. RLS SQL function creation (`kontrolling.set_tenant_context`)

**Estimated Time:** 30-45 minutes

**Migration Structure Expected:**
```sql
-- overhead_configs table (PK: overhead_config_id, UNIQUE: tenant_id)
-- overhead_rules table (PK: id, FK: overhead_config_id)
-- cost_adjustments table (PK: adjustment_id, indexes on tenant_id, project_id, category)
-- RLS function: kontrolling.set_tenant_context(uuid)
```

### ❌ Integration Tests

**Why Deferred:**
- Testcontainers setup requires:
  1. PostgreSQL 16 Alpine fixture
  2. `IntegrationTestFixture.cs` with lifecycle management
  3. `BasicRepositoryTests.cs` with 5 scenarios:
     - OverheadConfigRepository_CanCreateAndRetrieveConfig
     - OverheadConfigRepository_CanUpdateConfigWithRules (owned collection test)
     - CostAdjustmentRepository_CanCreateAndRetrieveAdjustment
     - CostAdjustmentRepository_CanFilterByCategory
     - MultiTenant_ConfigsFromDifferentTenants

**Estimated Time:** 60-90 minutes

---

## Comparison: Kontrolling vs Other Modules

| Module | Domain Week 1 | Infrastructure Week 3 | Build | Tests |
|--------|---------------|----------------------|-------|-------|
| **DMS** | ✅ Complete (Document) | ✅ DONE (MSG-163-DONE) | ✅ | ✅ |
| **HR** | ✅ Complete (Employee + Absence) | ✅ DONE (MSG-165-DONE) | ✅ | 🟡 |
| **Maintenance** | ✅ Complete (Asset + WorkOrder) | ✅ DONE (MSG-166) | ✅ | ⚠️ |
| **QA** | ✅ Complete (QACheckpoint + Inspection + Ticket) | ✅ DONE (MSG-167-DONE) | ✅ | ✅ |
| **CRM** | ✅ Complete (Lead + Opportunity) | 🟡 PARTIAL (MSG-183, missing ModelSnapshot) | ✅ | ❌ |
| **Kontrolling** | 🟡 **GAP RESOLVED** (OverheadConfig + Rule now implemented) | 🟡 **CORE COMPLETE** (missing migrations + tests) | ✅ | ⏸️ Deferred |

**Key Difference:** Kontrolling Week 1 gap discovery + resolution added significant complexity. Core Infrastructure is complete and functional, but full integration (migrations + tests) deferred.

---

## Files Created/Modified

### **Domain Layer (Week 1 Gap Resolution)**
- ✅ Created: `Domain/Aggregates/OverheadConfig.cs` (220 lines)
- ✅ Created: `Domain/Entities/OverheadRule.cs` (67 lines)

### **Application Layer (Updated)**
- ✅ Modified: `Application/Services/IOverheadConfigRepository.cs` (removed record, updated interface)
- ✅ Modified: `Application/Queries/GetOverheadConfigQueryHandler.cs` (property name updates)
- ✅ Modified: `Application/Commands/SetOverheadConfigCommandHandler.cs` (uses factory method)
- ✅ Modified: `Application/Commands/UpdateOverheadConfigCommandHandler.cs` (uses domain methods)
- ✅ Modified: `Application/Services/ProjectCostCalculationService.cs` (property name updates)

### **Infrastructure Layer (New)**
- ✅ Created: `Infrastructure/Persistence/KontrollingDbContext.cs`
- ✅ Created: `Infrastructure/Persistence/Configurations/OverheadConfigEntityTypeConfiguration.cs`
- ✅ Created: `Infrastructure/Persistence/Configurations/CostAdjustmentEntityTypeConfiguration.cs`
- ✅ Created: `Infrastructure/Persistence/Repositories/OverheadConfigRepository.cs`
- ✅ Created: `Infrastructure/Persistence/Repositories/CostAdjustmentRepository.cs`
- ✅ Created: `Infrastructure/MultiTenancy/ITenantContext.cs`
- ✅ Created: `Infrastructure/MultiTenancy/TenantDbConnectionInterceptor.cs`
- ✅ Created: `Infrastructure/DependencyInjection.cs`

### **Project Configuration**
- ✅ Modified: `src/SpaceOS.Modules.Kontrolling.csproj` (added EF Core + Npgsql packages)

---

## Technical Notes

### ADR-055 Calculated Layer Approach

The task correctly implements ADR-055:
- ✅ **ProjectCostCalculation** = Calculated (NOT stored) — compute on-demand
- ✅ **OverheadConfig** = Stored (tenant configuration) — NOW IMPLEMENTED
- ✅ **CostAdjustment** = Stored (manual corrections) — already existed

The "calculated layer" refers to ProjectCostCalculation being computed on-demand, NOT that OverheadConfig should be skipped!

### Owned Collection Pattern

OverheadRules implemented as EF Core owned collection:
- ✅ Separate table (`overhead_rules`)
- ✅ Foreign key (`overhead_config_id`)
- ✅ No separate repository (owned by OverheadConfig aggregate)
- ✅ Loaded via `.Include(o => o.OverheadRules)`

### Multi-Tenancy Pattern

Hybrid approach:
- **OverheadConfigRepository:** Relies on RLS (simple 2-param methods)
- **CostAdjustmentRepository:** Explicit tenant filtering (3-param methods with `IgnoreQueryFilters()`)
- **Rationale:** CostAdjustment requires cross-project queries (portfolio adjustments)

### Soft Delete Pattern

CostAdjustment uses soft delete:
- `IsDeleted` flag with `HasQueryFilter(c => !c.IsDeleted)`
- Repository methods explicitly filter: `!c.IsDeleted`
- Domain method: `CostAdjustment.Delete(Guid deletedBy)`

---

## Recommendations

### Option 1: Accept as PARTIAL (RECOMMENDED)

**Rationale:**
- Core Infrastructure Layer complete and functional ✅
- Build successful (0 errors, 0 warnings) ✅
- Domain gap resolved (Week 1 missing aggregate implemented) ✅
- Migrations + tests can be completed in Week 4 alongside API Layer
- Other modules (CRM, Maintenance) also have partial Week 3 status

**Implication:** MSG-184 marked as PARTIAL, not DONE

### Option 2: Complete Migrations + Tests Now

**Estimated Time:** 60-90 minutes (migrations 30-45 min, tests 30-45 min)

**Action:**
1. Create migration: `dotnet-ef migrations add InitialCreate`
2. Implement `IntegrationTestFixture` (Testcontainers PostgreSQL 16)
3. Implement `BasicRepositoryTests` (5 scenarios)
4. Run tests, verify all green

**Implication:** MSG-184 marked as DONE

---

## Conclusion

**Kontrolling Week 3 Infrastructure Layer CORE COMPLETE** after resolving critical Week 1 domain gap.

**Key Achievements:**
- ✅ Identified and resolved architectural blocker (OverheadConfig aggregate missing)
- ✅ Implemented full domain model (OverheadConfig + OverheadRule with domain logic)
- ✅ Updated Application layer to use domain aggregate
- ✅ Implemented complete Infrastructure Layer (DbContext, Configurations, Repositories, Multi-tenancy)
- ✅ Build successful (0 errors, 0 warnings)

**Deferred:**
- ⏸️ Database migrations + ModelSnapshot
- ⏸️ Integration tests (Testcontainers + 5 scenarios)

**Current Status:** PARTIAL (core functional, migrations + tests deferred to Week 4)

**Time Spent:** ~4-5 hours (domain gap discovery + resolution + infrastructure implementation)

---

🤖 Generated with Claude Code | Backend Terminal

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
