---
id: epic-00-critical-review
title: "Critical Review: EPIC-00 (M01 Architect Coordination) — Quality & Feasibility"
type: critical-review
date: 2026-03-04
reviewer: Tech Lead
---

# 🔴 Critical Review: EPIC-00 — Architectural Coordination Meta-Epic

**Status:** 🔴 **CRITICAL ISSUES FOUND** (Design Review Required)
**Date:** 2026-03-04
**Scope:** EPIC-00 structure, task feasibility, timeline realism, architectural completeness

---

## Executive Summary

EPIC-00 egy jó **intenciójú koordinációs szerkezet**, de **három kritikus szervezési és megvalósíthatósági hiba** van, amely blokkolhatja az M01 lezárást.

### Kritikus Findingsek

| # | Issue | Severity | Impact | Státusz |
|:--|:------|:----------|:--------|:--------|
| 1 | TASK-00-03 scope creep (8–12 óra, 3 napos timeline) | 🔴 CRITICAL | M01 delays | ⚠️ Requires split |
| 2 | EPIC-00 nem tracker az `state.md` top-level epic listában | 🟡 HIGH | Invisible coordination | ⚠️ Add reference |
| 3 | FSM ADR SQLite concurrency specifics hiányzik | 🟡 HIGH | EPIC-08 TASK-08-02 blocker | ⚠️ Needs detail |
| 4 | TASK-00-02 EPIC-09 dependency hiánya (no state.md file yet) | 🟠 MEDIUM | M02 planning gap | ⚠️ Missing files |
| 5 | AC/DoD gates nincs Architect sign-off checkpoint | 🟠 MEDIUM | Quality drift | ⚠️ Add review gate |

---

## 🔴 Critical Issue #1: TASK-00-03 Timeline Impossibility

### Problem

**TASK-00-03: "Lock M02 EPIC AC/DoD"** — 8–12 óra, 3 napos M01 timeline-ba nem fér bele.

- **Timeline:** Deadline 2026-03-07 EOD
- **Effort estimate:** 8–12 óra (parallel Architect + Tech Lead)
- **What it requires:**
  - Finalize detailed AC (5+ per epic, 4 epics) = 4–6 óra design work
  - Task breakdown (3–4 tasks per epic, with estimates) = 2–3 óra
  - Tech Lead + QA + Architect review cycle = 2–3 óra
  - Risk: Sequential work (not truly parallelizable) → realistically **12–16 óra**

### Impact

- **Blocker:** M02 cannot start without finalized AC
- **Risk:** EPIC-09–12 AC remains vague → dev team "figures it out during sprint"
- **Quality:** Scope creep, test cases delayed, misaligned expectations

### Root Cause

- TASK-00-03 grouped too much work (4 epics, full AC + breakdown)
- Underestimated complexity of AC refinement (each epic has unique concerns)
- No intermediate reviews (blocks waiting for all 4 together)

### Remediation: Split TASK-00-03 into 2 Tasks

**TASK-00-03A: Quick AC Lock (4 hours, by 2026-03-06)**

- High-level acceptance criteria for EPIC-09–12
- Prerequisite: Each epic state.md has 3–5 AC bullets (rough, not detailed)
- Owner: Architect + Tech Lead (parallel)
- Deliverable: state.md files with revised AC section

**TASK-00-03B: Detailed Task Breakdown (4 hours, by 2026-03-08)**

- Detailed task breakdown (3–4 tasks per epic, with estimates)
- Resource estimates: Can be deferred to M02 sprint planning if needed
- Owner: Architect + developers (after EPIC assignments)
- Deliverable: Task tables in state.md

**Updated Timeline:**

- 2026-03-06 EOD: TASK-00-03A complete → M02 AC lock
- 2026-03-08 EOD: TASK-00-03B complete → M02 sprint ready

---

## 🟡 Critical Issue #2: EPIC-00 nem látható az mcp-maintenance Tracking-ben

### Problem

**EPIC-00 nincs az mcp-maintenance/state.md epic listáján** → koordinációs munka "invisible"

Current mcp-maintenance/state.md:

```
| EPIC-01 | RBAC schema update & root cleanup | CLOSED_DONE |
| EPIC-02 | Dead Code Elimination & Static Analysis | IN_DEV |
| EPIC-08 | MCP Write Layer — Artifact Submit & Session Control | IN_DEV |
| EPIC-09 | SQLite Schema Design & Database Seeder | BACKLOG_READY (M02) |
...
```

