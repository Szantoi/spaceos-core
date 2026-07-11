# SpaceOS Marvin — Agent Infrastructure

> **Phase 2:** Planning Pipeline ✅ COMPLETE
> **Phase 3.0:** Reviewer + Nightwatch Prep ✅ COMPLETE
> **Phase 3.1:** Full Implementation ⏸️ BLOCKED (OPENAI_API_KEY)
> **Python:** 3.13.5 + venv
> **Status:** Production ready (Knowledge Service), staging (Planning + Nightwatch)

## Setup

### 1. Virtual Environment

```bash
cd /opt/spaceos/spaceos-nexus/marvin
source venv/bin/activate
```

### 2. Environment Variables

Copy `.env.example` to `.env` and configure:

```bash
cp .env.example .env
nano .env  # Add your OPENAI_API_KEY
```

Required:
- `OPENAI_API_KEY` - OpenAI API key for Marvin AI functions

Optional:
- `ANTHROPIC_API_KEY` - Claude backend (if supported by Marvin)

### 3. Dependencies

Already installed in venv:
- marvin==3.2.7
- All required dependencies

## Planning Tasks (MSG-NEXUS-012)

### Files

- **planning_functions.py** — Original Phase 2 prototype functions
- **planning_tasks.py** — New Marvin Tasks (scan, select, debate)
- **run_scan_task.sh** — Wrapper for scan_for_ideas()
- **run_select_task.sh** — Wrapper for select_best_ideas()
- **run_debate_task.sh** — Wrapper for parallel debate

### Task 1: scan_for_ideas()

**Replaces:** plan-scan.sh (Haiku scanner)

```python
from planning_tasks import scan_for_ideas

ideas = await scan_for_ideas(
    segment_name="fe-memory",
    segment_content="<segment text>",
    domain_focus="Frontend UX",
    recent_ideas="<recent ideas>"
)
```

**Bash Wrapper:**
```bash
./run_scan_task.sh fe-memory /path/to/segment.md
```

**Features:**
- Knowledge Service integration (auto context retrieval)
- Extracts 1-3 high-quality ideas per segment
- Priority assignment (critical/high/medium/low)
- Confidence scoring (0.0-1.0)

### Task 2: select_best_ideas()

**Replaces:** plan-select.sh (Sonnet selector + WebSearch)

```python
from planning_tasks import select_best_ideas

selected = await select_best_ideas(
    ideas=ideas,
    domain_focus="Frontend UX",
    top_n=5
)
```

**Bash Wrapper:**
```bash
./run_select_task.sh /path/to/ideas_dir 5
```

**Features:**
- Web research validation (planned)
- Feasibility scoring
- Prioritization + ranking
- Top-N selection

### Task 3: debate_idea() + Parallel Execution

**Replaces:** plan-debate.sh (2x Sonnet parallel + consensus)

```python
from planning_tasks import run_parallel_debate

consensus = await run_parallel_debate(
    idea=idea,
    codebase_status="Sprint 6 complete",
    domain_focus="Frontend UX"
)
```

**Bash Wrapper:**
```bash
./run_debate_task.sh '<idea_json>'
```

**Features:**
- Planner-A (Pro perspective) + Planner-B (Con perspective)
- Parallel execution with asyncio
- Consensus synthesis
- Go/No-go/Modify recommendation

## Usage

### Python API

```python
import asyncio
from planning_tasks import scan_for_ideas, select_best_ideas, run_parallel_debate

async def main():
    # 1. Scan segment
    ideas = await scan_for_ideas(
        segment_name="fe-memory",
        segment_content=open("segment.md").read(),
        domain_focus="Frontend improvements"
    )

    # 2. Select top ideas
    selected = await select_best_ideas(ideas, top_n=3)

    # 3. Debate best idea
    if selected:
        consensus = await run_parallel_debate(
            idea=selected[0].idea,
            codebase_status="Sprint status...",
            domain_focus="Frontend improvements"
        )
        print(f"Recommendation: {consensus.recommendation}")

asyncio.run(main())
```

### Bash Wrappers

```bash
# 1. Scan a segment
./run_scan_task.sh fe-memory docs/MEMORY.md > ideas.json

# 2. Select best ideas
./run_select_task.sh docs/planning/ideas 5 > selected.json

# 3. Debate an idea
./run_debate_task.sh "$(cat idea.json)" > consensus.json
```

