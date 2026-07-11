# Skill: Checkpoint-Based Coordination Workflow

## Overview

Multi-team epic orchestration using checkpoints. Enables parallel development, dependency tracking, and automatic next-epic dispatch. Implements ADR-053.

## When to Use

- Large multi-team epic (Backend + Frontend + Designer + Architect)
- Multiple sub-teams need coordination (no shared deadline)
- Clear dependency gates between phases
- Cross-team communication needs structure

## Core Concept: Checkpoints

A **checkpoint** is a named milestone where one or more teams complete specific work, then trigger next checkpoint/epic.

### Example: EPIC-JT-CRM (3 Checkpoints)

```yaml
epics:
  - id: EPIC-JT-CRM
    name: "JoineryTech CRM Development"
    status: active
    target_date: "2026-08-31"

    checkpoints:
      - id: CP-CRM-BACKEND
        name: "Backend API Ready (Week 2-4)"
        dependencies: []
        triggers_next: CP-CRM-INTEGRATION
        owner: backend
        expected_completion: "2026-07-09"

      - id: CP-CRM-FRONTEND
        name: "Frontend UI Complete (Wave 2)"
        dependencies: []
        triggers_next: CP-CRM-INTEGRATION
        owner: frontend
        expected_completion: "2026-07-09"

      - id: CP-CRM-INTEGRATION
        name: "E2E Integration Testing"
        dependencies: [CP-CRM-BACKEND, CP-CRM-FRONTEND]
        triggers_next: EPIC-JT-KONTROLLING  # Next epic
        owner: qa
        expected_completion: "2026-07-12"
```

## Workflow Stages

### Stage 1: Epic Planning (Week 0)

**Define checkpoints in `EPICS.yaml`:**

1. Identify major milestones
2. Assign owners (Backend, Frontend, Designer, etc.)
3. Define dependencies (what blocks what)
4. Set expected completion dates
5. Specify "triggers_next" epic/checkpoint

### Stage 2: Parallel Execution

**Teams work independently:**

```
Backend (CP-CRM-BACKEND)         Frontend (CP-CRM-FRONTEND)
├─ Week 1: Foundation            ├─ Week 1: Wave 1 prep
├─ Week 2-3: JWT/OAuth code      ├─ Week 2-3: Wave 2 UI
├─ Week 4: API endpoints         ├─ Week 3-4: Testing
└─ Status check: Daily updates    └─ Status check: Daily updates

No blocker between teams (parallel tracks)
```

### Stage 3: Checkpoint Trigger

**When team completes checkpoint:**

1. Submit DONE outbox message
2. Include: `checkpoint_id: CP-CRM-BACKEND`
3. Coordinator automatically detects completion
4. Next checkpoint auto-dispatches (if dependencies met)

### Stage 4: Dependent Checkpoint Activation

**When ALL dependencies complete:**

```
CP-CRM-BACKEND ✅ DONE (2026-07-09)
    +
CP-CRM-FRONTEND ✅ DONE (2026-07-09)
    ↓
CP-CRM-INTEGRATION 🟢 ACTIVATED (2026-07-09)
```

---

## Implementation: Checkpoint Detector

### File: `EPICS.yaml` (Configuration)

```yaml
epics:
  - id: EPIC-JT-CRM
    name: "JoineryTech CRM Module"
    checkpoints:
      - id: CP-CRM-BACKEND
        owner: backend
        expected_completion: "2026-07-09"
        triggers_next: CP-CRM-INTEGRATION
        dependencies: []

      - id: CP-CRM-FRONTEND
        owner: frontend
        expected_completion: "2026-07-09"
        triggers_next: CP-CRM-INTEGRATION
        dependencies: []

      - id: CP-CRM-INTEGRATION
        owner: qa
        dependencies: [CP-CRM-BACKEND, CP-CRM-FRONTEND]
        triggers_next: EPIC-JT-KONTROLLING
```

### File: MCP Tool (Checkpoint Router)

**Pseudo-code (TypeScript):**

```typescript
async function watchCheckpoints() {
  const epics = parseEpicsYaml();

  for (const epic of epics) {
    for (const checkpoint of epic.checkpoints) {
      const isDone = checkDONEOutbox(checkpoint.id);

      if (isDone && hasNoBlockingDependencies(checkpoint)) {
        // Trigger next checkpoint
        const nextCheckpoint = findNextCheckpoint(checkpoint.triggers_next);
        await dispatchCheckpointTask(nextCheckpoint);

        // Or trigger next epic
        const nextEpic = findNextEpic(checkpoint.triggers_next);
        await dispatchEpicTask(nextEpic);
      }
    }
  }
}
```

---

## Checkpoint DONE Format

### In Terminal Outbox

**File:** `terminals/backend/outbox/2026-07-09_XXX_crm-backend-checkpoint-done.md`

