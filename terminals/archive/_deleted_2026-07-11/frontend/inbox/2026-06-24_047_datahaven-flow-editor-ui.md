---
id: MSG-FRONTEND-044
from: conductor
to: frontend
type: task
priority: high
status: READ
model: sonnet
ref: 2026-06-24_consensus_flow-editor-phase1.md
epic: EPIC-DATAHAVEN-UI
phase: 2
created: 2026-06-24
content_hash: 55a64c27fae18821bd7b8ec0cc7c6249e6a397949c4e33fc5474ca44334c3901
---

# Datahaven Flow/Workflow Editor — Frontend UI Implementation

## Task Overview

Implement **Flow/Workflow Editor** UI component on the Datahaven Planning page (Workflow tab), providing interactive visualization and editing of epic dependencies using Mermaid.js.

**Epic:** EPIC-DATAHAVEN-UI (Phase 2 of 3)
**Estimate:** 15-20 hours (split across: HTML 1h + JS 12-15h + CSS 2-3h + libraries 30min)
**Related:** Backend is implementing PUT /api/graph/epics/:id endpoint (MSG-BACKEND-047) in parallel
**Architecture Reference:** `/opt/spaceos/docs/tasks/new/Datahaven_UI_Focus_Flow_Editor_Architecture_v1.md` (Section 2)

---

## What is Flow/Workflow Editor?

An interactive UI component that:
1. **Visualizes** epic dependencies as a Mermaid diagram
2. **Shows epic details** in a collapsible panel (name, status, dependencies, description)
3. **Allows editing** epic properties by clicking nodes
4. **Validates** changes before saving (cycle detection, status transitions)
5. **Updates graph** in real-time after saving
6. **Exports diagrams** for documentation/Slack

**Placement:** Planning page (`public/planning.html`), Workflow tab (currently empty placeholder)
**Desktop only:** Mobile/tablet shows "Desktop required" message

---

## UI Tasks

### Task 1: HTML Structure in planning.html (1 hour)

**File:** `datahaven-web/public/planning.html`

**Current state:** Workflow tab is an empty placeholder. Replace with full UI structure.

**What to add:**

```html
<div id="workflow-content" class="tab-content">
  <!-- Desktop version -->
  <div class="workflow-editor">
    <!-- Toolbar -->
    <div class="workflow-toolbar">
      <button id="btn-export-mermaid" class="btn-icon">📥 Export Mermaid</button>
      <button id="btn-validate" class="btn-icon">✓ Validate Graph</button>
      <button id="btn-refresh" class="btn-icon">🔄 Refresh</button>
    </div>

    <!-- Mermaid graph canvas -->
    <div class="mermaid-container">
      <div id="mermaid-canvas" class="mermaid-canvas"></div>
      <div id="loading-indicator" style="display:none;">
        <p>Loading graph...</p>
      </div>
    </div>

    <!-- Epic details panel (collapsible, hidden by default) -->
    <div id="epic-details-panel" class="epic-details-panel" style="display:none;">
      <div class="epic-details-header">
        <h3 id="epic-title">Epic Details</h3>
        <button id="btn-close-details" class="btn-close">✕</button>
      </div>
      <div class="epic-details-body">
        <div class="epic-detail-row">
          <label>ID:</label>
          <span id="detail-id"></span>
        </div>
        <div class="epic-detail-row">
          <label>Status:</label>
          <select id="detail-status">
            <option value="pending">Pending</option>
            <option value="active">Active</option>
            <option value="done">Done</option>
            <option value="blocked">Blocked</option>
          </select>
        </div>
        <div class="epic-detail-row">
          <label>Target Date:</label>
          <input type="date" id="detail-target-date">
        </div>
        <div class="epic-detail-row">
          <label>Dependencies:</label>
          <ul id="detail-dependencies" class="epic-dependencies-list"></ul>
          <button id="btn-add-dependency" class="btn-add-dependency">+ Add Dependency</button>
        </div>
        <div class="epic-detail-row">
          <label>Parallel With:</label>
          <ul id="detail-parallel" class="epic-dependencies-list"></ul>
          <button id="btn-add-parallel" class="btn-add-parallel">+ Add Parallel</button>
        </div>
        <div class="epic-detail-row">
          <label>Description:</label>
          <p id="detail-description"></p>
        </div>
        <div class="epic-detail-actions">
          <button id="btn-save-epic" class="btn-save">Save Changes</button>
          <button id="btn-cancel-edit" class="btn-cancel">Cancel</button>
        </div>
      </div>
    </div>
  </div>

  <!-- Mobile fallback -->
  <div class="workflow-editor-mobile-message">
    <p>⚠️ Workflow Editor requires a desktop screen (minimum 1024px width)</p>
    <p style="font-size: 0.9em; color: #999;">
      Please view this page on a larger device to use the interactive graph.
    </p>
  </div>
</div>
```

