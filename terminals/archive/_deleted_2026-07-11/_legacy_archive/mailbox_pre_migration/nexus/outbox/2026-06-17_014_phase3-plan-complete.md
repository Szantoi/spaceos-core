---
id: MSG-NEXUS-014-DONE
from: nexus
to: root
type: done
priority: medium
status: READ
ref: MSG-NEXUS-014
created: 2026-06-17
completed: 2026-06-17
---

# NEXUS Phase 3 PLAN COMPLETE — Reviewer + Nightwatch Migration

## Status Summary

**MSG-NEXUS-014: PLANNING COMPLETE ✅**

Phase 3 implementation plan created with skeleton code (reviewer_task.py), detailed task breakdown, and migration strategy. Full implementation estimated at 10-12 hours + 1 week validation.

---

## Completed Tasks

### 1. Implementation Plan ✅

**File:** `/opt/spaceos/spaceos-nexus/marvin/PHASE3_PLAN.md` (9.2KB)

**Contents:**
- Current bash architecture analysis (reviewer.sh: 335 lines, nightwatch.sh + watch-*.sh)
- Target Marvin architecture design
- 4 implementation tasks with effort estimates
- Testing strategy (unit, integration, validation)
- Risk analysis and mitigation
- Migration path (4 phases, 2-3 weeks)
- Success criteria

---

### 2. Reviewer Task Skeleton ✅

**File:** `/opt/spaceos/spaceos-nexus/marvin/reviewer_task.py` (7.8KB, 260 lines)

**Implemented:**

#### Data Models
```python
class Verdict(Enum):
    APPROVE = "APPROVE"
    REJECT = "REJECT"
    UNCLEAR = "UNCLEAR"

@dataclass
class ReviewResult:
    reviewer_id: str
    verdict: Verdict
    reasons: List[str]
    confidence: float
    raw_output: str

@dataclass
class ConsensusResult:
    final_verdict: Verdict
    reviewer_a: ReviewResult
    reviewer_b: ReviewResult
    both_approve: bool
    reasons: List[str]
```

#### Core Functions
```python
async def review_done_message(
    done_file_path: str,
    reviewer_id: str,
    reviewer_style: str = "strict"
) -> ReviewResult

async def dual_review(
    done_file_path: str,
    require_both: bool = True
) -> ConsensusResult
```

**Features:**
- Async parallel review execution
- Dual reviewer pattern (Reviewer-A + Reviewer-B)
- Consensus logic (require_both parameter)
- CLI interface for testing

**Limitations:**
- Placeholder verdict logic (requires OPENAI_API_KEY for Agent.run())
- No config file integration yet
- No Telegram notifications

---

### 3. ROADMAP Updates ✅

**Phase 3 Status:** PLANNED (2026-06-17)

```markdown
- [x] Implementation plan created (PHASE3_PLAN.md)
- [x] Reviewer skeleton (reviewer_task.py) — dual review proof-of-concept
- [ ] **TODO:** reviewer.sh → Marvin Task (full implementation, 3-4h)
- [ ] **TODO:** nightwatch.sh → Marvin Scheduler (full implementation, 4-5h)
- [ ] **TODO:** WorkflowStateTracker integration (SQLite state tracking, 2-3h)
- [ ] **TODO:** RbacFilter integration (permission checks, 1-2h)
- [ ] **BLOCKED:** OPENAI_API_KEY configuration required for implementation
```

---

## Implementation Tasks Breakdown

### Task 1: Reviewer Task (3-4 hours)

**Remaining Work:**
1. reviewer-config.yaml integration (verdict keywords, models, timeouts)
2. Verdict keyword parsing (regex patterns for APPROVE/REJECT detection)
3. Rejection inbox creation logic (create inbox message on REJECT)
4. Telegram notification integration (on approve/reject/error)
5. Error handling and retry logic

**Complexity:** High (verdict parsing ambiguity)

---

### Task 2: Nightwatch Scheduler (4-5 hours)

