# QA Tester Prompt — M01 EPIC-08

**Version:** 1.0 (M01 EPIC-08 QA Edition)
**Extends:** `qa-tester.core.prompt.md` (if exists) or use this as standalone
**Scope:** Milestone 01, EPIC-08 (MCP Write Layer — Artifact Submit & Session Control)
**Use with Agent:** `.github/agents/qa_tester.agent.md` (substitute `${MILESTONE_ROOT}` = `.../milestone_01/`)

---

## [3. Context: EPIC-08 Overview]

### EPIC-08: Write Layer Foundation

**Phase:** M01 — Write Layer (Artifact Submit & Session Control)
**Status:** 🔄 In Development (ongoing)
**Epic Goal:** Add write capabilities to MCP server (read-only → read+write)

### What EPIC-08 Delivers

1. **MCP Write Tools** (2 of 3):
   - `submit_artifact(artifact_content, session_id, artifact_type)` — submit work artifacts
   - `update_workflow_state(session_id, new_state, event)` — manage FSM state transitions

2. **SQLite Schema** (3 new tables):
   - `sessions` — agent session tracking
   - `artifacts` — submitted work (implementation summaries, test reports, PRs)
   - `workflow_events` — state change audit log

3. **RBAC Integration**:
   - Only `backend_developer` and `tech_lead` roles can call `submit_artifact()`
   - Role validation via `RbacFilter`

4. **FSM State Machine**:
   - States: `started` → `in_progress` → `submitted` → `processed` → `closed`
   - Transitions triggered by artifact submission

5. **Error Handling**:
   - Invalid session ID
   - Missing RBAC permission
   - Schema validation failures
   - Proper HTTP status codes + error messages

### EPIC-08 Success Criteria (QA Validation)

- [ ] `submit_artifact()` tool works (happy path)
- [ ] `update_workflow_state()` transitions FSM correctly
- [ ] SQLite schema valid (FK constraints, data types)
- [ ] RBAC enforced (unauthorized role rejected)
- [ ] Error scenarios handled gracefully (5+ test cases)
- [ ] E2E test passes (mock agent → submit → DB verify)
- [ ] Session management: sessions table correctly tracks agents
- [ ] Workflow events logged: every state change auditable

---

## [4. QA Test Strategy for EPIC-08]

### Test Scope

**Happy Path (Core Functionality):**
1. Create session → submit artifact (type: `implementation_summary`) → verify DB
2. Submit artifact → FSM state changes to `submitted` → workflow_events logged
3. Query session artifacts → retrieve all submitted work for session

**Error Scenarios (Boundary & Security):**
1. Invalid session ID → proper error response
2. Missing `submit_artifact_permission` → RBAC rejection
3. Malformed artifact JSON → schema validation failure
4. Concurrent submissions → no data loss/corruption
5. NULL/empty artifact content → validation error

**Integration Points:**
- RBAC Filter: Does it correctly check role permissions?
- SQLite Transactions: Are multi-table writes atomic?
- FSM Validation: Does state machine reject invalid transitions?
- HTTP Status Codes: Are error responses correct (400, 401, 422, 500)?

### Test Data Setup

```yaml
# Test Sessions
session_001:
  agent_id: "agent-backend-dev-1"
  role: "backend_developer"
  status: "started"
  created_at: "2026-03-01T10:00:00Z"

session_002:
  agent_id: "agent-tech-lead-1"
  role: "tech_lead"
  status: "in_progress"
  created_at: "2026-03-01T11:00:00Z"

# Test Artifacts (payloads)
artifact_001:
  type: "implementation_summary"
  content:
    task_id: "TASK-08-01"
    status: "COMPLETED"
    implementation: "Implemented submit_artifact() tool..."
    test_coverage: "85%"

artifact_002:
  type: "test_report"
  content:
    task_id: "TASK-08-02"
    total_tests: 12
    passed: 11
    failed: 1
    coverage: "78%"
```

### Test Environment

- **Database**: SQLite test DB (fresh schema per test)
- **Mocking**: Mock MCP router + fake session/role IDs
- **Framework**: Jest + E2E Playwright if frontend involved
- **Isolation**: Each test case resets session/artifact state

---

## [5. Detailed Test Cases]

### TC-01: Happy Path — Submit Implementation Summary

**Precondition:** Session exists (id: `session_001`, role: `backend_developer`)

**Steps:**
1. Call `submit_artifact()` with:
   ```json
   {
     "session_id": "session_001",
     "artifact_type": "implementation_summary",
     "artifact_content": {
       "task_id": "TASK-08-01",
       "status": "COMPLETED",
       "implementation_notes": "Implemented write layer tools..."
     }
   }
   ```
2. Verify HTTP 200 response with artifact ID
3. Query SQLite `artifacts` table → confirm row inserted
4. Query `workflow_events` → verify `artifact_submitted` event logged
5. Query `sessions` → verify `fsm_state` changed to `submitted`

