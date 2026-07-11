---
id: MSG-BACKEND-187
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: MSG-BACKEND-184
created: 2026-07-07
epic_id: EPIC-CUTTING-Q3
checkpoint_id: CP-CUTTING-WEEK4-API-FINAL
estimated_nwt: 40
content_hash: bc686048322c377864b9ee0230a2bc80325871dcba8956bbaf7aa7e334d2639f
---

# Kontrolling Week 4 API Layer Implementation — FINAL MODULE

**Epic:** EPIC-CUTTING-Q3 (JoineryTech Phase 1-4 Full Stack)
**Checkpoint:** CP-CUTTING-WEEK4-API-FINAL
**Module:** Kontrolling (Cost Controlling & EAC Calculation)
**Phase:** Week 4 — API Layer (FINAL MODULE — Pattern Validation + Epic Completion)

---

## 🎯 Objective

Implement **Minimal API endpoints** for the Kontrolling module with full CQRS/MediatR pattern, covering:
- OverheadConfig CRUD operations (+ owned collection: OverheadRules)
- **ProjectCostCalculation on-demand calculation** (ADR-055 calculated layer)
- CostAdjustment CRUD operations (soft delete)
- Request/Response DTOs
- FluentValidation rules
- API integration tests with Testcontainers + authentication

**Expected Time:** 40 NWT (~80 minutes)

**Pattern Reuse:** DMS + HR + Maintenance + QA + CRM Week 4 API patterns

**Reference ADR:** ADR-055 (Kontrolling Calculated Layer — NO stored EVM state, calculate on-demand)

**Strategic Role:** This is the **FINAL** Week 4 API module — **EPIC-CUTTING-Q3 COMPLETION** 🎉

---

## 📦 Deliverables

### 1. **Application Layer** — CQRS Commands & Queries

**Commands (Write operations):**
```
Application/Commands/
├── SetOverheadConfigCommand.cs        # Create or update (one per tenant)
├── UpdateOverheadConfigCommand.cs
├── AddOverheadRuleCommand.cs          # Owned collection: add category rule
├── RemoveOverheadRuleCommand.cs       # Owned collection: remove category rule
├── CreateCostAdjustmentCommand.cs
└── DeleteCostAdjustmentCommand.cs     # Soft delete
```

**Queries (Read operations):**
```
Application/Queries/
├── GetOverheadConfigQuery.cs
├── CalculateProjectCostQuery.cs       # CALCULATED — ProjectCostCalculation aggregate (NOT stored!)
├── CalculatePortfolioCostQuery.cs     # Calculated portfolio-level costs
├── GetCostAdjustmentQuery.cs
├── ListCostAdjustmentsByProjectQuery.cs
└── GetPortfolioCostAdjustmentsQuery.cs
```

**Handlers:** 12 total (6 command + 6 query handlers)

**DTOs:**
```
Application/DTOs/
├── OverheadConfigDto.cs               # Includes OverheadRules[]
├── OverheadRuleDto.cs                 # From owned collection
├── ProjectCostCalculationDto.cs       # CALCULATED result (EAC, Variance, Margin)
├── PortfolioCostCalculationDto.cs     # Aggregated portfolio costs
├── CostAdjustmentDto.cs
└── CostAdjustmentListDto.cs
```

**Validators:**
```
Application/Validators/
├── SetOverheadConfigCommandValidator.cs
├── AddOverheadRuleCommandValidator.cs
├── CreateCostAdjustmentCommandValidator.cs
└── CalculateProjectCostQueryValidator.cs
```

---

### 2. **API Layer** — Minimal API Endpoints

**File:** `Api/Endpoints/KontrollingEndpoints.cs`

**Endpoint Groups:**

**OverheadConfig Endpoints:**
```csharp
// Overhead Configuration (one per tenant)
GET    /api/kontrolling/overhead-config           → GetOverheadConfigQuery
PUT    /api/kontrolling/overhead-config           → SetOverheadConfigCommand (upsert)
PATCH  /api/kontrolling/overhead-config           → UpdateOverheadConfigCommand
POST   /api/kontrolling/overhead-config/rules     → AddOverheadRuleCommand
DELETE /api/kontrolling/overhead-config/rules/{category} → RemoveOverheadRuleCommand
```

**ProjectCostCalculation Endpoints (CALCULATED):**
```csharp
// Cost Calculation (on-demand, NOT stored)
GET    /api/kontrolling/projects/{projectId}/cost-calculation     → CalculateProjectCostQuery
GET    /api/kontrolling/portfolio/cost-calculation                → CalculatePortfolioCostQuery
```

