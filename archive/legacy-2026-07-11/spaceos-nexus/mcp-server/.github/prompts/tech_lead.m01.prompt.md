# Tech Lead Prompt — M01 Closure Coordination

**Version:** 1.0
**Milestone:** M01 (Closure Phase)
**Project:** mcp-context-server
**Audience:** Tech Leads, Architects, Project Managers
**Purpose:** Execute 5 critical closure coordination tasks (EPIC-00) before M02 start
**Use with Agent:** `.github/agents/tech_lead.coordination.agent.md` (substitute `${MILESTONE_ROOT}` = `.../milestone_01/`)

- **Scope clarification** → Resolve ambiguities (EPIC-08 checkpoint tool: M01 or M02?)
- **Dependency verification** → Confirm EPIC-08 → EPIC-09 blocker relationship
- **Acceptance criteria finalization** → Lock EPIC-09–12 AC/DoD for M02 sprint
- **FSM architecture documentation** → Validate security & concurrency model
- **Closure sign-off** → Verify all M01 epics meet Definition of Done

---

## 2. Audience & Stakeholders

- **Architect** — Reviews decisions, signs off scope, validates ADRs
- **EPIC-08 Tech Lead** — Confirms checkpoint implementation plan
- **EPIC-09–12 Tech Leads** — Provides AC/DoD feedback, priority ranking
- **QA Lead** — Verifies closure checklist, test coverage
- **Backend Developers** — Ready for EPIC-09 start (blocked on M01)

---

## 3. M01 Context (EPIC-00 Overview)

### EPIC-00: Architect Coordination & Audit Actions

**Goal:** Resolve 5 critical architectural gaps before M02 start
**Scope:** Closure coordination (not development)
**Status:** Planned (5 tasks to execute by 2026-03-07)

### M01 Dev Epics (Already In Progress)

| EPIC | Project | Tasks | Status | Tech Lead Owner |
|:-----|:--------|:-----:|:------:|:---|
| EPIC-01 | mcp-rbac | 4 | In Progress | — |
| EPIC-02 | mcp-rbac | 3 | In Progress | — |
| EPIC-08 | mcp-maintenance | 3 | In Dev | Write Layer owner |

### EPIC-00 Tasks (Your Coordination Work)

| Task | Owner | Title | AC | Sign-Off |
|:-----|:------|:------|:--:|:------:|
| TASK-00-01 | Architect + EPIC-08 Lead | Clarify EPIC-08 checkpoint responsibility | [ ] | [ ] |
| TASK-00-02 | EPIC-09–12 Leads | Lock EPIC-09–12 AC/DoD dependencies | [ ] | [ ] |
| TASK-00-03 | Architect | Generate M02 critical path & blockers | [ ] | [ ] |
| TASK-00-04 | Architect + FSM owner | FSM Security & Concurrency ADR review | [ ] | [ ] |
| TASK-00-05 | All Epic Owners | M01 closure verification & sign-off | [ ] | [ ] |

---

## 4. M01 Closure Blockers & Decisions

### Blocker #1: EPIC-08 Checkpoint Tool Scope

**Ambiguity:** Is `store_session_checkpoint()` MCP tool M01 or M02?

**Context:**
- EPIC-08 goal.md lists 3 write tools (submit_artifact, update_session_state, store_session_checkpoint)
- TASK-08-02 implementation guide only mentions 2 tools
- EPIC-12 (M02) naturally integrates checkpoints with episodic memory

**Your Decision (TASK-00-01):**
- ✅ **Recommended: Option A** — Defer `store_session_checkpoint()` to EPIC-12 (M02)
  - Rationale: M01 stays focused; checkpoint without recovery logic is incomplete
- ❌ Option B — Complete all 3 in M01 (requires EPIC-12 re-scope)

**Acceptance Criteria (TASK-00-01):**
- [ ] Decision made & documented in EPIC-08 goal.md § "Out-of-Scope"
- [ ] TASK-08-02 AC updated to reflect decision (2 tools vs. 3)
- [ ] EPIC-08 assignee + Tech Lead agreed
- [ ] Zero ambiguity in task breakdown

