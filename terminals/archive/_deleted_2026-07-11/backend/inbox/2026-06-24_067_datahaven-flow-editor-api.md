---
id: MSG-BACKEND-047
from: conductor
to: backend
type: task
priority: high
status: DONE
model: sonnet
ref: 2026-06-24_consensus_flow-editor-phase1.md
epic: EPIC-DATAHAVEN-UI
phase: 2
created: 2026-06-24
read: 2026-06-24
completed: 2026-06-24
content_hash: 9fb324194f1284f9d43b198afb611383f1c6e212ed9e53edb3ccdaab1c4b5267
---

# Datahaven Flow/Workflow Editor — Backend API Implementation

## Task Overview

Implement **PUT /api/graph/epics/:id** endpoint for the Flow/Workflow Editor, allowing users to edit epic properties (status, dependencies, target date) with full validation.

**Epic:** EPIC-DATAHAVEN-UI (Phase 2 of 3)
**Estimate:** 7-9 hours
**Related:** Frontend is implementing Mermaid.js visualization in parallel (MSG-FRONTEND-...)
**Architecture Reference:** `/opt/spaceos/docs/tasks/new/Datahaven_UI_Focus_Flow_Editor_Architecture_v1.md` (Section 2 & 5.2)

---

## What is Flow/Workflow Editor?

The Flow/Workflow Editor is an advanced feature that:
- **Visualizes** epic dependencies as an interactive graph (Planning → Projects → EPICS.yaml)
- **Allows editing** epic properties via API
- **Validates** state transitions (pending → active → done → blocked)
- **Prevents circular dependencies** via cycle detection
- **Updates** EPICS.yaml atomically

This backend task handles the **data validation and persistence layer**.

---

## API Implementation Tasks

### Task 1: PUT /api/graph/epics/:id Endpoint (4-6 hours)

**File:** `spaceos-nexus/knowledge-service/src/api/graphRoutes.ts` (EXTEND existing file)

**What to implement:**
1. Add new PUT route handler for `/epics/:id`
   - Extract epic ID from URL param
   - Parse request body (status, depends_on, parallel_with, target_date)
   - All fields are **optional** (can update just one)

2. Load EPICS.yaml data
   - File: `docs/projects/EPICS.yaml`
   - Parse YAML to extract epics array

3. Validate epic exists
   - Find epic by ID in array
   - Return 404 if not found

4. Validate status transition
   - Call `isValidStatusTransition()` function
   - Allow: `pending→active`, `active→done`, `active→blocked`, `blocked→active`
   - Deny: `done→pending`, `done→active`, etc.
   - Return 400 with error message if invalid

5. Validate dependencies (cycle detection)
   - If `depends_on` field provided:
     - Use existing `detectCycles()` function from `graph/operations.ts`
     - Build a test graph with the proposed changes
     - Return 400 with cycles if circular dependency detected
   - Ensure no epic depends on itself

6. Validate "done" status precondition
   - If setting status to "done":
     - Verify all epics in `depends_on` are also status "done"
     - Return 400 if any dependency is not done

7. Update epic in memory
   - Merge provided fields into epic object
   - Update `updated_at` timestamp

8. Write EPICS.yaml atomically
   - Call `writeEpicsYaml()` helper function
   - Must be atomic (temp file → rename)
   - Handle write errors gracefully (disk full, permission denied)

9. Invalidate cache
   - Call `clearEpicGraphCache()` if it exists
   - Ensures Mermaid graph will be regenerated

10. Return success response
    - HTTP 200 with updated epic object
    - Include validation results (valid: true, cycles: [])

**Expected Request:**
```
PUT /api/graph/epics/EPIC-CUTTING-Q3
Authorization: Bearer dev-token-spaceos-dashboard-2026
Content-Type: application/json

{
  "status": "done",
  "depends_on": ["EPIC-KERNEL-STABLE"],
  "parallel_with": ["EPIC-JOINERY-V2"],
  "target_date": "2026-09-30"
}
```

Note: All fields are **optional**. Client can send just `{ "status": "done" }` to update only status.

**Expected Response (200):**
```json
{
  "success": true,
  "epic": {
    "id": "EPIC-CUTTING-Q3",
    "name": "Cutting Module Q3",
    "status": "done",
    "depends_on": ["EPIC-KERNEL-STABLE"],
    "parallel_with": ["EPIC-JOINERY-V2"],
    "target_date": "2026-09-30",
    "description": "Lapszabász modul: nesting, optimization, CNC integration."
  },
  "validation": {
    "valid": true,
    "cycles": []
  }
}
```

**Error Cases:**
- 400: Invalid epic ID (not found) → `{ "error": "Epic not found" }`
- 400: Invalid status transition → `{ "error": "Cannot transition from active to pending" }`
- 400: Circular dependency detected → `{ "error": "Cycle detected", "cycles": [["EPIC-A", "EPIC-B", "EPIC-A"]] }`
- 400: Cannot set done when dependencies not done → `{ "error": "Cannot set done: EPIC-X not done" }`
- 400: Invalid dependency (depends on itself) → `{ "error": "Epic cannot depend on itself" }`
- 401: Missing/invalid Authorization header
- 500: YAML parse error, file write error

