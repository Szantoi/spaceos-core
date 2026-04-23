---
id: MSG-CUTTING-026
from: root
to: cutting
type: task
priority: high
status: READ
ref: SpaceOS_Growth_Strategy_v1.md
created: 2026-04-19
---

# CUTTING-026 — CUTTING Planning v1 Initialization — Phase 1: Data Model

## Context

Soft Launch is **momentarily blocked on Keycloak config** (non-blocking for you). Using this window for **parallel module development**.

**Initiative:** Cutting Planning v1 — enterprise-ready planning layer on Cutting Core v4

**Growth Strategy context:** MaxCut competitive feature parity + Doorstar production flow enablement

**Your scope:** Phase 1 (Data Model + API) — 3 days, ~21 developer-hours

---

## Phase 1: Data Model + API Endpoints (Days 1-3)

### Task 1: Data Model Design

**Entities to implement:**

1. **CuttingPlan**
   - `id` (UUID)
   - `planDate` (DateTime, start of rolling window)
   - `planDays` (int, default 14)
   - `status` (enum: Draft | Approved | InProgress | Closed)
   - `strategyId` (string, "maxcut-v1" | "fifo" | "priority" | "custom")
   - `tenantId` (FK, RLS)
   - `createdAt`, `updatedAt`

2. **DailyPlan** (child of CuttingPlan)
   - `id` (UUID)
   - `cuttingPlanId` (FK)
   - `date` (DateTime)
   - `availableCapacity` (decimal, machine hours)
   - `allocatedCapacity` (computed: sum of jobs)
   - `utilizationPercent` (computed: allocated / available)

3. **CuttingJob** (schedule entry linking Order to DailyPlan)
   - `id` (UUID)
   - `dailyPlanId` (FK)
   - `orderId` (FK to Joinery.Order or procurement.Order)
   - `scheduledDate` (DateTime)
   - `priority` (enum: Urgent | High | Normal | Low)
   - `estimatedTimeHours` (decimal)
   - `status` (enum: Pending | Scheduled | CuttingInProgress | Cut | QC | Delivered)

**Location:** `SpaceOS.Modules.Cutting/Core/CuttingPlan.cs` (main aggregate)

**Tests:** 10+ unit tests (entity creation, validation, computed properties)

---

### Task 2: EF Core Migration + DbContext

**Commands:**
```bash
cd SpaceOS.Modules.Cutting
dotnet ef migrations add AddCuttingPlanAggregate
```

**DbContext updates:**
- `DbSet<CuttingPlan>`
- `DbSet<DailyPlan>`
- `DbSet<CuttingJob>`
- Configure relationships + RLS filters
- Seed test data (1 rolling plan, 14 daily plans, 20 jobs)

**Tests:** 5+ integration tests (DB roundtrip, RLS enforcement)

---

### Task 3: HTTP Endpoints

**API Routes:**

```
POST   /api/cutting/plans
       Create rolling plan
       Body: { planDate, planDays, strategyId }
       Response: 201 + { planId, dailyPlans[14] }

GET    /api/cutting/plans/{planId}
       Retrieve full plan
       Response: 200 + { plan, dailyPlans, jobs, totalYield% }

PUT    /api/cutting/plans/{planId}
       Update plan status or re-schedule jobs
       Body: { status?, jobs[] }
       Response: 200 + updated plan

GET    /api/cutting/plans/{planId}/daily/{date}
       Retrieve specific day
       Response: 200 + { date, availableCapacity, allocatedCapacity, jobs }

GET    /api/cutting/plans
       List plans (paginated, filtered by tenant)
       Response: 200 + { plans[], total, page, pageSize }
```

**Implementation:**
- Use existing `CuttingEndpoints.cs` pattern
- Validate request models (FluentValidation)
- Return appropriate status codes (201, 200, 400, 404)

**Tests:** 5+ E2E tests (HTTP roundtrips)

---

## Technical Specs

**Database:** PostgreSQL (spaceos_cutting)

**RLS:** Tenant isolation via `tid` claim (existing pattern)

**Validation:**
- planDays: 7-90 (rollout window)
- strategyId: must exist + be valid
- planDate: must be >= today

**Error Handling:**
- 400: Invalid input (planDays < 7)
- 404: Plan not found
- 409: Plan status conflict (can't modify Closed plan)

---

## Deliverables (Phase 1 Complete)

- ✅ EF Core migrations applied (3 entities)
- ✅ 5 HTTP endpoints working (CRUD + list)
- ✅ 20+ tests passing (unit + integration + E2E)
- ✅ Postman/curl examples documented
- ✅ Ready for Phase 2 (Strategy pattern implementation)

---

## Timeline

- **Day 1:** Entity design + migration
- **Day 2:** EF Core integration + tests
- **Day 3:** HTTP endpoints + E2E validation

Total: 3 days, ~21 developer-hours

---

## Next: Phase 2

After Phase 1 DONE (MSG-CUTTING-026-DONE):
- Phase 2 inbox message (Strategy pattern implementation)
- Parallel: JOINERY & INVENTORY get their Phase 1 tasks

---

## Reference Docs

- Task plan: `docs/tasks/active/CUTTING-PLANNING-V1_initialization.md`
- Growth strategy: `SpaceOS_Growth_Strategy_v1.md`
- Architecture: `SpaceOS_Modules_Cutting_Core_Architecture_v4.md`

---

**Execute Phase 1 immediately. Report CUTTING-026-DONE when data model + API endpoints complete and tested.**