**CostAdjustment Endpoints:**
```csharp
// Cost Adjustments
POST   /api/kontrolling/cost-adjustments                          → CreateCostAdjustmentCommand
GET    /api/kontrolling/cost-adjustments/{id}                     → GetCostAdjustmentQuery
GET    /api/kontrolling/projects/{projectId}/cost-adjustments     → ListCostAdjustmentsByProjectQuery
GET    /api/kontrolling/portfolio/cost-adjustments                → GetPortfolioCostAdjustmentsQuery
DELETE /api/kontrolling/cost-adjustments/{id}                     → DeleteCostAdjustmentCommand (soft delete)
```

**Total:** 12 endpoints

**Authentication:** JWT Bearer (pattern from Identity module)

**Multi-Tenancy:** Hybrid (OverheadConfig: RLS, CostAdjustment: explicit tenant filtering)

---

### 3. **Integration Tests** — Testcontainers

**File:** `tests/Integration/Api/KontrollingEndpointsTests.cs`

**Test Scenarios (Minimum 7):**
1. ✅ SetOverheadConfig_CreatesConfigForTenant
2. ✅ AddOverheadRule_AddsToOwnedCollection
3. ✅ RemoveOverheadRule_RemovesFromOwnedCollection
4. ✅ CalculateProjectCost_ReturnsCalculatedEAC (NOT stored in DB!)
5. ✅ CalculateProjectCost_WithAdjustments_IncludesAdjustments
6. ✅ CreateCostAdjustment_AddsAdjustment
7. ✅ DeleteCostAdjustment_SoftDeletesAdjustment

**Test Infrastructure:**
- Testcontainers PostgreSQL 16 Alpine
- JWT authentication setup
- Multi-tenant test data
- IAsyncLifetime per test class

**CRITICAL TEST:** Verify ProjectCostCalculation is CALCULATED (not stored in DB)
```csharp
// After CalculateProjectCost API call:
var dbContext = testFixture.GetDbContext();
var storedCalculation = await dbContext.ProjectCostCalculations.FirstOrDefaultAsync();
Assert.Null(storedCalculation); // MUST BE NULL — calculated, not stored!
```

---

## ✅ Acceptance Criteria

### Build & Compile
- ✅ Build SUCCESS (0 errors)
- ✅ Warnings ≤ 3 (nullable reference warnings acceptable)

### API Endpoints
- ✅ 12 endpoints implemented (OverheadConfig: 5, Calculation: 2, Adjustments: 5)
- ✅ All endpoints return correct HTTP status codes (200, 201, 400, 404)
- ✅ Request/Response DTOs validated with FluentValidation

### Integration Tests
- ✅ Minimum 7 test scenarios PASS
- ✅ Testcontainers PostgreSQL setup working
- ✅ JWT authentication tested
- ✅ Multi-tenancy isolation tested
- ✅ **CRITICAL:** ProjectCostCalculation NOT stored in DB (calculated on-demand)

### Pattern Compliance
- ✅ CQRS/MediatR pattern (command/query separation)
- ✅ Minimal API pattern (endpoint registration)
- ✅ Repository pattern (via Application Layer)
- ✅ Multi-tenancy (hybrid: RLS + explicit filtering)
- ✅ Owned collections (OverheadRules separate table)
- ✅ Soft delete (CostAdjustment.IsDeleted)

### ADR-055 Compliance
- ✅ **ProjectCostCalculation = CALCULATED** (NOT stored in DB)
- ✅ OverheadConfig = STORED (tenant configuration)
- ✅ CostAdjustment = STORED (manual corrections)
- ✅ EAC, Variance, Margin calculated on-demand
- ✅ Calculation logic in Query Handlers (NOT DbContext)

---

## 📚 Reference Documentation

**ADR-055:** Kontrolling Calculated Layer Approach
- **ProjectCostCalculation:** Calculated aggregate (NOT DbSet, NOT stored)
  - Inputs: Direct costs, Overhead rate, Revenue, CostAdjustments
  - Outputs: EAC (Estimate at Completion), Variance, Margin
  - Calculation logic: `ProjectCostCalculationService`
- **OverheadConfig:** Stored (tenant-level overhead rate + allocation method)
- **CostAdjustment:** Stored (manual corrections for calculation gaps)

**Unique Constraint:** OverheadConfig has `UNIQUE INDEX (tenant_id)` — one config per tenant!

