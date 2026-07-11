---
id: MSG-FRONTEND-035
from: conductor
to: frontend
type: task
priority: high
status: READ
model: sonnet
ref: MSG-BACKEND-043
created: 2026-06-27
completed: 2026-06-27
content_hash: 684704e41cb45c07c055eb3c53e75fc5befce96a817280d12ff1fe0ac12aae2a
---

# Datahaven Focus Area Panel UI — Phase 1

## Task

Implement the **Focus Area Panel React component** for the Datahaven Planning page. This component displays and allows editing of the planning domain (manufacturing, sales, logistics, etc.).

## Scope

**Files**:
- `datahaven-web/public/planning.html` (modify)
- `datahaven-web/public/js/planning-focus.js` (NEW)
- `datahaven-web/public/css/planning.css` (modify)

**Location**: Planning page, **top panel** (above pipeline overview)

---

## UI Design

### Component Layout

```
┌─────────────────────────────────────────────────┐
│ Focus Area                          🔄 Sync   │
├─────────────────────────────────────────────────┤
│                                                 │
│ Domain:  [▼ sales             ]                │
│                                                 │
│ Criteria:                                       │
│ ┌─────────────────────────────────────────────┐│
│ │ - **Felhasználói érték**: Milyen funkció... ││
│ │ - **Backend kapcsolhatóság**: Van-e már... ││
│ │ - **Iparági minták**: Hogyan kezelik...   ││
│ │ - **Mobil első**: A csapatnak kell...      ││
│ │ - **Offline tűrés**: Ha az internet...     ││
│ └─────────────────────────────────────────────┘│
│                                                 │
│ [Edit Criteria] [Save Changes]                │
└─────────────────────────────────────────────────┘
```

---

## Implementation Details

### HTML Structure (planning.html)

Add this section AFTER the header, BEFORE the pipeline tabs:

```html
<div class="focus-area-panel" id="focusAreaPanel">
  <div class="focus-area-header">
    <h2>Focus Area</h2>
    <button id="syncButton" class="btn-sync" title="Sync from server">🔄 Sync</button>
  </div>

  <div class="focus-area-body">
    <div class="domain-selector">
      <label for="domainSelect">Domain:</label>
      <select id="domainSelect">
        <option value="manufacturing">Manufacturing</option>
        <option value="sales">Sales</option>
        <option value="logistics">Logistics</option>
        <option value="finance">Finance</option>
        <option value="quality">Quality</option>
        <option value="hr">HR</option>
        <option value="all">All</option>
      </select>
    </div>

    <div class="criteria-display" id="criteriaDisplay">
      <!-- Markdown rendered here -->
    </div>

    <div class="criteria-edit-mode" id="criteriaEditMode" style="display:none;">
      <textarea id="criteriaTextarea" placeholder="Edit criteria in markdown..."></textarea>
    </div>

    <div class="focus-area-actions">
      <button id="editButton" class="btn-edit">Edit Criteria</button>
      <button id="saveButton" class="btn-save" style="display:none;">Save Changes</button>
      <button id="cancelButton" class="btn-cancel" style="display:none;">Cancel</button>
    </div>
  </div>
</div>
```

### JavaScript (planning-focus.js)

**Functions to implement**:

1. **`initFocusArea()`** — Initialize on page load
   - Fetch GET /api/planning/domain-focus
   - Populate domain dropdown
   - Render criteria markdown → HTML (use `marked.js`)
   - Add event listeners

2. **`renderCriteria(markdown)`** — Convert markdown to HTML
   - Use `marked.js` library (already in use in `planning.html`)
   - Render as `<ul>` with markdown bold styling

3. **`onDomainChange()`** — When domain dropdown changes
   - Call `PUT /api/planning/domain-focus { domain: newDomain }`
   - Show success toast
   - Update display

4. **`onEditClick()`** — When Edit Criteria button clicked
   - Show textarea (hide display)
   - Show Save/Cancel buttons
   - Populate textarea with current criteria

5. **`onSaveClick()`** — When Save Changes clicked
   - Get textarea value
   - Call `PUT /api/planning/domain-focus { criteria: newCriteria }`
   - On success: hide textarea, update display, show toast
   - On error: show error toast, keep textarea visible

