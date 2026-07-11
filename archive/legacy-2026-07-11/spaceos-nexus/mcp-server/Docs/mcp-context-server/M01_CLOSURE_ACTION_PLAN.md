---
id: m01-closure-action-plan
title: "M01 Closure Action Plan: 5 Critical Items for Architect Sign-Off"
type: action-plan
date: 2026-03-04
priority: P0 — Blocking M01 closure & M02 start
owner: Architect + Tech Lead
timeline: 2026-03-04 to 2026-03-07
---

# 🎯 M01 Closure Action Plan

**Purpose:** Execute 5 critical items identified in [Architect Review Sign-Off v1](./ARCHITECT_REVIEW_SIGN_OFF_v1.md)
**Status:** Ready for team allocation
**Blocker for:** M01 closure, M02 start authorization

---

## 📋 Action Items (Prioritized)

### Action 1: Clarify EPIC-08 Checkpoint Responsibility

**Severity:** 🟡 MEDIUM | **Effort:** 2 hours | **Owner:** Architect + EPIC-08 Tech Lead

#### Problem

- EPIC-08 architecture defines 3 MCP tools in goal.md
- TASK-08-02 only specifies 2 tools
- Unclear: Is `store_session_checkpoint()` M01 or M02 scope?

#### Deliverable

- **File to update:** `Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_01/epic_08/goal.md`
- **Change:** Add explicit "Out-of-Scope for EPIC-08" section OR "Implementation Plan for store_session_checkpoint()"

#### Decision Needed: Option A (Recommended) or Option B

**Option A: Defer checkpoint tool to M02 (EPIC-12)**

```markdown
## Out-of-Scope for EPIC-08 (M01)
- `store_session_checkpoint()` MCP tool implementation → EPIC-12 (M02)
- Session recovery logic (replay, restore) → EPIC-12 (M02)
- Checkpoint data compression / advanced features → EPIC-12 (M02)

## EPIC-08 Delivers (M01)
- `submit_artifact()` tool + RBAC checks
- `update_workflow_state()` tool + FSM state transition logic
- SQLite schema (sessions, artifacts, workflow_events, checkpoints tables)
  - checkpoints table schema **designed** but tool deferred
```

**Option B: Complete all 3 tools in M01**

```markdown
## EPIC-08 Delivers (M01)
- All 3 MCP write tools: submit_artifact, update_workflow_state, store_session_checkpoint
- SQLite schema (full)
- E2E tests covering all 3 tools
```

#### Recommendation

**Adopt Option A** because:

- M01 stays focused (WriteLayer core: submit + state update)
- Checkpoint persistence simpler without recovery logic
- M02 (EPIC-12) integrates checkpoints with episodic memory naturally

#### Steps

1. **Architect decides** (today): Option A or B
2. **Tech Lead approves** decision
3. **EPIC-08 assignee updates** goal.md + TASK-08-02 AC
4. **Status:** ✅ Done when goal.md updated + team agreed

#### Success Criteria

- [ ] goal.md section "Out-of-Scope" or "Checkpoint Implementation" added
- [ ] TASK-08-02 AC explicitly lists `submit_artifact()` + `update_workflow_state()`
- [ ] No ambiguity in TASK breakdown

**Timeline:** 2026-03-04 (today, 2 hours)
**Gate:** Must complete before EPIC-08 dev starts

---

### Action 2: Add Formal EPIC-09 Dependency on EPIC-08

**Severity:** 🔴 CRITICAL | **Effort:** 3 hours | **Owner:** Architect + EPIC-09/10 assignees

#### Problem

- EPIC-08 designs: sessions, artifacts, workflow_events, checkpoints SQLite tables
- EPIC-09 also designs: same tables (potential duplication)
- No formal dependency link → risk of schema rewrite during M02

#### Deliverable

- **Files to update:**
  1. `Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_02/epic_09/state.md`
  2. `Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_02/epic_10/state.md`

#### Changes

**In EPIC-09 state.md, add "Dependencies" section:**

