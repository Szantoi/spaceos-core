---
id: MSG-FRONTEND-049
from: conductor
to: frontend
type: task
priority: high
status: READ
model: sonnet
ref: docs/planning/queue/2026-06-24_consensus_flow-editor-phase1.md
epic: EPIC-DATAHAVEN-UI
created: 2026-06-24
content_hash: 4f8d0384f110c8193226664ad61a872b66e82c0ab8cc8ca395b9c9fc7c23079c
---

# Flow/Workflow Editor — Frontend UI Implementation

## Epic: EPIC-DATAHAVEN-UI Phase 2 (Flow Editor)

Implement the Flow/Workflow Editor UI component on the Datahaven Planning page. This visualizes the epic dependency graph using Mermaid.js and allows users to manage epics interactively.

---

## Context

**Reference plan:** `/opt/spaceos/docs/planning/queue/2026-06-24_consensus_flow-editor-phase1.md`

**Location:** Datahaven Planning page (`/planning`), Workflow tab (currently empty placeholder)

**Tech stack:**
- Mermaid.js for graph visualization (CDN)
- Vanilla JavaScript (no React/Vue)
- Existing Planning page CSS framework

**Desktop only:** Mobile/tablet users see "Desktop required" message

---

## Dependencies

⚠️ **Backend API must be ready first!** Coordinate with Backend terminal (MSG-BACKEND-046).

The following API endpoints are required:
- GET `/api/graph/epics` (already exists)
- GET `/api/graph/mermaid/epic/EPICS` (already exists)
- PUT `/api/graph/epics/:id` (Backend implementing now)

---

## Tasks

### UI-001: Workflow Tab HTML Structure

**File:** `datahaven-web/public/planning.html`

**Requirements:**
- Replace Workflow tab placeholder with `.workflow-editor` container
- Add toolbar: [Export Mermaid] [Validate] [Refresh] buttons
- Add `.mermaid-container` div for graph canvas
- Add `.epic-details-panel` div (collapsible sidebar)
- Add mobile fallback message (hidden on desktop)

**HTML Template:**
```html
<div id="workflow-content" class="tab-content">
  <!-- Desktop only -->
  <div class="workflow-editor">
    <div class="workflow-toolbar">
      <button id="btn-export-mermaid" class="btn-secondary">
        📥 Export Mermaid
      </button>
      <button id="btn-validate" class="btn-secondary">
        ✓ Validate
      </button>
      <button id="btn-refresh-graph" class="btn-primary">
        🔄 Refresh
      </button>
    </div>
    <div class="mermaid-container">
      <div id="mermaid-canvas" class="mermaid-loading">
        Loading graph...
      </div>
    </div>
    <div class="epic-details-panel" id="epic-details" style="display:none">
      <!-- Populated by JS -->
    </div>
  </div>
  <!-- Mobile fallback -->
  <div class="workflow-editor-mobile-message">
    <p>📱 Workflow editor requires desktop screen (min 1024px width)</p>
  </div>
</div>
```

**Estimate:** 1-2 hours

---

### UI-002: Include Mermaid.js Library

**Requirements:**
- Add Mermaid.js CDN link to `planning.html` (v10+ recommended)
- Configure dark theme (matches Datahaven design)
- Set `startOnLoad: false` (manual init)
- Enable click callbacks (`securityLevel: 'loose'`)

**CDN Script:**
```html
<script src="https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.min.js"></script>
<script>
  mermaid.initialize({
    startOnLoad: false,
    theme: 'dark',
    securityLevel: 'loose', // Allow click callbacks
    flowchart: {
      curve: 'basis',
      padding: 20
    }
  });
</script>
```

**Estimate:** 30 minutes

---

### UI-003: Graph Loading & Rendering

**File:** `datahaven-web/public/js/planning-workflow.js` (NEW)

**Requirements:**
- Create `loadWorkflowGraph()` function
- Fetch GET `/api/graph/mermaid/epic/EPICS`
- Insert Mermaid syntax into `#mermaid-canvas`
- Call `mermaid.init()` to render
- Add loading spinner during fetch
- Handle errors (network, API, rendering)
- Cache fetched data (5 min TTL)

**Example Code:**
```javascript
async function loadWorkflowGraph() {
  const canvas = document.getElementById('mermaid-canvas');
  canvas.innerHTML = '<div class="spinner">Loading...</div>';

  try {
    const response = await fetch('/api/graph/mermaid/epic/EPICS', {
      headers: { 'Authorization': 'Bearer dev-token-spaceos-dashboard-2026' }
    });

    if (!response.ok) {
      throw new Error(`API error: ${response.status}`);
    }

    const data = await response.json();
    const mermaidSyntax = data.mermaid;

    canvas.innerHTML = mermaidSyntax;
    canvas.classList.remove('mermaid-loading');
    await mermaid.init(undefined, canvas);

    // Register node click handlers
    registerNodeClickHandlers();
  } catch (err) {
    console.error('Failed to load workflow graph:', err);
    canvas.innerHTML = `<div class="error">Failed to load graph: ${err.message}</div>`;
  }
}
```

