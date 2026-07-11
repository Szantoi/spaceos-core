---
id: MSG-FRONTEND-054
from: mcp-server
to: frontend
type: task
priority: high
status: READ
created: 2026-06-29
model: sonnet
content_hash: d2a3bbd548fa4fd0b176904d74e481a0dfdadfcf3fcef7c69f36cb7b16e930b7
---

## Datahaven UI — Phase 1, Task 4: Frontend Focus Area Panel Edit + Save + CSS

**Status:** ACTIVE (after Task 3 completes)
**Estimated time:** 1.5 hours
**Depends on:** MSG-FRONTEND-053 (display logic)
**Ref:** docs/tasks/active/Datahaven_UI_Focus_Flow_Editor_Architecture_v1.md (sections 1, 6.2)

### Task
Implement edit mode and save functionality for Focus Area Panel:
1. Add [Edit Criteria] button → toggle edit mode (textarea)
2. Add [Save Changes] button → PUT request to backend
3. Add [Cancel] button → discard edits
4. Validate: at least one of {domain, criteria} must change
5. Show success/error toast notifications
6. Refresh data display after successful save
7. Finalize CSS styling

### Requirements

**Edit Mode Toggle:**
```javascript
// Click [Edit Criteria] button
- Hide criteria-display div
- Show criteria-edit-mode textarea with current markdown
- Enable Save/Cancel buttons

// Click [Cancel] button
- Restore criteria-display div
- Discard textarea changes
- Hide Save/Cancel buttons

// Click [Save Changes] button
- Validate: domain or criteria changed?
- Call PUT /api/planning/domain-focus
- Show toast: "Saving..." (spinner)
- On success: show "✓ Saved" toast, refresh display
- On error: show "✗ Failed to save" toast with error message
```

**PUT Request Format:**
```javascript
const body = {};
if (domainChanged) body.domain = newDomain;
if (criteriaChanged) body.criteria = newCriteria;

PUT /api/planning/domain-focus
Authorization: Bearer dev-token-spaceos-dashboard-2026
Content-Type: application/json
{
  "domain": "sales",           // Optional
  "criteria": "- New text"     // Optional
}

Response (200):
{
  "success": true,
  "domain": "sales",
  "criteria": "- New text",
  "updated_at": "2026-06-29T14:20:00Z"
}

Errors:
400 Bad Request → Show toast: "Invalid domain" or "Invalid criteria"
401 Unauthorized → Show toast: "Authentication required"
429 Too Many Requests → Show toast: "Rate limited (max 10 saves/min)"
500 Server Error → Show toast: "Server error, please try again"
```

**UI State Machine:**
```
Display Mode (initial)
├─ [Edit Criteria] button visible
├─ Criteria rendered as HTML
└─ domain dropdown enabled for read-only display

Edit Mode
├─ [Edit Criteria] button hidden
├─ Criteria textarea visible with raw markdown
├─ [Save Changes] + [Cancel] buttons visible
└─ domain dropdown still enabled for change

Saving State
├─ [Save Changes] button disabled (loading spinner)
├─ [Cancel] button disabled
└─ Show spinner "Saving..."

Back to Display Mode (on success or cancel)
```

**CSS Classes** (add to `public/css/styles.css` if missing):
```css
.criteria-edit-mode {
  display: none;  /* Hidden by default */
}

.criteria-edit-mode.active {
  display: block;
}

.criteria-display.hidden {
  display: none;  /* Hidden during edit mode */
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

.focus-area-actions {
  display: flex;
  gap: 0.75rem;
  margin-top: 1rem;
}

.btn-edit, .btn-save, .btn-cancel {
  background: var(--accent-blue);
  color: white;
  border: none;
  padding: 0.5rem 1rem;
  border-radius: 6px;
  cursor: pointer;
  font-size: 0.875rem;
  font-weight: 500;
}

.btn-edit:hover, .btn-save:hover, .btn-cancel:hover {
  opacity: 0.9;
}

.btn-save:disabled, .btn-cancel:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.sync-indicator {
  color: var(--text-secondary);
  font-size: 0.75rem;
  margin-top: 0.5rem;
}
```

**HTML to Add** (update from Task 3):
```html
<!-- In .focus-area-body, add after .criteria-display: -->
<div class="criteria-edit-mode" id="criteria-edit-mode">
  <textarea id="criteria-textarea" placeholder="Edit criteria markdown..."></textarea>
</div>

<!-- Replace old action buttons with this: -->
<div class="focus-area-actions">
  <button id="edit-btn" class="btn-edit">Edit Criteria</button>
  <button id="save-btn" class="btn-save" style="display:none;">Save Changes</button>
  <button id="cancel-btn" class="btn-cancel" style="display:none;">Cancel</button>
</div>
```

**JavaScript Functions** (add to `planning-focus.js`):
```javascript
// Toggle edit mode
function toggleEditMode(enter = true)
// Save changes
async function saveFocusArea()
// Cancel edits
function cancelEdits()
// Show toast notification
function showToast(message, type='info')  // type: info|success|error
// Detect what changed
function getChangedFields()
```

### Implementation Checklist
1. Update HTML in `planning.html` (add textarea + buttons from above)
2. Add CSS classes to `public/css/styles.css`
3. Implement `toggleEditMode()` function
4. Implement `saveFocusArea()` function with PUT request
5. Implement `cancelEdits()` function
6. Implement `showToast()` for notifications (use existing toast library)
7. Add event listeners:
   - `#edit-btn` click → toggleEditMode(true)
   - `#save-btn` click → saveFocusArea()
   - `#cancel-btn` click → cancelEdits()
   - `#domain-dropdown` change → saveFocusArea() (auto-save on domain change)
8. Handle all error cases with appropriate toast messages
9. Test in browser: edit criteria → save → verify API call → verify display refresh

### Error Handling
- Show meaningful error messages in toast
- Don't leave UI in inconsistent state
- Allow retry after error

### Acceptance Criteria
- ✅ Edit button toggles edit mode
- ✅ Textarea shows raw markdown on edit
- ✅ Save button calls PUT /api/planning/domain-focus
- ✅ Domain dropdown change triggers save
- ✅ Success toast shown on save
- ✅ Error toast shown on API error
- ✅ Criteria display refreshes after save
- ✅ Cancel button discards changes
- ✅ Rate limit error handled (429 → toast)
- ✅ CSS classes applied correctly
- ✅ Manual browser test: edit → save → refresh → verify
- ✅ Code merged to main

**After This Task:**
Phase 1 Focus Area Panel complete! Next: Phase 2 Flow/Workflow Editor (Backend: PUT /api/graph/epics/:id, Frontend: Mermaid rendering)

### Reference Links
- Backend API: MSG-BACKEND-074 (GET), MSG-BACKEND-075 (PUT)
- Frontend display: MSG-FRONTEND-053 (Task 3)
- Architecture doc: sections 1.1-1.4
