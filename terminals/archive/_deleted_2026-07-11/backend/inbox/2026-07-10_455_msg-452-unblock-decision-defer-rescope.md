---
id: MSG-BACKEND-455
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: MSG-BACKEND-452-BLOCKED
created: 2026-07-10
estimated_nwt: 120
content_hash: 88cc003fcceeb6bffda2e4e27750fb927aa495b3feb136019c0097e3c7eccc5f
---

# Decision: MSG-452 Unblock — Defer & Re-scope (Option B)

**Original Task:** MSG-BACKEND-452 (EHS→HR Integration)
**Status:** BLOCKED due to missing HR/EHS domain aggregates
**Decision:** **Option B — Defer & Re-scope**

---

## Rationale

The blocker reveals a **foundational issue**: HR and EHS modules lack domain aggregates despite being marked "complete". This requires two sequential tasks:

1. **Foundation First:** Implement HR/EHS domain models (60 NWT)
2. **Integration Second:** Connect the modules via events (30 NWT)

**Why not Option A (expand scope)?**
- 90 NWT (3 hours) would push session beyond safe limits
- Context saturation already at 54+ turns
- Risk of incomplete work due to session degradation

**Why not Option C (accept partial)?**
- Partial completion = non-functional integration
- Would require re-work later
- Creates technical debt

---

## New Tasks to Create

### Task 1: HR Employee Domain Model Implementation
**ID:** MSG-BACKEND-456
**Estimated:** 60 NWT (2 hours)
**Scope:**
1. ✅ Employee aggregate (DONE - Backend created it)
2. ✅ IEmployeeRepository interface (DONE)
3. ❌ EF Core EmployeeConfiguration
4. ❌ EmployeeRepository implementation
5. ❌ Database migration (employees + employee_competencies tables)
6. ❌ Integration tests (EmployeeRepository CRUD)
7. ❌ Build verification

**Priority:** HIGH (blocks CP-EHS-HR-INTEGRATION checkpoint)

### Task 2: EHS→HR Integration Event Handlers
**ID:** MSG-BACKEND-457
**Estimated:** 30 NWT (1 hour)
**Scope:**
1. ✅ TrainingCompletedEvent contract (DONE)
2. ✅ TrainingCompletedEventHandler (DONE)
3. ❌ Event registration in DI container
4. ❌ Integration tests (Event → Employee.CompetencyMatrix update)
5. ❌ E2E test (EHS training complete → HR competency added)

**Depends on:** MSG-BACKEND-456 (Employee repository must exist)
**Priority:** HIGH (completes CP-EHS-HR-INTEGRATION checkpoint)

---

## Files Already Created (Salvage from MSG-452)

Backend created these components during investigation:
1. `TrainingCompletedEvent.cs` (Contracts) ✅
2. `Employee.cs` (HR Domain) ✅
3. `IEmployeeRepository.cs` (HR Domain) ✅
4. `TrainingCompletedEventHandler.cs` (HR Application) ✅

**These should be retained** and used as the foundation for MSG-456 and MSG-457.

---

## Checkpoint Update

**CP-EHS-HR-INTEGRATION:**
- **Status:** BLOCKED → Re-scoped into 2 sequential tasks
- **ETA:** MSG-456 (2h) + MSG-457 (1h) = ~3 hours total
- **Next:** Dispatch MSG-456 after session handoff (fresh Conductor)

---

## Action for Backend

**Do NOT continue MSG-452 work.** The task is being re-scoped into MSG-456 and MSG-457.

**Mark MSG-452 as CANCELLED** (work salvaged into new tasks).

---

## Context for Fresh Session

Next Conductor session should:
1. Create MSG-456 (HR Employee Domain Implementation - 60 NWT)
2. Dispatch to Backend
3. After MSG-456 DONE, create MSG-457 (EHS→HR Event Handlers - 30 NWT)
4. Update CP-EHS-HR-INTEGRATION checkpoint when MSG-457 completes

---

**Decision:** Option B - Defer & Re-scope ✅
**New Tasks:** MSG-456 + MSG-457
**Blocker:** Resolved (clear path forward)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
