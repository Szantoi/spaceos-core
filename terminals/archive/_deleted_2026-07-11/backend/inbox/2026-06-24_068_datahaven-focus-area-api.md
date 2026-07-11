---
id: MSG-BACKEND-048
from: conductor
to: backend
type: task
priority: high
status: DUPLICATE
model: sonnet
ref: 2026-06-24_consensus_focus-area-panel.md
epic: EPIC-DATAHAVEN-UI
phase: 1
created: 2026-06-24
read: 2026-06-24
duplicate_of: MSG-BACKEND-043
content_hash: c915ec079c1d81a4d290f78e03cd07d5b33d3c6b50ebd1e81bc44b060a6b6ff3
---

# Datahaven Focus Area Panel — Backend API Implementation

## Task Overview

Implement the **GET and PUT endpoints** for the Focus Area Panel, allowing users to view and edit the planning domain configuration (`docs/planning/domain-focus.md`) from the Datahaven Dashboard.

**Epic:** EPIC-DATAHAVEN-UI (Phase 1 of 3)
**Estimate:** 4-5 hours
**Priority:** HIGH
**Dependencies:** None (can start immediately)
**Architecture Reference:** `/opt/spaceos/docs/tasks/new/Datahaven_UI_Focus_Flow_Editor_Architecture_v1.md` (Section 3)

---

## What is Focus Area Panel?

The Focus Area Panel is a new UI component on the Datahaven Dashboard Planning page that:
- Displays the current planning domain (manufacturing, sales, logistics, finance, quality, hr, or all)
- Shows the domain criteria (in markdown format)
- Allows users to switch domains and edit criteria without manually editing `domain-focus.md`

This backend task handles the **API layer** for reading and writing domain configuration.

---

## API Implementation Tasks

### API-001: GET /api/planning/domain-focus (2-3 hours)

**File:** `spaceos-nexus/knowledge-service/src/api/planningRoutes.ts` (CREATE NEW)

**What to implement:**

1. Create new file `planningRoutes.ts`
2. Implement GET endpoint at `/api/planning/domain-focus`
   - Read `docs/planning/domain-focus.md` file
   - Parse YAML frontmatter to extract `domain` field
   - Parse markdown body to extract `criteria`
   - Return JSON response with:
     - `domain` — current domain string
     - `criteria` — markdown content
     - `updated_at` — file modification timestamp (ISO 8601)

3. Add authentication middleware
   - Require Bearer token (same as other /api/planning endpoints)
   - Return 401 if missing or invalid

4. Error handling
   - Return 404 if `domain-focus.md` doesn't exist
   - Return 500 on file read errors

**Request/Response Example:**

```typescript
// GET /api/planning/domain-focus
// Response (200 OK):
{
  "domain": "manufacturing",
  "criteria": "- **Felhasználói érték**: prioritás az asztalos KKV-k...\n- **Performance**: API response <100ms",
  "updated_at": "2026-06-24T12:34:56.000Z"
}
```

---

### API-002: PUT /api/planning/domain-focus (2-3 hours)

**File:** `spaceos-nexus/knowledge-service/src/api/planningRoutes.ts` (EXTEND)

**What to implement:**

1. Implement PUT endpoint at `/api/planning/domain-focus`
   - Accept request body with optional `domain` and/or `criteria` fields
   - At least one field must be present (return 400 if both missing)

2. Validation
   - **Domain whitelist validation:** Must be in `["manufacturing", "sales", "logistics", "finance", "quality", "hr", "all"]`
   - Return 400 with error message if invalid
   - **Markdown sanitization:** Strip `<script>` tags and other dangerous HTML (regex or DOMPurify)
   - **Rate limiting:** Max 10 writes per minute per IP address
   - Return 429 (Too Many Requests) if exceeded

3. File writing
   - **Atomic operation:** Write to temp file first, then rename
   - Preserve YAML frontmatter format:
     ```yaml
     ---
     domain: manufacturing
     updated_at: 2026-06-24T12:35:01Z
     ---

     [criteria markdown here]
     ```
   - Update `updated_at` field to current timestamp
   - **Permissions:** Set file mode to 0600 (read/write owner only)

4. Cache invalidation
   - Clear any cached data related to domain focus (if you added caching in API-001)

5. Error handling
   - Return 400 for validation errors (with clear message)
   - Return 429 for rate limit exceeded
   - Return 500 on file write errors

