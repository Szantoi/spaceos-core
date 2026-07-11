---
id: MSG-BACKEND-184-BLOCKED
from: backend
to: conductor
type: blocked
status: RESOLVED
ref: MSG-BACKEND-184
created: 2026-07-07
resolved: 2026-07-10
resolved_by: root
resolution: "Kontrolling Week 3 Infrastructure COMPLETED. See MSG-BACKEND-184-DONE (2026-07-08_MSG-BACKEND-184-Kontrolling-Week3-Infrastructure-DONE.md). Domain gap resolved, OverheadConfig implemented."
content_hash: 0a59e9cdd7a5c9edc537b1ef6508941d7552b1ddc1cad8c9aeae6977bdfbc6ca
---

## BLOCKED: Kontrolling Week 3 Infrastructure Layer — Domain Model Gap

### Summary

A **Kontrolling Week 3 Infrastructure Layer** implementation BLOCKED, mert a **domain model hiányos** a Week 1 implementációból.

**Blocker:** OverheadConfig aggregate **NOT FOUND** in Domain layer
**Impact:** Infrastructure Layer cannot be implemented without proper domain model
**Status:** 🔴 BLOCKED (domain gap from Week 1)

---

## Root Cause Analysis

### Task Expectation (MSG-BACKEND-184)

**Task spec szerint 2 aggregate root kell:**
1. **OverheadConfig** — Tenant-level overhead configuration (+ owned collection: OverheadRules)
2. **CostAdjustment** — Manual cost corrections

**Task quote:**
```
Two Aggregate Roots (STORED):
1. OverheadConfig — Tenant-level overhead configuration (rate, allocation method)
2. CostAdjustment — Manual corrections for calculation gaps
```

**Infrastructure Layer expectations:**
- `OverheadConfigEntityTypeConfiguration.cs` with owned collection `OverheadRules`
- `KontrollingDbContext` with `DbSet<OverheadConfig>` + `DbSet<CostAdjustment>`

---

### Current Implementation (Week 1 - MSG-BACKEND-141-DONE)

**Domain Layer (`src/Domain/Aggregates/`):**
- ✅ ProjectCostCalculation.cs — CALCULATED aggregate (NOT stored)
- ❌ **OverheadConfig.cs — MISSING!**

**Domain Layer (`src/Domain/Entities/`):**
- ✅ CostAdjustment.cs — EXISTS (but as Entity, not Aggregate Root)

**Application Layer (`src/Application/Services/`):**
- ⚠️ OverheadConfig defined as **Application record** (NOT domain aggregate):
  ```csharp
  public record OverheadConfig(
      Guid TenantId,
      OverheadAllocationMethod Method,
      decimal Rate,
      DateTime UpdatedAt,
      Guid UpdatedBy
  );
  ```

**Application Layer already uses OverheadConfig:**
- `GetOverheadConfigQueryHandler.cs` — uses `IOverheadConfigRepository`
- `UpdateOverheadConfigCommandHandler.cs` — updates OverheadConfig
- `SetOverheadConfigCommandHandler.cs` — sets OverheadConfig

---

## Architectural Mismatch

| Aspect | **Task Spec (MSG-184)** | **Current Implementation (Week 1)** |
|--------|------------------------|-------------------------------------|
| OverheadConfig location | Domain/Aggregates/ | Application/Services/ |
| OverheadConfig type | Aggregate Root (class) | Record (data holder) |
| Owned collection | ✅ OverheadRules | ❌ MISSING |
| Domain logic | ✅ Business rules | ❌ None (just data) |
| EF Core mapping | ✅ EntityTypeConfiguration | ❌ Cannot map record |

---

## Why This Blocks Week 3 Infrastructure

**EF Core Entity Type Configuration requires domain aggregate:**
- Cannot create `OverheadConfigEntityTypeConfiguration` for Application record
- Cannot use `OwnsMany(o => o.OverheadRules)` — OverheadRules collection doesn't exist
- Cannot map StronglyTypedId conversions — record doesn't have domain identity
- Cannot apply multi-tenancy patterns — no TenantId property management

**Infrastructure Layer cannot be implemented without:**
1. OverheadConfig domain aggregate (Domain/Aggregates/OverheadConfig.cs)
2. OverheadRule entity (Domain/Entities/OverheadRule.cs)
3. Proper aggregate root structure (Id, TenantId, domain methods)

---

## Recommended Solutions

### Option 1: Complete Week 1 Domain Gap (RECOMMENDED)

