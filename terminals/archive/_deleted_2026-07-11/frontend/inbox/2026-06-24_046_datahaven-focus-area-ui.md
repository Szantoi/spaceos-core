---
id: MSG-FRONTEND-043
from: conductor
to: frontend
type: task
priority: high
status: SUPERSEDED
model: sonnet
ref: 2026-06-24_consensus_focus-area-panel.md
epic: EPIC-DATAHAVEN-UI
phase: 1
created: 2026-06-24
superseded_by: MSG-FRONTEND-042
superseded_reason: "UI component already implemented in previous work cycle. See planning-focus.js (7.4 KB) in public/js/."
content_hash: 9ddeb8b373b8ae7c971f48554d1c352d73458ef5d3fc96c38e65e6496b4225bb
---

# ⚠️ SUPERSEDED — Focus Area Panel UI Already Implemented

## ⚠️ NOTE: This task is superseded by previous work

The **Focus Area Panel** UI component on the Datahaven Planning page was **already fully implemented** in the previous work cycle.

**Epic:** EPIC-DATAHAVEN-UI (Phase 1 of 3)
**Estimate:** 5-6 hours
**Related:** Backend is implementing API endpoints (MSG-BACKEND-046) in parallel
**Architecture Reference:** `/opt/spaceos/docs/tasks/new/Datahaven_UI_Focus_Flow_Editor_Architecture_v1.md`

---

## What is Focus Area Panel?

A **card-based UI component** that:
1. Shows the current planning domain (dropdown with 7 options)
2. Displays domain criteria in markdown format
3. Allows editing criteria with a textarea
4. Saves changes via API call
5. Shows sync status (last update time or "Not synced")

**Placement:** Planning page (`public/planning.html`), top section before Pipeline Overview

---

## UI Tasks

### Task 1: HTML Structure in planning.html (1 hour)

**File:** `datahaven-web/public/planning.html`

**What to add:**
1. Add a new `<section>` with class `focus-area-panel` before the pipeline-overview section
2. Structure:
   ```html
   <section class="focus-area-panel">
     <div class="focus-area-header">
       <h2>Focus Area</h2>
       <button class="btn-sync">🔄 Sync</button>
     </div>
     <div class="focus-area-body">
       <div class="domain-selector">
         <label for="domain-dropdown">Domain:</label>
         <select id="domain-dropdown">
           <option value="manufacturing">Manufacturing</option>
           <option value="sales">Sales</option>
           <option value="logistics">Logistics</option>
           <option value="finance">Finance</option>
           <option value="quality">Quality</option>
           <option value="hr">HR</option>
           <option value="all">All</option>
         </select>
       </div>
       <div class="criteria-display" id="criteria-display"></div>
       <div id="criteria-edit-mode" style="display:none;">
         <textarea id="criteria-textarea" placeholder="Enter criteria..."></textarea>
       </div>
       <div class="focus-area-actions">
         <button id="btn-edit" class="btn-edit">Edit Criteria</button>
         <button id="btn-save" class="btn-save" style="display:none;">Save Changes</button>
       </div>
     </div>
   </section>
   ```

**Checklist:**
- [ ] Section added before pipeline-overview
- [ ] All UI elements properly structured
- [ ] IDs match the JS selectors (domain-dropdown, criteria-display, etc.)
- [ ] Button classes align with CSS framework

---

### Task 2: JavaScript Logic (3-4 hours)

**File:** Create `datahaven-web/public/js/planning-focus.js` (NEW)

**What to implement:**

#### 2.1 Page Load — Fetch domain focus
```javascript
document.addEventListener('DOMContentLoaded', async () => {
  // Fetch from GET /api/planning/domain-focus
  // Populate domain dropdown
  // Render criteria as HTML (markdown → HTML using marked.js)
  // Show sync status
});
```

**Steps:**
- Call `GET /api/planning/domain-focus` with Authorization header
- Parse response: `{ domain, criteria, updated_at }`
- Set dropdown value to current domain
- Parse markdown criteria (use `marked.js` library) and display in `criteria-display` div
- Show "Synced 2 minutes ago" or similar timestamp