```markdown
## Dependencies

### HARD BLOCKER
- **EPIC-08 (M01) must be CLOSED_DONE before EPIC-09 starts**
  - Rationale: EPIC-08 finalizes write-layer schema (sessions, artifacts, workflow_events, checkpoints)
  - EPIC-09 must **adopt** EPIC-08's schema; no re-design
  - EPIC-09 adds **new tables**: roles, role_schemas, runbooks, workflows, templates

### Coordination Task
- **EPIC-09 TASK-00: Schema Compatibility Review** (4 hours)
  - Assignee: EPIC-09 Tech Lead + Architect
  - Deliverable: Verified schema merging (EPIC-08 schema + EPIC-09 new tables = agent.db)
  - Approval: Architect sign-off on merged schema before TASK-01 starts

## Acceptance Criteria

- [x] EPIC-08 (M01) schema design reviewed by Architect
- [x] Artifact permissions FSM security model documented ([ADR Link](#))
- [ ] TASK-00 completed: EPIC-08 + EPIC-09 schema merge verified
- [ ] agent.db schema locked (no changes until M02 closure)
```

**In EPIC-10 state.md, add dependency to EPIC-09:**

```markdown
## Dependencies

- **Dependent on: EPIC-09 (M02)**
  - Rationale: bootstrap_agent() queries roles, runbooks, workflows tables (EPIC-09 tables)
  - Cannot implement bootstrap_agent() until EPIC-09 schema + seeder complete

### Implementation Order
1. EPIC-09 complete (SQLite schema + seeder)
2. EPIC-10 starts (bootstrap_agent() implementation)
```

#### Steps

1. **Architect writes** dependency sections
2. **Tech Lead reviews** and approves timeline impact
3. **EPIC-09 & EPIC-10 assignees review** and confirm they understand coordination gates
4. **Commit** updated state.md files

#### Success Criteria

- [ ] EPIC-09 state.md has "Dependencies" section with EPIC-08 blocker + TASK-00
- [ ] EPIC-10 state.md references EPIC-09 dependency
- [ ] Team acknowledges: No parallel schema design

**Timeline:** 2026-03-04 to 2026-03-05 (3 hours including review)
**Gate:** M02 sprint plan must reflect TASK-00 schedule

---

### Action 3: Lock M02 EPIC AC/DoD/Task Breakdown

**Severity:** 🟡 HIGH | **Effort:** 8–12 hours | **Owner:** Architect + Tech Lead (parallel work)

#### Problem

- EPIC-09–12 have state files but **AC/DoD not finalized**
- Implementation Plan § 3.1 says "EPIC-09–12 AC Draft (IN PROGRESS)"
- M02 cannot start dev until AC locked

#### Deliverables

**3a. EPIC-09 (SQLite Schema) Refinement — 4 hours**

- **File:** `Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_02/epic_09/state.md`
- **Add/Update:**

```markdown
## Detailed Acceptance Criteria

1. Schema design includes all tables (roles, role_schemas, runbooks, workflows, templates, sessions, artifacts, workflow_events, checkpoints)
2. Schema validated: No circular FK dependencies, indexes on frequently queried columns
3. Seeder script: Reads database/roles/** → populates agent.db (< 5 sec latency for 100+ roles)
4. Data consistency checks: No orphaned records (artifact without session, workflow without role, etc.)
5. Migration path: If agent.db exists, seeder can update (backward compat) or reset (dev-only)
6. E2E validation: testDb = seeded → queries all tables → performance acceptable

## Task Breakdown

| ID | Task | Estimate | Owner |
|:---|:-----|:---------|:------|
| 09-01 | Schema design & migration SQL | 3–4 days | Backend Dev |
| 09-02 | Seeder script implementation | 2–3 days | Backend Dev |
| 09-03 | E2E tests (schema validation, performance) | 2 days | QA + Backend |
```

**3b. EPIC-10 (bootstrap_agent) Refinement — 4 hours**

- **File:** `Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_02/epic_10/state.md`
- **Add/Update:**