**File Updates Required:**
1. `Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_01/epic_08/goal.md`
   - Add or clarify § "Out-of-Scope" section
2. `Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_01/epic_08/tasks/TASK-08-02.md`
   - Update AC to specify 2 or 3 tools

---

### Blocker #2: EPIC-09–12 AC/DoD Dependencies

**Ambiguity:** M02 tasks lack finalized acceptance criteria & priority sequencing.

**Context:**
- EPIC-09: SQLite schema (baseline for all others)
- EPIC-10: bootstrap_agent() aggregate tool (depends on EPIC-09 schema)
- EPIC-11: RBAC middleware (depends on EPIC-09 + EPIC-10)
- EPIC-12: Episodic memory (depends on EPIC-09, includes EPIC-08 checkpoint tool)
- EPIC-13: Discovery track tools (parallel to EPIC-09–11)
- EPIC-14: Modern transports (depends on EPIC-11, parallel final phase)

**Your Task (TASK-00-02):**
- Gather **EPIC-09–14 Tech Leads**
- Validate AC is **concrete & measurable** (not vague)
- Lock **priority ordering** (EPIC-09 must start first)
- Document **blocking relationships** (9→10, 9→11, 9→12, etc.)

**Acceptance Criteria (TASK-00-02):**
- [ ] All 6 EPICs have finalized AC (functional + quality + delivery)
- [ ] No conflicting AC across milestones
- [ ] Dependency graph locked (captured in M02 prompt context)
- [ ] Tech Lead consensus documented
- [ ] M02 sprint can start without scope creep

**File Updates Required:**
1. Update each EPIC goal.md (if AC needs clarification):
   - `Docs/.../milestone_02/epic_09/goal.md`
   - `Docs/.../milestone_02/epic_10/goal.md`
   - … etc.
2. Create/update `M02_CRITICAL_PATH.md` (dependency diagram)

---

### Blocker #3: M02 Critical Path & Risk Mitigation

**Ambiguity:** No single-source-of-truth for M02 blockers, parallel work, risk lanes.

**Your Task (TASK-00-03):**
- Map **critical path** (EPIC-09 → EPIC-10/11 → EPIC-14)
- Identify **parallel tracks** (EPIC-13, optional EPIC-14 early prep)
- Highlight **schema conflicts** (EPIC-08/09 write layer schema overlap)
- Flag **resource risks** (schema expertise, concurrent SQLite locking knowledge)
- Create **M02 Release Burn-Down** (weekly milestones)

**Acceptance Criteria (TASK-00-03):**
- [ ] Critical path diagram created (`M02_CRITICAL_PATH.md`)
- [ ] Parallel work tracks identified (≥2 tracks can run simultaneously)
- [ ] Risk matrix populated (5-10 identified risks with mitigations)
- [ ] M02 sprint can be sliced into phases (Phase 1: EPIC-09, Phase 2: EPIC-10/11, etc.)

**File Updates Required:**
1. Create `Docs/mcp-context-server/delivery/mcp-maintenance/M02_CRITICAL_PATH.md`
2. Create `Docs/mcp-context-server/delivery/mcp-maintenance/M02_RISK_MATRIX.md`

---

### Blocker #4: FSM Security & Concurrency ADR Review

**Ambiguity:** FSM (Finite State Machine) workflow model has security & concurrency gaps.

**Context:**
- EPIC-00 task identified potential issues:
  - Race condition: Two agents updating same session state simultaneously
  - Missing transaction isolation (SQLite WAL mode, PRAGMA foreign_keys)
  - Checkpoint safety (concurrent access to checkpoint data)
- ADR draft exists: `database/joinerytech-flow/discovery/02-fsm-security-concurrency-draft.md`

**Your Task (TASK-00-04):**
- Review ADR draft with **Architect + FSM owner**
- Validate **locking strategy** (pessimistic vs. optimistic)
- Confirm **SQLite config** (WAL, foreign_keys, journal_mode)
- Get **security sign-off** from Architect
- Lock ADR as ACCEPTED (no changes in M02)

