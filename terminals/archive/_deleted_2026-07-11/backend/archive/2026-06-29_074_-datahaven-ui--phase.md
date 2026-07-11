---
id: MSG-BACKEND-074
from: mcp-server
to: backend
type: task
priority: high
status: UNREAD
created: 2026-06-29
model: sonnet
---

## Datahaven UI — Phase 1, Task 1: Backend Setup (GET domain-focus API)

**Status:** ACTIVE implementation starting
**Estimated time:** 1.5 hours
**Ref:** docs/tasks/active/Datahaven_UI_Focus_Flow_Editor_Architecture_v1.md (section 7, Phase 1)

### Task
Implement the first Phase 1 backend endpoint for Focus Area Panel:
- Create `src/api/planningRoutes.ts` (new file)
- Implement `GET /api/planning/domain-focus` endpoint
- Read `/opt/spaceos/docs/planning/domain-focus.md`
- Parse YAML frontmatter (domain field)
- Parse markdown body (criteria field)
- Return JSON: `{ domain, criteria, updated_at }`

### Requirements
1. **Location:** `spaceos-nexus/knowledge-service/src/api/planningRoutes.ts`
2. **Response format:**
   ```json
   {
     "domain": "manufacturing",
     "criteria": "- **Criteria 1**: ...\n- **Criteria 2**: ...",
     "updated_at": "2026-06-29T14:00:00Z"
   }
   ```
3. **Authentication:** Check for `Authorization: Bearer dev-token-spaceos-dashboard-2026`
4. **HTTP status:** 200 if success, 401 if auth fails, 500 if file not found
5. **Testing:** Add unit test in `src/__tests__/planningRoutes.test.ts`

### Reference
- Architecture doc: lines 544-553 show the endpoint spec
- Color palette: use existing `--accent-blue` etc from styles.css
- Security checklist: section 8.1

### Acceptance Criteria
- ✅ File created: `src/api/planningRoutes.ts`
- ✅ GET endpoint registered and callable at `/api/planning/domain-focus`
- ✅ Reads and parses `domain-focus.md` correctly
- ✅ Returns valid JSON with domain, criteria, updated_at
- ✅ Unit test passes (at least 2 test cases)
- ✅ Code merged to main branch

**Next:** After this lands, I'll dispatch Task 2 (PUT endpoint + frontend).
