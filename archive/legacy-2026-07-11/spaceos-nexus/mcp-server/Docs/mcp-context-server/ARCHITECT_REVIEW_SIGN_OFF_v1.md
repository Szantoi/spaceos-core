---
id: architect-sign-off-m01-closure
title: "Architect Sign-Off: MCP Context Server M01 Closure & M02 Readiness"
type: architect-review
date: 2026-03-04
reviewer: Architect
scope: mcp-context-server (M01 + M02 planning)
status: draft
approval: pending
---

# 🏛️ Architect Sign-Off: MCP Context Server — M01 Closure & M02 Readiness

**Review Date:** 2026-03-04
**Reviewer:** Architect Agent
**Scope:** Delivery track (mcp-context-server) — M01 Closure readiness, M02 planning validation
**Purpose:** Validate architecture quality, identify risks, and provide A/B recommendations for M01→M02 transition

---

## Executive Summary

### Current Status

- ✅ **M01 Planning**: Well-structured (EPIC-01, EPIC-02, EPIC-08 defined)
- ✅ **EPIC-08 Organization**: Fixed (goal.md, state.md, tasks/ now exist)
- ✅ **Architecture Documentation**: Write Layer defined (MCP_Server_Architecture.md § 3.2)
- ⚠️ **M02 Epic Definition**: EPIC-09–12 have states but **AC/DoD missing** (BACKLOG_READY, not dev-ready)
- 🔴 **Critical: EPIC-08 ↔ EPIC-09 Coordination**: Schema will be duplicated if not coordinated

### Recommendation

**GATE M01 closure** to verify M02 prerequisite AC/DoD planning is complete. No scope creep into M02; instead, lock M02 AC before M02 start.

---

## 🔍 Detailed Architectural Findings

### Finding 1: EPIC-08 Write Layer — Task Breakdown Incomplete

**Severity:** 🟡 **MEDIUM** (affects M01 closure DoD)
**Category:** Architecture Definition

#### Current State

- **[EPIC-08 goal.md](../delivery/mcp-maintenance/milestones/milestone_01/epic_08/goal.md)** defines 3 MCP tools:
  - `submit_artifact(artifact_content, session_id, artifact_type)`
  - `update_workflow_state(session_id, new_state, event, evidence?)`
  - `store_session_checkpoint(session_id, checkpoint_data)` ← **Out of Scope for M01**

- **[EPIC-08 task breakdown](../delivery/mcp-maintenance/milestones/milestone_01/epic_08/tasks/)** defines 3 tasks:
  - **TASK-08-01:** SQLite Schema (sessions, artifacts, workflow_events, checkpoints) ✅
  - **TASK-08-02:** MCP Write Tools → **`submit_artifact()` + `update_workflow_state()`** ❓
  - **TASK-08-03:** E2E Tests ✅

#### Problem

- TASK-08-02 definition does **not mention `store_session_checkpoint()`**
- Per architecture: 3 tools need implementation; per task breakdown: only 2 are in EPIC-08

#### Risk

- **Incomplete implementation**:
  - If `store_session_checkpoint()` removed from M01 scope → only `submit_artifact()` covers artifact persistence
  - If `store_session_checkpoint()` is to be implemented → TASK-08-02 needs updated acceptance criteria
- **Missing clarity for EPIC-12 (Episodic Memory)**:
  - EPIC-12 might re-discover and duplicate checkpoint logic

#### Recommendation

**Action 1: Clarify checkpoint responsibility in EPIC-08 goal**

In [EPIC-08 goal.md](../delivery/mcp-maintenance/milestones/milestone_01/epic_08/goal.md), explicitly decide:

- **Option A (Minimal M01)**: `store_session_checkpoint()` is **EPIC-12 (M02)** work. EPIC-08 = `submit_artifact()` + `update_workflow_state()` only.
  - **Pro**: Smaller scope, M01 focused
  - **Con**: Checkpoint data has nowhere to persist until EPIC-12

- **Option B (Complete M01)**: `store_session_checkpoint()` is **EPIC-08 (M01)** work. All 3 MCP tools implemented M01.
  - **Pro**: Full write-layer capabilities, no gap
  - **Con**: Larger scope, might slip M01

**Recommendation:** Adopt **Option A (Minimal M01)**, but add explicit section to goal.md:

```markdown
## Out-of-Scope for EPIC-08 (M01)
- `store_session_checkpoint()` MCP tool implementation → EPIC-12 (M02)
- Session recovery logic (replay, restore) → EPIC-12 (M02)
- Checkpoint schema (advanced features) → EPIC-12 (M02)
```

Then update TASK-08-02 to clarify: "Implement `submit_artifact()` + `update_workflow_state()` tools; checkpoint schema designed but tool deferred."

---

### Finding 2: EPIC-08 ↔ EPIC-09 Schema Coordination — No Formal Dependency

**Severity:** 🔴 **CRITICAL** (blocks M02 start)
**Category:** Milestone Planning

#### Current State

- **[M01 plan](../delivery/mcp-maintenance/milestones/milestone_01/plan.md)**: "EPIC-08 writes sessions, artifacts, workflow_events, checkpoints"
- **[M02 plan](../delivery/mcp-maintenance/milestones/milestone_02/plan.md)**:
  - EPIC-09: "SQLite Schema Design & Database Seeder"
  - Explicitly says: "Coordinate with EPIC-08 schema; finalize AC"
- **[EPIC-09 state](../delivery/mcp-maintenance/milestones/milestone_02/epic_09/state.md)**:
  - Defines **full schema** (roles, role_schemas, runbooks, workflows, templates, **+ sessions, artifacts, workflow_events, checkpoints**)
  - No mention of EPIC-08's existing schema design

#### Problem

- **Duplication Risk**: Both EPIC-08 (M01) and EPIC-09 (M02) design the **same tables** (sessions, artifacts, workflow_events, checkpoints)
- **Potential Conflict**:
  - EPIC-08 (M01) finalizes schema → TASK-08-01 done
  - EPIC-09 (M02) re-examines schema → discovers it's "not optimal" → redesign request
  - Result: **TASK-08-01 rework**, M01 DoD compromised

#### Root Cause

- No **formal dependency link** from EPIC-09 → EPIC-08 in state machines
- No **explicit coordination gate** between task completion

#### Recommendation

**Action 2: Add explicit dependency + coordination gate**

**Option A: EPIC-09 depends on EPIC-08 completion (recommended)**

In [EPIC-09 state.md](../delivery/mcp-maintenance/milestones/milestone_02/epic_09/state.md), add:

```markdown
## Dependencies

- **HARD BLOCKER**: EPIC-08 (M01) must be CLOSED_DONE before EPIC-09 starts
  - EPIC-08 defines: sessions, artifacts, workflow_events, checkpoints schema
  - EPIC-09 **must adopt** EPIC-08 schema; no re-design
  - EPIC-09 adds: roles, role_schemas, runbooks, workflows, templates tables

## Acceptance Criteria

- [ ] EPIC-08 (M01) design is reviewed and approved by Architect
- [ ] EPIC-09 task-01: "Verify EPIC-08 schema compatibility; merge into agent.db design"
  (2–4 hours) — done before EPIC-09 main development
```

**Option B: Coordinate in M01 planning (faster, risky)**

Add a **coordination sub-task** in EPIC-08:

```markdown
## EPIC-08: Additional Task (M1-A)

- **TASK-08-00: Architecture Coordination (1 day)**
  - Architect + EPIC-09 assignee review EPIC-08 schema with EPIC-09 requirements
  - Outcome: Frozen schema (no changes during EPIC-08 implementation)
```

**Recommendation: Adopt Option A.**

- Clearer timeline
- Avoids mid-implementation changes
- M02 start is naturally after M01 closure anyway

---

### Finding 3: M02 EPIC-09–12 Acceptance Criteria — Missing (BACKLOG_READY, but not dev-ready)

**Severity:** 🟡 **HIGH** (blocks M02 sprint planning)
**Category:** Definition of Done

#### Current State

- [EPIC-09 state.md](../delivery/mcp-maintenance/milestones/milestone_02/epic_09/state.md): ✅ Has AC section (schema design)
- [EPIC-10 state.md](../delivery/mcp-maintenance/milestones/milestone_02/epic_10/state.md): ✅ Has AC section (bootstrap payload structure)
- [EPIC-11 state.md](../delivery/mcp-maintenance/milestones/milestone_02/epic_11/state.md): ❓ *Needs review*
- [EPIC-12 state.md](../delivery/mcp-maintenance/milestones/milestone_02/epic_12/state.md): ❓ *Needs review*