**Error handling:**
- If API returns 401 → show "Authentication failed"
- If API returns 404 → show "Domain focus file not found"
- If API returns 500 → show "Server error — try again later"

---

#### 2.2 Domain Dropdown Change
```javascript
document.getElementById('domain-dropdown').addEventListener('change', async (e) => {
  // User selects new domain
  // Call PUT /api/planning/domain-focus with { domain: newValue }
  // Show toast: "Domain updated ✓"
  // Refresh display
});
```

**Steps:**
- Listen to dropdown change event
- Get new domain value
- Call `PUT /api/planning/domain-focus` with `{ domain: newValue }`
- Update display with new criteria for that domain
- Show success toast

---

#### 2.3 Edit Mode Toggle
```javascript
document.getElementById('btn-edit').addEventListener('click', () => {
  // Show textarea with current criteria
  // Hide display div
  // Show [Save Changes] button
});
```

**Steps:**
- Hide `criteria-display` div
- Show `criteria-edit-mode` div
- Populate textarea with raw markdown from API
- Change button from [Edit Criteria] to [Save Changes]

---

#### 2.4 Save Criteria
```javascript
document.getElementById('btn-save').addEventListener('click', async () => {
  // Get textarea content
  // Validate (not empty, no XSS attempts)
  // Call PUT /api/planning/domain-focus with { criteria: newValue }
  // Return to display mode
});
```

**Steps:**
- Get textarea content
- Validate: not empty, reasonable length (<5000 chars)
- Disable button while saving (prevent double-click)
- Call `PUT /api/planning/domain-focus` with `{ criteria: value }`
- Show "Saved ✓" toast
- Return to display mode (hide textarea, show markdown-rendered criteria)
- Re-fetch display (call GET again) to ensure consistency

---

#### 2.5 Sync Button
```javascript
document.querySelector('.btn-sync').addEventListener('click', async () => {
  // Refresh data from API
  // Update display
});
```

**Steps:**
- Call `GET /api/planning/domain-focus` to refresh
- Re-render display
- Update timestamp
- Show "Refreshed ✓" toast

---

#### 2.6 Helper Functions

Create utility functions:
```javascript
async function fetchDomainFocus() {
  // GET /api/planning/domain-focus with auth header
}

async function updateDomainFocus(payload) {
  // PUT /api/planning/domain-focus with auth header
}

function renderCriteria(markdownText) {
  // Parse markdown using marked.js
  // Return HTML
}

function showToast(message, type) {
  // Show toast notification (success/error)
}
```

---

### Task 3: CSS Styling (1-2 hours)

**File:** Extend `datahaven-web/public/css/planning.css` (or styles.css)

**What to add:**

Use the design guidelines from architecture doc (Section 6.2). Key classes:

```css
.focus-area-panel {
  background: var(--bg-card);
  border-radius: 12px;
  border: 1px solid var(--border-color);
  margin-bottom: 1.5rem;
}

.focus-area-header {
  padding: 1rem 1.5rem;
  border-bottom: 1px solid var(--border-color);
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.domain-selector {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 1rem;
}

.domain-selector select {
  background: var(--bg-secondary);
  color: var(--text-primary);
  border: 1px solid var(--border-color);
  border-radius: 6px;
  padding: 0.5rem 1rem;
  font-size: 0.875rem;
  min-width: 200px;
}

.criteria-display {
  background: var(--bg-secondary);
  padding: 1rem;
  border-radius: 6px;
  border: 1px solid var(--border-color);
  max-height: 300px;
  overflow-y: auto;
}

.criteria-display ul {
  list-style: disc;
  padding-left: 1.5rem;
}

.criteria-display strong {
  color: var(--accent-blue);
}

.criteria-edit-mode textarea {
  width: 100%;
  min-height: 200px;
  background: var(--bg-secondary);
  color: var(--text-primary);
  border: 1px solid var(--border-color);
  border-radius: 6px;
  padding: 1rem;
  font-family: monospace;
  font-size: 0.875rem;
}

.btn-edit, .btn-save {
  background: var(--accent-blue);
  color: white;
  border: none;
  padding: 0.5rem 1rem;
  border-radius: 6px;
  cursor: pointer;
  font-size: 0.875rem;
  font-weight: 500;
}

.btn-edit:hover, .btn-save:hover {
  opacity: 0.9;
}

.focus-area-actions {
  display: flex;
  gap: 0.75rem;
  margin-top: 1rem;
}
```

