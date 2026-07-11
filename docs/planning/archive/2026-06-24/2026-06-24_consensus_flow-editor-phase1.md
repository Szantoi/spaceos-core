---
created: 2026-06-24
selected_by: architect
status: ready_for_dispatch
epic: EPIC-DATAHAVEN-UI
domain_focus: all
phase: 2
estimate_days: 10-14
depends_on: []
parallel_with: [EPIC-CUTTING-Q3]
ref: Datahaven_UI_Focus_Flow_Editor_Architecture_v1.md
---

# SpaceOS Planning — Flow/Workflow Editor Phase 1

## Epic: EPIC-DATAHAVEN-UI
## Phase: 2 of 3 (Flow Editor — Mermaid Visualization)

---

## Executive Summary

A **Flow/Workflow Editor** új UI komponens a Datahaven Dashboard Planning oldalán, amely lehetővé teszi:
- Az EPICS.yaml dependency gráf vizualizációját (Mermaid diagram)
- Epic részletek megtekintését (name, status, dependencies)
- Epic státusz módosítását (pending → active → done → blocked)
- Dependency-k hozzáadását/eltávolítását (cycle detection-nel)

**Elhelyezés:** Planning page, Workflow tab (jelenleg üres placeholder)
**Tech stack:** Node.js API + Mermaid.js + Vanilla JS frontend
**Desktop only:** Mobil/tablet "Desktop required" üzenetet kap

---

## Backend Tasks (Backend Terminal)

### API-001: PUT /api/graph/epics/:id

**File:** `spaceos-nexus/knowledge-service/src/api/graphRoutes.ts` (EXTEND)

**Subtasks:**
- [ ] Add new PUT route for `/epics/:id`
- [ ] Load EPICS.yaml from `docs/projects/EPICS.yaml`
- [ ] Find epic by ID (return 404 if not found)
- [ ] Validate status transition (state machine)
- [ ] Validate dependencies (cycle detection using `detectCycles()`)
- [ ] If setting `done`: verify all `depends_on` epics are `done`
- [ ] Update epic fields (status, depends_on, parallel_with, target_date)
- [ ] Write EPICS.yaml atomically (temp file + rename)
- [ ] Invalidate graph cache (`clearEpicGraphCache()`)
- [ ] Return updated epic + validation result

**Estimate:** 4-6 hours

**Status Transition Rules:**
```
pending → active ✅
active → done ✅
active → blocked ✅
blocked → active ✅ (retry)
done → pending ❌
done → active ❌
```

**Request/Response:**
```typescript
// PUT /api/graph/epics/EPIC-CUTTING-Q3
// Request:
{
  "status": "done",                     // Optional
  "depends_on": ["EPIC-KERNEL-STABLE"], // Optional
  "parallel_with": ["EPIC-JOINERY-V2"], // Optional
  "target_date": "2026-09-30"           // Optional
}

// Response:
{
  "success": true,
  "epic": {
    "id": "EPIC-CUTTING-Q3",
    "name": "Cutting Module Q3",
    "status": "done",
    "depends_on": ["EPIC-KERNEL-STABLE"],
    // ...
  },
  "validation": {
    "valid": true,
    "cycles": []
  }
}
```

---

### API-002: Status Transition Validator

**Subtasks:**
- [ ] Create `isValidStatusTransition(currentStatus, newStatus)` function
- [ ] Implement state machine logic
- [ ] Return boolean + error message if invalid

**Estimate:** 1 hour

---

### API-003: Dependency Validator (Cycle Detection)

**Subtasks:**
- [ ] Use existing `detectCycles()` from `graph/operations.ts`
- [ ] Build test graph with proposed changes
- [ ] Return cycle paths if detected

**Estimate:** 1 hour

---

### API-004: EPICS.yaml Atomic Write Helper

**Subtasks:**
- [ ] Create `writeEpicsYaml(path, data)` utility
- [ ] Write to temp file first
- [ ] Rename temp file to target (atomic on POSIX)
- [ ] Handle errors gracefully

**Estimate:** 1 hour

---

## Frontend Tasks (Frontend Terminal)

### UI-001: Workflow Tab Content

**File:** `datahaven-web/public/planning.html`