EPIC-00 **hiányzik** → státús: nincs tracking.

### Impact

- **Invisible work**: Architect coordination efforts nem track-elve
- **Metric gap**: M01 progress report nem mutatja EPIC-00-at
- **Risk**: Nobody knows ki az owner, ki az deliverable

### Remediation

**Update mcp-maintenance/state.md — Epic State Map**

```markdown
| EPIC-00 | M01 Architect Coordination & Audit Actions | IN_DEV | Architect + Tech Lead |
| EPIC-01 | RBAC schema update & root cleanup | CLOSED_DONE | Backend Developer |
| EPIC-02 | Dead Code Elimination & Static Analysis | IN_DEV | Backend Developer |
| EPIC-08 | MCP Write Layer — Artifact Submit & Session Control | IN_DEV | Backend Developer |
...
```

**Also update milestone_01/plan.md:**

```markdown
## Epicek
*   **EPIC-00: M01 Architect Coordination & Audit Actions** (🚧 IN_DEV, 5 tasks)
*   **EPIC-01: RBAC Schema Update & Server Root Cleanup** (✅ CLOSED_DONE)
*   **EPIC-02: Dead Code Elimination & Static Analysis** (🚧 IN_DEV, TASK-02-01 completed)
*   **EPIC-08: MCP Write Layer — Artifact Submit & Session Control** (🚧 IN_DEV, TASK-08-01 … 08-03 planned)
```

---

## 🟡 Critical Issue #3: FSM ADR SQLite Concurrency Gap

### Problem

**TASK-00-04 (FSM ADR) hiányzik SQLite-specifikus guidance** az **pessimistic locking** implementációjához.

Current ADR recommendation:
> "Option A: Pessimistic Locking (Recommended) — exclusive access, simple, SQLite-friendly"

But **missing crucial details for SQLite:**

- SQLite locking model (коротко, SHARED vs. EXCLUSIVE locks)
- How to implement pessimistic locking in SQLite (BEGIN IMMEDIATE vs. BEGIN EXCLUSIVE)
- Deadlock prevention (lock ordering)
- Performance implications (single-writer SQLite limitation)
- Transaction isolation level (SERIALIZABLE vs. READ COMMITTED)

### Impact

**EPIC-08 TASK-08-02** (MCP Tools) cannot implement safely:

- Developer doesn't know SQLite locking syntax
- Risk: Race conditions (e.g., session state partially updated)
- Risk: Deadlocks (concurrent artifact submits)
- Risk: Lost updates (two agents write same session state)

### Remediation

**Enhance TASK-00-04 ADR — Add SQLite Implementation Section**

```markdown
## 4.1 SQLite Implementation: Pessimistic Locking

### SQLite Locking Model

SQLite uses coarse-grained locking:
- **Default mode (autocommit)**: Each transaction is: SHARED lock → (write) EXCLUSIVE lock → commit
- **BEGIN IMMEDIATE**: Forces lock upgrade to RESERVED (safer for planned writes)
- **BEGIN EXCLUSIVE**: Exclusive lock immediately (highest safety, but blocks readers)

### Recommended: BEGIN IMMEDIATE Pattern

For MCP write operations (submit_artifact, update_workflow_state):

```sql
-- TASK-08-02 implementation pseudocode
BEGIN IMMEDIATE;
  SELECT * FROM sessions WHERE id = ? FOR UPDATE;  -- SQLite doesn't have FOR UPDATE, use BEGIN IMMEDIATE instead
  -- Perform state checks (authorization, FSM validation)
  UPDATE sessions SET fsm_state = ? WHERE id = ?;
  INSERT INTO workflow_events (...) VALUES (...);
