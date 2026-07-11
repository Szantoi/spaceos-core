# M01 — Epics Reference (Consolidated)
## milestone_01 | Status: ✅ CLOSED_DONE | Completed: 2026-03-06

Ez a fájl tartalmaz minden M01 épic adatát (goal + state). Az eredeti epic_* mappák helyett egyetlen referencia dokumentum.

---

## EPIC-00: M01 Architect Coordination & Audit Actions

**FSM State:** `CLOSED_DONE` | **Completed:** 2026-03-05

### Objective
5 kritikus Architect-azonosított akció koordinálása és végrehajtása az M01 lezárási minőségének és az M02 felkészültségének biztosítására. Ez az épic az **project orchestration hub** az Architect Review auditban feltárt architekturális rések megoldásához.

### Task Map

| ID | Title | Status | Owner | Effort |
|:---|:------|:--------|:------|:-------|
| TASK-00-01 | Clarify EPIC-08 Checkpoint Responsibility | ✅ Done | Architect + EPIC-08 TL | 2h |
| TASK-00-02 | Add Formal EPIC-09 Dependency on EPIC-08 | ✅ Done | Architect + EPIC-09/10 | 3h |
| TASK-00-03A | Quick AC Lock (EPIC-09–12 high-level) | ✅ Done | Architect + Tech Lead | 4h |
| TASK-00-03B | Detailed Task Breakdown & Estimates | ✅ Done | Architect + Devs | 4h |
| TASK-00-04 | Draft FSM Security & Concurrency ADR | ✅ Done | Architect | 3h |
| TASK-00-05 | Add EPIC-02 Implementation Summary | ✅ Done | Backend Dev (EPIC-02) | 3h |

### Key Decisions
- ✅ EPIC-08 scope lock: checkpoint tool → EPIC-12 (M02), NOT M01
- ✅ EPIC-09 formal dependency on EPIC-08 dokumentálva
- ✅ FSM Security & Concurrency ADR: SQLite `BEGIN IMMEDIATE` + pessimistic locking pattern
- ✅ M02 EPIC-09–12 high-level AC locked (Phase 1 complete 2026-03-06)

### Acceptance Criteria (All Met)
1. ✅ EPIC-08 goal.md frissítve checkpoint scope-val (Option B: defer to EPIC-12)
2. ✅ EPIC-09 state.md has "Dependencies" section
3. ✅ EPIC-09–12 state.md files have high-level AC
4. ✅ FSM Security ADR reviewed by dev team
5. ✅ EPIC-02 implementation summary created

---

## EPIC-01: RBAC Schema Update & Server Root Cleanup

**FSM State:** `CLOSED_DONE` | **Completed:** 2026-03-05

### Objective
Minden agent schema fájl megkapja a hiányzó `mcp_tool_permissions` blokkot fail-closed elv alapján, valamint a szerver gyökérkönyvtára megtisztul a teszt fájloktól.

### Task Map

| ID | Title | Status | Description |
|:---|:------|:--------|:----------|
| T01-01 | RBAC schema YAML fixes | ✅ Completed | 14 db. yaml fájl frissítése |
| T01-02 | Server Root Cleanup | ✅ Completed | Felesleges logok, teszt json-ök törlése |
| T01-03 | E2E RBAC Validation | ✅ Completed | `mcp-rbac.test.ts` futtatása |

### Result
- ✅ 14 YAML fájl frissítve (fail-closed RBAC)
- ✅ Server root megtisztítva
- ✅ E2E RBAC tesztek zöldek

---

## EPIC-02: Dead Code Elimination & Static Analysis

**FSM State:** `CLOSED_DONE` | **Completed:** 2026-03-05

### Objective
Felesleges fájlok, metódusok és exportált (`unused`) interfészek megkeresése és eltávolítása a `src/agent-system/server/` könyvtárból statikus elemzők (`ts-prune`, `tsc`) segítségével.

### Task Map

| ID | Title | Status | Description |
|:---|:------|:--------|:----------|
| T02-01 | Statikus analízis lefuttatása és törlések | ✅ Completed | `ts-prune` és `tsc --noUnusedLocals` riport elemzése |

### Result
- ✅ 0 dead code azonosítva (`ts-prune` clean pass)
- ✅ `tsc --noUnusedLocals` — 0 warning
- ✅ Codebase clean

---

## EPIC-08: MCP Write Layer — Artifact Submit & Session Control

**FSM State:** `CLOSED_DONE` | **Completed:** 2026-03-05

### Objective
Az MCP server rendszer eddig **read-only** volt. Az EPIC-08 célja a **write capabilities** hozzáadása: artifact submission, FSM workflow state módosítás, session history.

### Task Map

| ID | Title | Status | Tests |
|:---|:------|:--------|:------|
| TASK-08-01 | SQLite Schema: sessions, artifacts, workflow_events, checkpoints | ✅ Completed | 10/10 AC |
| TASK-08-02 | MCP Tools: `submit_artifact()` + `update_workflow_state()` | ✅ Completed | 10/10 AC |
| TASK-08-03 | E2E Tests: agent mock → artifact submit → FSM verify | ✅ Completed | 9/9 tests |

**Total: 51/51 tests passing (100%)**

### Key Implementation

**MCP Tools (Active):**
- `submit_artifact(artifact_content, session_id, artifact_type)` — artifacts táblába
- `update_workflow_state(session_id, new_state, event)` — FSM state change + workflow_events

