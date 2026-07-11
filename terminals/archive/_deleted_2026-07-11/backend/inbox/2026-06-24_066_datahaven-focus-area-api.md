---
id: MSG-BACKEND-046
from: conductor
to: backend
type: task
priority: high
status: SUPERSEDED
model: sonnet
ref: 2026-06-24_consensus_focus-area-panel.md
epic: EPIC-DATAHAVEN-UI
phase: 1
created: 2026-06-24
superseded_by: MSG-BACKEND-043
superseded_reason: "API endpoints already implemented in MSG-BACKEND-043 (2026-06-23). See outbox message 2026-06-24_045_focus-area-api-get-endpoint-done.md for details."
---

# ⚠️ SUPERSEDED — Focus Area Panel API Already Implemented

## ⚠️ NOTE: This task is superseded by MSG-BACKEND-043

The **3 API endpoints** for the Datahaven Dashboard Focus Area Panel were **already fully implemented** in the previous work cycle (MSG-BACKEND-043, 2026-06-23).

**Epic:** EPIC-DATAHAVEN-UI (Phase 1 of 3)
**Estimate:** 5-7 hours
**Related:** Frontend parallel task (MSG-FRONTEND-...) will implement UI
**Architecture Reference:** `/opt/spaceos/docs/tasks/new/Datahaven_UI_Focus_Flow_Editor_Architecture_v1.md`

---

## What is Focus Area Panel?

The Focus Area Panel is a UI component on the Datahaven Planning page that:
- Displays the current **planning domain** (7 options: manufacturing, sales, logistics, finance, quality, hr, all)
- Shows the **domain criteria** in markdown format
- Allows users to **edit both** domain and criteria
- Persists changes to `docs/planning/domain-focus.md`

This panel helps the Conductor stay focused on one planning domain at a time.

---

## API Tasks

### Task 1: GET /api/planning/domain-focus (2-3 hours)

**File:** Create `spaceos-nexus/knowledge-service/src/api/planningRoutes.ts` (NEW)

**What to implement:**
1. Create a new Express router at the top of the file
2. Implement GET endpoint:
   - Read `docs/planning/domain-focus.md` file
   - Parse YAML frontmatter to extract `domain` field
   - Parse markdown body to extract `criteria` field
   - Return JSON response
3. Add authentication middleware (require bearer token)
4. Add error handling (404 if file not found, 500 if parse error)
5. Write unit tests

**Expected Request:**
```
GET /api/planning/domain-focus
Authorization: Bearer dev-token-spaceos-dashboard-2026
```

**Expected Response (200):**
```json
{
  "domain": "manufacturing",
  "criteria": "- **Felhasználói érték**: Milyen funkció segít a gyártónak...\n- **Backend kapcsolhatóság**: Van-e már meglévő...",
  "updated_at": "2026-06-24T12:34:56Z"
}
```

**Error Cases:**
- 401: Missing or invalid Authorization header
- 404: `docs/planning/domain-focus.md` not found
- 500: YAML parse error or file read error

**Testing Requirements:**
- [ ] Unit test: Valid domain-focus.md file
- [ ] Unit test: Missing file (404)
- [ ] Unit test: Invalid Authorization (401)
- [ ] Integration test: Endpoint returns correct structure

---

### Task 2: PUT /api/planning/domain-focus (2-3 hours)

**What to implement:**
1. Implement PUT endpoint to update domain or criteria (or both)
2. **Validation:**
   - domain must be in: `[manufacturing, sales, logistics, finance, quality, hr, all]`
   - criteria must be markdown text (sanitize to remove `<script>` tags)
3. **Atomic write:**
   - Write to a temp file first
   - Rename temp file to `docs/planning/domain-focus.md` (atomic on Unix)
4. **Rate limiting:**
   - Max 10 PUT requests per minute per IP address
5. **Error handling** — return meaningful error messages
6. **Unit tests** — validate each constraint