**Responsive Design:**
```css
@media (max-width: 768px) {
  .domain-selector select {
    width: 100%;
  }

  .criteria-display {
    max-height: 150px;
  }
}
```

**Checklist:**
- [ ] Panel looks consistent with existing Datahaven style
- [ ] Colors use CSS variables (--accent-blue, --text-primary, etc.)
- [ ] Responsive on mobile/tablet
- [ ] Edit mode textarea is clearly visually distinct
- [ ] Buttons are clearly clickable

---

### Task 4: JavaScript Library Integration (30 minutes)

**Library:** `marked.js` (markdown parser)

**What to do:**
1. Add to `planning.html` head:
   ```html
   <script src="https://cdn.jsdelivr.net/npm/marked/marked.min.js"></script>
   ```

2. Use in your JS:
   ```javascript
   const htmlContent = marked.parse(markdownText);
   criteria-display.innerHTML = htmlContent;
   ```

**Security note:** `marked.js` is safe, but sanitize HTML if needed:
```javascript
const htmlContent = DOMPurify.sanitize(marked.parse(markdownText));
```

(If DOMPurify already available in project, use it)

---

### Task 5: Integration Testing (1 hour)

**Test Scenarios:**
- [ ] Load planning page → domain focus panel appears
- [ ] Panel shows current domain from API
- [ ] Panel shows criteria as formatted markdown
- [ ] Change dropdown → API called, display updates
- [ ] Click Edit → textarea appears with raw markdown
- [ ] Edit criteria → Save → API called, display updates
- [ ] Sync button → refreshes from API
- [ ] Error case: API returns 401 → shows error message
- [ ] Error case: Network timeout → shows error and retry button
- [ ] Mobile view: Panel responsive, readable on small screen

---

## Definition of Done

**All tasks must be complete:**

- [ ] HTML structure added to planning.html
- [ ] `planning-focus.js` created with all event handlers
- [ ] Domain dropdown synced with API
- [ ] Criteria display renders markdown correctly
- [ ] Edit mode toggle works (textarea appears/disappears)
- [ ] Save handler calls API and updates display
- [ ] Sync button refreshes data
- [ ] CSS styling applied (consistent with design)
- [ ] Responsive design works on mobile/tablet
- [ ] marked.js library integrated
- [ ] Error handling for all API failure cases
- [ ] Toast notifications for user feedback
- [ ] Integration tests pass (manual or automated)
- [ ] No JavaScript console errors
- [ ] No TypeScript compilation errors (if applicable)

---

## Architecture Reference

See the full architecture document for context:

**File:** `/opt/spaceos/docs/tasks/new/Datahaven_UI_Focus_Flow_Editor_Architecture_v1.md`

Key sections:
- Section 1.2 — UI mockup and visual design
- Section 1.4 — Data flow diagram
- Section 6.2 — CSS design guidelines
- Section 4.2 — Focus Area Panel write flow (helps understand what happens on save)

---

## Backend Parallel Work

**Backend is implementing:** MSG-BACKEND-046 (API endpoints)
- GET /api/planning/domain-focus
- PUT /api/planning/domain-focus
- Route registration

**These endpoints are blocking your work** until they're available. Once Backend deploys:
1. Integration testing can begin
2. Phase 2 dispatch (Flow/Workflow Editor) starts
3. Phase 3 polish and optimization

---

## Next Steps

1. **You implement** this UI component
2. **Backend implements** API endpoints (parallel)
3. **Once both DONE**, Conductor will:
   - Run E2E tests (API + UI together)
   - Dispatch Phase 2 (Flow/Workflow Editor)

---

## Questions?

If you need clarification:
- Check the architecture document (full specs in Section 1-6)
- Contact Conductor via outbox message (BLOCKED status)

---

**Estimate:** 5-6 hours total
**Start:** Immediately (Backend is working in parallel)
**Report:** DONE outbox when all tasks complete