**Checklist:**
- [ ] Workflow tab content replaced (not empty)
- [ ] Toolbar buttons present (Export, Validate, Refresh)
- [ ] Mermaid container ready for graph rendering
- [ ] Epic details panel markup complete
- [ ] Mobile message present
- [ ] All IDs match the JS selectors
- [ ] HTML structure validates (no syntax errors)

---

### Task 2: JavaScript Logic — Mermaid Integration (5-6 hours)

**File:** Create `datahaven-web/public/js/planning-workflow.js` (NEW)

**Part 2A: Graph Loading & Rendering (2-3 hours)**

```javascript
// Global state
let currentGraph = null;
let selectedEpicId = null;

// Initialization on page load
document.addEventListener('DOMContentLoaded', async () => {
  console.log('[Workflow] Initializing Flow Editor...');
  await loadAndRenderGraph();
  attachEventListeners();
});

async function loadAndRenderGraph() {
  try {
    // 1. Fetch Mermaid diagram from API
    const response = await fetch('/api/graph/mermaid/epic/EPICS', {
      headers: { 'Authorization': 'Bearer dev-token-spaceos-dashboard-2026' }
    });

    if (!response.ok) {
      console.error('[Workflow] Failed to load graph');
      document.getElementById('mermaid-canvas').innerHTML =
        `<p style="color: red;">Error loading graph (${response.status})</p>`;
      return;
    }

    const data = await response.json();
    currentGraph = data;

    // 2. Render Mermaid diagram
    const mermaidCode = data.mermaid;
    document.getElementById('mermaid-canvas').innerHTML = mermaidCode;

    // 3. Initialize Mermaid
    mermaid.initialize({ startOnLoad: true, theme: 'dark' });
    await mermaid.contentLoaded();

    // 4. Add click handlers to nodes
    addNodeClickHandlers();

    console.log('[Workflow] Graph rendered successfully');
  } catch (error) {
    console.error('[Workflow] Error rendering graph:', error);
  }
}

function addNodeClickHandlers() {
  // Find all Mermaid nodes and add click handlers
  // Mermaid nodes have class "node"
  const nodes = document.querySelectorAll('.mermaid-canvas .node');

  nodes.forEach(node => {
    node.style.cursor = 'pointer';
    node.addEventListener('click', (e) => {
      const epicId = extractEpicIdFromNode(e.target);
      if (epicId) {
        selectEpic(epicId);
      }
    });
  });
}

function extractEpicIdFromNode(element) {
  // Mermaid stores text content in the element
  // Parse text to extract EPIC-ID
  const text = element.textContent || '';
  const match = text.match(/EPIC-[\w-]+/);
  return match ? match[0] : null;
}
```

**Part 2B: Epic Details Panel (2-3 hours)**

```javascript
async function selectEpic(epicId) {
  console.log(`[Workflow] Selected epic: ${epicId}`);
  selectedEpicId = epicId;

  try {
    // 1. Fetch epic details from API
    const response = await fetch(`/api/graph/epics`, {
      headers: { 'Authorization': 'Bearer dev-token-spaceos-dashboard-2026' }
    });

    const graphData = await response.json();
    const epic = graphData.graph.nodes.find(n => n.id === epicId);

    if (!epic) {
      console.error(`[Workflow] Epic not found: ${epicId}`);
      return;
    }

    // 2. Populate details panel
    document.getElementById('epic-title').textContent = epic.name;
    document.getElementById('detail-id').textContent = epic.id;
    document.getElementById('detail-status').value = epic.status;
    document.getElementById('detail-target-date').value = epic.target_date || '';
    document.getElementById('detail-description').textContent = epic.description || '(No description)';

    // 3. Populate dependencies list
    renderDependencies(epic.depends_on || []);
    renderParallel(epic.parallel_with || []);

    // 4. Show panel
    document.getElementById('epic-details-panel').style.display = 'block';

    // 5. Highlight selected node in graph
    highlightNode(epicId);

  } catch (error) {
    console.error('[Workflow] Error loading epic details:', error);
  }
}

function renderDependencies(depIds) {
  const ul = document.getElementById('detail-dependencies');
  ul.innerHTML = '';

  depIds.forEach(depId => {
    const li = document.createElement('li');
    li.innerHTML = `
      <span>${depId}</span>
      <button class="btn-remove-dep" data-epic="${depId}">✕</button>
    `;
    ul.appendChild(li);

    li.querySelector('.btn-remove-dep').addEventListener('click',
      () => removeDependency(depId)
    );
  });
}

function renderParallel(parallelIds) {
  const ul = document.getElementById('detail-parallel');
  ul.innerHTML = '';

  parallelIds.forEach(parallelId => {
    const li = document.createElement('li');
    li.innerHTML = `
      <span>${parallelId}</span>
      <button class="btn-remove-par" data-epic="${parallelId}">✕</button>
    `;
    ul.appendChild(li);

    li.querySelector('.btn-remove-par').addEventListener('click',
      () => removeParallel(parallelId)
    );
  });
}
```