**Acceptance Criteria (TASK-00-04):**
- [ ] ADR draft reviewed by Architect & FSM owner
- [ ] Concurrency model finalized (pessimistic locking recommended for M01)
- [ ] SQLite config locked in EPIC-09 schema task
- [ ] No FSM changes allowed in M02 (ADR is frozen)
- [ ] Architect sign-off recorded

**File Updates Required:**
1. Finalize `database/joinerytech-flow/discovery/02-fsm-security-concurrency-draft.md`
   - Add "Status: ACCEPTED" frontmatter
   - Lock decision: Pessimistic locking (Session-level locks)
2. Document SQLite config in EPIC-09 task

---

### Blocker #5: M01 Closure Verification & Sign-Off

**Ambiguity:** M01 cannot close without all epics meeting Definition of Done.

**Context:**
- EPIC-01: ✅ Should be CLOSED_DONE (4 tasks completed)
- EPIC-02: ✅ Should be CLOSED_DONE (3 tasks completed)
- EPIC-08: 🚧 In Dev, TASK-08-01/02 estimated to complete by 2026-03-06
- EPIC-00: 🔄 Coordination tasks (5 tasks, your responsibility)

**Your Task (TASK-00-05):**
- Verify all EPIC-01, EPIC-02 tasks have **Implementation Summaries**
- Track EPIC-08 task progress (ETA 2026-03-06)
- Ensure EPIC-00 tasks complete by deadline (2026-03-07)
- Update `M01_CLOSURE_ACTION_PLAN.md` status
- Obtain **Architect sign-off** for M02 gate approval

**Acceptance Criteria (TASK-00-05):**
- [ ] EPIC-01 tasks have implementation-summary/ folders
- [ ] EPIC-02 tasks have implementation-summary/ folders
- [ ] EPIC-08 TASK-08-01/02 marked CLOSED_DONE (by 2026-03-06)
- [ ] EPIC-00 TASK-00-01 through TASK-00-05 marked CLOSED_DONE (by 2026-03-07)
- [ ] M01 closure documentation complete
- [ ] Architect grants M02 gate approval

**File Updates Required:**
1. Update each epic implementation-summary/ (if missing):
   - `Docs/.../milestone_01/epic_01/implementation-summary/EPIC-01-summary.md`
   - `Docs/.../milestone_01/epic_02/implementation-summary/EPIC-02-summary.md`
   - `Docs/.../milestone_01/epic_08/implementation-summary/EPIC-08-summary.md`
2. Update `Docs/mcp-context-server/delivery/mcp-maintenance/M01_CLOSURE_ACTION_PLAN.md`
   - Add status checkboxes completed

---

## 5. Execution Workflow (Your Process)

### Step 1: Kickoff (2026-03-04, 30 min)
- [ ] Schedule calls with TASK-00-01 through TASK-00-05 owners
- [ ] Share this prompt with tech leads
- [ ] Confirm deadline (2026-03-07)
- [ ] Clarify escalation (Architect is final approver)

### Step 2: Resolve Blocker #1 (TASK-00-01, 2-4 hours)
- [ ] Meet with EPIC-08 Tech Lead
- [ ] Decide: Option A (defer checkpoint to M02) or Option B (complete in M01)
- [ ] Update EPIC-08 goal.md + TASK-08-02 AC
- [ ] Get Architect sign-off

### Step 3: Finalize M02 AC/DoD (TASK-00-02, 4-6 hours)
- [ ] Gather EPIC-09–14 Tech Leads
- [ ] Review each epic's AC (functional, quality, delivery)
- [ ] Lock priority ordering (EPIC-09 first, then 10/11, 13 parallel, 14 final)
- [ ] Document dependency graph in M02 context
- [ ] Consensus sign-off

### Step 4: Map M02 Critical Path (TASK-00-03, 2-4 hours)
- [ ] Create dependency diagram (Mermaid or ASCII)
- [ ] Identify parallel tracks & sprint phases
- [ ] Risk matrix (5-10 risks + mitigations)
- [ ] Architect review & sign-off