```markdown
## Detailed Acceptance Criteria

1. `bootstrap_agent(domain, role, intent?, context?)` function signature defined
2. Happy path: Returns complete bootstrap payload JSON (identity, role_content, runbook, allowed_tools)
3. Error cases:
   - Unknown domain/role → 404 + error message
   - Unauthorized session → 403 + error message
   - DB error → 500 + retry guidance
4. SessionManager integration: Generates + returns session_id as part of response
5. RBAC: Only roles with "can_bootstrap" permission can call this tool
6. Performance: Response < 200ms for typical role (cold cache)

## Task Breakdown

| ID | Task | Estimate | Owner |
|:---|:-----|:---------|:------|
| 10-01 | bootstrap_agent payload design & typed contracts | 1–2 days | Backend Dev + Architect |
| 10-02 | bootstrap_agent() MCP tool implementation | 2–3 days | Backend Dev |
| 10-03 | Integration tests (SessionManager + RBAC) | 2 days | QA + Backend |
```

**3c. EPIC-11 (RBAC Migration) Refinement — 2 hours**

- **File:** `Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_02/epic_11/state.md`
- **Add/Update:**

```markdown
## Detailed Acceptance Criteria

1. Migration strategy defined: YAML file scan → SQLite normalization rules
2. Backward compat: Old .role.md files still readable during transition
3. Migration script: Tests on staging DB; verifies all permissions imported
4. Rollback plan: If RBAC migration fails, can revert to YAML-based RBAC
5. Performance: Migration completes in < 1 minute for 100+ role files

## Task Breakdown

| ID | Task | Estimate | Owner |
|:---|:-----|:---------|:------|
| 11-01 | Migration design & data mapping | 1–2 days | Architect + Backend |
| 11-02 | Migration script + validation | 2–3 days | Backend Dev |
| 11-03 | E2E tests (YAML → SQLite equivalence, rollback) | 2 days | QA + Backend |
```

**3d. EPIC-12 (Episodic Memory) Refinement — 2 hours**

- **File:** `Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_02/epic_12/state.md`
- **Update:**

```markdown
## Scope Clarification (Critical for M02 vs. M03 split)

**M02 In-Scope (EPIC-12 MVP):**
- Session recovery: Restore agent from last checkpoint
- ChromaDB write-back: Store agent reflection as embedding

**M02 Out-of-Scope (Deferred to M03):**
- Reflection loop (LLM-driven learning from past sessions)
- Experience clustering (group similar sessions for pattern detection)
- Active learning (recommend new domains based on history)

## Task Breakdown (M02 MVP)

| ID | Task | Estimate | Owner |
|:---|:-----|:---------|:------|
| 12-01 | Session recovery logic (checkpoint → restore) | 2–3 days | Backend Dev |
| 12-02 | ChromaDB write-back (store reflection)) | 2–3 days | Backend Dev |
| 12-03 | E2E tests (session crash → recovery) | 2 days | QA + Backend |
| 12-04 | (DEFERRED M03) Reflection loop implementation | - | Backend Dev |
```

#### Steps

1. **Architect + Tech Lead divide 4 epics** (parallel work, 2 per person)
2. **Each owner refines** their epic (AC + task breakdown) by end of day 2026-03-05
3. **Cross-review:** Architect + Tech Lead review each other's refinements
4. **Lock:** By COB 2026-03-07, all 4 epics have finalized AC/DoD
5. **Communicate:** Email team: "M02 AC locked, dev can begin 2026-03-10"

#### Success Criteria

- [ ] EPIC-09–12 state.md each have detailed AC section
- [ ] EPIC-09–12 each have task breakdown table (3–4 tasks per epic)
- [ ] Task estimates aligned with 10–14 day M02 timeline
- [ ] Tech Lead + QA reviewed and approved

**Timeline:** 2026-03-05 to 2026-03-07 (3 days, parallel)
**Gate:** M02 sprint planning meeting sign-off

---

### Action 4: Draft FSM Security & Concurrency ADR

**Severity:** 🟡 MEDIUM | **Effort:** 3 hours | **Owner:** Architect (solo)

#### Deliverable Status

✅ **ALREADY CREATED:** `database/joinerytech-flow/discovery/02-fsm-security-concurrency-draft.md`

#### What Architect Did