6. **`onCancelClick()`** — When Cancel clicked
   - Hide textarea
   - Hide Save/Cancel buttons
   - Restore previous criteria text

7. **`onSyncClick()`** — When Sync button clicked
   - Fetch latest from server
   - Re-render all fields
   - Show "Synced at 14:30" indicator

### API Integration

```javascript
// GET current domain/criteria
fetch('/api/planning/domain-focus', {
  headers: { 'Authorization': 'Bearer dev-token-spaceos-dashboard-2026' }
})
.then(r => r.json())
.then(data => {
  // { domain: "sales", criteria: "...", updated_at: "..." }
})

// PUT update
fetch('/api/planning/domain-focus', {
  method: 'PUT',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': 'Bearer dev-token-spaceos-dashboard-2026'
  },
  body: JSON.stringify({ domain: "manufacturing" })
})
.then(r => r.json())
.then(data => {
  // { success: true, domain: "manufacturing", ... }
})
```

### CSS Classes (planning.css)

Use the CSS from Architect's design document **section 6.2**.

**Key classes**:
- `.focus-area-panel` — Main container
- `.focus-area-header` — Header with title + sync button
- `.focus-area-body` — Content area
- `.domain-selector` — Domain dropdown section
- `.criteria-display` — Rendered criteria text
- `.criteria-edit-mode` — Textarea for editing
- `.focus-area-actions` — Button group
- `.btn-edit`, `.btn-save`, `.btn-cancel` — Buttons

**Color palette** (from existing styles.css):
- `--accent-blue: #1d9bf0` — Active/links
- `--bg-card: #242b33` — Card background
- `--text-primary: #e7e9ea` — Main text
- `--border-color: #2f3336` — Borders

---

## Dependencies

**Already available**:
- ✅ `marked.js` — Markdown rendering (already in planning.html)
- ✅ Toast notifications — Use existing `showToast()` if available

**If missing**:
- Add toast notification helper or use `alert()` for MVP

---

## Testing Checklist

- [ ] Page loads, Focus Area Panel renders
- [ ] Domain dropdown displays all 7 options
- [ ] GET /api/planning/domain-focus returns data correctly
- [ ] Criteria markdown renders as `<ul>` with proper formatting
- [ ] Edit button shows textarea with current criteria
- [ ] Save button calls PUT /api/planning/domain-focus
- [ ] Success toast shown after save
- [ ] Cancel button hides textarea without saving
- [ ] Sync button fetches latest data from server
- [ ] Responsive: Works on desktop (1024+), tablet (768+), mobile (<768)

---

## Component Placement Strategy

**Page**: `/planning.html`
**Position**: Top panel, ABOVE existing pipeline tabs
**Reason**: Domain directly affects planning pipeline algorithm

**Layout Hierarchy** (after implementation):
```
Planning Page
├── Header
├── ⭐ Focus Area Panel (NEW) ← This component
├── [Workflow] [Ideas] [Selected] [Debate] [Queue] [Logs] tabs
└── Tab content
```

---

## Dependencies

**Blocking**: Waiting for Backend MSG-BACKEND-043 API to be ready

**Blocked by**: None (can start immediately)

---

## Success Criteria

✅ Focus Area Panel renders on Planning page
✅ Domain dropdown changes are persisted
✅ Criteria edits are saved to server
✅ Markdown renders correctly
✅ Responsive on desktop/tablet/mobile
✅ Error handling (show toast on API failures)
✅ No console errors

---

## Background

**Related**: `docs/tasks/new/Datahaven_UI_Focus_Flow_Editor_Architecture_v1.md` (Approved)

**Specification**: Section 1 (Focus Area Panel UI Design)

**API Spec**: Section 5.1 (Planning Focus API — Backend task MSG-BACKEND-043)

---

## Estimate

5-7 days (including testing + responsive polish)

## Notes

- This is **Phase 1** of Datahaven UI expansion (Phase 2: Flow/Workflow Editor follows)
- Backend API (MSG-BACKEND-043) must be deployed first
- Success = Conductor can change planning domain in <5 clicks

---

**STATUS**: Ready for implementation
**BLOCKED BY**: MSG-BACKEND-043 (API endpoint not yet deployed)
**START DATE**: After Backend ready
**TARGET DATE**: 2026-07-04 (same as Backend)
