---
completed: 2026-07-08
processed: 2026-07-08
id: MSG-ARCHITECT-072
from: root
to: architect
type: task
priority: high
status: COMPLETED
model: sonnet
ref: MSG-BACKEND-176
created: 2026-07-08
---

# Task: HR Specification Alignment with ADR-056

## Context

**Backend is blocked** on HR Week 2 Application Layer (MSG-BACKEND-176) due to **specification mismatch** with the actual HR domain model defined in ADR-056.

**Blocker Duration:** 39+ hours (since 2026-07-07 ~14:00 UTC)

**Root Cause:** Task specification was generated from a generic CRUD template without cross-referencing ADR-056, leading to specification/architecture mismatch.

**SYSTEMIC ISSUE:** This is the **2nd of 3 identical blockers** (CRM, HR, Maintenance) — all caused by the same specification generator flaw.

---

## The Problem

### Generic CRUD Template vs Domain Model

**Current Specification (Generic Template):**
```csharp
// Generic Employee CRUD (doesn't match ADR-056)
CreateEmployeeCommand.cs
UpdateEmployeeCommand.cs
ArchiveEmployeeCommand.cs
GetEmployeeByIdQuery.cs
GetAllEmployeesQuery.cs
```

**ADR-056 HR Domain Model (Actual Architecture):**
- **Employee** aggregate root (with FSM: Active, OnLeave, Terminated)
- **Position** aggregate (with assignment lifecycle)
- **Payroll** integration (not owned by HR)
- **Attendance** tracking (domain service)
- **Leave Management** (FSM: Requested, Approved, Rejected)

**Conflict:** Specification assumes generic CRUD, ADR-056 requires domain-driven FSM transitions.

---

## Your Task

**Generate an aligned HR Week 2 Application Layer specification** that:

1. **Respects ADR-056 domain model:**
   - Employee aggregate with FSM states
   - Position assignment lifecycle
   - Leave Management FSM
   - Domain services (Attendance tracking, Payroll integration)

2. **Provides Backend with:**
   - CQRS command/query structure (aligned with domain model)
   - Validation rules for each command
   - Event definitions (what events are raised)
   - Integration contracts (HR → Payroll, HR → Identity)

3. **Includes what's already implemented:**
   - Domain Layer (Week 1): Employee + Position aggregates + FSM + ValueObjects
   - Acknowledge existing Command Handlers (if any)
   - Specify what's missing

4. **Clarifies Week 2 scope:**
   - What Application Layer components need to be added?
   - Query Handlers? Validators? Integration services?
   - Or is Week 2 already complete given existing implementation?

---

## Reference: CRM Pattern (MSG-ARCHITECT-071)

You just completed a similar alignment for CRM (MSG-ARCHITECT-071). **Apply the same pattern:**
- Cross-reference ADR domain model
- FSM-based command structure (not generic Update)
- Integration contracts clearly defined
- Week 2 scope explicitly clarified

---

## Deliverable

**File:** Aligned specification document (markdown or ADR format)

**Structure:**
1. **Executive Summary** — What changed from original spec, why
2. **Scope Clarification** — What IS and what IS NOT HR module responsibility
3. **Application Layer Components** — Commands, Queries, Handlers, Validators
4. **Integration Contracts** — HR → Payroll, Identity
5. **Week 2 Task Definition** — Clear, actionable scope for Backend

**Output Location:** `/opt/spaceos/terminals/architect/outbox/` as DONE message

---

## References

- **ADR-056:** `/opt/spaceos/docs/architecture/decisions/ADR-056-joinerytech-hr-domain-model.md`
- **Backend Blocker:** MSG-BACKEND-176 (specification mismatch)
- **CRM Pattern:** MSG-ARCHITECT-071 (same issue, already solved)

---

## Acceptance Criteria

✅ **Specification aligns with ADR-056** — FSM transitions, no generic CRUD
✅ **Backend can proceed immediately** — Clear, actionable task definition
✅ **Cross-referenced with existing implementation** — Acknowledges Week 1 work
✅ **Integration contracts defined** — HR → Payroll/Identity interfaces
✅ **Root decision:** Approved or requires modification

---

**Priority:** HIGH (part of systemic fix for 3 modules)
**Timeline Impact:** 30-45 minutes (Architect review + spec generation)
**Unblocks:** MSG-BACKEND-176 (39+ hours blocked)

🏛️ **Root Terminal — Architect Alignment Request for HR Specification (2/3)**

---

## Completion Report
*2026-07-08T15:47:17.139Z*

### Summary
HR Specification Alignment with ADR-056 completed. 4 conflicts identified (Contract scope error, TimeLog gap, Assignment gap, naming mismatches). 3 resolution options provided. Recommendation: Cancel MSG-176, acknowledge partial implementation (Employee + Absence complete, TimeLog + Assignment deferred). Integration contracts documented (HR → Controlling, Production, Logistics, EHS). Unblocks MSG-BACKEND-176 (39+ hours blocked).

### Files Changed
- `terminals/architect/outbox/2026-07-08_072_hr-specification-alignment-done.md`