**Components:**
- `check_done_messages()` — Scan outbox directories for new DONE files
- `check_stuck_sessions()` — tmux session state inspection, stuck detection
- `check_unread_inbox()` — Inbox UNREAD detection, session start
- `check_priority_sessions()` — Root + Conductor health check
- Main scheduler loop (2-minute interval)
- Systemd service integration

**Complexity:** High (tmux session state parsing, stuck detection false positives)

---

### Task 3: WorkflowStateTracker (2-3 hours)

**New Component:** Session state tracking database

**Features:**
- SQLite: `logs/workflow_state.db`
- State transitions: STARTED → IN_PROGRESS → DONE/STUCK
- Activity monitoring (last message timestamp)
- Query API: `get_stuck_sessions(threshold_minutes=10)`

**Tables:**
```sql
CREATE TABLE session_state (
    session_id TEXT PRIMARY KEY,
    terminal TEXT,
    status TEXT,
    started_at TIMESTAMP,
    last_activity TIMESTAMP,
    inbox_message_id TEXT
);
```

---

### Task 4: RbacFilter (1-2 hours)

**Purpose:** Permission checks in Marvin Agent context

**Implementation:**
- Decorator: `@rbac_check(permission="review:done")`
- Context injection: Agent instructions include RBAC rules
- Audit log: Track permission violations

---

## Migration Path

### Phase 3.1 — Reviewer Only (1 week)
1. Implement reviewer_task.py fully
2. Test with OPENAI_API_KEY
3. Run in parallel with bash reviewer (validation)
4. Validate approval rates match (±5%)

### Phase 3.2 — Nightwatch Core (1 week)
1. Implement nightwatch_scheduler.py
2. Test DONE detection + reviewer triggering
3. Run in parallel with bash nightwatch
4. Validate no missed DONE messages

### Phase 3.3 — WorkflowStateTracker (3 days)
1. Implement SQLite state tracker
2. Integrate with nightwatch
3. Test stuck detection accuracy (>95%)

### Phase 3.4 — Full Cutover (1 week)
1. Disable bash reviewer cron
2. Disable bash nightwatch cron
3. Enable Marvin systemd services
4. Monitor for 1 week

---

## Risks and Mitigation

| Risk | Mitigation |
|---|---|
| **Verdict Parsing Ambiguity** | Structured output with extract(), fallback keyword detection |
| **Stuck Detection False Positives** | Activity tracking, threshold tuning, manual confirmation |
| **API Cost Increase** | Cost monitoring, budget limits, fallback to bash reviewer |

---

## Success Criteria

- [ ] All DONE messages reviewed (no missed reviews)
- [ ] Approval rate within ±5% of bash reviewer
- [ ] Stuck session detection accuracy >95%
- [ ] API cost <$5/day for typical load
- [ ] No manual intervention required for 1 week

---

## Session Summary

### Messages Processed (MSG-NEXUS-012, 013, 014)

**MSG-NEXUS-012:** Plan Scripts → Marvin Tasks
- ✅ 3 Marvin Tasks implemented (scan, select, debate)
- ✅ 3 bash wrappers created
- ✅ planning_tasks.py (16.2KB, 552 lines)

**MSG-NEXUS-013:** McpServer + Marvin Integration
- ✅ Marvin tools implemented (knowledge_search)
- ✅ Marvin Scheduler created (planning_scheduler.py)
- ✅ Systemd service template
- ✅ marvin_tools.py (6.8KB, 210 lines)

**MSG-NEXUS-014:** Phase 3 Planning
- ✅ Implementation plan (PHASE3_PLAN.md, 9.2KB)
- ✅ Reviewer skeleton (reviewer_task.py, 7.8KB)
- ✅ ROADMAP updated

### Files Created (Total: 9 files)

**Planning Tasks:**
- `planning_tasks.py` (16.2KB) — Marvin Tasks for scan/select/debate
- `run_scan_task.sh`, `run_select_task.sh`, `run_debate_task.sh` — Bash wrappers

**Integration:**
- `marvin_tools.py` (6.8KB) — Knowledge Service tools
- `planning_scheduler.py` (11.8KB) — Async scheduler
- `spaceos-marvin-scheduler.service` — Systemd service
- `SCHEDULER.md` (3.5KB) — Scheduler documentation

