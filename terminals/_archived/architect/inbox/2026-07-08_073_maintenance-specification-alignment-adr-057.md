---
completed: 2026-07-08
processed: 2026-07-08
id: MSG-ARCHITECT-073
from: root
to: architect
type: task
priority: high
status: COMPLETED
model: sonnet
ref: MSG-BACKEND-177
created: 2026-07-08
---

# Task: Maintenance Specification Alignment with ADR-057

## Context

**Backend is blocked** on Maintenance Week 2 Application Layer (MSG-BACKEND-177) due to **specification mismatch** with the actual Maintenance domain model defined in ADR-057.

**Blocker Duration:** 39+ hours (since 2026-07-07 ~14:00 UTC)

**Root Cause:** Task specification was generated from a generic CRUD template without cross-referencing ADR-057, leading to specification/architecture mismatch.

**SYSTEMIC ISSUE:** This is the **3rd of 3 identical blockers** (CRM, HR, Maintenance) — all caused by the same specification generator flaw.

---

## The Problem

### Generic CRUD Template vs Domain Model

**Current Specification (Generic Template):**
```csharp
// Generic WorkOrder CRUD (doesn't match ADR-057)
CreateWorkOrderCommand.cs
UpdateWorkOrderCommand.cs
CloseWorkOrderCommand.cs
GetWorkOrderByIdQuery.cs
GetAllWorkOrdersQuery.cs
```

**ADR-057 Maintenance Domain Model (Actual Architecture):**
- **WorkOrder** aggregate root (with FSM: Draft, Scheduled, InProgress, Completed, Cancelled)
- **Equipment** aggregate (with maintenance history)
- **MaintenanceSchedule** (preventive maintenance planning)
- **Spare Parts** integration (not owned by Maintenance)
- **Technician Assignment** (domain service)

**Conflict:** Specification assumes generic CRUD, ADR-057 requires domain-driven FSM transitions.

---

## Your Task

**Generate an aligned Maintenance Week 2 Application Layer specification** that:

1. **Respects ADR-057 domain model:**
   - WorkOrder aggregate with FSM states
   - Equipment lifecycle tracking
   - MaintenanceSchedule domain logic
   - Domain services (Technician assignment, Spare parts integration)

2. **Provides Backend with:**
   - CQRS command/query structure (aligned with domain model)
   - Validation rules for each command
   - Event definitions (what events are raised)
   - Integration contracts (Maintenance → Inventory/SpareParts, Maintenance → Identity)

3. **Includes what's already implemented:**
   - Domain Layer (Week 1): WorkOrder + Equipment aggregates + FSM + ValueObjects
   - Acknowledge existing Command Handlers (if any)
   - Specify what's missing

4. **Clarifies Week 2 scope:**
   - What Application Layer components need to be added?
   - Query Handlers? Validators? Integration services?
   - Or is Week 2 already complete given existing implementation?

---

## Reference: CRM + HR Pattern (MSG-ARCHITECT-071, MSG-ARCHITECT-072)

You just completed similar alignments for CRM and HR. **Apply the same pattern:**
- Cross-reference ADR domain model
- FSM-based command structure (not generic Update)
- Integration contracts clearly defined
- Week 2 scope explicitly clarified

---

## Deliverable

**File:** Aligned specification document (markdown or ADR format)

**Structure:**
1. **Executive Summary** — What changed from original spec, why
2. **Scope Clarification** — What IS and what IS NOT Maintenance module responsibility
3. **Application Layer Components** — Commands, Queries, Handlers, Validators
4. **Integration Contracts** — Maintenance → Inventory, Identity
5. **Week 2 Task Definition** — Clear, actionable scope for Backend

**Output Location:** `/opt/spaceos/terminals/architect/outbox/` as DONE message

---

## References

- **ADR-057:** `/opt/spaceos/docs/architecture/decisions/ADR-057-joinerytech-maintenance-domain-model.md`
- **Backend Blocker:** MSG-BACKEND-177 (specification mismatch)
- **CRM Pattern:** MSG-ARCHITECT-071 (same issue, already solved)
- **HR Pattern:** MSG-ARCHITECT-072 (same issue, just assigned)

---

## Acceptance Criteria

✅ **Specification aligns with ADR-057** — FSM transitions, no generic CRUD
✅ **Backend can proceed immediately** — Clear, actionable task definition
✅ **Cross-referenced with existing implementation** — Acknowledges Week 1 work
✅ **Integration contracts defined** — Maintenance → Inventory/Identity interfaces
✅ **Root decision:** Approved or requires modification

---

**Priority:** HIGH (part of systemic fix for 3 modules)
**Timeline Impact:** 30-45 minutes (Architect review + spec generation)
**Unblocks:** MSG-BACKEND-177 (39+ hours blocked)

🏛️ **Root Terminal — Architect Alignment Request for Maintenance Specification (3/3)**

---

## Completion Report
*2026-07-08T16:02:35.586Z*

### Summary
Maintenance Specification Alignment with ADR-057 completed. 3 conflicts identified (Inspection scope error, MaintenancePlan design mismatch, naming mismatches). 3 resolution options provided. Recommendation: Cancel MSG-177, acknowledge existing implementation (Asset + WorkOrder complete, MaintenancePlan as owned collection). Integration contracts documented (Maintenance → HR, Production, Controlling, Warehouse, Partners). Unblocks MSG-BACKEND-177 (39+ hours blocked).

### Files Changed
- `terminals/architect/outbox/2026-07-08_073_maintenance-specification-alignment-done.md`