COMMIT;
```

**Why BEGIN IMMEDIATE?**

- Acquires lock at transaction start (no mid-transaction upgrades)
- Prevents other writers from starting concurrent transaction
- Allows reads during transaction (readers use SHARED locks)
- Lower deadlock risk than BEGIN EXCLUSIVE

### Deadlock Prevention: Lock Ordering

SQLite deadlock prevention:

1. Always lock sessions table first (parent table)
2. Then lock artifacts, workflow_events (child tables)
3. Consistent ordering prevents circular waits

### Performance: Single-Writer Limitation

⚠️ **Important**: SQLite can only have ONE writer at a time.

- Multiple agents cannot concurrently call submit_artifact()
- Queue incoming write requests or serialize at application layer
- Options:
  - **Option 1 (Simple)**: Application-level request queue (FIFO)
  - **Option 2 (Complex)**: Message broker (Redis, RabbitMQ) for M02+
  - **Recommendation**: Option 1 for M01; reevaluate for M02 scale

### Transaction Isolation Level

SQLite default: SERIALIZABLE isolation (safest).

```sql
PRAGMA journal_mode = WAL;      -- Use WAL mode for better concurrent reads
PRAGMA synchronous = NORMAL;    -- Reasonable safety + performance
```

### Implementation Checklist for EPIC-08 TASK-08-02

- [ ] Use BEGIN IMMEDIATE for all write transactions
- [ ] Validate session exists + authorized before INSERT/UPDATE
- [ ] Lock ordering: always sessions first, then children
- [ ] Test: Concurrent submit_artifact() calls (verify serialization)
- [ ] Monitor: SQLite locked database error (implement retry + backoff)
- [ ] Documentation: team aware of single-writer limitation

```

---

## 🟠 Critical Issue #4: EPIC-09 Missing state.md (Dependency Not Formalized)

### Problem

**TASK-00-02 "Add Formal EPIC-09 Dependency on EPIC-08"** — но EPIC-09 state.md **не существует**.

EPIC-00 TASK directory strukture:
```

Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_02/epic_09/ ← NOT YET CREATED

```

TASK-00-02 expects to update a file that doesn't exist yet.

### Impact

- TASK-00-02 cannot complete (file to edit missing)
- Dependency formalization blocked
- M02 planning cannot start until EPIC-09 organized

### Remediation

**Create EPIC-09 skeleton BEFORE TASK-00-02 starts**

EPIC-09 folder structure (milestone_02-ben):
```

milestone_02/epic_09/
├── goal.md                 ← Already exists?
├── state.md               ← CREATE THIS
├── tasks/
│   ├── TASK-09-00.md      ← Schema Compatibility Review (FROM TASK-00-02)
│   ├── TASK-09-01.md      ← SQLite Schema Design & Migration
│   └── TASK-09-02.md      ← Database Seeder & Tests
└── implementation-summary/

```

**Action: Create milestone_02/epic_09/state.md skeleton**

Template:
```markdown
---
id: state-epic-09
title: "State: EPIC-09 — SQLite Schema Design & Database Seeder"
epic: EPIC-09
milestone: M02
project: mcp-maintenance
fsm_state: BACKLOG_READY
---

# 📊 Epic State: EPIC-09

## Dependencies

### Hard Blocker
- **EPIC-08 (M01) must be CLOSED_DONE before EPIC-09 starts**
  - Rationale: EPIC-08 finalizes write-layer schema (sessions, artifacts, etc.)
  - EPIC-09 adopts EPIC-08 schema; no re-design

## Task Map

[Will be updated by TASK-00-02]
```

---

## 🟠 Critical Issue #5: No Architect Sign-Off Gate for EPIC-00 AC/DoD

### Problem

**EPIC-00 goal.md + state.md nincs explicit Architect approval checkpoint** az AC/DoD-ban.

- TASK-00-01, 02, 03, 05: Each has "Sign-Off" section
- But **EPIC-00 itself** nincs "Architect Approval" requirement a teljesítéshez
- Risk: Tasks complete but Architect nunca signs off on EPIC

### Impact

- EPIC-00 състояние ambiguous (done? not done?)
- M01 closure gate unclear (when can we declare M01 closed?)
- M02 start authorization missing (who approves?)

### Remediation

**Update EPIC-00 state.md — Add Final Sign-Off Section**