**SQLite Tables:**
- `sessions` — agent session-ök (agent_id, domain, role, started_at, fsm_state)
- `artifacts` — submitted content
- `workflow_events` — FSM event log

**FSM Transitions:**
- `started → in_progress → submitted → processed → closed`
- Invalid: backwards transitions, closed → anything

**RBAC:**
- ✅ `backend_developer`, `tech_lead` → write allowed
- ❌ `explorer`, `qa_tester` → read-only

**Deferred to EPIC-12 (M02):**
- `store_session_checkpoint()` tool
- Session recovery logic
- ChromaDB write-back

### Acceptance Criteria (All Met)
1. ✅ Session persistence: sessions tábla működik
2. ✅ Artifact submission: `submit_artifact()` → artifacts tábla
3. ✅ Workflow state: `update_workflow_state()` → FSM + workflow_events
4. ✅ RBAC: csak authorized role-ok írhatnak
5. ✅ Error handling: invalid session, permission denied, schema fail
6. ✅ E2E test: 9/9 passing (100%)
7. ✅ Documentation: tool descriptions, parameters, error codes

---

## EPIC-09: Write Layer Performance & Reliability Optimization

**FSM State:** `CLOSED_DONE` | **Completed:** 2026-03-06

### Objective
Az EPIC-08 audit 3 kritikus optimalizálási lehetőséget tárt fel. Megoldás M02 előtt, hogy a multi-agent workload 50%+ gyorsabban fusson.

**Audit Findings Addressed:**
1. Exponential Backoff WITHOUT Jitter — O(N²) mutex contention egyidejű terhelés alatt
2. CPU-Intensive Busy-Wait — Node.js event loop blokkolva 1–8 mp lock contention alatt
3. Lock Contention Blind Spot — nincs observability a retry behavior-hoz

### Task Map

| Task ID | Title | Status | Result |
|---------|-------|--------|--------|
| TASK-09-01 | Exponential Backoff with Jitter | ✅ COMPLETE | 24/24 tests ✅ |
| TASK-09-02 | Async/Await Evaluation | ⏳ DEFERRED TO M02 | Design TBD |
| TASK-09-03 | Lock Contention Metrics | ✅ COMPLETE | Logging ✅ |
| TASK-09-04 | Load Testing Framework | ✅ FOUNDATION LAID | M02 completion |

**Total: 115/115 tests passing | QA signed off**

### Key Implementation

**TASK-09-01 — Equal Jitter Pattern (AWS Best Practice):**
```
cap/2 + random(0, cap/2)
```
Applied to: `submitArtifactWithLocking()`, `updateWorkflowStateWithLocking()`

**TASK-09-03 — Lock Contention Metrics:**
- Counters: `lock_contention_count`, `max_retry_delay_ms`, `total_retry_time_ms`
- Env var: `WRITE_LAYER_METRICS=true/false`
- Prometheus-ready format

**TASK-09-04 — Load Testing:**
- 50+ concurrent agent scenario
- Baseline: pre-jitter vs post-jitter comparison documented

**TASK-09-02 — Async/Await:**
- Deferred to M02 (architectural decision: MCP SDK review needed)
- Spike plan created for M02 sprint

### Performance Results
- ✅ Jitter: >10% latency improvement under synthetic high-contention
- ✅ Lock contention visibility: INFO level logging
- ✅ Load test: 50-agent scenario documented and runnable
- ✅ No regressions: 115/115 tests passing

### Deferred to M02
- Full async/await refactor (EPIC-10 or later)
- Circuit breaker pattern
- Prometheus integration

---

## M01 Summary Statistics

| Metric | Value |
|--------|-------|
| EPICs Completed | 5 (EPIC-00, 01, 02, 08, 09) |
| Total Tests | 247/251 passing |
| EPIC-08 Tests | 51/51 (100%) |
| EPIC-09 Tests | 115/115 (100%) |
| Code Coverage | ≥80% |
| Deferred Items | 3 (all to EPIC-12/M02) |
| Milestone Timeline | 2026-02-27 → 2026-03-06 |
| Architect Sign-Off | ✅ APPROVED |
| QA Sign-Off | ✅ APPROVED |

## M01 Key Decisions (Locked)

1. **FSM Architecture**: SQLite `BEGIN IMMEDIATE` + pessimistic locking (ADR approved)
2. **EPIC-08 Scope**: 2 tools (submit_artifact, update_workflow_state), 3 tables — checkpoint → EPIC-12
3. **EPIC-09 Optimization**: Equal Jitter (AWS pattern) + metrics — async/await deferred to M02
4. **RBAC Pattern**: fail-closed, per-tool permissions in YAML schema files
5. **Write Layer Separation**: read (RAG/ChromaDB) vs write (SQLite tools) fully separated

## Source Files

| File | Location |
|------|----------|
| EPIC-08 implementation-summary | `implementation-summary/` (deleted with epic folders) |
| EPIC-09 implementation-summary | `implementation-summary/` (deleted with epic folders) |
| M01 Completion Report | `M01_COMPLETION_REPORT.md` |
| M01 Consolidated Summary | `M01_CONSOLIDATED_SUMMARY.md` |
| Architect Sign-Off | `../../../ARCHITECT_REVIEW_SIGN_OFF_v1.md` |
| FSM ADR | `database/joinerytech-flow/discovery/02-fsm-security-concurrency-draft.md` |
