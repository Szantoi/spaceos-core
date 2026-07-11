---
id: adr-02-fsm-security-concurrency
title: "ADR: FSM Security & Concurrency Protocol for Workflow State Machine"
type: adr
status: draft (awaiting EPIC-08 & EPIC-10 team feedback)
date: 2026-03-04
author: Architect
related_epic: EPIC-08, EPIC-10
related_adr: adr-01-write-layer-architecture
---

# ADR: FSM Security & Concurrency Protocol

## Context

The MCP server implements a state machine (FSM) for workflow sessions. Key operations:

1. **`submit_artifact(session_id, artifact_content, type)`** — Write artifact to DB
2. **`update_workflow_state(session_id, new_state, event, evidence)`** — Transition state
3. **`store_session_checkpoint(session_id, checkpoint_data)`** — M02's work, but schema needs planning now

**Key decisions needed:**

- **State Change Authorization:** Who is permitted to call `update_workflow_state()`?
- **Error Recovery:** Transaction guarantees if state change partially fails (DB error mid-operation)?
- **Concurrent Access:** Can two agents concurrently submit artifacts for the same session?

---

## Problem Statement

### 1. State Change Authorization

**Scenario A:** Agent A working on task T submits artifact → state moves to "SUBMITTED"
**Scenario B:** Same task T, Admin wants to force state back to "ABANDONED" (e.g., due to timeout)

**Questions:**

- Should only the original agent (Agent A) be able to call `update_workflow_state()`?
- Should admins/orchestrators have authority to override agent state?
- What if agent's role is revoked mid-task? (e.g., backend_developer → inactive)

### 2. Error Recovery & Transaction Safety

**Scenario:** `update_workflow_state(session_id="S1", new_state="PROCESSED")`

1. DB: Session.fsm_state = "PROCESSED" ✅
2. DB: Insert WorkflowEvent → **CONSTRAINT VIOLATION** (foreign key to artifacts) ❌
3. Result: **Inconsistent state** — (Session in PROCESSED, but no corresponding event in log)

**Questions:**

- Should this be an all-or-nothing transaction (Atomicity)?
- What does the system do on failure? (Rollback? Partial state stranded?)
- How does the agent recover? (Retry? Manual intervention?)

### 3. Concurrent Session Access

**Scenario A:** Agent X and Agent Y both working on task T (same session_id)

1. Agent X: `submit_artifact(session_id="S1", ...) → inserted artifact ID 101`
2. Agent Y: `submit_artifact(session_id="S1", ...) → inserted artifact ID 102`
3. Both agents: `update_workflow_state(session_id="S1", new_state="SUBMITTED")`
4. Result: **Race condition** — both agents assert "I submitted the work"

**Questions:**

- Should sessions be exclusive-access (locked to one agent)?
- Or allow multi-agent workspace (e.g., pair programming)?
- If multi-agent, how to avoid conflicting state transitions?

---

## Options

### Option A: Pessimistic Locking (Recommended for M01/M02)

**Implementation:**

```sql
ALTER TABLE sessions ADD COLUMN locked_by TEXT;
ALTER TABLE sessions ADD COLUMN locked_at TIMESTAMP;
```

**Workflow:**

```typescript
// Before update_workflow_state():
1. TRY: SELECT * FROM sessions WHERE id = ? FOR UPDATE (NOWAIT)
   (SQLite doesn't have FOR UPDATE, use PRAGMA BUSY_TIMEOUT + BEGIN EXCLUSIVE)
2. If lock acquired: Agent holds exclusive access
   - Update state + insert event in single transaction
   - Release lock
3. If lock fails (timeout): Return error; agent retries
```

**Pros:**

- ✅ Simple, deterministic locking
- ✅ Prevents concurrent state changes
- ✅ One agent = one session (clear ownership)
- ✅ Works well with SQLite (simpler than complex concurrency)

**Cons:**

- ❌ Sequential execution (slow if many concurrent submissions)
- ❌ Lock contention can cause failures

**Risk:** If agent dies with lock held, manual unlock required.
**Mitigation:** Implement lock timeout (e.g., 5 min) + "stale lock detection"

---

### Option B: Optimistic Concurrency (Future enhancement for M03)

**Implementation:**

```sql
ALTER TABLE sessions ADD COLUMN version INTEGER DEFAULT 1;
```

**Workflow:**

```typescript
// update_workflow_state():
1. Read current state + version
2. Compute new state locally
3. UPDATE sessions SET fsm_state = ?, version = version + 1
   WHERE id = ? AND version = ? (old version)
4. If 0 rows affected: Version mismatch → conflict
   - Retry or fail with "concurrent modification" error
```

**Pros:**

- ✅ Lock-free (higher concurrency)
- ✅ No deadlock risk
- ✅ Better for high-throughput systems

**Cons:**

- ❌ Retry logic complex (exponential backoff needed)
- ❌ Harder to debug (race conditions emerge in production)
- ❌ "Lost update" risk if retry logic wrong

---

### Option C: Event Sourcing (Long-term for M03+)

**Implementation:**