**Estimate:** 2-3 hours

---

### UI-004: Node Click Handlers

**Requirements:**
- Register Mermaid click callbacks for each epic node
- On click: highlight node (add `.selected` class)
- On click: fetch epic details (GET `/api/graph/epics` or use cached data)
- On click: open epic details panel
- Remove highlight when different node clicked

**Mermaid Click Syntax:**
```mermaid
click EPIC-CUTTING-Q3 call selectEpic("EPIC-CUTTING-Q3")
```

**JavaScript:**
```javascript
function registerNodeClickHandlers() {
  window.selectEpic = function(epicId) {
    // Remove previous selection
    document.querySelectorAll('.selected').forEach(el => el.classList.remove('selected'));

    // Highlight clicked node
    const node = document.querySelector(`[id="${epicId}"]`);
    if (node) node.classList.add('selected');

    // Load epic details
    loadEpicDetails(epicId);
  };
}

async function loadEpicDetails(epicId) {
  const panel = document.getElementById('epic-details');
  panel.innerHTML = '<div class="spinner">Loading...</div>';
  panel.style.display = 'block';

  try {
    const response = await fetch('/api/graph/epics', {
      headers: { 'Authorization': 'Bearer dev-token-spaceos-dashboard-2026' }
    });
    const data = await response.json();
    const epic = data.graph.nodes.find(n => n.id === epicId);

    if (!epic) throw new Error('Epic not found');

    renderEpicDetails(epic);
  } catch (err) {
    panel.innerHTML = `<div class="error">${err.message}</div>`;
  }
}
```

**Estimate:** 2-3 hours

---

### UI-005: Epic Details Panel

**Requirements:**
- Create `renderEpicDetails(epic)` function
- Display: name, status, target_date, description
- Status dropdown (editable)
- Dependencies list with status badges
- [+ Add Dependency] button (opens modal)
- [Save Changes] button

**HTML Template:**
```html
<div class="epic-details-content">
  <button class="epic-details-close" onclick="closeEpicDetails()">✕</button>
  <h3 id="epic-details-title"></h3>

  <div class="epic-detail-row">
    <span class="label">Status:</span>
    <select id="epic-status-select" onchange="onStatusChange()">
      <option value="pending">⏳ Pending</option>
      <option value="active">⚡ Active</option>
      <option value="done">✅ Done</option>
      <option value="blocked">🚫 Blocked</option>
    </select>
  </div>

  <div class="epic-detail-row">
    <span class="label">Target Date:</span>
    <span id="epic-target-date"></span>
  </div>

  <div class="epic-dependencies">
    <h4>Dependencies:</h4>
    <ul id="epic-dependencies-list"></ul>
    <button class="btn-add-dependency" onclick="openAddDependencyModal()">
      [+ Add Dependency]
    </button>
  </div>

  <button class="btn-primary" id="btn-save-epic" onclick="saveEpicChanges()">
    Save Changes
  </button>
</div>
```

**Estimate:** 3-4 hours

---

### UI-006: Status Change Handler

**Requirements:**
- Status dropdown `onChange` → PUT `/api/graph/epics/:id`
- Show loading indicator (disable dropdown)
- On success: re-render graph (node colors update)
- On error: show toast with validation message, rollback dropdown
- Optimistic UI: update node color immediately, rollback on error

**Example Code:**
```javascript
async function onStatusChange() {
  const select = document.getElementById('epic-status-select');
  const newStatus = select.value;
  const epicId = currentEpicId;

  select.disabled = true;

  try {
    const response = await fetch(`/api/graph/epics/${epicId}`, {
      method: 'PUT',
      headers: {
        'Authorization': 'Bearer dev-token-spaceos-dashboard-2026',
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ status: newStatus })
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.error || 'Failed to update status');
    }

    // Success: reload graph
    showToast('Status updated successfully', 'success');
    await loadWorkflowGraph();
  } catch (err) {
    showToast(err.message, 'error');
    // Rollback dropdown
    select.value = currentEpic.status;
  } finally {
    select.disabled = false;
  }
}
```

**Estimate:** 2-3 hours

---

### UI-007: Add Dependency Modal

**Requirements:**
- Create modal HTML (hidden by default)
- Dropdown with available epics (filter out self + existing deps)
- [Add] button → PUT `/api/graph/epics/:id` with updated `depends_on`
- Handle cycle detection error (show message with cycle path)
- On success: re-render graph + details panel
- [Cancel] button closes modal

**Modal HTML:**
```html
<div id="add-dependency-modal" class="modal" style="display:none">
  <div class="modal-content">
    <h3>Add Dependency</h3>
    <p>Select epic that <strong id="modal-epic-name"></strong> depends on:</p>
    <select id="dependency-select">
      <!-- Populated by JS -->
    </select>
    <div class="modal-actions">
      <button class="btn-primary" onclick="addDependency()">Add</button>
      <button class="btn-secondary" onclick="closeAddDependencyModal()">Cancel</button>
    </div>
  </div>
</div>
```