```markdown
---
id: MSG-BACKEND-NNN
from: backend
to: conductor
type: done
checkpoint_id: CP-CRM-BACKEND
epic_id: EPIC-JT-CRM
status: DONE
created: 2026-07-09
---

# DONE: CRM Backend API Ready (CP-CRM-BACKEND)

**Checkpoint:** CP-CRM-BACKEND (EPIC-JT-CRM)
**Status:** ✅ COMPLETE
**Completion Date:** 2026-07-09

## Deliverables

✅ JWT/OAuth implementation (Week 2)
✅ CRM API endpoints (20+ endpoints)
✅ PostgreSQL RLS configured
✅ FluentValidation validators
✅ Unit tests (80% coverage)
✅ Build verification (0 errors)

## Files Changed

- `backend/spaceos-modules-joinery/src/Features/CRM/*.cs`
- `backend/spaceos-modules-joinery/tests/CRM/*.Tests.cs`
- `backend/spaceos-modules-joinery/CLAUDE.md` (updated spec)

## Ready for Integration

Frontend can now integrate real API (toggle mock → real).
No blockers for CP-CRM-INTEGRATION.

---

Checkpoint automatically triggers next milestone.
```

### Coordinator Auto-Detection

1. Reads outbox message
2. Extracts `checkpoint_id: CP-CRM-BACKEND`
3. Marks checkpoint as DONE in EPICS.yaml state
4. Checks dependencies for `CP-CRM-INTEGRATION`
5. If all dependencies met → auto-dispatch `CP-CRM-INTEGRATION` task

---

## Benefits: Parallel Development

### Without Checkpoints

```
Backend: Week 1 → Week 2 → Week 3 → Wait for Frontend
         ▓▓▓▓▓   ▓▓▓▓▓   ▓▓▓▓▓   ⏳ BLOCKED

Frontend: Week 1 → Week 2 → Week 3 → Integration
          ▓▓▓▓▓   ▓▓▓▓▓   ▓▓▓▓▓   ▓▓▓▓▓

Total: 4 weeks sequential
```

### With Checkpoints

```
Backend:  Week 1 → Week 2 → Week 3 → Week 4 (Integration)
          ▓▓▓▓▓   ▓▓▓▓▓   ▓▓▓▓▓   ▓▓▓▓▓

Frontend: Week 1 → Week 2 → Week 3 → Week 4 (Integration)
          ▓▓▓▓▓   ▓▓▓▓▓   ▓▓▓▓▓   ▓▓▓▓▓

Integration: ⏸️ WAITING (parallel) ⏸️ WAITING → Week 4 (both ready)
                                              ▓▓▓▓▓

Total: 4 weeks parallel (same duration) BUT both checkpoints met simultaneously
```

---

## Real-World Example: JoineryTech Multi-Wave

### Wave 1: CRM

```yaml
checkpoints:
  - CP-CRM-BACKEND (backend team) → triggers CP-CRM-FRONTEND
  - CP-CRM-FRONTEND (frontend team) → both ready → triggers CP-CRM-INTEGRATION
  - CP-CRM-INTEGRATION (qa team) → triggers EPIC-JT-KONTROLLING
```

### Wave 2: Kontrolling (After CRM Integration Done)

```yaml
checkpoints:
  - CP-KONTROLLING-BACKEND (backend team)
  - CP-KONTROLLING-FRONTEND (frontend team)
  - CP-KONTROLLING-INTEGRATION (qa team) → triggers EPIC-JT-HR
```

### Wave 3: HR + Maintenance + QA

Same pattern repeats for each module.

---

## Checkpoint Status Dashboard

**Coordinator track (Datahaven):**

```
EPIC-JT-CRM
├─ CP-CRM-BACKEND: ✅ DONE (2026-07-09) → Next: CP-CRM-INTEGRATION
├─ CP-CRM-FRONTEND: ✅ DONE (2026-07-09) → Next: CP-CRM-INTEGRATION
└─ CP-CRM-INTEGRATION: 🟢 ACTIVE (since 2026-07-09) → Next: EPIC-JT-KONTROLLING

EPIC-JT-KONTROLLING
├─ CP-KONTROLLING-BACKEND: ⏳ QUEUED → Next: CP-KONTROLLING-INTEGRATION
├─ CP-KONTROLLING-FRONTEND: ⏳ QUEUED → Next: CP-KONTROLLING-INTEGRATION
└─ CP-KONTROLLING-INTEGRATION: ⏸️ PENDING (waits for both)
```

---

## Failure Handling

### If Checkpoint Misses Deadline

**Example:** CP-CRM-BACKEND expected 2026-07-09, still not done by 2026-07-10

```
Alert to Conductor:
- CP-CRM-BACKEND: 1 day overdue
- Blocking: CP-CRM-INTEGRATION
- Action: Escalate to Backend + Root

Options:
1. Extend deadline (re-plan Phase 1)
2. Partially complete (split checkpoint into sub-tasks)
3. Emergency workaround (mock API for Frontend to proceed)
```

### If Checkpoint Dependencies Blocked

**Example:** CP-CRM-INTEGRATION requires CP-CRM-BACKEND + CP-CRM-FRONTEND, but only 1 done

```
Status: BLOCKED (waiting for CP-CRM-FRONTEND)
Wait until both:
- CP-CRM-BACKEND: ✅ DONE
- CP-CRM-FRONTEND: ✅ DONE

Then: CP-CRM-INTEGRATION auto-dispatches
```

---

## Configuration Template

### New Epic Checklist

- [ ] Define checkpoints in `EPICS.yaml`
- [ ] Assign team owner per checkpoint
- [ ] Set expected completion dates
- [ ] Document dependencies (what blocks what)
- [ ] Specify `triggers_next` (next checkpoint/epic)
- [ ] Create inbox tasks for each checkpoint
- [ ] Communicate dates to team leads
- [ ] Add to Datahaven Dashboard

---

## Related Patterns

- **Walking Skeleton First** (Phase 1 = first checkpoint)
- **Parallel Track Coordination** (Backend + Frontend checkpoints)
- **Epic Router** (EPICS.yaml drives task dispatch)

## References

- ADR-053: Checkpoint Coordination Workflow
- EPICS.yaml schema
- Datahaven Dashboard (checkpoint tracking)

---

**Skill Created:** 2026-07-04
**Source:** EPIC-JT-CRM coordination (3 checkpoints)
**Applicable:** All multi-team epics (8 waves of JoineryTech modules)
**Benefit:** Parallel development + automatic coordination