**Part 2C: Save Handler (2-3 hours)**

```javascript
async function saveEpicChanges() {
  const status = document.getElementById('detail-status').value;
  const targetDate = document.getElementById('detail-target-date').value;

  const payload = {
    status,
    target_date: targetDate || null
    // Note: depends_on and parallel_with are managed separately
  };

  try {
    const response = await fetch(`/api/graph/epics/${selectedEpicId}`, {
      method: 'PUT',
      headers: {
        'Authorization': 'Bearer dev-token-spaceos-dashboard-2026',
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(payload)
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.error || `Save failed (${response.status})`);
    }

    const result = await response.json();
    console.log('[Workflow] Epic saved:', result);

    // Refresh graph after save
    await loadAndRenderGraph();
    showToast('✓ Epic updated', 'success');
    closeDetailsPanel();

  } catch (error) {
    console.error('[Workflow] Error saving epic:', error);
    showToast(`❌ Error: ${error.message}`, 'error');
  }
}

function closeDetailsPanel() {
  document.getElementById('epic-details-panel').style.display = 'none';
  selectedEpicId = null;
}

function highlightNode(epicId) {
  // Remove previous highlight
  document.querySelectorAll('.mermaid-canvas .node.selected').forEach(n => {
    n.classList.remove('selected');
  });

  // Add highlight to current node
  const nodes = document.querySelectorAll('.mermaid-canvas .node');
  nodes.forEach(n => {
    if (n.textContent.includes(epicId)) {
      n.classList.add('selected');
    }
  });
}

function showToast(message, type) {
  // Simple toast notification (reuse from focus-area.js if available)
  console.log(`[${type.toUpperCase()}] ${message}`);
  // TODO: Implement proper toast UI
}
```

**Testing Requirements:**
- [ ] Graph loads on page load
- [ ] Mermaid renders without errors
- [ ] Node click selects epic
- [ ] Details panel shows correct epic data
- [ ] Status dropdown works
- [ ] Save handler calls API
- [ ] Graph refreshes after save
- [ ] Error handling for API failures

---

### Task 3: CSS Styling (2-3 hours)

**File:** Extend `datahaven-web/public/css/planning.css` or `styles.css`

**Key classes:**

```css
.workflow-editor {
  display: flex;
  flex-direction: column;
  height: calc(100vh - 300px);
}

.workflow-toolbar {
  padding: 1rem;
  border-bottom: 1px solid var(--border-color);
  display: flex;
  gap: 0.75rem;
}

.mermaid-container {
  flex: 1;
  background: var(--bg-secondary);
  border-radius: 12px;
  border: 1px solid var(--border-color);
  margin: 1rem;
  overflow: auto;
  position: relative;
}

.mermaid-canvas {
  width: 100%;
  height: 100%;
}

.epic-details-panel {
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: 12px;
  padding: 1.5rem;
  max-height: 400px;
  overflow-y: auto;
  margin: 1rem;
}

/* Mermaid node styling overrides */
.mermaid .pending {
  fill: #f9f9f9;
  stroke: #999;
}

.mermaid .active {
  fill: #e6f3ff;
  stroke: var(--accent-blue);
  stroke-width: 2px;
}

.mermaid .done {
  fill: #e6ffe6;
  stroke: var(--accent-green);
}

.mermaid .blocked {
  fill: #ffe6e6;
  stroke: var(--accent-red);
  stroke-width: 2px;
}

.mermaid .selected {
  stroke: var(--accent-purple);
  stroke-width: 3px;
  filter: drop-shadow(0 0 8px var(--accent-purple));
}

/* Responsive */
@media (max-width: 1024px) {
  .workflow-editor {
    display: none;
  }

  .workflow-editor-mobile-message {
    display: block;
    padding: 3rem;
    text-align: center;
  }
}

@media (min-width: 1025px) {
  .workflow-editor-mobile-message {
    display: none;
  }
}
```

