---
id: MSG-BACKEND-046
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: docs/planning/queue/2026-06-24_consensus_flow-editor-phase1.md
epic: EPIC-DATAHAVEN-UI
created: 2026-06-24
content_hash: 55a4ef9b779adb8825b702f47b11d9cb4638b2e91ac14276c13467149749bc52
---

# Flow/Workflow Editor — Backend API Implementation

## Epic: EPIC-DATAHAVEN-UI Phase 2 (Flow Editor)

Implement the backend API for the Flow/Workflow Editor component. This enables epic management through a visual Mermaid graph on the Datahaven Planning page.

---

## Context

**Reference plan:** `/opt/spaceos/docs/planning/queue/2026-06-24_consensus_flow-editor-phase1.md`

The Flow Editor allows users to:
- View epic dependency graph (Mermaid visualization)
- Change epic status (pending/active/done/blocked)
- Add/remove dependencies with cycle detection
- Validate state transitions

**Existing API:** GET endpoints already exist (`/api/graph/epics`, `/api/graph/mermaid`). You need to add PUT operations.

---

## Tasks

### API-001: PUT /api/graph/epics/:id

**File:** `spaceos-nexus/knowledge-service/src/api/graphRoutes.ts` (EXTEND)

**Requirements:**
- Add PUT route for `/epics/:id`
- Load EPICS.yaml from `docs/projects/EPICS.yaml`
- Find epic by ID (404 if not found)
- Validate status transition using state machine
- Validate dependencies using cycle detection
- If setting status to `done`: verify all `depends_on` epics are `done`
- Update epic fields: `status`, `depends_on`, `parallel_with`, `target_date`
- Write EPICS.yaml atomically (temp file + rename)
- Invalidate graph cache
- Return updated epic + validation result

**Status Transition Rules:**
```
pending → active ✅
active → done ✅
active → blocked ✅
blocked → active ✅ (retry)
done → pending ❌
done → active ❌
```

**Request/Response Example:**
```typescript
// PUT /api/graph/epics/EPIC-CUTTING-Q3
// Request:
{
  "status": "done",
  "depends_on": ["EPIC-KERNEL-STABLE"],
  "parallel_with": ["EPIC-JOINERY-V2"],
  "target_date": "2026-09-30"
}

// Response (200 OK):
{
  "success": true,
  "epic": {
    "id": "EPIC-CUTTING-Q3",
    "name": "Cutting Module Q3",
    "status": "done",
    "depends_on": ["EPIC-KERNEL-STABLE"],
    "parallel_with": ["EPIC-JOINERY-V2"],
    "target_date": "2026-09-30"
  },
  "validation": {
    "valid": true,
    "cycles": []
  }
}

// Response (400 Bad Request - cycle detected):
{
  "success": false,
  "error": "Cycle detected in dependency graph",
  "cycles": [["EPIC-A", "EPIC-B", "EPIC-A"]]
}

// Response (400 Bad Request - invalid transition):
{
  "success": false,
  "error": "Invalid status transition: done → pending"
}
```

**Estimate:** 4-6 hours

---

### API-002: Status Transition Validator

Create `isValidStatusTransition(currentStatus, newStatus)` helper function.

**Requirements:**
- Implement state machine logic (see rules above)
- Return boolean + error message if invalid
- Use TypeScript type guards for status enum

**Estimate:** 1 hour

---

### API-003: Dependency Validator (Cycle Detection)

**Requirements:**
- Use existing `detectCycles()` from `graph/operations.ts`
- Build test graph with proposed changes
- Return cycle paths if detected
- Support multiple cycles (return all)

**Estimate:** 1 hour

---

### API-004: EPICS.yaml Atomic Write Helper

Create `writeEpicsYaml(path, data)` utility function.

**Requirements:**
- Write to temp file first (`.tmp` suffix)
- Validate YAML structure before writing
- Use atomic rename (POSIX guarantees atomicity)
- Handle errors gracefully (cleanup temp file)
- Add file locking if possible (prevent concurrent writes)

**Example:**
```typescript
async function writeEpicsYaml(path: string, data: any): Promise<void> {
  const tempPath = `${path}.tmp`;
  try {
    await fs.writeFile(tempPath, yaml.stringify(data), 'utf8');
    await fs.rename(tempPath, path); // atomic
  } catch (err) {
    await fs.unlink(tempPath).catch(() => {}); // cleanup
    throw err;
  }
}
```

**Estimate:** 1 hour

---

### TEST-001: Backend API Tests

**File:** `spaceos-nexus/knowledge-service/src/__tests__/graphRoutes.put.test.ts` (NEW)

**Test cases:**
- ✅ PUT with valid status transition (pending → active)
- ✅ PUT with invalid transition returns 400 (done → pending)
- ✅ PUT creating cycle returns 400 with cycles array
- ✅ PUT on done → done (no-op, returns 200)
- ✅ PUT with unknown epic ID returns 404
- ✅ PUT without auth token returns 401
- ✅ PUT updates target_date successfully
- ✅ PUT updates depends_on successfully
- ✅ PUT with status=done but unfinished dependencies returns 400

**Estimate:** 2-3 hours

---

### TEST-002: Cycle Detection Tests

**File:** `spaceos-nexus/knowledge-service/src/__tests__/graph/cycles.test.ts`

**Test cases:**
- ✅ Simple cycle (A → B → A)
- ✅ Complex cycle (A → B → C → A)
- ✅ Valid DAG (no cycles)
- ✅ Multiple disconnected cycles
- ✅ Self-loop (A → A)

**Estimate:** 1 hour

---

## Performance Targets

- PUT endpoint: <300ms (p95)
- Cycle detection: <100ms for graphs with <50 nodes
- YAML write: <50ms

---

## Security Checklist

- ✅ Authentication required (bearer token)
- ✅ Input validation (status enum, epic IDs, dates)
- ✅ Cycle detection (prevent invalid DAGs)
- ✅ Atomic file operations (no partial writes)
- ✅ Cache invalidation (no stale data)
- ✅ Rate limiting (10 writes/min per IP)

---

## Acceptance Criteria

- [ ] PUT /api/graph/epics/:id endpoint implemented
- [ ] Status transitions validated correctly
- [ ] Dependency cycles prevented
- [ ] EPICS.yaml written atomically
- [ ] All tests passing (TEST-001, TEST-002)
- [ ] No regressions in existing GET endpoints
- [ ] API responds <300ms (p95)

---

## Implementation Notes

1. **Start with existing code:** Review `graphRoutes.ts` GET handlers to understand current structure
2. **Reuse graph utilities:** `detectCycles()`, `buildGraph()` already exist in `graph/operations.ts`
3. **Test first:** Write tests before implementing (TDD approach recommended)
4. **Cache invalidation:** Look for existing cache layer, invalidate on PUT

---

## Estimate: 2-3 days

**Breakdown:**
- API implementation: 1-1.5 days
- Testing: 0.5-1 day
- Integration + fixes: 0.5 day

---

**When done, write DONE outbox with:**
- Files changed
- Test results (all passing)
- API endpoint URLs
- Any blockers or issues encountered