```sql
-- No state column; derive from events
CREATE TABLE workflow_events (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    session_id TEXT NOT NULL,
    event_type TEXT NOT NULL,  -- "started", "artifact_submitted", "test_passed", etc.
    timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    data JSON,
    UNIQUE(session_id, id)  -- Append-only log
);
```

**Workflow:**

```typescript
// Current state is computed by replaying events:
function getCurrentState(session_id) {
    const events = db.query("SELECT * FROM workflow_events WHERE session_id = ? ORDER BY timestamp");
    let state = "started";
    for (const event of events) {
        state = FSM_TRANSITIONS[state][event.type];
    }
    return state;
}
```

**Pros:**

- ✅ Complete audit trail (who did what, when)
- ✅ Natural session recovery (replay events)
- ✅ Conflict resolution via timestamps (last-write-wins)
- ✅ Scales to distributed systems (CQRS pattern)

**Cons:**

- ❌ Requires event schema coordination (all transitions predefined)
- ❌ State recomputation cost (scan all events per query)
- ❌ Larger DB footprint (every change is a new record)
- ❌ Needs compensating events for rollback

---

## 4. SQLite Implementation Details (Pessimistic Locking)

### SQLite Locking Model

SQLite uses coarse-grained locking at the database level:

- **SHARED mode**: Multiple readers can access DB simultaneously
- **RESERVED mode**: Ready to write, but other reads still allowed
- **EXCLUSIVE mode**: Single writer, no readers allowed

By default, SQLite transactions:
1. Start with SHARED lock (read phase)
2. Upgrade to EXCLUSIVE lock when write needed (prevents other writers)
3. Release lock on COMMIT/ROLLBACK

### Recommended: BEGIN IMMEDIATE Pattern

For MCP write operations (`submit_artifact()`, `update_workflow_state()`, `store_session_checkpoint()`):

**Use `BEGIN IMMEDIATE` transaction:**

```sql
BEGIN IMMEDIATE;
  -- All operations within this tx have exclusive write lock
  SELECT * FROM sessions WHERE id = ?;  -- Verify existence + permissions
  UPDATE sessions SET fsm_state = ?;
  INSERT INTO workflow_events (...) VALUES (...);
COMMIT;
-- Lock released on COMMIT
```

**Why BEGIN IMMEDIATE?**
- Acquires lock **before** any statements run (not mid-transaction)
- Prevents other writers from starting concurrent transaction
- Allows readers (SHARED locks) during write transaction
- Simpler than PRAGMA BUSY_TIMEOUT patterns
- Safer than relying on autocommit + implicit locks

### Deadlock Prevention: Lock Ordering

When multiple tables need updates in a single transaction, always acquire locks in consistent order:

```typescript
// EPIC-08 TASK-08-02 pseudocode

async function updateWorkflowState(sessionId, newState, event) {
  db.exec('BEGIN IMMEDIATE');
  try {
    // Step 1: Lock + read sessions (parent table)
    const session = db.query(
      'SELECT * FROM sessions WHERE id = ? LIMIT 1',
      [sessionId]
    );
    if (!session) throw new Error('SESSION_NOT_FOUND');
    
    // Step 2: Validate FSM transition
    if (!isValidTransition(session.fsm_state, newState)) {
      throw new Error('FSM_INVALID_TRANSITION');
    }
    
    // Step 3: Update state (still in BEGIN IMMEDIATE)
    db.run(
      'UPDATE sessions SET fsm_state = ? WHERE id = ?',
      [newState, sessionId]
    );
    
    // Step 4: Insert event (child table)
    db.run(
      'INSERT INTO workflow_events (...) VALUES (...)',
      [...eventData]
    );
    
    db.exec('COMMIT');  // All-or-nothing
  } catch (error) {
    db.exec('ROLLBACK');  // Undo entire transaction
    throw error;
  }
}
```

**Key principles:**
1. Always lock parent (sessions) before children (workflow_events, artifacts)
2. Validate preconditions before mutations
3. Single db.exec('COMMIT') confirmsall-or-nothing semantics
4. Errors trigger ROLLBACK (inconsistency prevented)

### Performance: Single-Writer Limitation ⚠️

**Important:** SQLite enforces a **single concurrent writer** across the entire database.

- Only ONE `BEGIN IMMEDIATE` transaction can hold the lock at a time
- Other agents calling `submit_artifact()` or `update_workflow_state()` will block
- After 5 seconds (default timeout), get "database is locked" error