- **[IMPLEMENTATION_PLAN_v1.md § Phase 3](../IMPLEMENTATION_PLAN_v1.md#phase-3-implementation--m02-backlog-5-7-days)**: "EPIC-09–12 ready for dev" depends on **"AC + DoD documented"**

#### Problem

- **EPIC-09 AC is generic** (schema definition, not testability criteria)
  - Missing: "DB must be queryable in < 10ms", "seeder must complete in < 5 sec", "data consistency checks"
- **EPIC-10 AC is defined** but **Task breakdown missing**
  - Bootstrap payload is defined, but `bootstrap_agent()` function signature, error cases, integration with SessionManager not detailed
- **EPIC-11 & EPIC-12: State files exist but AC/Task breakdown not structured**

#### Risk

- **M02 sprint planning = "we'll figure out AC during dev"** → scope creep, delays
- **QA cannot test** (no acceptance criteria to verify against)
- **No "done" definition** (per Architect mode: `ENFORCE_WORKFLOW_ITEMS`)

#### Recommendation

**Action 3: Lock M02 EPIC AC/DoD/Task breakdown before M02 start**

Create a **phased milestone planning task** (1–2 days, can start now):

**Phase 3a (this week): Lock EPIC-09–12 AC + Task Breakdown**

- [ ] **EPIC-09 AC refinement** (4 hours)
  - Add testability criteria: performance SLAs, seeder validation checks, data consistency
  - Lock task breakdown: 3 tasks (schema design, seeder, tests)

- [ ] **EPIC-10 AC refinement** (4 hours)
  - Define bootstrap payload contracts (success case, 3+ error cases)
  - Lock task breakdown: 3 tasks (payload design, tool impl, integration tests)

- [ ] **EPIC-11 AC refinement** (4 hours)
  - Define migration strategy, backward compat, rollback plan
  - Lock task breakdown: 3 tasks (design, migration script, tests)

- [ ] **EPIC-12 AC refinement** (4 hours)
  - Define episodic memory scope: session recovery, ChromaDB write-back, reflection loop
  - Lock task breakdown: 4+ tasks (can span M02 + M03)

**Owner:** Architect + Tech Lead
**Timeline:** 2026-03-05 to 2026-03-07 (3 days parallel work)
**Gate:** M02 start (2026-03-10?) requires all AC/DoD locked

---

### Finding 4: FSM (Workflow State Machine) Architecture — Underdocumented

**Severity:** 🟡 **MEDIUM** (affects security, error handling)
**Category:** Workflow Design

#### Current State

- [MCP_Server_Architecture.md § 3.2](../../MCP_Server_Architecture.md#32-write-layer-post-operations): Defines state transitions ("IN_PROGRESS" → "SUBMITTED" → "PROCESSED" → "CLOSED")
- [WorkflowStateTracker.ts](../../../src/metadata/WorkflowStateTracker.ts): Implements FSM with hardcoded AGILE_EPIC_LIFECYCLE_V1 schema
- **Missing**: FSM security model, error recovery, concurrent access protocol

#### Problem

- **State Transition Security**: Who is allowed to call `update_workflow_state()`?
  - Can only the agent that created the session modify its state?
  - Can admin roles force state changes (e.g., "ESCALATED" → "BACKLOG_READY")?
  - **Not defined**

- **Error Recovery**: What happens if `update_workflow_state()` fails halfway (DB error after state change but before event logging)?
  - Incomplete transaction → state + event log mismatch
  - **No compensating transaction logic**

- **Concurrent Access**: Can two agents submit artifacts for the same session?
  - Race condition: both see "submitted" → both try state change → FK constraint failure?
  - **No pessimistic locking documented**

#### Recommendation

**Action 4: Draft FSM Security & Concurrency ADR (link to this review)**

Create a lightweight ADR:

**File:** `database/joinerytech-flow/discovery/mcp-context-server/02_ideate/adrs/02-fsm-security-concurrency-draft.md`

```markdown
---
id: fsm-security-concurrency
title: "ADR: FSM Security & Concurrency Protocol"
status: draft
---

## Context
MCP server implements state machine for sessions. Need to define:
1. State change authorization (who can update state?)
2. Error recovery (transaction safety)
3. Concurrent session handling

## Options
### Option A: Pessimistic Lock (simple)
- Session table has `locked_by` column
- `update_workflow_state()` acquires lock before state change
- Retry logic on lock timeout

### Option B: Optimistic Concurrency (complex)
- Session has `version` column
- Lock-free; rely on version mismatch for conflict detection
- Retry on conflict

### Option C: Event Sourcing (future)
- No state column; derive state from workflow_events table
- Audit trail built-in; conflict resolution via event timestamps

## Recommendation
**Adopt Option A (Pessimistic)** for M01/M02 (simplicity);
**Plan Option C for M03** (robustness, audit).

---
```

**Owner:** Architect
**Deliverable:** ADR draft (no code) for EPIC-08 & EPIC-10 task teams to reference

---

### Finding 5: Implementation Summary — EPIC-02 needs documentation

**Severity:** 🟠 **MEDIUM** (M01 closure gate)
**Category:** Definition of Done

#### Current State

- [EPIC-02 state.md](../delivery/mcp-maintenance/milestones/milestone_01/epic_02/state.md): Status = "IN_DEV" (only TASK-02-01 shown)
- [EPIC-02 implementation-summary/](../delivery/mcp-maintenance/milestones/milestone_01/epic_02/implementation-summary/): **1 file exists** (maybe TASK-02-01 summary)
- **Missing**: Full implementation summary for EPIC-02 itself

#### Problem

- Per Architect mode: `ENFORCE_WORKFLOW_ITEMS` → "All tasks have AC/DoD + implementation summaries"
- EPIC-02 is "Dead Code Elimination" — already has TASK-02-01
- But **no EPIC-02-level summary** (what code was removed, impact, before/after metrics)

#### Risk

- M01 closure **incomplete DoD**: "All epics have implementation-summary/"
- Unclear: How much dead code was removed? Any side effects?

#### Recommendation

**Action 5: Add EPIC-02 implementation summary template**

Before M01 closure, create skeleton:

```
Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_01/epic_02/implementation-summary/EPIC-02-summary.md
```

Content template:

```markdown
---
id: epic-02-summary
type: implementation-summary
epic: EPIC-02
title: "Implementation Summary: Dead Code Elimination & Static Analysis"
date: 2026-03-XX
author: Backend Developer
---

# EPIC-02: Dead Code Elimination Summary

## Tasks Completed
- [x] TASK-02-01: Static analysis & removal

## Code Removed
- Files deleted: N
- Unused exports: N
- Lines of dead code: N

## Quality Metrics
- tsc --noUnusedLocals: 0 errors
- ts-prune report: reviewed and accepted

## Risks Addressed
- No breaking changes (verified in EPIC-01 E2E tests)

## Approval
- [✅ QA Sign-off]
- [✅ Tech Lead Sign-off]
- [ ] Architect Sign-off (pending)
```

**Owner:** Backend Developer / Framer
**Timeline:** Before M01 closure

---

## 📊 M01 Closure Readiness Checklist

| Item | Status | Owner | Notes |
|:-----|:-------|:------|:------|
| EPIC-01: RBAC Schema | ✅ CLOSED_DONE | Backend Dev | Architecture implemented |
| EPIC-02: Dead Code | 🚧 IN_DEV → Needs impl summary | Framer | Awaiting summary |
| EPIC-08: Write Layer | 🚧 IN_DEV → Ready for dev | Backend Dev | Tasks clarify checkpoint scope |
| EPIC-01 E2E Tests | ? | QA | Need test report |
| EPIC-02 Test Report | ? | QA | Need test report |
| EPIC-08 Test Coverage | ⏳ Planned | QA | E2E test planning ongoing |
| Architect Sign-off | ⏳ Pending | Architect | **This review** |
| Dependencies locked | ⏳ Pending | Architect | ADR for FSM, EPIC-08↔09 link |

---

## 🎯 Recommendations Summary

### For M01 Closure (2026-03-XX)

1. **Action 1:** Clarify checkpoint responsibility in EPIC-08 goal.md
   - Decide: Option A (M01) or Option B (M02)
   - **Recommended:** Option A (Minimal M01, defer to EPIC-12)

2. **Action 2:** Add formal EPIC-09 dependency on EPIC-08 completion
   - Prevents duplicate schema design
   - Lock EPIC-09 AC to "adopt EPIC-08 schema, add role tables"

3. **Action 3:** Complete M02 EPIC AC/DoD/Task breakdown (parallel work)
   - Timeline: 2026-03-05 to 2026-03-07
   - Lock before M02 sprint start

4. **Action 4:** Draft FSM Security & Concurrency ADR (for EPIC-10 team)
   - Recommends Pessimistic Locking (Option A)
   - File: `database/joinerytech-flow/discovery/mcp-context-server/02_ideate/adrs/02-fsm-security-concurrency-draft.md`

5. **Action 5:** Add EPIC-02 implementation summary before M01 closure
   - Template provided
   - Owner: Backend Developer

### For M02 Start (2026-03-10 estimated)

1. **Dependency Gate:** EPIC-08 must be CLOSED_DONE before EPIC-09 starts
   - No concurrent schema design
   - EPIC-09 TASK-00: Verify schema compatibility (2–4 hours task)

2. **M02 Epic Readiness:** All EPIC-09–12 AC/DoD/task breakdowns locked
   - No "figure out during spring" approach
   - QA and Tech Lead sign-off on AC before dev starts

---

## 🏆 Architect Sign-Off Decision

### Conditional Approval ✅ (Pending 5 Actions Above)

**Status:** 🟢 **READY FOR M01 CLOSURE**

**Conditions:**

- [ ] Action 1: EPIC-08 checkpoint scope clarified (goal.md updated)
- [ ] Action 2: EPIC-09 formal dependency added to EPIC-09 state.md
- [ ] Action 3: M02 EPIC-09–12 AC/DoD/task breakdown completed
- [ ] Action 4: FSM Security ADR draft created
- [ ] Action 5: EPIC-02 implementation summary added

**Once all 5 actions complete:**

- ✅ **Architect Sign-Off APPROVED**
- M01 DoD = COMPLETE
- M02 start = AUTHORIZED

**Estimated Timeline:**

- Actions 1–2: 2026-03-04 to 2026-03-05 (1 day) — Architect + Tech Lead
- Action 3: 2026-03-05 to 2026-03-07 (3 days) — Architect + Tech Lead
- Action 4: 2026-03-04 (same day, parall el) — Architect
- Action 5: Before M01 closure — Backend Dev

### Release Gate (if all Actions done)

**✅ M01 CLOSED_DONE** → proceed to M02 start w/ no blockers

---

## Next Steps

1. **This Week (2026-03-04 to 2026-03-07)**
   - Architect shares this review with Tech Lead + team
   - Team implements Actions 1–5 (parallel work)
   - Track status on EPIC-08 state.md or M01 milestone dashboard

2. **M01 Closure (2026-03-XX)**
   - Tech Lead verifies all 5 actions complete
   - Architect provides final sign-off
   - M01 gate closes; M02 starts

3. **M02 Start Gate**
   - Engineer reviews EPIC-09–12 AC and locked task breakdowns
   - No work begins until acceptance criteria explicitly agreed

---

## 📎 Appendix: Related Documents

- Architecture: [Docs/MCP_Server_Architecture.md](../../MCP_Server_Architecture.md)
- M01 Plan: [mcp-context-server/delivery/mcp-maintenance/milestones/milestone_01/plan.md](../delivery/mcp-maintenance/milestones/milestone_01/plan.md)
- M02 Plan: [mcp-context-server/delivery/mcp-maintenance/milestones/milestone_02/plan.md](../delivery/mcp-maintenance/milestones/milestone_02/plan.md)
- EPIC-08: [milestone_01/epic_08/](../delivery/mcp-maintenance/milestones/milestone_01/epic_08/)
- EPIC-09: [milestone_02/epic_09/](../delivery/mcp-maintenance/milestones/milestone_02/epic_09/)
- EPIC-10: [milestone_02/epic_10/](../delivery/mcp-maintenance/milestones/milestone_02/epic_10/)
- IMPLEMENTATION_PLAN: [mcp-context-server/IMPLEMENTATION_PLAN_v1.md](../IMPLEMENTATION_PLAN_v1.md)
- QUALITY_AUDIT: [mcp-context-server/QUALITY_AUDIT_REPORT.md](../QUALITY_AUDIT_REPORT.md)