## Integration Points

1. **Knowledge Service**: POST localhost:3456/api/knowledge/search
2. **Planning Scripts**: plan-scan.sh → Marvin functions
3. **Architect Inbox**: Auto-delivery of consensus

## Testing

### Test Suite (Phase 3.0)

```bash
# Activate venv
source venv/bin/activate

# Run all tests
pytest

# Run with verbose output
pytest -v

# Run specific test file
pytest tests/test_workflow_state_tracker.py -v

# Run unit tests only
pytest -m unit

# Check for deprecation warnings
pytest -W error::DeprecationWarning
```

**Test Statistics:**
- Total: 108 tests (86 Phase 3.0 + 22 Phase 2 coverage)
- Pass rate: 100% ✅
- Runtime: ~2.6s
- Warnings: 0 ✅
- Skipped: 5 tests (Marvin extract requires API key)
- Coverage: Phase 3.0 complete, Phase 2 planning tasks

**Test Files:**
- `test_workflow_state_tracker.py` — 15 tests (SQLite state tracker)
- `test_config_loading.py` — 14 tests (YAML config parsing)
- `test_utils.py` — 37 tests (frontmatter, verdict extraction)
- `test_nightwatch_scheduler.py` — 20 tests (nightwatch mock tests)
- `test_planning_tasks.py` — 22 tests NEW (planning pipeline: select, debate, consensus)

## MSG-NEXUS-015: 9-Segment Configuration (2026-06-18)

### Configuration File

**File:** `config.yaml`

```yaml
planning:
  interval_seconds: 1800  # 30 minutes
  segments:
    - kernel-memory
    - orch-memory
    - fe-memory
    - joinery-memory
    - cutting-memory
    - infra-memory
    - sales-memory
    - identity-memory
    - abstractions-memory
```

### submitArtifact Tool

**Function:** `submit_artifact(content, artifact_type, metadata)`

```python
from planning_scheduler import submit_artifact

# Submit idea
submit_artifact(
    content="# Idea content...",
    artifact_type="idea",
    metadata={"title": "Example Idea", "segment": "fe-memory"}
)

# Submit consensus
submit_artifact(
    content="# Consensus content...",
    artifact_type="consensus",
    metadata={"title": "Consensus Title"}
)
```

### Systemd Service

**Interval:** 30 minutes (1800 seconds)

```bash
sudo cp spaceos-marvin-scheduler.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable spaceos-marvin-scheduler
sudo systemctl start spaceos-marvin-scheduler
```

**Monitor:**
```bash
sudo journalctl -u spaceos-marvin-scheduler -f
```

## Phase 3.0: Reviewer + Nightwatch (PREP COMPLETE)

> **Status:** All prep work complete, test suite clean, BLOCKED by OPENAI_API_KEY
> **Effort:** 6 sessions, ~115 minutes (2026-06-18)
> **Deliverables:** 3,224 lines (code + tests + docs), 86/86 tests passing, 0 warnings
> **Next:** Phase 3.1 implementation (8-10h when unblocked)

### Overview

Phase 3.0 prepares the migration of bash `reviewer.sh` and `nightwatch.sh` to Marvin-native Python:

```
nightwatch_scheduler.py (2 min interval)
  ├─ check_priority_sessions() → ROOT/CONDUCTOR health check
  ├─ check_done_messages() → dual_review() trigger
  ├─ check_stuck_sessions() → WorkflowStateTracker query
  └─ check_unread_inbox() → terminal session start
        ↓
workflow_state_tracker.py
  ├─ SQLite: logs/workflow_state.db
  ├─ SessionStatus FSM (STARTED → IN_PROGRESS → STUCK/DONE)
  └─ Activity monitoring
        ↓
reviewer_task.py (Phase 2 skeleton)
  ├─ dual_review() → ConsensusResult
  └─ APPROVE/REJECT verdict
```

### Components

#### 1. WorkflowStateTracker

**File:** `workflow_state_tracker.py`
**Status:** ✅ Tested and operational

Session state management with SQLite:

```python
from workflow_state_tracker import WorkflowStateTracker, SessionStatus

tracker = WorkflowStateTracker()

# Record session start
tracker.session_started("fe", "MSG-FE-042", "/path/to/inbox.md")

# Update activity
tracker.update_activity("fe")

# Get stuck sessions
stuck = tracker.get_stuck_sessions(threshold_minutes=10)

# Get session state
state = tracker.get_session("fe")
print(f"Status: {state.status.value}")
```