**Request/Response Example:**

```typescript
// PUT /api/planning/domain-focus
// Request:
{
  "domain": "sales",                    // Optional
  "criteria": "- **Focus**: Sales workflows\n- **Priority**: Lead conversion"  // Optional
}

// Response (200 OK):
{
  "success": true,
  "domain": "sales",
  "criteria": "- **Focus**: Sales workflows\n- **Priority**: Lead conversion",
  "updated_at": "2026-06-24T12:35:01.000Z"
}

// Error Response (400 Bad Request):
{
  "success": false,
  "error": "Invalid domain: 'invalid'. Must be one of: manufacturing, sales, logistics, finance, quality, hr, all"
}
```

---

### API-003: Register routes in server.ts (30 min)

**File:** `spaceos-nexus/knowledge-service/src/server.ts`

**What to implement:**

1. Import the new `planningRoutes` module
2. Mount routes at `/api/planning` prefix
3. Ensure auth middleware applies to all routes
4. Verify no conflicts with existing endpoints

**Code pattern:**
```typescript
import { Router } from 'express';
import planningRoutes from './api/planningRoutes';

// In main server setup:
app.use('/api/planning', authMiddleware, planningRoutes);
```

---

## Testing Requirements

### Unit Tests (1-2 hours)

**File:** `spaceos-nexus/knowledge-service/src/__tests__/planningRoutes.test.ts` (CREATE NEW)

**Test cases:**

1. **GET endpoint:**
   - ✅ Valid request returns 200 with correct JSON structure
   - ✅ Missing auth token returns 401
   - ✅ Invalid auth token returns 401
   - ✅ Missing `domain-focus.md` returns 404

2. **PUT endpoint:**
   - ✅ Valid domain update returns 200 and persists to file
   - ✅ Invalid domain returns 400 with error message
   - ✅ XSS attempt `<script>alert('xss')</script>` gets stripped
   - ✅ Missing auth token returns 401
   - ✅ 11th request in 1 minute returns 429 (rate limit)
   - ✅ Empty request body returns 400

3. **File operations:**
   - ✅ File write is atomic (no partial writes on error)
   - ✅ File permissions are 0600

---

## Success Criteria

- ✅ Both endpoints implemented and return correct JSON
- ✅ Authentication required on both endpoints
- ✅ Domain validation working (whitelist + error message)
- ✅ XSS prevention (script tags stripped)
- ✅ Rate limiting implemented (10/min per IP)
- ✅ File operations atomic (temp + rename)
- ✅ All unit tests passing
- ✅ Code follows existing TypeScript patterns in codebase

---

## Important Notes

**File location:** `docs/planning/domain-focus.md`
- Current content: See the root `docs/planning/domain-focus.md` file
- YAML frontmatter contains `domain` field
- Markdown body contains criteria text

**Architecture reference:** See `/opt/spaceos/docs/tasks/new/Datahaven_UI_Focus_Flow_Editor_Architecture_v1.md` Section 3 for full context.

**Related Frontend task:** MSG-FRONTEND-046 will implement the UI for this API (parallel, no dependency)

---

## Definition of Done

- [ ] Both API endpoints implemented in `planningRoutes.ts`
- [ ] Registered in `server.ts` with auth middleware
- [ ] All validation and sanitization implemented
- [ ] Rate limiting working (test with 11+ requests)
- [ ] Unit tests created and passing (>80% coverage for new code)
- [ ] No TypeScript errors (`npm run build` succeeds)
- [ ] Manual test: GET returns current domain correctly
- [ ] Manual test: PUT updates file and returns 200
- [ ] Manual test: PUT with invalid domain returns 400
- [ ] Manual test: XSS attempt gets stripped
- [ ] Outbox message written when complete

---

## Notes for Implementation

- Use existing `readFile`/`writeFile` patterns from codebase (check other routes)
- For YAML parsing: `js-yaml` library (likely already available)
- For rate limiting: consider `express-rate-limit` or manual Map-based tracking
- For sanitization: `DOMPurify` or simple regex `/\<script\>/gi`

---

**Estimated completion:** 2-3 hours for API implementation + 1 hour testing = **3-4 hours total**

When complete, write your DONE message to `terminals/backend/outbox/YYYY-MM-DD_NNN_<slug>-done.md`
