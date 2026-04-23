---
id: CUTTING-PLANNING-V1
title: Cutting Planning v1 Initialization — N-day Rolling Plan + Strategy Pattern
status: active
priority: high
assignee: CUTTING
epic: cutting-planning-v1
blocked_by: none
created: 2026-04-19
updated: 2026-04-19
docs:
  - docs/tasks/new/SpaceOS_Growth_Strategy_v1.md
  - SpaceOS_Modules_Cutting_Vision_v1.md
  - SpaceOS_Modules_Cutting_Core_Architecture_v4.md
---

# CUTTING-PLANNING-V1 — Initialization

## Overview

Foundation for **Cutting Planning v1** — enterprise-ready planning layer on top of existing Cutting Core (v4).

**Scope:** N-day rolling plan + Strategy pattern architecture + Extension points for v1.5/v2

**Why:** MaxCut competitive feature parity + Doorstar production flow enablement

---

## Architecture Components

### 1. N-day Rolling Plan Data Model

**Entities:**
- `CuttingPlan` (rolling window, e.g., 14 days)
  - `planDate`: start date
  - `planDays`: window (default 14)
  - `status`: Draft | Approved | InProgress | Closed
  - `strategyId`: FK to Strategy

- `DailyPlan` (per day within rolling window)
  - `date`: plan date
  - `availableCapacity`: machine hours
  - `allocatedCapacity`: sum of job allocations
  - `jobs`: FK to CuttingJob array

- `CuttingJob` (orders scheduled in plan)
  - `jobId`: FK to Order
  - `scheduledDate`: when to cut
  - `priority`: enum (Urgent | High | Normal | Low)
  - `estimatedTime`: hours
  - `status`: Pending | Scheduled | CuttingInProgress | Cut | QC | Delivered

### 2. Strategy Pattern (4 Variants)

**Interface:** `IPlanningStrategy`
```csharp
interface IPlanningStrategy {
  ScheduleJobs(jobs: Job[], capacity: Capacity): DailyPlan[];
  CalculateYield(plan: DailyPlan): decimal;
  GetLabel(job: Job): string;
  Validate(plan: DailyPlan): ValidationResult;
}
```

**4 Implementations:**
1. **MaxCutStrategy** — Guillotine + yield optimization (default)
2. **FIFOStrategy** — First-in-first-out scheduling (simple)
3. **PriorityStrategy** — By priority + due date
4. **CustomStrategy** — Tenant-specific rules (extension point)

### 3. Extension Points (v1.5/v2)

**For future phases without refactor:**
- `ILabelPrinter` — 4 label variants (v1 basic, v1.5 premium)
- `ISupplierOrderGenerator` — Link to Procurement (v1.5)
- `IJobCostCalculator` — Accurate costing (v1.5)
- `IOffcutTracker` — Offcut reuse workflow (v2)

---

## Development Phases

### Phase 1: Data Model + API (Days 1-3)

**Tasks:**
1. Define `CuttingPlan`, `DailyPlan`, `CuttingJob` entities
2. Create EF Core migrations
3. Implement HTTP endpoints:
   - `POST /api/cutting/plans` — Create rolling plan
   - `GET /api/cutting/plans/{planId}` — Retrieve
   - `PUT /api/cutting/plans/{planId}` — Update jobs/status
   - `GET /api/cutting/plans/{planId}/daily/{date}` — Day details

**Tests:** 20+ unit tests (CRUD, validations)

---

### Phase 2: Strategy Pattern Implementation (Days 4-6)

**Tasks:**
1. Implement `IPlanningStrategy` interface
2. Code 4 strategies: MaxCut, FIFO, Priority, Custom
3. Integrate strategy selection in `POST /plans` endpoint
4. Implement yield calculation + validation

**Tests:** 15+ unit tests (strategy scenarios), 5+ integration tests

---

### Phase 3: UI + Integration (Days 7-10)

**Tasks:**
1. Portal: Rolling plan calendar view + job list
2. Portal: Strategy selector dropdown
3. Integration: Link to Inventory (available materials)
4. Integration: Link to Procurement (supplier sync endpoint)
5. E2E tests: Create plan → Schedule jobs → Generate output

**Tests:** 8+ E2E tests

---

## Data Flow Example

```
User: "Create 14-day rolling plan with MaxCut strategy"
  ↓
POST /api/cutting/plans
  {
    "planDate": "2026-04-19",
    "planDays": 14,
    "strategyId": "maxcut-v1"
  }
  ↓
CuttingPlanner.Create()
  → Fetches orders (status: Pending, due within 14 days)
  → Queries Inventory.GetAvailableMaterials()
  → Runs MaxCutStrategy.ScheduleJobs()
  → Allocates jobs to DailyPlan[0..13]
  ↓
Returns: CuttingPlan { id, dailyPlans[14], totalYield: 91.5% }
  ↓
Portal: Displays calendar, shows:
  - Day 1: 5 jobs scheduled, 6.2 machine hours
  - Day 2: 3 jobs, 4.1 hours
  - ...
  - Yield: 91.5% (MaxCut benchmark)
```

---

## Acceptance Criteria

- ✅ All 4 strategies working + validated
- ✅ Rolling plan CRUD complete
- ✅ UI: Calendar view + strategy selector
- ✅ E2E: Create → Schedule → View → Validate
- ✅ Tests: 40+ unit, 10+ integration, 8+ E2E
- ✅ Extension points documented (for v1.5/v2)

---

## Blockers & Dependencies

- ✅ Cutting Core v4 (DEPLOYED)
- ✅ Inventory.GetAvailableMaterials() API (available)
- ✅ Procurement module ready (for v1.5 integration)

**No blockers.** Ready to start immediately.

---

## Post-Phase Handoff

**After CUTTING-PLANNING-V1 DONE:**
- Document: `SpaceOS_Modules_Cutting_Planning_Architecture_v4.md`
- Code: New `ICuttingPlanner` interface in Contracts
- Tests: 60+ passing (unit + integration)
- Portal: Rolling plan feature live
- Ready for: JOINERY-V2 (gyártásilap generation)

---

## Timeline

- **Phase 1:** Apr 19-21 (Data model)
- **Phase 2:** Apr 22-24 (Strategies)
- **Phase 3:** Apr 25-28 (UI + Integration)
- **Total:** 10 days = ~64 developer-hours (1 FTE)

---

**Status: ACTIVE — Ready for CUTTING terminál to start Phase 1 (data model)**