**Features:**
- SQLite database (`logs/workflow_state.db`)
- Session lifecycle tracking (6 states)
- Stuck detection with configurable threshold
- Activity monitoring

**Database Schema:**
```sql
CREATE TABLE session_state (
    session_id TEXT PRIMARY KEY,      -- e.g., "spaceos-fe"
    terminal TEXT NOT NULL,
    status TEXT NOT NULL,              -- SessionStatus enum
    started_at TIMESTAMP NOT NULL,
    last_activity TIMESTAMP NOT NULL,
    inbox_message_id TEXT,
    done_message_path TEXT,
    stuck_count INTEGER DEFAULT 0
);
```

#### 2. Nightwatch Scheduler

**File:** `nightwatch_scheduler.py`
**Status:** ✅ Skeleton complete (TODO markers for API key integration)

Async session monitoring daemon:

```bash
# Single cycle (testing)
python nightwatch_scheduler.py

# Daemon mode (default 2 min interval)
python nightwatch_scheduler.py run

# Custom interval (5 min)
python nightwatch_scheduler.py run 300
```

**Tasks:**
- **check_priority_sessions()** — Ensures ROOT/CONDUCTOR always running
- **check_done_messages()** — Scans outbox for UNREAD DONE → triggers dual_review()
- **check_stuck_sessions()** — Queries WorkflowStateTracker → sends Enter nudge
- **check_unread_inbox()** — Detects UNREAD inbox → starts terminal session

**Configuration:** `nightwatch-config.yaml`

```yaml
nightwatch:
  interval_seconds: 120          # 2 minutes
  priority_terminals: [root, conductor]

stuck_detection:
  threshold_minutes: 10
  nudge_retry_delay: 300         # 5 minutes
```

#### 3. Reviewer Configuration

**File:** `reviewer-config.yaml`
**Status:** ✅ Ready for Phase 3.1

Dual review configuration:

```yaml
reviewer:
  model_a: haiku
  model_b: haiku
  parallel: true
  require_both: true

verdict:
  approve_keywords: [APPROVE, APPROVED, "✅"]
  reject_keywords: [REJECT, REJECTED, "❌"]
  fallback_strategy: "reject"

marvin:
  temperature: 0.1
  max_tokens: 2000
```

**Reject Inbox Template:**
```yaml
reject_inbox:
  priority: high
  model_fallback: sonnet
  template: |
    ---
    id: {original_id}-REJECT
    from: reviewer
    to: {terminal}
    type: task
    priority: {priority}
    status: UNREAD
    ---

    # Review Failed — REJECTED

    {rejection_reason}
```

### Testing

**WorkflowStateTracker:**
```bash
source venv/bin/activate
python workflow_state_tracker.py
```

**Output:**
```
✅ Session started: fe
✅ Activity updated: fe
Stuck sessions: 1
Session state: IN_PROGRESS
```

### Known Issues

1. **Python 3.12 datetime adapter deprecation** ✅ FIXED (Session 6)
   - Status: Resolved with explicit ISO format conversion
   - All 69 warnings eliminated
   - Test suite: 86/86 passing, 0 warnings

2. **OPENAI_API_KEY blocking**
   - All Marvin Agent.run() calls are TODO-marked
   - Cannot test dual_review() without API key
   - Phase 3.1 blocked until VPS Operator configures API key

### Next Steps (Phase 3.1)

**When OPENAI_API_KEY ready:**
1. Remove TODO markers from nightwatch_scheduler.py
2. Complete reviewer_task.py (verdict parsing, inbox generation)
3. RbacFilter integration (permission checks)
4. E2E testing (8-10 hours)
5. Parallel validation (1 week)

**Documentation:**
- PHASE3_PREP_SUMMARY.md — Full phase 3.0 summary
- PHASE3_PLAN.md — Implementation roadmap

---

## References

- Marvin Docs: https://www.askmarvin.ai/
- ROADMAP: /opt/spaceos/docs/agent-infrastructure/ROADMAP.md
- Phase 2: MSG-NEXUS-011, MSG-NEXUS-012, MSG-NEXUS-013, MSG-NEXUS-015
- Phase 3.0: PHASE3_PREP_SUMMARY.md (2026-06-18)
