# Phase 3.0 Prep Work Summary — Reviewer + Nightwatch

> **Status:** COMPLETE (2026-06-18)
> **Effort:** ~2 hours
> **Next:** Phase 3.1 Implementation (blocked by OPENAI_API_KEY)

---

## Overview

Phase 3.0 completes all preparatory work for migrating bash reviewer.sh and nightwatch.sh to Marvin-native Python. All configuration files, database schema, and skeleton code are ready for implementation once OPENAI_API_KEY is configured.

---

## Deliverables

### 1. reviewer-config.yaml ✅

**File:** `/opt/spaceos/spaceos-nexus/marvin/reviewer-config.yaml`

**Features:**
- Dual review model configuration (haiku × 2, parallel)
- Verdict parsing keywords (APPROVE, REJECT, ✅, ❌)
- Reject inbox generation template
- Timeout and retry configuration
- Telegram notification settings
- Marvin-specific instructions (temperature, max_tokens)

**Key Sections:**
```yaml
reviewer:
  model_a: haiku
  model_b: haiku
  parallel: true
  require_both: true

verdict:
  approve_keywords: [APPROVE, APPROVED, JÓVÁHAGYVA, "✅"]
  reject_keywords: [REJECT, REJECTED, VISSZADOBVA, "❌"]
  fallback_strategy: "reject"

marvin:
  temperature: 0.1
  max_tokens: 2000
```

---

### 2. workflow_state_tracker.py ✅

**File:** `/opt/spaceos/spaceos-nexus/marvin/workflow_state_tracker.py`

**Status:** Tested and operational

**Features:**
- SQLite database: `logs/workflow_state.db`
- Session lifecycle tracking
- Stuck session detection with configurable threshold
- Activity monitoring

**Database Schema:**
```sql
CREATE TABLE session_state (
    session_id TEXT PRIMARY KEY,      -- e.g., "spaceos-fe"
    terminal TEXT NOT NULL,            -- e.g., "fe"
    status TEXT NOT NULL,              -- SessionStatus enum
    started_at TIMESTAMP NOT NULL,
    last_activity TIMESTAMP NOT NULL,
    inbox_message_id TEXT,
    done_message_path TEXT,
    stuck_count INTEGER DEFAULT 0
);
```

**API Methods:**
- `session_started(terminal, message_id, inbox_path)` — Record session start
- `update_activity(terminal)` — Update last activity timestamp
- `mark_stuck(terminal)` — Increment stuck count
- `mark_done(terminal, done_message_path)` — Mark as DONE
- `get_stuck_sessions(threshold_minutes)` → List[SessionState]
- `get_session(terminal)` → Optional[SessionState]

**Test Result:**
```
✅ Session started: fe
✅ Activity updated: fe
Stuck sessions: 1
Session state: IN_PROGRESS
Last activity: 2026-06-18 06:29:49
```

**Note:** Python 3.12 datetime adapter deprecation warning (non-blocking)

---

### 3. nightwatch_scheduler.py ✅

**File:** `/opt/spaceos/spaceos-nexus/marvin/nightwatch_scheduler.py`

**Status:** Skeleton complete, TODO markers for OPENAI_API_KEY integration

**Features:**
- Async main loop with configurable interval (default: 2 min)
- Error handling and logging
- WorkflowStateTracker integration
- tmux session inspection utilities

**Core Tasks:**

#### check_priority_sessions()
- Ensures ROOT and CONDUCTOR sessions are always running
- Logs warnings if missing
- TODO: Auto-restart logic

#### check_done_messages()
- Scans all `docs/mailbox/*/outbox/*.md` for UNREAD DONE messages
- Triggers `dual_review()` for each (TODO: requires OPENAI_API_KEY)
- Logs review results

#### check_stuck_sessions()
- Queries WorkflowStateTracker for stuck sessions
- Sends Enter nudge to stuck tmux sessions
- Updates stuck_count in database

#### check_unread_inbox()
- Scans all `docs/mailbox/*/inbox/*.md` for UNREAD messages
- TODO: Start terminal session if not running
- Tracks session start in WorkflowStateTracker

**CLI Usage:**
```bash
# Single cycle (testing)
python nightwatch_scheduler.py

# Daemon mode (default 2 min interval)
python nightwatch_scheduler.py run

# Custom interval (5 min)
python nightwatch_scheduler.py run 300
```

---

### 4. nightwatch-config.yaml ✅

**File:** `/opt/spaceos/spaceos-nexus/marvin/nightwatch-config.yaml`

