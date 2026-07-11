---
id: MSG-BACKEND-073
from: mcp-server
to: backend
type: task
priority: high
status: READ
created: 2026-06-24
model: sonnet
content_hash: 91d8a8d7dfb1693d1c356397f0069ba12c50f7a99f81fb18188bbfa4a9f70b63
---

## Datahaven UI — Focus Area Panel — Phase 1 Backend

**Architecture:** `/opt/spaceos/docs/tasks/new/Datahaven_UI_Focus_Flow_Editor_Architecture_v1.md` (sections 1-4, 5.1)

**Scope:** Implement backend API endpoints for the Focus Area Panel

### Tasks

#### 1. Create Planning Routes Module
- File: `spaceos-nexus/knowledge-service/src/api/planningRoutes.ts` (NEW)
- Implement `GET /api/planning/domain-focus` endpoint
- Read `docs/planning/domain-focus.md`
- Parse YAML frontmatter (domain field) + markdown body (criteria)
- Return JSON: `{ domain, criteria, updated_at }`

#### 2. Implement PUT Endpoint
- Endpoint: `PUT /api/planning/domain-focus`
- Request body: `{ domain?: string, criteria?: string }`
- Validation:
  - Domain must be in: `[manufacturing, sales, logistics, finance, quality, hr, all]`
  - Criteria must be valid markdown (strip HTML tags)
- Write atomically to `docs/planning/domain-focus.md`
- Use DOMPurify or sanitize-html to prevent XSS
- Return: `{ success: true, domain, criteria, updated_at }`

#### 3. Add Security
- Require `Authorization: Bearer dev-token-spaceos-dashboard-2026` on both endpoints
- Implement rate limiting: max 10 PUT requests/minute per IP
- Add error handling with proper HTTP status codes

#### 4. Add Tests
- Test `GET` endpoint (read parsing)
- Test `PUT` endpoint with valid/invalid domains
- Test XSS prevention (inject <script> tags in criteria)
- Test rate limiting

#### 5. Register Routes
- Mount routes in `src/server.ts` under `/api/planning/`
- Verify no conflicts with existing routes

### Acceptance Criteria

- ✅ Both endpoints respond with correct JSON format
- ✅ Domain validation prevents invalid values
- ✅ File writes are atomic (no partial writes)
- ✅ XSS attempts are blocked (sanitization)
- ✅ All tests pass
- ✅ No TypeScript errors

### Reference Files

- Architecture doc: Section 5.1 (API details)
- Section 1.4 (data flow)
- Section 4.1-4.2 (read/write flows)

### Notes

- Frontend will call these endpoints
- Domain changes should be reflected in next `plan-scan.sh` cycle
- Keep response format consistent with existing Datahaven API patterns

---

**Estimate:** 5-7 days  
**Phase:** 1/3 (Focus Area Panel backend complete)"