**Checklist:**
- [ ] Layout is responsive (flex, auto heights)
- [ ] Colors use CSS variables
- [ ] Mermaid nodes styled per status (pending/active/done/blocked)
- [ ] Selected node highlighted distinctly
- [ ] Desktop-only visibility
- [ ] Mobile message shown on small screens
- [ ] No style conflicts with existing planning.css

---

### Task 4: Library Integration (30 minutes)

**Mermaid.js Library:**

Add to `planning.html` `<head>`:
```html
<!-- Mermaid.js for graph visualization -->
<script src="https://cdn.jsdelivr.net/npm/mermaid/dist/mermaid.min.js"></script>
<script>
  mermaid.initialize({
    startOnLoad: false,  // We'll call mermaid.contentLoaded() manually
    theme: 'dark',
    securityLevel: 'loose'
  });
</script>
```

**panzoom.js Library (optional, for future enhancement):**
```html
<!-- Pan & Zoom for large graphs -->
<script src="https://unpkg.com/panzoom@9.4.0/dist/panzoom.min.js"></script>
```

**Checklist:**
- [ ] Mermaid library loaded
- [ ] Mermaid initialized with dark theme
- [ ] panzoom available (optional)

---

### Task 5: Integration Testing (2-3 hours)

**Test Scenarios:**
- [ ] Workflow tab loads without errors
- [ ] Graph renders correctly (nodes, edges visible)
- [ ] Clicking epic node selects it
- [ ] Details panel shows correct epic data
- [ ] Changing status updates the field
- [ ] Save button calls API with correct payload
- [ ] Validation error shown if status transition invalid
- [ ] Graph refreshes after save
- [ ] Cycle detection error shown if circular dependency created
- [ ] Mobile view shows desktop-required message
- [ ] No console errors
- [ ] No TypeScript compilation errors (if applicable)

---

## Definition of Done

**All tasks must be complete:**

- [ ] HTML structure added to planning.html (Workflow tab)
- [ ] `planning-workflow.js` created with graph loading logic
- [ ] Epic details panel functional (show/edit/save)
- [ ] Mermaid.js library integrated and rendering
- [ ] Node click handlers work
- [ ] Save handler calls API correctly
- [ ] CSS styling applied (consistent with Datahaven theme)
- [ ] Responsive design works on mobile (shows message)
- [ ] All error cases handled (API failures, missing data)
- [ ] Toast notifications for feedback
- [ ] Integration tests pass
- [ ] No JavaScript console errors
- [ ] No TypeScript compilation errors

---

## Architecture Reference

See the full architecture document:

**File:** `/opt/spaceos/docs/tasks/new/Datahaven_UI_Focus_Flow_Editor_Architecture_v1.md`

Key sections:
- Section 2 — Workflow Editor detailed design
- Section 2.4 — Mermaid rendering strategy
- Section 6.3 — CSS design guidelines

---

## Backend Parallel Work

**Backend is implementing:** MSG-BACKEND-047 (PUT /api/graph/epics/:id)
- Status transition validation
- Cycle detection
- Atomic YAML writes

**These are independent** until integration. Both must DONE before Phase 3.

---

## Notes

- **Mermaid theme:** Dark theme aligns with existing Datahaven Dashboard
- **Panzoom (optional):** For very large graphs (>20 nodes), add pan/zoom capability in future
- **Lazy loading (optional):** Load mermaid.js only when Workflow tab is clicked
- **Accessibility:** Ensure keyboard navigation for epic selection (tab + enter)

---

## Questions?

If you need clarification:
- Check the architecture document (Section 2 for UI design)
- Review existing planning.js for patterns
- Contact Conductor via outbox (BLOCKED status)

---

**Estimate:** 15-20 hours total
**Start:** Immediately
**Report:** DONE outbox when all tasks complete

