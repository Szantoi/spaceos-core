---
id: MSG-BACKEND-045
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: Datahaven_UI_Focus_Flow_Editor_Architecture_v1
created: 2026-06-24
processed: 2026-06-24
duplicate_of: MSG-BACKEND-043
content_hash: 3e9ee2e7fe3071ae542566ac6d72110a0154bc1e71597c8df890b099be4c7a41
---

# Implement GET /api/planning/domain-focus endpoint

## Context

The Datahaven UI needs a Focus Area Panel to display and edit the planning domain configuration (`docs/planning/domain-focus.md`). This task implements the read endpoint.

**Architecture Doc:** `/opt/spaceos/docs/tasks/new/Datahaven_UI_Focus_Flow_Editor_Architecture_v1.md` (sections 1.4, 5.1)

## Task (<2h)

Create the GET endpoint that reads the domain-focus.md file and returns JSON.

### Implementation Steps

1. **Create API route file** (if not exists):
   - File: `spaceos-nexus/knowledge-service/src/api/planningRoutes.ts`
   - Export Express router

2. **Implement GET /api/planning/domain-focus**:
   ```typescript
   // Read /opt/spaceos/docs/planning/domain-focus.md
   // Parse YAML frontmatter (extract "domain" field)
   // Parse markdown body (criteria list)
   // Return JSON: { domain, criteria, updated_at }
   ```

3. **Register route** in `src/server.ts`:
   ```typescript
   import planningRoutes from './api/planningRoutes';
   app.use('/api/planning', planningRoutes);
   ```

4. **Test manually**:
   ```bash
   curl -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
     http://localhost:3456/api/planning/domain-focus
   ```

### Expected Response Format

```json
{
  "domain": "manufacturing",
  "criteria": "- **Felhasználói érték**: Milyen funkció...\n- **Backend kapcsolhatóság**: ...",
  "updated_at": "2026-06-24T10:30:00Z"
}
```

### Requirements

- ✅ Read file at `/opt/spaceos/docs/planning/domain-focus.md`
- ✅ Parse YAML frontmatter (use `gray-matter` or similar)
- ✅ Extract markdown body as-is (raw string)
- ✅ Return file mtime as `updated_at` (ISO 8601 format)
- ✅ Return 404 if file doesn't exist
- ✅ Return 500 on read errors with error message

### Security

- Authentication: Require `Authorization: Bearer dev-token-spaceos-dashboard-2026`
- Use existing auth middleware (if available in knowledge-service)
- No input validation needed (GET endpoint, no user input)

### Acceptance Criteria

- [ ] Endpoint responds at `GET /api/planning/domain-focus`
- [ ] Returns JSON with `domain`, `criteria`, `updated_at` fields
- [ ] Authentication required (401 if missing token)
- [ ] Manual curl test passes

### File Locations

- **Target file to read**: `/opt/spaceos/docs/planning/domain-focus.md`
- **API route**: `spaceos-nexus/knowledge-service/src/api/planningRoutes.ts`
- **Server registration**: `spaceos-nexus/knowledge-service/src/server.ts`

## Estimate

1-2 hours (simple file read + JSON return)

## Next Task

After this is done and tested, the next task will be implementing the PUT endpoint for writes.