### Step 5: Review FSM ADR (TASK-00-04, 2-3 hours)
- [ ] Review `02-fsm-security-concurrency-draft.md`
- [ ] Validate locking strategy & SQLite config
- [ ] Lock decision: Pessimistic locking (Session-level)
- [ ] Architect final sign-off

### Step 6: Closure Verification (TASK-00-05, 2-4 hours)
- [ ] Verify all implementation-summary/ folders exist
- [ ] Track EPIC-08 progress (must be CLOSED_DONE by 2026-03-06)
- [ ] Confirm EPIC-00 tasks completed
- [ ] Update M01_CLOSURE_ACTION_PLAN.md
- [ ] Architect grants M02 gate approval

---

## 6. Requirements & Constraints

### Must Have
- ✅ **Scope clarity** — Zero ambiguity in EPIC-08, EPIC-09–12
- ✅ **Architect sign-off** — Every decision requires Architect approval
- ✅ **Documentation** — All decisions recorded in goal.md files
- ✅ **No scope creep** — M02 AC locked, no changes after 2026-03-07

### Nice to Have
- 🔷 Risk matrix with mitigations
- 🔷 M02 sprint phase breakdown
- 🔷 Resource allocation plan

### Constraints
- ⏰ **Hard deadline:** 2026-03-07 (M01 closure)
- 🔒 **One approval gate:** Architect final sign-off (no parallel approvals)
- 📋 **Escalation path:** If blocked, escalate to Architect immediately (no delays)

---

## 7. Golden Rules (M01 Tech Lead)

1. **Scope is binary** — Option A or Option B (no Option C)
2. **Architect decides** — You facilitate, Architect approves
3. **Document everything** — Decisions in goal.md, not Slack
4. **Dependencies lock M02** — EPIC-09 is blocker for all others
5. **No half-measures** — Closure means all AC passing (80%+ coverage)
6. **Risk transparency** — Surface concerns early, escalate to Architect
7. **Stakeholder alignment** — Tech Leads must agree before Architect review
8. **FSM is frozen** — No FSM changes in M02 (ADR is locked)

---

## 8. Deliverables Checklist

### TASK-00-01 Output
- [ ] EPIC-08 goal.md updated (Out-of-Scope section clear)
- [ ] TASK-08-02.md AC reflects decision
- [ ] Architect sign-off recorded

### TASK-00-02 Output
- [ ] EPIC-09–12 AC finalized & documented
- [ ] Dependency graph locked (6 EPICs, clear ordering)
- [ ] Tech Lead consensus document
- [ ] Architect sign-off

### TASK-00-03 Output
- [ ] M02_CRITICAL_PATH.md created (dependency diagram)
- [ ] M02_RISK_MATRIX.md (5-10 risks identified)
- [ ] Sprint phase breakdown (Phase 1: EPIC-09, Phase 2: 10/11, etc.)

### TASK-00-04 Output
- [ ] ADR finalized & accepted (02-fsm-security-concurrency-draft.md)
- [ ] SQLite config locked (PRAGMA foreign_keys, WAL mode)
- [ ] Architect concurrency model approval

### TASK-00-05 Output
- [ ] All implementation-summary/ folders verified (EPIC-01, 02, 08)
- [ ] M01_CLOSURE_ACTION_PLAN.md updated (status = ✅ COMPLETE)
- [ ] Architect M02 gate approval

---

## 9. References

**Documents:**
- [M01_CLOSURE_ACTION_PLAN.md](../Docs/mcp-context-server/M01_CLOSURE_ACTION_PLAN.md)
- [02-fsm-security-concurrency-draft.md](../database/joinerytech-flow/discovery/02-fsm-security-concurrency-draft.md)
- EPIC-08 goal.md, TASK-08-01, TASK-08-02, TASK-08-03
- EPIC-09–14 goal.md files

**Contact:**
- Architect: Approver for all decisions
- EPIC-08 Tech Lead: Checkpoint tool scope
- EPIC-09–14 Tech Leads: AC/DoD feedback
- QA Lead: Test coverage validation

---

**Version History:**
- v1.0 (2026-03-05): M01 closure coordination framework for EPIC-00 tasks