**Estimate:** 2-3 hours

---

### UI-008: Workflow CSS Styles

**File:** `datahaven-web/public/css/planning.css` (extend)

**Requirements:**
- `.workflow-editor` flex layout (toolbar top, graph main, details sidebar)
- `.mermaid-container` styling (dark background, overflow auto, min-height 500px)
- Mermaid node color overrides:
  - `pending`: gray (#666)
  - `active`: blue (#4a9eff)
  - `done`: green (#4caf50)
  - `blocked`: red (#f44336)
- `.selected` node highlight (yellow border, glow effect)
- `.epic-details-panel` styling (slide-in animation, max-width 400px)
- `.dep-badge` status color badges (small, rounded)
- Responsive: `@media (max-width: 1024px)` hide `.workflow-editor`, show `.workflow-editor-mobile-message`

**CSS Example:**
```css
.workflow-editor {
  display: flex;
  flex-direction: column;
  height: calc(100vh - 200px);
}

.workflow-toolbar {
  display: flex;
  gap: 10px;
  padding: 10px;
  background: rgba(255,255,255,0.05);
}

.mermaid-container {
  flex: 1;
  overflow: auto;
  background: #1a1a1a;
  padding: 20px;
  min-height: 500px;
}

.mermaid-container .node.selected rect {
  stroke: #ffd700 !important;
  stroke-width: 3px;
  filter: drop-shadow(0 0 10px #ffd700);
}

.epic-details-panel {
  position: fixed;
  right: 0;
  top: 60px;
  width: 400px;
  height: calc(100vh - 60px);
  background: #2a2a2a;
  box-shadow: -2px 0 10px rgba(0,0,0,0.3);
  padding: 20px;
  overflow-y: auto;
  animation: slideIn 0.3s ease;
}

@keyframes slideIn {
  from { transform: translateX(100%); }
  to { transform: translateX(0); }
}

@media (max-width: 1024px) {
  .workflow-editor { display: none; }
  .workflow-editor-mobile-message { display: block; text-align: center; padding: 40px; }
}
```

**Estimate:** 2-3 hours

---

### UI-009: Export Mermaid Button

**Requirements:**
- [Export Mermaid] click → GET `/api/graph/mermaid/epic/EPICS`
- Create downloadable text file (Mermaid syntax)
- Trigger browser download as `EPICS.mmd`

**Example Code:**
```javascript
async function exportMermaid() {
  try {
    const response = await fetch('/api/graph/mermaid/epic/EPICS', {
      headers: { 'Authorization': 'Bearer dev-token-spaceos-dashboard-2026' }
    });
    const data = await response.json();
    const mermaidContent = data.mermaid;

    const blob = new Blob([mermaidContent], { type: 'text/plain' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'EPICS.mmd';
    a.click();
    URL.revokeObjectURL(url);

    showToast('Mermaid diagram exported', 'success');
  } catch (err) {
    showToast('Export failed: ' + err.message, 'error');
  }
}
```

**Estimate:** 1 hour

---

### TEST-003: Frontend Integration Test

**Manual test scenarios:**
- [ ] Navigate to Planning page → Workflow tab
- [ ] Graph loads and renders (<2 seconds)
- [ ] Nodes colored by status (pending gray, active blue, done green, blocked red)
- [ ] Click epic node → details panel opens
- [ ] Change status → graph updates colors
- [ ] Add dependency → graph re-renders with new arrow
- [ ] Attempt cycle (A→B, B→A) → error message shown
- [ ] Export Mermaid → file downloads successfully
- [ ] Mobile view → "Desktop required" message shown

**Estimate:** 1-2 hours

---

## Acceptance Criteria

- [ ] Workflow tab renders graph in <2 seconds
- [ ] User can view epic details in <3 clicks
- [ ] User can change epic status with visual feedback
- [ ] User can add dependencies with cycle prevention
- [ ] Graph updates immediately on changes
- [ ] Mobile users see helpful message (not broken layout)
- [ ] Export Mermaid works correctly
- [ ] All manual tests passing

---

## Performance Targets

- Initial graph render: <2 seconds
- Node click → details panel: <500ms
- Status change → graph update: <1 second

---

## Implementation Notes

1. **Coordinate with Backend:** Wait for MSG-BACKEND-046 DONE before starting UI-006 (status change handler)
2. **Test incrementally:** Get Mermaid rendering working first, then add interactions
3. **Use existing patterns:** Follow Datahaven Planning page CSS conventions
4. **Handle errors gracefully:** Show user-friendly messages, log details to console

---

## Estimate: 5-7 days

**Breakdown:**
- HTML structure + Mermaid setup: 0.5 day
- Graph loading & rendering: 1 day
- Node interactions: 1 day
- Details panel: 1.5 days
- Status change + dependencies: 1.5 days
- CSS polish: 1 day
- Testing + fixes: 0.5-1 day

---

**When done, write DONE outbox with:**
- Screenshots of working UI
- Manual test results
- Browser compatibility notes (Chrome/Firefox tested)
- Any UX improvements suggested
- Any blockers or issues encountered