**For M01/M02 acceptable because:**
- Single-agent workflow (one person works per session)
- Expected concurrency: low (agents don't overlap)

**Monitoring/Scaling advice (M03+):**
- Track "database is locked" errors in metrics
- If error rate > 5% at scale, consider:
  - **Option 1 (simple):** Application-level request queue (FIFO buffer)
  - **Option 2 (complex):** Message broker (Redis/RabbitMQ) for distributed locking
  - **Option 3 (future):** Event Sourcing (Option C) + read replicas

### Transaction Isolation Level

SQLite default PRAGMA settings:

```sql
-- Recommended for agent.db (good balance of safety + performance)
PRAGMA journal_mode = WAL;        -- Write-Ahead Logging (better concurrent reads)
PRAGMA synchronous = NORMAL;      -- Reasonable durability (not every fsync)
PRAGMA isolation_level = SERIALIZABLE;  -- Strongest isolation (default in SQLite)
```

**Why these?**
- **WAL mode**: Allows readers while writers persisting (improves read throughput)
- **NORMAL**: Less aggressive fsync than FULL (good for single-machine use)
- **SERIALIZABLE**: Prevents all race conditions (safest for FSM)

---

## Recommendation

### For M01/M02: **Option A (Pessimistic Locking)**

**Rationale:**

1. **SQLite-friendly** — pessimistic locking works well with SQLite's transaction model
2. **Simple to reason about** — exclusive access prevents races
3. **Fast implementation** — 1–2 days vs. 3–4 days for Event Sourcing
4. **Fits M01 scope** — EPIC-08 can deliver with simpler locking

**Implementation Plan (EPIC-08 TASK-08-02):**

```typescript
// EPIC-08 TASK-08-02: Add pessimistic locking

// sessions table update
sessions: {
  locked_by: string | NULL,      // agent_id or NULL
  locked_at: timestamp | NULL
}

// Method 1: acquire_lock(session_id, agent_id) -> Bool
// Method 2: update_workflow_state_locked(...)  // requires lock first

// Error handling:
// - Lock timeout: Return 409 Conflict (Locked by another agent)
// - Stale lock: Architect to define timeout (default: 5 min)
```

### For M03: **Evaluate Option C (Event Sourcing)**

After M02 stabilizes, review Event Sourcing for:

- Session recovery resilience
- Better audit trail for compliance
- Future distributed systems

---

## Security Gate: State Change Authorization

**DECISION (locks in by EPIC-10 team):**

1. **Same-agent-only (Recommended):**
   - Only the agent that created the session can `update_workflow_state()`
   - Admins can force state via special "admin_escalate_state()" tool (separate permission)

2. **Role-based (Alternative):**
   - Any role with "can_escalate_workflows" permission can force state
   - RBAC checks in MCP tool gateway

**Action:** Add to [EPIC-10 acceptance criteria](../delivery/mcp-maintenance/milestones/milestone_02/epic_10/state.md):

- [ ] `update_workflow_state()` enforces agent_id == session.owner_id
- [ ] Admin escalation path is documented (tool name, permission)
- [ ] Unauthorized attempts logged (audit trail)

---

## Implementation Checklist

### EPIC-08 (M01) Deliverables

- [x] Schema: Add `locked_by`, `locked_at` to sessions table
- [x] TASK-08-02: Implement pessimistic locking primitives
  - [ ] `acquire_workflow_lock(session_id, agent_id)` → Bool or error
  - [ ] `release_workflow_lock(session_id)` → void
  - [ ] `update_workflow_state()` uses locks internally
- [x] TASK-08-03: E2E test covers lock contention
  - [ ] Happy path: Agent X acquires lock, submits, releases
  - [ ] Error case: Agent Y tries concurrent update → 409 Conflict
  - [ ] Timeout: Lock held > 5 min → auto-release + alert

### EPIC-10 (M02) Deliverables

- [ ] `bootstrap_agent()` returns `can_update_workflow_state` permission flag
- [ ] `update_workflow_state()` enforces authorization
- [ ] Audit log: "Agent X updated session S1 state from A → B at 2026-03-15T10:25:33Z"

### Future (M03+)

- [ ] Evaluate Event Sourcing for performance/auditability
- [ ] Consider CQRS pattern if multi-agent scenarios become common
- [ ] Document lock timeout tuning (if high contention observed)

---

## Decision Record

| Decision | Option | Rationale |
|:---------|:-------|:----------|
| Concurrency model | A (Pessimistic Locking) | Simple, SQLite-friendly |
| State change authority | Same agent (recommended) | Prevents unauthorized escalation |
| Lock timeout | 5 minutes (default) | Balances responsiveness vs. stale lock recovery |
| Audit trail | Log all state changes | Compliance, debugging |
| Future approach | Option C (Event Sourcing) M03 | Better for scaling post-agent.db |

---

## Approval

- [ ] EPIC-08 Tech Lead (Backend Developer) — understands implementation cost
- [ ] EPIC-10 Tech Lead (Backend Developer) — aligns bootstrap_agent authorization
- [ ] Architect — ADR approved for implementation
- [ ] QA Lead — test strategy understood

**Status:** Draft → Approved (pending team feedback)
**Target Approval Date:** 2026-03-05 (before EPIC-08 implementation starts)

---

## References

- [EPIC-08 Write Layer](../delivery/mcp-maintenance/milestones/milestone_01/epic_08/)
- [EPIC-10 Bootstrap Agent](../delivery/mcp-maintenance/milestones/milestone_02/epic_10/)
- [MCP_Server_Architecture.md § 3.2 Write Layer](../../MCP_Server_Architecture.md#32-write-layer-post-operations)
- SQLite Transactions: <https://www.sqlite.org/lang_transaction.html>
- CQRS Pattern: <https://martinfowler.com/bliki/CQRS.html>