**Expected Results:**
- ✅ Artifact record created in DB
- ✅ FSM state = `submitted`
- ✅ Workflow event logged with timestamp
- ✅ HTTP 200 + artifact_id in response

**AC Validation:** ✅ EPIC-08 AC #1 passed

---

### TC-02: Happy Path — Update FSM State

**Precondition:** Session exists, FSM state = `in_progress`

**Steps:**
1. Call `update_workflow_state()` with:
   ```json
   {
     "session_id": "session_001",
     "new_state": "submitted",
     "event": "artifact_submitted",
     "metadata": { "artifact_id": "artifact_123" }
   }
   ```
2. Verify HTTP 200
3. Query `sessions.fsm_state` → should be `submitted`
4. Query `workflow_events` → should have entry for state transition

**Expected Results:**
- ✅ FSM state updated correctly
- ✅ Workflow event recorded
- ✅ HTTP 200 response
- ✅ No side effects (other sessions unchanged)

**AC Validation:** ✅ EPIC-08 AC #2 passed

---

### TC-03: Error — Invalid Session ID

**Precondition:** Non-existent session ID

**Steps:**
1. Call `submit_artifact()` with `session_id: "invalid_session_xyz"`
2. Verify HTTP 400 or 404 response
3. Verify error message: "Session not found"
4. Verify no artifact record created

**Expected Results:**
- ✅ HTTP 400/404 error
- ✅ Meaningful error message
- ✅ No data corruption
- ✅ No workflow event created

**AC Validation:** ✅ EPIC-08 Error Scenario #1 passed

---

### TC-04: Security — RBAC Permission Denied

**Precondition:** Session role = `qa_tester` (no `submit_artifact_permission`)

**Steps:**
1. Call `submit_artifact()` with session (role: `qa_tester`)
2. Verify HTTP 403 (Forbidden)
3. Verify error message: "Role not authorized for submit_artifact"
4. Verify no artifact created

**Expected Results:**
- ✅ HTTP 403 Forbidden
- ✅ RBAC filter blocked submission
- ✅ No data written
- ✅ Security constraint enforced

**AC Validation:** ✅ EPIC-08 RBAC verification passed

---

### TC-05: Error — Schema Validation Failed

**Precondition:** Invalid artifact content (missing required fields)

**Steps:**
1. Call `submit_artifact()` with malformed content:
   ```json
   {
     "session_id": "session_001",
     "artifact_type": "implementation_summary",
     "artifact_content": {
       "task_id": "TASK-08-01"
       // missing: "status", "implementation_notes"
     }
   }
   ```
2. Verify HTTP 422 (Unprocessable Entity)
3. Verify error includes missing field names
4. Verify no artifact record created

**Expected Results:**
- ✅ HTTP 422 error
- ✅ Clear validation error message
- ✅ Input rejected safely
- ✅ No DB corruption

**AC Validation:** ✅ EPIC-08 Error Scenario #3 passed

---

### TC-06: Concurrency — Multiple Submissions Same Session

**Precondition:** Session with `session_001`

**Steps:**
1. Spawn 3 concurrent threads, each calls `submit_artifact()` for same session
2. Wait for all to complete
3. Query `artifacts` table → verify exactly 3 rows (no collision)
4. Verify each artifact has unique ID
5. Verify all workflow_events recorded (3 entries)

**Expected Results:**
- ✅ All 3 artifacts created (no race condition)
- ✅ No duplicate IDs
- ✅ All transactions atomic
- ✅ DB integrity maintained

**AC Validation:** ✅ EPIC-08 Concurrency test passed

---

### TC-07: Integration — FSM Invalid Transition

**Precondition:** Session FSM state = `closed` (terminal state)

**Steps:**
1. Try to submit artifact while FSM state = `closed`
2. Verify HTTP 409 (Conflict) or graceful rejection
3. Verify no artifact created
4. Verify FSM state remains `closed`

**Expected Results:**
- ✅ HTTP 409 or 422
- ✅ Invalid transition rejected
- ✅ FSM integrity maintained
- ✅ Clear error message

**AC Validation:** ✅ EPIC-08 FSM validation passed

---

### TC-08: Data Persistence — Query After Submit

**Precondition:** Artifact submitted, SessionID = `session_001`

**Steps:**
1. Submit artifact via `submit_artifact()`
2. Close Database connection (simulating app restart)
3. Reopen DB
4. Query by session ID → retrieve all artifacts
5. Verify artifact still exists with correct data

**Expected Results:**
- ✅ Data persisted to disk
- ✅ Query retrieves artifact unchanged
- ✅ No data loss on reconnect
- ✅ Referential integrity maintained

**AC Validation:** ✅ EPIC-08 Persistence test passed

---

## [6. Test Coverage & Results]

### Coverage Matrix

| Category | Test Cases | Status |
|:---------|:-----------|:-------|
| Happy Path | TC-01, TC-02 | ⏳ Pending |
| Error Scenarios | TC-03, TC-04, TC-05 | ⏳ Pending |
| Concurrency | TC-06 | ⏳ Pending |
| Integration | TC-07 | ⏳ Pending |
| Persistence | TC-08 | ⏳ Pending |
| **Total** | **8 test cases** | **80% Coverage Target** |

