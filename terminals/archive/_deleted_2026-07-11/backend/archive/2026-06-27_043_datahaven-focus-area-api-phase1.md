---
id: MSG-BACKEND-043
from: conductor
to: backend
type: task
priority: high
status: UNREAD
model: sonnet
ref: MSG-ARCHITECT-010
created: 2026-06-27
---

# Datahaven Focus Area Panel API — Phase 1

## Task

Implement the **Focus Area Panel REST API** for the Datahaven Dashboard. This API manages the planning domain (manufacturing, sales, logistics, etc.) that affects the planning pipeline.

## Scope

**File**: Create `spaceos-nexus/knowledge-service/src/api/planningRoutes.ts` (NEW)

**Endpoints**:
```
GET  /api/planning/domain-focus
PUT  /api/planning/domain-focus
```

## Specification

### GET /api/planning/domain-focus

**Purpose**: Fetch current planning domain and criteria

**Response** (200 OK):
```json
{
  "domain": "sales",
  "criteria": "- **Felhasználói érték**: ...\n- **Backend kapcsolhatóság**: ...",
  "updated_at": "2026-06-27T14:30:00Z"
}
```

**Source**: Read from `/opt/spaceos/docs/planning/domain-focus.md` (YAML frontmatter + markdown body)

**Caching**: Yes, cache in memory (invalidate on PUT)

---

### PUT /api/planning/domain-focus

**Purpose**: Update domain and/or criteria

**Request Body**:
```json
{
  "domain": "manufacturing",    // Optional
  "criteria": "- New criteria"  // Optional
}
```

**Response** (200 OK):
```json
{
  "success": true,
  "domain": "manufacturing",
  "criteria": "- New criteria",
  "updated_at": "2026-06-27T14:31:00Z"
}
```

**Validation Rules**:
- `domain` must be one of: `manufacturing`, `sales`, `logistics`, `finance`, `quality`, `hr`, `all`
- `criteria` must be markdown (no HTML tags allowed)

**Security** (CRITICAL):
- **Authentication**: Require `Authorization: Bearer dev-token-spaceos-dashboard-2026`
- **Markdown Sanitization**: Strip HTML tags using `DOMPurify` (import `isomorphic-dompurify`)
- **Rate Limiting**: Max 10 writes/minute per IP address
- **File Write**: Use atomic write pattern (temp file + rename)

**File Write**:
- Parse existing YAML frontmatter from `docs/planning/domain-focus.md`
- Update `domain` and/or `criteria` fields
- Update `updated_at` timestamp
- Write atomically (no partial writes)

---

## Implementation Checklist

### Code
- [ ] Create `src/api/planningRoutes.ts`
- [ ] Implement `router.get('/domain-focus', ...)`
- [ ] Implement `router.put('/domain-focus', ...)`
- [ ] Import `isomorphic-dompurify` for sanitization
- [ ] Implement YAML frontmatter parser (use `js-yaml`)
- [ ] Implement atomic file write helper
- [ ] Add authentication middleware (check bearer token)

### Testing
- [ ] Unit test: GET returns correct structure
- [ ] Unit test: PUT validates domain enum
- [ ] Unit test: PUT sanitizes HTML tags (XSS prevention)
- [ ] Unit test: PUT writes file atomically
- [ ] Integration test: Rate limiting works (11th request in 1 minute → 429)
- [ ] Security test: XSS payload `<script>alert(1)</script>` is removed

### Documentation
- [ ] Add endpoint to API contract (`docs/knowledge/API_CONTRACT_CATALOGUE.md`)
- [ ] Add to planning routes swagger/API docs

---

## Background

**Datahaven Dashboard** is the central UI for SpaceOS agent infrastructure. The Focus Area Panel allows the Conductor to select the planning domain (which affects the planning pipeline algorithm).

**Related Architecture**: `docs/tasks/new/Datahaven_UI_Focus_Flow_Editor_Architecture_v1.md` (Approved, sections 1, 5.1, 8.1)

**Approval Status**: ✅ Approved by Conductor (2026-06-27)

---

## Success Criteria

✅ GET /api/planning/domain-focus returns valid JSON
✅ PUT /api/planning/domain-focus validates domain
✅ Markdown sanitization removes all HTML tags
✅ Rate limiting blocks >10 writes/minute
✅ File writes are atomic (no corrupted state)
✅ All tests pass (unit + integration)

---

## Estimate

5-7 days (including testing + deployment)

## Notes

- **XSS Priority**: Sanitization is CRITICAL, not optional (security incident found during review)
- **Blocking**: Frontend is blocked until this API is ready
- **Phase 2**: Flow/Workflow Editor API will follow similar pattern

---

**STATUS**: Ready for implementation
**START DATE**: 2026-06-27
**TARGET DATE**: 2026-07-04
