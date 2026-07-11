---
id: MSG-BACKEND-075
from: mcp-server
to: backend
type: task
priority: high
status: UNREAD
created: 2026-06-29
model: sonnet
---

## Datahaven UI — Phase 1, Task 2: Backend PUT domain-focus API + Sanitization

**Status:** ACTIVE (after Task 1 completes)
**Estimated time:** 1.5 hours
**Depends on:** MSG-BACKEND-074 (GET endpoint)
**Ref:** docs/tasks/active/Datahaven_UI_Focus_Flow_Editor_Architecture_v1.md (section 7, Phase 1)

### Task
Implement the second Phase 1 backend endpoint: PUT /api/planning/domain-focus

This endpoint allows the Conductor to:
1. Change the active planning domain (7 options: manufacturing, sales, logistics, finance, quality, hr, all)
2. Edit the criteria markdown text
3. Save changes atomically to `domain-focus.md`

### Requirements

**Endpoint spec:**
```
PUT /api/planning/domain-focus
Content-Type: application/json
Authorization: Bearer dev-token-spaceos-dashboard-2026

Request body:
{
  "domain": "sales",          // Optional
  "criteria": "- **New criteria**: ..."  // Optional
}

Response (200 OK):
{
  "success": true,
  "domain": "sales",
  "criteria": "- **New criteria**: ...",
  "updated_at": "2026-06-29T14:15:30Z"
}

Errors:
- 400: Invalid domain (not in 7-option list)
- 400: Invalid criteria (contains script tags after sanitization)
- 401: Missing/invalid auth token
- 500: File write failed
```

**Validation Rules:**
1. Domain must be in list: `["manufacturing", "sales", "logistics", "finance", "quality", "hr", "all"]`
2. At least one of {domain, criteria} must be provided
3. Criteria must be sanitized markdown (no HTML/script tags)
4. Write must be atomic (use temp file + rename pattern)

**Sanitization Strategy:**
- Use `DOMPurify` library (npm available)
- Strip all HTML tags: `<script>`, `<iframe>`, `onclick`, `onerror`
- Keep markdown formatting: `**bold**, _italic_, - bullet lists, [links](url)`
- Test: malicious input like `criteria: "<script>alert('xss')</script>"` should be stripped

**Rate Limiting:**
- Max 10 writes per minute per IP
- Return 429 (Too Many Requests) if exceeded

### Implementation Checklist
1. Add `PUT` handler to `planningRoutes.ts`
2. Extract domain-focus.md path to constant
3. Implement domain validation function
4. Add markdown sanitization with DOMPurify
5. Implement atomic write (temp → rename)
6. Add rate limiting middleware
7. Add comprehensive error handling
8. Write unit tests (success + all error cases)

### File Structure
```
src/api/planningRoutes.ts
├── GET /domain-focus (from Task 1)
├── PUT /domain-focus (NEW)
├── Helpers:
│   ├── validateDomain(domain)
│   ├── sanitizeMarkdown(criteria)
│   └── writeFileAtomic(path, content)
```

### Security Checklist (Section 8.1)
- ✅ Authentication required on all endpoints
- ✅ Input validation: domain in whitelist
- ✅ Markdown sanitization: DOMPurify + strip tags
- ✅ Rate limiting: 10/min per IP
- ✅ Atomic write: temp file + rename

### Acceptance Criteria
- ✅ PUT endpoint created and callable
- ✅ Domain validation working (rejects invalid values)
- ✅ Criteria sanitization removes all dangerous content
- ✅ File writes atomically (no corruption on interrupt)
- ✅ Rate limiting returns 429 when exceeded
- ✅ All error cases return proper HTTP status codes
- ✅ Unit tests pass (minimum 5 test cases)
- ✅ Code merged to main

**Next Step:** After this lands, dispatch Frontend Task 1 (HTML + JS).