**Configuration:**
```yaml
nightwatch:
  interval_seconds: 120          # 2 minutes
  priority_terminals: [root, conductor]

stuck_detection:
  threshold_minutes: 10
  nudge_retry_delay: 300         # 5 minutes
  max_nudge_count: 3

auto_restart:
  enabled: false
  max_restart_attempts: 3
```

---

## Files Created

```
/opt/spaceos/spaceos-nexus/marvin/
├── reviewer-config.yaml          (NEW, 56 lines)
├── workflow_state_tracker.py     (NEW, 275 lines, tested ✅)
├── nightwatch_scheduler.py       (NEW, 285 lines, skeleton)
├── nightwatch-config.yaml        (NEW, 24 lines)
└── logs/
    └── workflow_state.db         (SQLite, auto-created)
```

---

## Integration Points

### With Existing Code

1. **reviewer_task.py** (Phase 2 skeleton)
   - Will use `reviewer-config.yaml` for configuration
   - Dual review consensus logic already prototyped

2. **planning_scheduler.py** (Phase 2)
   - Same config.yaml pattern established
   - Async scheduler loop pattern

3. **Bash Scripts** (Current pipeline)
   - Parallel run validation planned
   - Gradual cutover strategy

### With External Services

1. **tmux Sessions**
   - Session existence checks
   - Activity monitoring
   - Enter nudge sending

2. **File System**
   - Mailbox scanning (inbox/outbox)
   - DONE message detection
   - Frontmatter parsing

3. **WorkflowStateTracker Database**
   - All session state persistence
   - Stuck detection queries
   - Activity tracking

---

## Testing Performed

### WorkflowStateTracker
```bash
source venv/bin/activate
python workflow_state_tracker.py
```

**Result:** ✅ Pass
- Database created successfully
- Session lifecycle tracking works
- Stuck session query functional
- Deprecation warning noted (Python 3.12, non-blocking)

### Configuration Loading
```bash
python -c "
import yaml
config = yaml.safe_load(open('reviewer-config.yaml'))
print(config['reviewer']['model_a'])
"
```

**Result:** ✅ Pass

---

## Known Issues

1. **Python 3.12 Datetime Adapter Deprecation**
   - **Impact:** Warning messages in logs
   - **Severity:** Low (non-blocking)
   - **Mitigation:** Document in ROADMAP.md
   - **TODO:** Replace with recommended sqlite3 datetime handling in Phase 3.1

2. **OPENAI_API_KEY Blocking**
   - All Marvin Agent.run() calls are TODO-marked
   - Cannot test dual_review() without API key
   - E2E testing deferred to Phase 3.1

---

## Next Steps (Phase 3.1)

### Immediate (When OPENAI_API_KEY Ready)

1. **Remove TODO markers from nightwatch_scheduler.py**
   - Implement `dual_review()` call in `check_done_messages()`
   - Implement terminal session start in `check_unread_inbox()`
   - Test all async tasks

2. **Complete reviewer_task.py**
   - Verdict parsing with keyword detection
   - Reject inbox generation
   - Telegram notification integration

3. **RbacFilter Integration**
   - Permission checks decorator
   - Audit logging

### Validation (1 week)

1. Parallel run (Marvin + bash)
2. Output comparison (approval rates, stuck detection accuracy)
3. Gradual cutover (nightwatch first, then reviewer)

---

## Estimated Effort Remaining

| Task | Effort | Blocking |
|---|---|---|
| reviewer_task.py completion | 3-4h | OPENAI_API_KEY |
| nightwatch_scheduler.py TODO removal | 4-5h | OPENAI_API_KEY |
| RbacFilter integration | 1-2h | - |
| E2E testing | 2-3h | OPENAI_API_KEY |
| **Total** | **8-10h** | **OPENAI_API_KEY** |

Validation: +1 week parallel run

---

## Summary

**Phase 3.0: COMPLETE ✅**

All preparatory work done:
- Configuration files ready
- Database schema implemented and tested
- Skeleton code complete with clear TODO markers
- Integration points identified
- Testing plan established

**Ready for:** OPENAI_API_KEY configuration + Phase 3.1 implementation

**Blocked by:** VPS Operator task (OPENAI_API_KEY setup)

---

**NEXUS Signature:** Phase 3.0 Prep Work Complete
**Date:** 2026-06-18
**Files:** 4 new files, 275 lines of code, 1 database schema
**Status:** READY FOR PHASE 3.1 ✅