- Defined 3 options (pessimistic locking, optimistic, event sourcing)
- Recommended **Option A: Pessimistic Locking** for M01/M02
- Provided implementation checklist for EPIC-08 TASK-08-02

#### What This Action Requires

1. **Architect shares** ADR draft with EPIC-08 TASK-08-02 assignee (Backend Dev)
2. **Backend Dev reviews** and comments
3. **Architect incorporates feedback** by 2026-03-05
4. **Status:** ADR moves from "draft" to "team feedback received"

#### Success Criteria

- [ ] ADR draft shared with project team
- [ ] EPIC-08 TASK-08-02 assignee confirms pessimistic locking approach
- [ ] Implementation checklist reviewed (lock acquire/release, test cases)

**Timeline:** 2026-03-04 (3 hours for share + feedback collect)
**Gate:** EPIC-08 dev cannot start without FSM architecture agreement

---

### Action 5: Add EPIC-02 Implementation Summary

**Severity:** 🠆 MEDIUM | **Effort:** 2–3 hours | **Owner:** Backend Developer (EPIC-02 assignee)

#### Problem

- EPIC-02 (Dead Code Elimination) is "IN_DEV" but no EPIC-level implementation summary
- M01 closure DoD requires: "All epics have implementation-summary/"

#### Deliverable

- **File to create:** `Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_01/epic_02/implementation-summary/EPIC-02-summary.md`

#### Template Content

```markdown
---
type: implementation-summary
epic: EPIC-02
title: "Implementation Summary: Dead Code Elimination & Static Analysis"
date: 2026-03-XX
author: Backend Developer
reviewed_by: Tech Lead
---

# EPIC-02 Implementation Summary: Dead Code Elimination & Static Analysis

## Tasks Completed

| Task | Status | Description |
|:-----|:--------|:------------|
| TASK-02-01 | ✅ | Statistic analysis (ts-prune + tsc --noUnusedLocals) and dead code removal |

## Code Changes Summary

### Files Deleted
- Count: N files
- Examples: ...unused files...

### Unused Exports Removed
- Count: N exports
- Examples: ...from which modules...

### Total Dead Code Removed
- Lines deleted: N
- Modules simplified: N

## Quality Metrics

| Metric | Before | After |
|:-------|:--------|:------|
| tsc --noUnusedLocals errors | X | 0 |
| ts-prune "unused" warnings | Y | 0 |
| Total TypeScript errors | Z | (no new errors introduced) |

## Testing & Validation

- [x] Existing E2E tests pass (EPIC-01 RBAC tests ran green → no regression)
- [x] No runtime errors after deletion
- [x] Module imports still resolve correctly

## Risks Addressed

- ✅ No breaking API changes (internal code only)
- ✅ No side effects on public MCP tool surface
- ✅ No dependencies on deleted exports (verified via grep)

## Approval Checklist

- [ ] **QA Sign-Off:** Test report filed (E2E tests green)
- [ ] **Tech Lead Sign-Off:** Code review completed
- [ ] **Architect Sign-Off:** Pending (tied to M01 closure gate)

## Notes

- Static analysis tool versions: ts-prune 4.x.x, tsc 5.x.x
- Next phase: Full TypeScript strict mode (out of scope for M01)
```

#### Steps

1. **EPIC-02 assignee creates** summary file (with template above)
2. **Fill in specifics:** Actual files deleted, lines removed, metrics
3. **Tech Lead reviews** and signs off (test report confirms no regression)
4. **Architect reviews** as part of M01 closure gate

#### Success Criteria

- [ ] EPIC-02-summary.md exists with populated metrics
- [ ] QA signed off (E2E tests green, test report linked)
- [ ] Tech Lead reviewed
- [ ] No ambiguity about what was removed and why

**Timeline:** 2026-03-05 to 2026-03-06 (2–3 hours including review)
**Gate:** Before M01 closure DoD finalization

---

## 📊 Execution Timeline

