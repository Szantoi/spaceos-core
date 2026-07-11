---
created: 2026-06-24
selected_by: architect
status: ready_for_dispatch
epic: EPIC-DATAHAVEN-UI
domain_focus: all
phase: 1
estimate_days: 5-7
depends_on: []
parallel_with: []
ref: Datahaven_UI_Focus_Flow_Editor_Architecture_v1.md
---

# SpaceOS Planning — Focus Area Panel Implementation

## Epic: EPIC-DATAHAVEN-UI
## Phase: 1 of 3 (Focus Area Panel)

---

## Executive Summary

A **Focus Area Panel** új UI komponens a Datahaven Dashboard Planning oldalán, amely lehetővé teszi:
- A tervezési domain kiválasztását (7 opció: manufacturing, sales, logistics, finance, quality, hr, all)
- A domain criteria szerkesztését (markdown formátum)
- A `docs/planning/domain-focus.md` fájl vizuális kezelését

**Elhelyezés:** Planning page, top panel (a Pipeline Overview felett)
**Tech stack:** Node.js API + Vanilla JS frontend

---

## Backend Tasks (Backend Terminal)

### API-001: GET /api/planning/domain-focus

**File:** `spaceos-nexus/knowledge-service/src/api/planningRoutes.ts` (NEW)

**Subtasks:**
- [ ] Create `planningRoutes.ts` file
- [ ] Implement GET endpoint: read `docs/planning/domain-focus.md`
- [ ] Parse YAML frontmatter (domain field)
- [ ] Parse markdown body (criteria)
- [ ] Return JSON: `{ domain, criteria, updated_at }`
- [ ] Add authentication middleware (bearer token)
- [ ] Add unit tests

**Estimate:** 2-3 hours

**Request/Response:**
```typescript
// GET /api/planning/domain-focus
// Response:
{
  "domain": "manufacturing",
  "criteria": "- **Felhasználói érték**: ...",
  "updated_at": "2026-06-24T12:34:56Z"
}
```

---

### API-002: PUT /api/planning/domain-focus

**Subtasks:**
- [ ] Implement PUT endpoint: validate + write `docs/planning/domain-focus.md`
- [ ] Domain validation: must be in `[manufacturing, sales, logistics, finance, quality, hr, all]`
- [ ] Markdown sanitization: strip `<script>` tags (DOMPurify or regex)
- [ ] Atomic file write: temp file + rename pattern
- [ ] Rate limiting: max 10 writes/minute per IP
- [ ] Add unit tests

**Estimate:** 2-3 hours

**Request/Response:**
```typescript
// PUT /api/planning/domain-focus
// Request:
{
  "domain": "sales",           // Optional
  "criteria": "- New criteria" // Optional
}

// Response:
{
  "success": true,
  "domain": "sales",
  "criteria": "- New criteria",
  "updated_at": "2026-06-24T12:35:01Z"
}
```

---

### API-003: Register routes in server.ts

**Subtasks:**
- [ ] Import `planningRoutes` in `server.ts`
- [ ] Mount at `/api/planning`
- [ ] Verify auth middleware applies

**Estimate:** 30 minutes

---

## Frontend Tasks (Frontend Terminal)

### UI-001: Focus Area Panel HTML Structure

**File:** `datahaven-web/public/planning.html`

**Subtasks:**
- [ ] Add `.focus-area-panel` div above Pipeline Overview
- [ ] Domain dropdown: 7 options
- [ ] Criteria display div (markdown rendered)
- [ ] Edit button + Save button
- [ ] Sync indicator (last updated time)