**Subtasks:**
- [ ] Replace Workflow tab placeholder with `.workflow-editor` container
- [ ] Add `.mermaid-container` div (graph canvas)
- [ ] Add `.epic-details-panel` div (collapsible)
- [ ] Add toolbar: [Export Mermaid] [Validate] [Refresh]
- [ ] Add mobile message div (hidden on desktop)

**HTML Structure:**
```html
<div id="workflow-content" class="tab-content">
  <!-- Desktop only -->
  <div class="workflow-editor">
    <div class="workflow-toolbar">
      <button id="btn-export-mermaid">Export Mermaid</button>
      <button id="btn-validate">Validate</button>
      <button id="btn-refresh-graph">🔄 Refresh</button>
    </div>
    <div class="mermaid-container">
      <div id="mermaid-canvas"></div>
    </div>
    <div class="epic-details-panel" id="epic-details" style="display:none">
      <!-- Populated by JS -->
    </div>
  </div>
  <!-- Mobile fallback -->
  <div class="workflow-editor-mobile-message">
    Workflow editor requires desktop screen (min 1024px width)
  </div>
</div>
```

**Estimate:** 1-2 hours

---

### UI-002: Include Mermaid.js Library

**Subtasks:**
- [ ] Add Mermaid.js CDN link to planning.html
- [ ] Configure Mermaid theme (dark mode compatible)
- [ ] Initialize on page load

**CDN:**
```html
<script src="https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.min.js"></script>
<script>
  mermaid.initialize({
    startOnLoad: false,
    theme: 'dark',
    securityLevel: 'loose' // Allow click callbacks
  });
</script>
```

**Estimate:** 30 minutes

---

### UI-003: Graph Loading & Rendering

**File:** `datahaven-web/public/js/planning-workflow.js` (NEW)

**Subtasks:**
- [ ] `loadWorkflowGraph()`: GET /api/graph/mermaid/epic/EPICS
- [ ] Insert Mermaid syntax into `#mermaid-canvas`
- [ ] Call `mermaid.init()` to render
- [ ] Add loading spinner during fetch
- [ ] Handle errors (show error message)

**Estimate:** 2-3 hours

---

### UI-004: Node Click Handlers

**Subtasks:**
- [ ] Register Mermaid click callbacks for each node
- [ ] On node click: highlight node (add `.selected` class)
- [ ] On node click: fetch epic details (GET /api/graph/epics or from cached data)
- [ ] Populate epic details panel

**Mermaid Click Syntax:**
```mermaid
click EPIC-CUTTING-Q3 call selectEpic("EPIC-CUTTING-Q3")
```

**Estimate:** 2-3 hours

---

### UI-005: Epic Details Panel

**Subtasks:**
- [ ] Create `renderEpicDetails(epic)` function
- [ ] Display: name, status, target_date, description
- [ ] Status dropdown (editable)
- [ ] Dependencies list with status badges
- [ ] [+ Add Dependency] button (opens modal)
- [ ] [Save Changes] button

**HTML Template:**
```html
<div class="epic-details-panel">
  <h3>EPIC-CUTTING-Q3</h3>
  <div class="epic-detail-row">
    <span class="label">Name:</span>
    <span class="value">Cutting Module Q3</span>
  </div>
  <div class="epic-detail-row">
    <span class="label">Status:</span>
    <select id="status-select">
      <option value="pending">Pending</option>
      <option value="active" selected>Active</option>
      <option value="done">Done</option>
      <option value="blocked">Blocked</option>
    </select>
  </div>
  <!-- Dependencies list -->
  <div class="epic-dependencies">
    <h4>Dependencies:</h4>
    <ul class="epic-dependencies-list">
      <li>EPIC-KERNEL-STABLE <span class="dep-badge done">done</span></li>
    </ul>
    <button class="btn-add-dependency">[+ Add Dependency]</button>
  </div>
  <button class="btn-save" id="btn-save-epic">Save Changes</button>
</div>
```

**Estimate:** 3-4 hours

---

### UI-006: Status Change Handler

