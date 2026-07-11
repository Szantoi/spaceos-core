---
id: MSG-BACKEND-195-BLOCKED
from: backend
to: conductor
type: blocked
status: READ
ref: MSG-BACKEND-195
epic_id: EPIC-DOORSTAR-SOFTLAUNCH
created: 2026-07-10
content_hash: b8e46961bb89b55a286e02f48fab57b08be816bf25be1f5eda1f0ab5375d2f0e
---

# Doorstar Production QA — Integration Tests (BLOCKED)

**Task:** MSG-BACKEND-195
**Epic:** EPIC-DOORSTAR-SOFTLAUNCH
**Blocker Type:** Missing Dependency (Production module not implemented)

---

## 🚧 BLOCKER SUMMARY

**MSG-BACKEND-195 QA integration tests CANNOT be implemented** because the **Production module does not exist**.

---

## ROOT CAUSE ANALYSIS

### Task Dependency Chain

```
MSG-BACKEND-194 (DONE 2026-07-08)
  ├─ Status: Implementation Plan ONLY
  ├─ Deliverable: OpenAPI contract draft + task breakdown
  └─ Code Status: ❌ NOT IMPLEMENTED (only planning)

MSG-BACKEND-195 (BLOCKED 2026-07-10)
  ├─ Requires: Production module Domain/Application/Infrastructure layers
  ├─ Current State: Module does NOT exist
  └─ Block Reason: Cannot write integration tests for non-existent code
```

### Evidence

**1. Module filesystem check:**
```bash
$ ls /opt/spaceos/backend/spaceos-modules/
spaceos-modules-crm
spaceos-modules-dms
spaceos-modules-hr
spaceos-modules-kontrolling
# ❌ spaceos-modules-production does NOT exist
```

**2. ProductionJob aggregate search:**
```bash
$ grep -r "ProductionJob" backend/spaceos-modules* --include="*.cs"
# ❌ NO RESULTS (aggregate not implemented)
```

**3. Related findings:**
- `Abstractions.Domain.Results.ProductionStep.cs` exists (parametric engine, unrelated)
- `Joinery.Infrastructure.Pdf.ProductionSheetGenerator.cs` exists (Munkamenet.pdf, legacy)
- No `SpaceOS.Modules.Production.*` projects

---

## IMPACT ASSESSMENT

### Timeline Impact
- **Original estimate:** 30 NWT (~1 hour)
- **Actual blocker:** +4 days (Production module implementation)
- **Critical path:** Backend implementation → QA tests → Frontend integration

### Epic Dependency
- **EPIC-DOORSTAR-SOFTLAUNCH** target: 2026-09-30
- **Current date:** 2026-07-10
- **Buffer:** 82 days remaining
- **Risk:** Medium (buffer sufficient IF unblocked soon)

---

## ACTIONABLE NEXT STEPS

### Option A: Sequential Implementation (RECOMMENDED)

**Step 1: Implement MSG-BACKEND-194 Sections 2.1-2.4**
```
Task: MSG-BACKEND-196 (new) — Production Module Implementation
Scope:
  - Domain Layer (ProductionJob aggregate, WorkflowStep entity, events)
  - Application Layer (Commands, Handlers, Queries)
  - Infrastructure Layer (DbContext, Repository, EventBus)
  - API Layer (Minimal API endpoints)
Estimate: ~4 days (Backend)
```

**Step 2: Resume MSG-BACKEND-195** (after Step 1 DONE)
```
Task: MSG-BACKEND-195 (resume)
Scope: 4 E2E integration tests (Testcontainers PostgreSQL + RabbitMQ)
Estimate: 30 NWT (~1 hour)
```

**Total time:** 4 days + 1 hour

### Option B: Parallel Development (RISKIER)

**Frontend can start on UI mockups** (Frontend MSG-FRONTEND-XXX):
- ProductionJobCard component
- WorkflowStepStepper component
- KioskMobileLayout
- Mock API (json-server or MSW)

**Backend implements Production module** (MSG-BACKEND-196)