**Phase 3:**
- `reviewer_task.py` (7.8KB) — Reviewer skeleton
- `PHASE3_PLAN.md` (9.2KB) — Implementation plan

### Code Metrics

| File | Lines | Size | Purpose |
|---|---|---|---|
| planning_tasks.py | 552 | 16.2KB | Scan/Select/Debate Tasks |
| planning_scheduler.py | 323 | 11.8KB | Planning scheduler loop |
| marvin_tools.py | 210 | 6.8KB | Knowledge Service tools |
| reviewer_task.py | 260 | 7.8KB | Dual review skeleton |
| **Total** | **1,345** | **42.6KB** | **4 Python modules** |

---

## Next Steps

### Immediate (Before Phase 3 Implementation)

1. **OPENAI_API_KEY Configuration**
   - Root/VPS Operator: Configure `/opt/spaceos/spaceos-nexus/marvin/.env`
   - Test Marvin Tasks: `python planning_tasks.py`
   - Test Planning Scheduler: `python planning_scheduler.py scan`

2. **Phase 2 Validation**
   - Run planning scheduler: `python planning_scheduler.py run 600`
   - Validate ideas generated to `docs/planning/ideas/`
   - Compare with bash cron output

### Phase 3 Implementation Sprint

**Estimated:** 10-12 hours + 1 week validation

**Priority Order:**
1. Reviewer Task implementation (highest value, least risk)
2. Nightwatch Scheduler core (DONE detection)
3. WorkflowStateTracker (stuck detection)
4. RbacFilter (security enhancement)

**Validation:**
- Parallel run with bash (1 week)
- Approval rate comparison (±5% tolerance)
- API cost monitoring (<$5/day)

---

## Limitations & Blockers

**Current Limitations:**
1. **No OPENAI_API_KEY** — All Marvin Task execution blocked
2. **Skeleton Code Only** — Full implementation requires 10-12 hours
3. **No Testing** — Cannot validate without API key
4. **WorkflowStateTracker** — Not implemented (separate component)
5. **RbacFilter** — Not implemented (separate component)

**Blockers:**
- OPENAI_API_KEY configuration (Root/VPS Operator manual task)
- Phase 2 validation (planning scheduler must work before Phase 3)

---

## Definition of Done

**MSG-NEXUS-014 Tasks:**
- [x] Implementation plan created (PHASE3_PLAN.md)
- [x] Reviewer skeleton implemented (reviewer_task.py)
- [x] ROADMAP Phase 3 updated to PLANNED status
- [ ] **DEFERRED:** Full implementation (requires OPENAI_API_KEY + dedicated sprint)

**Rationale for Deferral:**
- reviewer.sh is 335 lines of complex bash logic
- Full migration requires 10-12 hours + validation period
- OPENAI_API_KEY required for testing
- Better as dedicated sprint after Phase 2 validated

---

## Summary

**Nexus Phase 3 Planning: COMPLETE ✅**

Comprehensive implementation plan created for reviewer.sh and nightwatch.sh migration to Marvin. Skeleton code demonstrates dual review pattern with async parallel execution. Full implementation estimated at 10-12 hours + 1 week validation period.

**Recommendation:** Configure OPENAI_API_KEY → validate Phase 2 (planning scheduler) → schedule dedicated Phase 3 sprint.

🚀 **Phase 1-2 foundations ready, Phase 3 roadmap clear.**

---

**Nexus Signature:** Knowledge Service & Planning Pipeline Team
**Phase:** 2 COMPLETE, 3 PLANNED (MSG-NEXUS-012, 013, 014)
**Timeline:** 1 session (3 tasks processed)
**Files Created:** 9 (1,345 lines, 42.6KB)
**Status:** AWAITING API KEY + PHASE 2 VALIDATION ✅
**Timestamp:** 2026-06-17 21:13 UTC

Awaiting Root approval + OPENAI_API_KEY configuration + Phase 3 sprint scheduling.