```
┌─ DAY 1 (2026-03-04 — TODAY) ──────────────────────────────────────────┐
│                                                                         │
│  PARALLEL WORK:                                                         │
│  ├─ Action 1 (2 h): Architect + EPIC-08 Tech Lead decide checkpoint    │
│  ├─ Action 2 (3 h): Architect + EPIC-09/10 owners add dependencies      │
│  └─ Action 4 (✅ DONE): ADR draft created; share with team             │
│                                                                         │
│  END OF DAY 1: 3 actions assigned, 1 done, 1 in progress               │
└─────────────────────────────────────────────────────────────────────────┘

┌─ DAY 2–3 (2026-03-05 to 2026-03-06) ───────────────────────────────┐
│                                                                     │
│  PARALLEL WORK:                                                     │
│  ├─ Action 3 (8 h): Architect + Tech Lead refine EPIC-09–12 AC      │
│  ├─ Action 5 (3 h): EPIC-02 assignee drafts implementation summary  │
│  └─ Action 4 follow-up: Architect collects team feedback on ADR     │
│                                                                     │
│  END OF DAY 3: All 5 actions substantially complete                 │
└─────────────────────────────────────────────────────────────────────┘

┌─ DAY 4 (2026-03-07) ───────────────────────────────────────────┐
│                                                                │
│  FINAL REVIEW & SIGN-OFF:                                      │
│  ├─ Architect + Tech Lead cross-review all 5 action outcomes  │
│  ├─ Any gaps identified → quick fixes                         │
│  └─ Accept or deny M01 closure                                │
│                                                                │
│  End of day 4: Architect sign-off ready (if all 5 done)       │
└────────────────────────────────────────────────────────────────┘

┌─ DAY 5–6 (2026-03-10 or later) ─────────────────────────────┐
│                                                              │
│  M02 START (if all gates pass):                             │
│  ├─ EPIC-08 dev continues (no blockers from M01)            │
│  ├─ EPIC-09 TASK-00: Schema compatibility review starts     │
│  ├─ EPIC-10 waits for EPIC-09 seeder (dependency gate)      │
│  └─ M02 sprint planning meeting w/ finalized AC             │
│                                                              │
│  M01 CLOSED_DONE ✅                                          │
└──────────────────────────────────────────────────────────────┘
```

---

## 🎯 Sign-Off Gate

### Architect Will Approve M01 Closure ✅ When

- [ ] **Action 1 Complete:** EPIC-08 goal.md clarified (checkpoint scope)
- [ ] **Action 2 Complete:** EPIC-09 & EPIC-10 state.md updated (dependencies)
- [ ] **Action 3 Complete:** EPIC-09–12 AC/DoD/task breakdown finalized
- [ ] **Action 4 Complete:** FSM Security ADR draft reviewed by dev team
- [ ] **Action 5 Complete:** EPIC-02 implementation summary filed

### Then Architect Provides

✅ **ARCHITECT_REVIEW_SIGN_OFF_v1.md** → updates "Sign-Off Decision" section to **APPROVED**
✅ **Email to team:** "M01 gates met. M02 start authorized. See finalized epic AC."

---

## 📎 Related Documents

1. **[Architect Review Sign-Off v1](./ARCHITECT_REVIEW_SIGN_OFF_v1.md)** — Full review with findings
2. **[FSM Security & Concurrency ADR](../../../database/joinerytech-flow/discovery/02-fsm-security-concurrency-draft.md)** — Technical design for state machine
3. **[IMPLEMENTATION_PLAN_v1](./IMPLEMENTATION_PLAN_v1.md)** — Original tech lead plan (this action plan refines Phase 3.1–3.5)
4. **[QUALITY_AUDIT_REPORT](./QUALITY_AUDIT_REPORT.md)** — Original audit (superseded by Architect Review)

---

## 🚀 Success

**When all 5 actions complete (by 2026-03-07):**

✅ M01 closure is architecturally sound
✅ M02 is ready to start (no architectural blockers)
✅ Team has clear acceptance criteria for all 12 EPIC-09–12 tasks
✅ FSM security model documented and agreed
✅ EPIC-08 ↔ EPIC-09 dependency prevents schema rework

**Result:** Smooth M01→M02 transition; no scope creep; quality maintained.