### Pass Criteria

- ✅ All 8 test cases pass
- ✅ No P0 or P1 failures
- ✅ Error messages clear and actionable
- ✅ DB schema integrity verified
- ✅ RBAC correctly enforced
- ✅ FSM state transitions valid
- ✅ Coverage ≥ 80% for write layer code

### Flaky Test Risk

**Potential Issues:**
- Timing: Concurrent submissions might cause intermittent race conditions
- Database: SQLite WAL mode locking timeouts under high concurrency
- Mock agents: Session ID generation consistency

**Mitigation:**
- Retry flaky tests 3x; if still fails, escalate as legitimate bug
- Increase test DB WAL timeout to 10s for concurrency tests
- Use deterministic session IDs in tests (not random UUIDs)

---

## [7. EPIC-08 Out-of-Scope (M02)]

❌ **NOT tested in M01:**
- Session recovery logic (`store_session_checkpoint()` tool)
- Checkpoint persistence & compression
- Session history replay
- ChromaDB write-back for episodes
- Workflow reflection / learning loop

**Why deferred?** These belong in M02 EPIC-12 (Episodic Memory) where they integrate with the full session management system.

---

## [8. QA Sign-Off Workflow]

### For EPIC-08 Complete (All ACsValidated)

1. **Run all 8 test cases** (TC-01 through TC-08)
2. **Verify AC checklist:**
   - [ ] `submit_artifact()` works
   - [ ] `update_workflow_state()` works
   - [ ] SQLite schema valid
   - [ ] RBAC enforced
   - [ ] Error handling complete
   - [ ] E2E passes
   - [ ] Session management correct
   - [ ] Workflow audit log complete

3. **Generate QA Sign-Off Memo:**
   ```markdown
   ## QA Sign-Off: EPIC-08 Write Layer

   **Status:** ✅ APPROVED / ❌ BLOCKED

   **Test Results:**
   - Total Test Cases: 8
   - Passed: 8
   - Failed: 0
   - Coverage: 85%

   **Blockers (if any):**
   - [List any P0/P1 failures]

   **Recommendations:**
   - [Suggestions for refinement or follow-up]

   **QA Sign-Off Date:** [Date]
   **QA Engineer:** [Name]
   ```

4. **Document in:**
   - `Docs/.../milestone_01/epic_08/implementation-summary/QA-SIGNOFF.md`
   - Update `state.md` with QA status

---

## [9. Commands Reference]

### Run EPIC-08 Tests

```bash
# Run all unit tests for write layer
npm run test -- --grep "submit_artifact|update_workflow_state"

# Run integration tests
npm run test:integration -- --grep "EPIC-08"

# Run E2E tests
npm run test:e2e -- --grep "write-layer"

# Run with coverage report
npm run test:coverage -- src/mcp/WriteLayerTools.ts src/metadata/WorkflowStateTracker.ts

# Check database schema
sqlite3 agent.db ".schema sessions"
sqlite3 agent.db ".schema artifacts"
sqlite3 agent.db ".schema workflow_events"
```

### Quick Verification

```bash
# Insert test session
sqlite3 agent.db "INSERT INTO sessions (session_id, agent_id, role, fsm_state) VALUES ('test_001', 'agent-1', 'backend_developer', 'started');"

# Verify write layer tools registered
npm start & curl -X POST http://localhost:3000/api/mcp/tools -d '{"tool": "submit_artifact"}'

# Clean test database
rm agent.test.db && npm run seed:test-db
```

---

## [10. Golden Rules for Testing EPIC-08]

1. **Atomicity:** All multi-table writes (artifacts + workflow_events + sessions) must be atomic
2. **RBAC Always:** Never skip RBAC check, test with unauthorized role
3. **Error First:** Test errors BEFORE happy path to catch edge cases
4. **Isolation:** Each test case uses fresh DB state (no test dependencies)
5. **Concurrency Stress:** At least one concurrency test with 3+ parallel submissions
6. **Data Persistence:** Test DB reconnect to verify data is durably written
7. **FSM Validation:** Every state transition must be validated against state machine spec
8. **Audit Trail:** Verify workflow_events captures every action (immutable log)

---

## [Integration with Core QA Prompt]

This EPIC-08 prompt **extends** generic QA capabilities:

- ✅ Reuse: Test case templates, error classification, QA sign-off format
- ✅ Reuse: Coverage metrics, flaky test triage process
- ✅ Reuse: RBAC testing patterns
- 🆕 M01-Specific: Write layer test strategy, FSM validation, SQLite schema verification

---

**Ready for M01 EPIC-08 QA! 🚀**

For generic QA guidance, reference `qa-tester.core.prompt.md` (if available).
For EPIC-08 specifics, use this prompt with the `qa_tester.agent.md` agent.

Run all 8 test cases, validate all ACs, and provide QA sign-off before EPIC-08 closure.
