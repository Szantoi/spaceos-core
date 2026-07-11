# Phase 3 Implementation Plan — Reviewer + Nightwatch

> **Status:** PLANNING (MSG-NEXUS-014)
> **Estimated Effort:** 3-5 sessions
> **Dependencies:** OPENAI_API_KEY, Phase 2 validated

## Overview

Phase 3 migrates the final bash dispatcher components to Marvin:
- `reviewer.sh` (335 lines) → Marvin dual review Task
- `nightwatch.sh` + watch-*.sh (4 scripts) → Marvin Scheduler

## Current Bash Architecture

```
nightwatch.sh (cron */2)
  ├─ watch-priority.sh → Root + Conductor always running
  ├─ watch-done.sh → DONE detection → reviewer.sh
  ├─ watch-stuck.sh → Stuck session → Enter nudge
  └─ watch-inbox.sh → UNREAD inbox → session start

reviewer.sh
  ├─ Reviewer-A (Haiku, parallel)
  ├─ Reviewer-B (Haiku, parallel)
  └─ Consensus: both APPROVE required
```

## Target Marvin Architecture

```
nightwatch_scheduler.py (systemd)
  ├─ check_priority_sessions() — Every 2 min
  ├─ check_done_messages() → dual_review()
  ├─ check_stuck_sessions() → nudge_session()
  └─ check_unread_inbox() → start_terminal_session()

reviewer_task.py
  ├─ review_done_message(Reviewer-A)
  ├─ review_done_message(Reviewer-B)
  └─ dual_review() → ConsensusResult
```

## Implementation Tasks

### Task 1: Reviewer Task (3-4 hours)

**File:** `reviewer_task.py` (skeleton created)

**Subtasks:**
1. ✅ Data models (ReviewResult, ConsensusResult, Verdict enum)
2. ✅ review_done_message() skeleton
3. ✅ dual_review() with asyncio.gather()
4. ⏳ reviewer-config.yaml integration
5. ⏳ Verdict keyword parsing (APPROVE/REJECT detection)
6. ⏳ Rejection inbox creation logic
7. ⏳ Telegram notification integration

**Complexity:**
- Config file parsing: Medium
- Verdict keyword extraction: High (regex patterns, ambiguous cases)
- Consensus logic: Medium (already prototyped)

**Dependencies:**
- OPENAI_API_KEY for Agent.run()
- reviewer-config.yaml structure
- Access to telegram.conf for notifications

### Task 2: Nightwatch Scheduler (4-5 hours)

**File:** `nightwatch_scheduler.py` (not created yet)

**Subtasks:**
1. ⏳ check_done_messages() — Scan outbox directories
2. ⏳ check_stuck_sessions() — tmux session state inspection
3. ⏳ check_unread_inbox() — Inbox UNREAD detection
4. ⏳ check_priority_sessions() — Root + Conductor health check
5. ⏳ Main scheduler loop (2-minute interval)
6. ⏳ Systemd service integration

**Complexity:**
- Outbox scanning: Low (glob patterns)
- Stuck detection: High (tmux session state parsing)
- Priority session management: Medium (tmux existence + activity checks)

**Dependencies:**
- tmux session inspection commands
- Access to docs/mailbox/**/outbox/ directories
- Telegram notification integration

### Task 3: WorkflowStateTracker Integration (2-3 hours)

**New Component:** Session state tracking database

**Features:**
- Track terminal session lifecycle (STARTED, IN_PROGRESS, STUCK, DONE)
- Timestamp tracking for stuck detection
- Activity monitoring (last message timestamp)

**Implementation:**
- SQLite database: `logs/workflow_state.db`
- State transitions: STARTED → IN_PROGRESS → DONE/STUCK
- Query API: `get_stuck_sessions(threshold_minutes=10)`

**Tables:**
```sql
CREATE TABLE session_state (
    session_id TEXT PRIMARY KEY,
    terminal TEXT,
    status TEXT,  -- STARTED, IN_PROGRESS, STUCK, DONE
    started_at TIMESTAMP,
    last_activity TIMESTAMP,
    inbox_message_id TEXT
);
```