**Converge when Backend DONE** (MSG-BACKEND-195 + Frontend integration)

**Risk:** Frontend rewrites if API contract changes

---

## TASK BREAKDOWN (MSG-BACKEND-196 suggestion)

### Domain Layer (~1 day)
- [ ] `ProductionJob` aggregate root
  - [ ] `ProductionJobId` value object
  - [ ] `WorkflowStep` entity (6 STAGE)
  - [ ] `ProductionStatus` enum (Queued → ShippingReady)
  - [ ] FSM validation (only current step completable)
- [ ] Domain events:
  - [ ] `ProductionJobStarted`
  - [ ] `WorkflowStepCompleted`
  - [ ] `ProductionJobShippingReady`
- [ ] Value objects:
  - [ ] `WorkflowStepName` (Szabászat, Megmunkálás, etc.)
  - [ ] `ProductionDeadline`

### Application Layer (~1 day)
- [ ] Commands:
  - [ ] `StartProductionJobCommand` + Handler
  - [ ] `CompleteWorkflowStepCommand` + Handler
  - [ ] `MarkAsShippingReadyCommand` + Handler
- [ ] Queries:
  - [ ] `GetProductionQueueQuery` + Handler (műhelyvezető UI)
  - [ ] `GetJobStatusForOwnerQuery` + Handler (tulaj/sales view)
  - [ ] `GetProductionJobByIdQuery` + Handler

### Infrastructure Layer (~1 day)
- [ ] `ProductionDbContext` (EF Core)
- [ ] `ProductionJobRepository` (aggregate persistence)
- [ ] Event subscribers:
  - [ ] `CuttingCompletedEventHandler` (ADR-038 integration)
  - [ ] `OrderConfirmedEventHandler` (ProductionJob creation trigger)

### API Layer (~1 day)
- [ ] `ProductionController` (REST endpoints)
- [ ] OpenAPI spec (from MSG-BACKEND-194)
- [ ] Build verification (0 error, 0 warning)

---

## RECOMMENDATIONS

### For Conductor

**1. Create MSG-BACKEND-196** (Production Module Implementation)
- Priority: **HIGH** (unblocks MSG-BACKEND-195 + Frontend)
- Estimate: 4 days (Backend only)
- Assignee: Backend terminal
- Dependencies: None (can start immediately)

**2. Hold MSG-BACKEND-195** (QA Integration Tests)
- Status: **PARKED** until MSG-BACKEND-196 DONE
- Estimate: 30 NWT (unchanged)
- Dependencies: MSG-BACKEND-196 DONE

**3. Notify Frontend** (parallel track option)
- Option: Start UI mockups with mock API
- Risk: API contract changes (Medium)
- Decision: Frontend terminal decides (mock API vs. wait)

### For Root

**Strategic decision:** Sequential vs. Parallel?
- **Sequential:** Lower risk, predictable (4 days + 1 hour)
- **Parallel:** Faster time-to-market, higher rework risk

**My recommendation:** **Sequential** (Production module first, then QA + Frontend converge)

---

## REFERENCES

- **MSG-BACKEND-194 DONE:** Implementation Plan (2026-07-08, 26KB)
- **MSG-BACKEND-195 inbox:** QA Integration Tests (this task)
- **Doorstar domain spec:** `/tmp/doorstar_domain_spec.md` (MSG-ROOT-038)
- **6 STAGE workflow:** Szabászat → Megmunkálás → Felületkezelés → Összeszerelés → Csomagolás → Kiszállítható

---

## NEXT ACTION

**Waiting for Conductor to:**
1. Create MSG-BACKEND-196 (Production Module Implementation)
2. Assign to Backend terminal
3. Update EPIC-DOORSTAR-SOFTLAUNCH timeline (+4 days)

**ETA after unblock:** 4 days (Backend) + 1 hour (QA tests)

---

📋 Backend Terminal — MSG-BACKEND-195 BLOCKED Report (2026-07-10 15:52 UTC)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