**Calculation Formula (ADR-055):**
```
DirectCosts = Material + Labor + Equipment + Subcontractor (from Project)
OverheadCosts = DirectCosts × OverheadRate (from OverheadConfig)
TotalCosts = DirectCosts + OverheadCosts + Adjustments (from CostAdjustment)
EAC = TotalCosts
Variance = EAC - BudgetedCost
Margin = Revenue - EAC
```

**Pattern Reuse:**
- DMS Week 4 API: Minimal API pattern, authentication
- HR Week 4 API: Owned collection endpoints
- Maintenance Week 4 API: Multi-entity CRUD pattern
- QA Week 4 API: Integration test structure
- CRM Week 4 API: Owned collection + business workflow

---

## 🔗 Dependencies

**Week 1 (Domain Layer):** ✅ DONE + GAP RESOLVED (MSG-BACKEND-184 — OverheadConfig aggregate implemented)
**Week 2 (Application Layer):** ✅ DONE (MSG-BACKEND-143)
**Week 3 (Infrastructure Layer):** 🟡 PARTIAL (MSG-BACKEND-184 — core complete, migrations + tests deferred)

**Can proceed:** Yes (API Layer depends on Application Layer, not full Infrastructure)

---

## 📊 Effort Estimation

| Component | NWT | Notes |
|-----------|-----|-------|
| Commands + Handlers | 8 | 6 commands × 1.3 NWT average |
| Queries + Handlers | 8 | 6 queries (2 calculated!) × 1.3 NWT |
| DTOs + Validators | 8 | 6 DTOs + 4 validators |
| API Endpoints | 8 | 12 endpoints × 0.7 NWT (calculated complexity) |
| Integration Tests | 8 | 7 scenarios (1 calculated validation!) × 1.1 NWT |
| **TOTAL** | **40 NWT** | **~80 minutes** |

**Acceleration:** 60 NWT → 40 NWT (33% faster via pattern reuse)

---

## 🎯 Strategic Context — EPIC COMPLETION

**Kontrolling Week 4 API = 6th of 6 modules** in JoineryTech Phase 1-4 Full Stack.

**Epic Progress After Completion:**
- Week 1: 6/6 DONE (100%)
- Week 2: 6/6 DONE (100%)
- Week 3: 3/6 DONE + 3/6 PARTIAL (~75%)
- Week 4: 6/6 DONE (ALL MODULES COMPLETE!)

**Expected Epic Progress:** **~90% (NEAR COMPLETION)**

**Remaining Work After This Task:**
- Week 3 migrations + tests completion (optional, can defer)
- Integration testing across modules
- Documentation updates

**CELEBRATE:** This task completes the JoineryTech Phase 1-4 Full Stack implementation core! 🎉

---

## 🚨 Critical Implementation Notes

### ADR-055 Calculated Layer — MUST NOT STORE ProjectCostCalculation

**CORRECT Implementation:**
```csharp
// Query Handler — Calculate on-demand
public class CalculateProjectCostQueryHandler : IRequestHandler<CalculateProjectCostQuery, ProjectCostCalculationDto>
{
    private readonly IProjectCostCalculationService _calculationService;

    public async Task<ProjectCostCalculationDto> Handle(CalculateProjectCostQuery request, CancellationToken cancellationToken)
    {
        // Calculate (NOT retrieve from DB!)
        var calculation = await _calculationService.CalculateAsync(request.ProjectId, cancellationToken);

        // Return DTO (NOT save to DB!)
        return new ProjectCostCalculationDto
        {
            ProjectId = calculation.ProjectId,
            EAC = calculation.EAC,
            Variance = calculation.Variance,
            Margin = calculation.Margin,
            // ... other calculated fields
        };
    }
}
```

**WRONG Implementation (DO NOT DO THIS):**
```csharp
// ❌ WRONG — Do NOT save calculated result to DB
var calculation = await _calculationService.CalculateAsync(projectId);
_dbContext.ProjectCostCalculations.Add(calculation); // ❌ NO!
await _dbContext.SaveChangesAsync(); // ❌ NO!
```

**Rationale (ADR-055):**
- ProjectCostCalculation is **derived state** (EAC, Variance, Margin)
- Storing derived state violates single source of truth
- Direct costs, overhead config, adjustments = source of truth
- Calculate on-demand = always fresh, no stale data

---

**Priority:** High (EPIC-CUTTING-Q3 completion depends on this task)
**Model:** Sonnet (CQRS + calculated layer complexity)

🤖 Generated by Conductor

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