### Task 4: RbacFilter Integration (1-2 hours)

**Purpose:** Permission checks in Marvin Agent context

**Use Cases:**
1. Reviewer can only approve/reject, not modify code
2. Planning agents can only suggest, not implement
3. Terminal-specific access control

**Implementation:**
- Decorator: `@rbac_check(permission="review:done")`
- Context injection: Agent instructions include RBAC rules
- Audit log: Track permission violations

**Example:**
```python
@rbac_check(permission="review:done")
async def dual_review(done_file_path: str) -> ConsensusResult:
    # RBAC check: Can this agent review DONE messages?
    ...
```

## Testing Strategy

### Unit Tests
- `test_reviewer_task.py` — ReviewResult, ConsensusResult, verdict parsing
- `test_nightwatch_scheduler.py` — DONE detection, stuck detection
- `test_workflow_state_tracker.py` — State transitions, queries

### Integration Tests
- E2E: DONE message → dual review → APPROVE/REJECT
- E2E: Stuck session detection → nudge
- E2E: UNREAD inbox → terminal session start

### Validation Period
1. Run Marvin + bash in parallel (1 week)
2. Compare outputs (approval rates, stuck detection accuracy)
3. Gradual cutover: nightwatch first, then reviewer

## Risks and Mitigation

### Risk 1: Verdict Parsing Ambiguity
**Problem:** Marvin Agent output may not clearly indicate APPROVE/REJECT

**Mitigation:**
- Structured output with `extract()` targeting Verdict enum
- Fallback: keyword detection (APPROVE, REJECT, ✅, ❌)
- Manual review for UNCLEAR verdicts

### Risk 2: Stuck Detection False Positives
**Problem:** Session may be long-running but not stuck

**Mitigation:**
- Activity tracking (last message timestamp)
- Threshold tuning (10 min → 15 min)
- Manual confirmation before nudge

### Risk 3: API Cost Increase
**Problem:** Every DONE message → 2 Haiku API calls

**Mitigation:**
- Cost monitoring dashboard
- Budget limits (max N reviews per day)
- Fallback to bash reviewer if budget exceeded

## Migration Path

### Phase 3.1 — Reviewer Only (1 week)
1. Implement reviewer_task.py fully
2. Test with OPENAI_API_KEY
3. Run in parallel with bash reviewer
4. Validate approval rates match

### Phase 3.2 — Nightwatch Core (1 week)
1. Implement nightwatch_scheduler.py
2. Test DONE detection + reviewer triggering
3. Run in parallel with bash nightwatch
4. Validate no missed DONE messages

### Phase 3.3 — WorkflowStateTracker (3 days)
1. Implement SQLite state tracker
2. Integrate with nightwatch
3. Test stuck detection accuracy

### Phase 3.4 — Full Cutover (1 week)
1. Disable bash reviewer cron
2. Disable bash nightwatch cron
3. Enable Marvin systemd services
4. Monitor for 1 week

## Success Criteria

- [ ] All DONE messages reviewed (no missed reviews)
- [ ] Approval rate within ±5% of bash reviewer
- [ ] Stuck session detection accuracy >95%
- [ ] API cost <$5/day for typical load
- [ ] No manual intervention required for 1 week

## Estimated Timeline

| Phase | Effort | Duration |
|---|---|---|
| 3.1 Reviewer | 3-4 hours | 1-2 days |
| 3.2 Nightwatch Core | 4-5 hours | 2-3 days |
| 3.3 WorkflowStateTracker | 2-3 hours | 1 day |
| 3.4 Full Cutover | - | 1 week validation |
| **Total** | **10-12 hours** | **2-3 weeks** |

## Next Steps (After OPENAI_API_KEY Config)

1. Complete reviewer_task.py implementation
2. Test dual review with real DONE messages
3. Implement nightwatch_scheduler.py
4. Create WorkflowStateTracker database
5. Parallel validation period
6. Full cutover

---

**Status:** Planning complete, skeleton code created
**Blocked By:** OPENAI_API_KEY configuration
**Ready For:** Sprint planning and implementation