**Action:**
1. Implement missing **OverheadConfig** aggregate in `Domain/Aggregates/OverheadConfig.cs`:
   - StronglyTypedId: `OverheadConfigId`
   - TenantId property (unique constraint: ONE config per tenant)
   - OverheadRate (decimal 0.0-1.0)
   - AllocationMethod (enum)
   - Owned collection: `List<OverheadRule>` (OverheadRules)
   - Domain methods: `AddRule()`, `RemoveRule()`, `UpdateRate()`

2. Implement **OverheadRule** entity in `Domain/Entities/OverheadRule.cs`:
   - CostCategory (enum)
   - Exclude (bool) — exclude category from overhead calculation
   - CustomRate (decimal?) — override rate for specific category

3. Update Application layer to use Domain aggregate (not record)

4. Continue with Week 3 Infrastructure Layer

**Estimated Time:** 45-60 minutes (domain model) + 60 NWT (infrastructure)
**Total:** ~2.5-3 hours

### Option 2: Defer to Week 1 Backlog (NOT RECOMMENDED)

**Action:**
1. Create backlog task: "Implement missing OverheadConfig aggregate (Week 1 gap)"
2. Mark MSG-184 as BLOCKED until domain model complete
3. Wait for Week 1 gap resolution

**Implication:** Week 3 Infrastructure remains incomplete

---

## Context: Week 1 Implementation Status

**MSG-BACKEND-141-DONE** (Week 1 Domain Layer) included:
- ✅ ProjectCostCalculation (calculated aggregate)
- ✅ CostAdjustment (entity)
- ✅ Value Objects: Money, CategoryCost, Revenue, Margin
- ❌ **OverheadConfig aggregate — MISSING**

**MSG-BACKEND-143** (Week 2 Application Layer) already implemented:
- ✅ GetOverheadConfigQueryHandler (uses IOverheadConfigRepository)
- ✅ UpdateOverheadConfigCommandHandler
- ✅ SetOverheadConfigCommandHandler

**Application Layer dependency on OverheadConfig:**
- Week 2 implementation **assumes OverheadConfig exists** in Infrastructure
- Infrastructure cannot be implemented without domain model

---

## Comparison: Kontrolling vs Other Modules Week 3

| Module | Domain Week 1 | Infrastructure Week 3 |
|--------|---------------|----------------------|
| **DMS** | ✅ Complete (Document aggregate) | ✅ DONE (MSG-163-DONE) |
| **HR** | ✅ Complete (Employee + Absence) | ✅ DONE (MSG-165-DONE) |
| **Maintenance** | ✅ Complete (Asset + WorkOrder) | ✅ DONE (MSG-166) |
| **QA** | ✅ Complete (QACheckpoint + Inspection + Ticket) | ✅ DONE (MSG-167-DONE) |
| **CRM** | ✅ Complete (Lead + Opportunity) | 🟡 PARTIAL (MSG-183, missing ModelSnapshot) |
| **Kontrolling** | 🔴 **INCOMPLETE** (OverheadConfig MISSING) | ❌ **BLOCKED** (cannot implement) |

---

## Conclusion

**Kontrolling Week 3 Infrastructure Layer BLOCKED** due to missing OverheadConfig domain aggregate from Week 1.

**Recommendation:**
- **Complete Week 1 domain gap FIRST** (implement OverheadConfig + OverheadRule)
- **Then continue** with Week 3 Infrastructure Layer

**Current Status:** BLOCKED (awaiting domain model completion)

---

## Technical Notes

### ADR-055 Calculated Layer Approach

The task references **ADR-055: CALCULATED LAYER APPROACH**:
- **ProjectCostCalculation** = Calculated (NOT stored) ✅ Implemented correctly
- **OverheadConfig** = Stored (tenant configuration) ❌ MISSING from Domain
- **CostAdjustment** = Stored (manual corrections) ✅ Implemented (but as Entity, not Aggregate Root)

The "calculated layer" means ProjectCostCalculation is computed on-demand, NOT that OverheadConfig should be skipped!

### Why OverheadConfig Must Be Domain Aggregate

1. **Business Logic:** Overhead calculation rules are domain logic
2. **Invariants:** One config per tenant (unique constraint)
3. **Owned Collection:** OverheadRules collection requires aggregate root
4. **Identity:** Needs StronglyTypedId (OverheadConfigId)
5. **Multi-Tenancy:** TenantId property management

Application record cannot provide these!

---

🤖 Generated with Claude Code | Backend Terminal

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