**Subtasks:**
- [ ] Status dropdown onChange → PUT /api/graph/epics/:id
- [ ] Show loading indicator
- [ ] On success: re-render graph (colors update)
- [ ] On error: show toast with validation message
- [ ] Optimistic UI: update node color immediately, rollback on error

**Estimate:** 2-3 hours

---

### UI-007: Add Dependency Modal

**Subtasks:**
- [ ] Create modal HTML (hidden by default)
- [ ] Dropdown with available epics (filter out self + existing deps)
- [ ] [Add] button → PUT /api/graph/epics/:id with updated depends_on
- [ ] Handle cycle detection error (show message)
- [ ] On success: re-render graph + details panel

**Estimate:** 2-3 hours

---

### UI-008: Workflow CSS Styles

**File:** `datahaven-web/public/css/planning.css` (extend)

**Subtasks:**
- [ ] `.workflow-editor` flex layout
- [ ] `.mermaid-container` styling (background, overflow, min-height)
- [ ] Mermaid node color overrides (pending/active/done/blocked)
- [ ] `.selected` node highlight effect
- [ ] `.epic-details-panel` styling
- [ ] `.dep-badge` status color badges
- [ ] Responsive: hide editor on mobile, show message

**Estimate:** 2-3 hours

---

### UI-009: Export Mermaid Button

**Subtasks:**
- [ ] [Export Mermaid] click → GET /api/graph/mermaid/epic/EPICS
- [ ] Create downloadable file (mermaid content)
- [ ] Trigger browser download as `EPICS.mmd`

**Estimate:** 1 hour

---

## Testing Tasks

### TEST-001: Backend API Tests

**File:** `spaceos-nexus/knowledge-service/src/__tests__/graphRoutes.put.test.ts`

**Subtasks:**
- [ ] Test PUT with valid status transition
- [ ] Test PUT with invalid status transition returns 400
- [ ] Test PUT with cycle-creating dependency returns 400 with cycles array
- [ ] Test PUT on done → done (no-op, success)
- [ ] Test PUT with unknown epic ID returns 404

**Estimate:** 2-3 hours

---

### TEST-002: Cycle Detection Tests

**Subtasks:**
- [ ] Test simple cycle (A → B → A)
- [ ] Test complex cycle (A → B → C → A)
- [ ] Test valid DAG (no cycles)

**Estimate:** 1 hour

---

### TEST-003: Frontend Integration Test

**Subtasks:**
- [ ] Manual test: load Workflow tab → see Mermaid graph
- [ ] Manual test: click node → see details panel
- [ ] Manual test: change status → graph updates colors
- [ ] Manual test: add dependency → graph re-renders
- [ ] Manual test: attempt cycle → error message shown

**Estimate:** 1-2 hours

---

## Success Criteria

- ✅ Epic graph renders in <2 seconds
- ✅ User can change epic status in <3 clicks
- ✅ Dependency changes are validated (no cycles allowed)
- ✅ Graph updates reflect immediately
- ✅ Mobile users see helpful message (not broken layout)

---

## Dependencies

- **Focus Area Panel (Phase 1):** Independent, can run in parallel
- **Mermaid.js:** Required for rendering
- **Existing Graph API:** GET /api/graph/epics, GET /api/graph/mermaid already exist

---

## Estimated Timeline

| Task | Owner | Estimate |
|------|-------|----------|
| API-001 to API-004 | Backend | 2-3 days |
| TEST-001, TEST-002 | Backend | 1 day |
| UI-001 to UI-009 | Frontend | 5-7 days |
| TEST-003 | Frontend | 1 day |
| Buffer + integration | Both | 1-2 days |
| **Total** | | **10-14 days** |

---

## Security Checklist

- ✅ Authentication required (bearer token)
- ✅ Input validation (status transitions, epic IDs)
- ✅ Cycle detection (prevent invalid DAGs)
- ✅ Atomic file operations (EPICS.yaml)
- ✅ Cache invalidation (no stale data)

---

## Risk Mitigation

- **Large graphs (50+ epics):** Add zoom-to-fit button, consider lazy loading
- **Concurrent edits:** File locking not implemented in Phase 1 (single-user assumption)
- **Mermaid rendering bugs:** Pin Mermaid.js version, test with complex graphs

---

**END OF PLAN**