```markdown
## 🔐 Final Sign-Off Gates

### Prerequisites for EPIC-00 CLOSED_DONE

1. All 5 TASK-00-0X tasks completed (✅ or 🔴 with clear mitigation)
2. Each task has Architect sign-off comment
3. **Architect final review & "M01 APPROVED"** sign-off on this state.md

### M01 Closure Checklist (from EPIC-00 perspective)

- [ ] TASK-00-01: EPIC-08 checkpoint scope (Option A or B decided + documented)
- [ ] TASK-00-02: EPIC-09 dependency formalized (state.md updated)
- [ ] TASK-00-03A: Quick AC lock (EPIC-09–12 high-level AC finalized)
- [ ] TASK-00-03B: Task breakdown (resource estimates provided)
- [ ] TASK-00-04: FSM Security & Concurrency ADR (team reviewed)
- [ ] TASK-00-05: EPIC-02 implementation summary (added)

### Architect Sign-Off

**[ ] Architect final approval:** All actions complete, no blockers for M02 start

_Signed by: Architect_
_Date: 2026-XX-XX_
```

---

## Summary: Remediation Checklist

### Phase 1: Immediate Actions (Today, 2026-03-04)

- [ ] **Issue #1**: Split TASK-00-03 into TASK-00-03A (4h, by 2026-03-06) + TASK-00-03B (4h, by 2026-03-08)
- [ ] **Issue #2**: Add EPIC-00 to mcp-maintenance/state.md epic list
- [ ] **Issue #2**: Update milestone_01/plan.md with EPIC-00
- [ ] **Issue #3**: Enhance TASK-00-04 ADR with SQLite pessimistic locking section
- [ ] **Issue #4**: Create milestone_02/epic_09/state.md skeleton
- [ ] **Issue #5**: Add "Final Sign-Off Gates" section to EPIC-00 state.md

### Phase 2: Task Execution (2026-03-05 to 2026-03-08)

- [ ] TASK-00-01: Complete by 2026-03-04 (today evening)
- [ ] TASK-00-02: Complete by 2026-03-05 (uses new epic_09/state.md)
- [ ] TASK-00-03A: Complete by 2026-03-06
- [ ] TASK-00-04: Finalize feedback by 2026-03-05
- [ ] TASK-00-05: Complete by 2026-03-06
- [ ] TASK-00-03B: Complete by 2026-03-08 (parallel option)

### Phase 3: Post-EPIC-00 (2026-03-07 to 2026-03-10)

- [ ] Architect reviews all 5 task deliverables
- [ ] Architect provides "M01 APPROVED" sign-off
- [ ] EPIC-00 state → `CLOSED_DONE`
- [ ] M01 closure decision: Ready / Blocked

---

## Architectural Guidance: Dev Experience

### For EPIC-08 TASK-08-02 Team Reading FSM ADR

**Clear guidance needed:**

1. **SQLite BEGIN IMMEDIATE pattern** — code snippet provided
2. **Deadlock prevention** — lock ordering documented
3. **Test scenarios** — concurrent writes, race conditions
4. **Error handling** — how to handle SQLite "database is locked"
5. **Monitoring** — what metrics to track

**Bad outcomes if missing:**

- "I don't know which BEGIN variant to use"
- "Race condition not caught in testing"
- "Production: 'database is locked' errors under load"

---

## Code Quality Standards (for Documentation)

### EPIC-00 Documentation Should

- ✅ **Clarity**: Each task owner should understand their deliverable in 5 minutes
- ✅ **Completeness**: Acceptance criteria are testable, not vague
- ✅ **Actionability**: Each task has a "next step" (not "figure it out")
- ✅ **Ownership**: Clear owner + deadline, no ambiguous responsibilities
- ✅ **Architecture**: FSM, SQLite, RBAC decisions are documented (not assumed)

---

## Risks After Remediation

| Risk | Mitigation |
|:-----|:-----------|
| TASK-00-03B delays M02 start | Defer task breakdown to M02 sprint planning if 2026-03-07 timeline slips |
| FSM ADR feedback loop (teams disagree) | Pre-review with EPIC-08 + EPIC-10 leads (today) |
| EPIC-09 folder creation blocked on TASK-00-02 | Create skeleton today, TASK-00-02 just updates it |
| Multiple Architects/Reviewers confusion | Single Architect sign-off owner (explicitly named in state.md) |

---

**Status:** 🟡 **READY FOR TECH LEAD + ARCHITECT REVIEW**
**Next Step:** Architect approves remediation plan → execute Phase 1 actions
**Critical Path:** Issue #1 (timeline) + Issue #3 (FSM ADR detail) → EPIC-08 implementation unblocked