**HTML Structure:**
```html
<div class="focus-area-panel">
  <div class="focus-area-header">
    <span>Focus Area</span>
    <button class="btn-sync">🔄 Sync</button>
  </div>
  <div class="focus-area-body">
    <div class="domain-selector">
      <label>Domain:</label>
      <select id="domain-select">
        <option value="manufacturing">Manufacturing</option>
        <option value="sales">Sales</option>
        <!-- ... -->
      </select>
    </div>
    <div class="criteria-display" id="criteria-display"></div>
    <div class="criteria-edit-mode" id="criteria-edit" style="display:none">
      <textarea id="criteria-textarea"></textarea>
    </div>
    <div class="focus-area-actions">
      <button class="btn-edit" id="btn-edit">Edit Criteria</button>
      <button class="btn-save" id="btn-save" style="display:none">Save Changes</button>
    </div>
  </div>
</div>
```

**Estimate:** 1-2 hours

---

### UI-002: Focus Area JavaScript Logic

**File:** `datahaven-web/public/js/planning.js` (extend existing or create `planning-focus.js`)

**Subtasks:**
- [ ] `loadFocusArea()`: GET /api/planning/domain-focus → render
- [ ] `renderCriteria(markdown)`: convert markdown to HTML (use marked.js)
- [ ] Domain dropdown onChange → PUT API call
- [ ] Edit button click → show textarea, hide display
- [ ] Save button click → PUT API call → hide textarea, show display
- [ ] Sync button click → GET API call → re-render
- [ ] Toast notification on save success/error

**Estimate:** 2-3 hours

---

### UI-003: Focus Area CSS Styles

**File:** `datahaven-web/public/css/planning.css`

**Subtasks:**
- [ ] `.focus-area-panel` card styling (bg-card, border-radius, border)
- [ ] `.domain-selector` flex layout
- [ ] `.criteria-display` scrollable container (max-height: 300px)
- [ ] `.criteria-edit-mode textarea` styling
- [ ] `.btn-edit`, `.btn-save` button styles
- [ ] Responsive breakpoints (mobile: 768px)

**Estimate:** 1-2 hours

---

### UI-004: Include marked.js Library

**Subtasks:**
- [ ] Add marked.js CDN link to planning.html
- [ ] Or: npm install + bundle

**Estimate:** 30 minutes

---

## Testing Tasks

### TEST-001: Backend API Tests

**File:** `spaceos-nexus/knowledge-service/src/__tests__/planningRoutes.test.ts`

**Subtasks:**
- [ ] Test GET returns valid JSON
- [ ] Test PUT with valid domain updates file
- [ ] Test PUT with invalid domain returns 400
- [ ] Test PUT without auth returns 401
- [ ] Test rate limiting (11th request in 1 minute returns 429)

**Estimate:** 1-2 hours

---

### TEST-002: Frontend Integration Test

**Subtasks:**
- [ ] Manual test: load page → see current domain + criteria
- [ ] Manual test: change domain dropdown → verify file updated
- [ ] Manual test: edit criteria → save → verify persisted
- [ ] XSS test: inject `<script>alert(1)</script>` in criteria → verify stripped

**Estimate:** 1 hour

---

## Success Criteria

- ✅ Conductor can change planning domain in <5 clicks
- ✅ Domain change reflects in next `plan-scan.sh` run
- ✅ Criteria edits persist across page reloads
- ✅ No XSS vulnerabilities (pass security scan)
- ✅ Page load time <1.5 seconds

---

## Dependencies

- **Backend first:** API endpoints must be ready before frontend integration
- **marked.js:** Required for markdown rendering

---

## Estimated Timeline

| Task | Owner | Estimate |
|------|-------|----------|
| API-001, API-002, API-003 | Backend | 1 day |
| TEST-001 | Backend | 0.5 day |
| UI-001, UI-002, UI-003, UI-004 | Frontend | 2 days |
| TEST-002 | Frontend | 0.5 day |
| Buffer + integration | Both | 1-2 days |
| **Total** | | **5-7 days** |

---

## Security Checklist

- ✅ Authentication required (bearer token)
- ✅ Input validation (domain whitelist)
- ✅ Markdown sanitization (XSS prevention)
- ✅ Rate limiting (10 writes/min)
- ✅ Atomic file operations

---

**END OF PLAN**