**Testing Requirements:**
- [ ] Unit test: Valid status update (pending → active)
- [ ] Unit test: Valid status update (active → done)
- [ ] Unit test: Invalid transition (done → pending) returns 400
- [ ] Unit test: Circular dependency detection works
- [ ] Unit test: "Done" precondition validation
- [ ] Unit test: File is actually written to disk
- [ ] Integration test: Multiple consecutive updates
- [ ] Integration test: Atomic write (no partial files)

---

### Task 2: Status Transition Validator (1 hour)

**What to implement:**

Create a helper function:
```typescript
function isValidStatusTransition(
  currentStatus: string,
  newStatus: string
): { valid: boolean; error?: string } {
  // Implement state machine logic
}
```

**Valid Transitions:**
```
pending → active   ✅
active → done      ✅
active → blocked   ✅
blocked → active   ✅ (retry)
pending → blocked  ✅ (optional, if in spec)

All other transitions → ❌ Invalid
```

**Return format:**
```typescript
// Valid:
{ valid: true }

// Invalid:
{ valid: false, error: "Cannot transition from done to pending" }
```

**Testing:**
- [ ] Unit test each valid transition
- [ ] Unit test each invalid transition
- [ ] Unit test edge cases (null, undefined)

---

### Task 3: Dependency Validator — Cycle Detection (1 hour)

**What to implement:**

This functionality likely **already exists** in the codebase:
- Check `spaceos-nexus/knowledge-service/src/graph/operations.ts` for `detectCycles()` function
- If exists: use it directly in PUT endpoint
- If missing: implement basic cycle detection

**Cycle Detection Algorithm:**
```
Input: Graph (epics with depends_on edges), proposed change
Output: Array of cycles (or empty if valid DAG)

Algorithm:
1. Build adjacency list from all epics
2. Add proposed edges (depends_on updates)
3. Run DFS-based cycle detection
4. Return all cycles found
```

**Usage in PUT endpoint:**
```typescript
const testGraph = buildGraphWithProposedChanges(epicsData, epicId, newDependsOn);
const cycles = detectCycles(testGraph);
if (cycles.length > 0) {
  return res.status(400).json({ error: 'Cycle detected', cycles });
}
```

**Testing:**
- [ ] Unit test: No cycle detected in valid DAG
- [ ] Unit test: Direct cycle detected (A→B→A)
- [ ] Unit test: Indirect cycle detected (A→B→C→A)
- [ ] Unit test: Self-cycle detected (A→A)
- [ ] Integration test: Real EPICS.yaml cycles

---

### Task 4: EPICS.yaml Atomic Write Helper (1 hour)

**What to implement:**

Create a utility function:
```typescript
async function writeEpicsYaml(
  filePath: string,
  epicsData: EpicsYaml
): Promise<void> {
  // Atomic write implementation
}
```

**Implementation:**
```
1. Serialize epicsData to YAML format
2. Write to temp file (filePath + '.tmp')
3. Rename temp file to target (atomic on POSIX systems)
4. Handle errors:
   - ENOENT (file not found) - create new
   - EACCES (permission denied) - throw
   - ENOSPC (disk full) - throw
5. No partial files on failure
```

**Testing:**
- [ ] Unit test: Successful write
- [ ] Unit test: File permissions respected
- [ ] Unit test: No orphaned .tmp files on failure
- [ ] Integration test: Data integrity after write

---

## Definition of Done

**All tasks must be complete:**

- [ ] PUT endpoint implemented in graphRoutes.ts
- [ ] Status transition validation working (state machine logic)
- [ ] Dependency validation (cycle detection) working
- [ ] Atomic YAML write helper implemented
- [ ] Request/response format matches spec exactly
- [ ] All error cases handled with proper HTTP status codes
- [ ] Unit tests for each validation function (80%+ coverage)
- [ ] Integration tests with real EPICS.yaml file
- [ ] No TypeScript compilation errors
- [ ] No test failures
- [ ] Manual testing with curl/Postman:
  - Valid status update works
  - Invalid transition returns 400
  - Circular dependency detected
  - File actually updated on disk

---

## Architecture Reference

See the full architecture document for context:

**File:** `/opt/spaceos/docs/tasks/new/Datahaven_UI_Focus_Flow_Editor_Architecture_v1.md`

Key sections:
- Section 2.5 — Mermaid rendering strategy
- Section 5.2 — Epic Update API specification (lines 570-620)
- Section 2.6 — Status transition rules

---

## Parallel Work

**Frontend is working on:** MSG-FRONTEND-... (Flow Editor UI)
- Mermaid.js graph rendering
- Epic details panel
- Node click handlers
- Interactive editing

**These are independent** until integration testing. Once both API and UI are DONE:
1. Integration testing (API + UI together)
2. E2E testing (full user workflow)
3. Phase 3 dispatch (Polish + deployment)

---

## Notes

- **Uses existing code:** Reuse `detectCycles()` from `graph/operations.ts` if it exists
- **Uses existing code:** Reuse YAML parsing/serialization patterns from existing routes
- **Consider caching:** Invalidate graph cache when EPICS.yaml changes (use existing cache functions)
- **Consider git:** Optional: auto-commit EPICS.yaml changes for audit trail

---

## Questions?

If you need clarification:
- Check the architecture document (Section 5.2 for API spec)
- Review existing graph routes for patterns
- Contact Conductor via outbox message (BLOCKED status)

---

**Estimate:** 7-9 hours total
**Start:** Immediately
**Report:** DONE outbox when all tasks complete