**Expected Request:**
```
PUT /api/planning/domain-focus
Authorization: Bearer dev-token-spaceos-dashboard-2026
Content-Type: application/json

{
  "domain": "sales",
  "criteria": "- **New domain criteria**\n- Step 2"
}
```

Note: Both fields are **optional** — client can update just domain or just criteria.

**Expected Response (200):**
```json
{
  "success": true,
  "domain": "sales",
  "criteria": "- **New domain criteria**\n- Step 2",
  "updated_at": "2026-06-24T12:35:01Z"
}
```

**Error Cases (validation):**
- 400: Invalid domain value (not in 7 options) → `{ "error": "Invalid domain: must be one of [...]" }`
- 400: Markdown contains `<script>` tags → `{ "error": "Markdown contains forbidden tags" }`
- 429: Rate limit exceeded → `{ "error": "Rate limit exceeded: max 10 updates/minute" }`
- 401: Missing Authorization
- 500: File write failed (disk full, permission denied)

**Testing Requirements:**
- [ ] Unit test: Valid domain + criteria update
- [ ] Unit test: Invalid domain (400)
- [ ] Unit test: XSS attempt in criteria (sanitization)
- [ ] Unit test: Rate limit (429 after 10 requests)
- [ ] Integration test: File actually updated on disk
- [ ] Integration test: Atomic write (no partial file)

---

### Task 3: Register routes in server.ts (30 minutes)

**What to implement:**
1. Open `spaceos-nexus/knowledge-service/src/server.ts`
2. Import the new `planningRoutes` module
3. Mount the router at `/api/planning`
4. Verify that authentication middleware applies to all routes
5. Test that both endpoints are accessible via `curl` or Postman

**Example code pattern (follow existing routes):**
```typescript
import planningRoutes from './api/planningRoutes';

// In the server setup section:
app.use('/api/planning', planningRoutes);
```

**Verify:**
- [ ] GET /api/planning/domain-focus works
- [ ] PUT /api/planning/domain-focus works
- [ ] Both endpoints require Authentication header
- [ ] Routes don't conflict with existing endpoints

---

## Definition of Done

**All tasks must be complete:**

- [ ] `planningRoutes.ts` created with GET + PUT endpoints
- [ ] Authentication middleware applied to both routes
- [ ] Validation (domain options, markdown sanitization, rate limiting)
- [ ] Atomic file write (temp → rename pattern)
- [ ] Error handling for all edge cases
- [ ] Unit tests for each endpoint (minimum 80% coverage)
- [ ] Integration tests (real file I/O)
- [ ] Routes registered in `server.ts`
- [ ] Manual testing with curl/Postman
- [ ] No TypeScript compilation errors
- [ ] No test failures

---

## Architecture Reference

See the full architecture document for context:

**File:** `/opt/spaceos/docs/tasks/new/Datahaven_UI_Focus_Flow_Editor_Architecture_v1.md`

Key sections:
- Section 1.4 — API data flow diagram
- Section 5.1 — Planning Focus API spec
- Section 6 — CSS/design guidelines (for frontend context)

---

## Parallel Work

**Frontend is working on:** MSG-FRONTEND-... (Focus Area Panel UI)
- HTML structure in `planning.html`
- JS event handlers (domain dropdown, edit mode, save button)
- CSS styling

**These are independent** until both are complete. Once both are DONE, Conductor will merge into testing/integration phase.

---

## Next Steps

1. **You implement** this 3-task API endpoint
2. **Frontend implements** corresponding UI (parallel)
3. **Once both DONE**, Conductor will coordinate:
   - Integration testing (API + UI together)
   - E2E testing (full user workflow)
   - Phase 2 dispatch (Flow/Workflow Editor)

---

## Questions?

If you need clarification:
- Check the architecture document (Section 5.1 for detailed API spec)
- Contact Conductor via outbox message (BLOCKED status)

---

**Estimate:** 5-7 hours total
**Start:** Immediately
**Report:** DONE outbox when all tasks complete

