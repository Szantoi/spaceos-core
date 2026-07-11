---
id: MSG-FRONTEND-045
from: conductor
to: frontend
type: task
priority: high
status: READ
model: sonnet
ref: 2026-06-24_consensus_focus-area-panel.md
epic: EPIC-DATAHAVEN-UI
phase: 1
created: 2026-06-24
completed: 2026-06-24
content_hash: 9a900419cf04bb52170e89f707b40073d0f3c08ed923972665345dac56418a16
---

# Datahaven Focus Area Panel — Frontend UI Implementation

## Task Overview

Implement the **Focus Area Panel** UI component on the Datahaven Dashboard Planning page, allowing users to view and edit the planning domain configuration via a visual interface.

**Epic:** EPIC-DATAHAVEN-UI (Phase 1 of 3)
**Estimate:** 4-5 hours
**Priority:** HIGH
**Dependency:** Backend API (MSG-BACKEND-048) — **Backend should start first, but UI can begin in parallel once API spec is clear**
**Architecture Reference:** `/opt/spaceos/docs/tasks/new/Datahaven_UI_Focus_Flow_Editor_Architecture_v1.md` (Section 4)

---

## What is Focus Area Panel?

A new UI panel at the top of the Planning page that displays:
- Current planning domain (7 options: manufacturing, sales, logistics, finance, quality, hr, all)
- Domain criteria (markdown rendered as HTML)
- Buttons to edit criteria and sync with backend
- Last updated timestamp

The panel provides a visual way to manage the planning focus without editing files directly.

---

## Frontend Implementation Tasks

### UI-001: HTML Structure (1 hour)

**File:** `datahaven-web/public/planning.html`

**What to implement:**

1. Add new `.focus-area-panel` div **above the existing Pipeline Overview** (at the top of the Planning page body)

2. Panel structure:
   ```html
   <div class="focus-area-panel">
     <div class="focus-area-header">
       <h3>Focus Area</h3>
       <button class="btn-sync" id="btn-sync-focus" title="Sync with server">🔄 Sync</button>
     </div>

     <div class="focus-area-body">
       <!-- Domain selector -->
       <div class="domain-selector">
         <label for="domain-select">Domain:</label>
         <select id="domain-select">
           <option value="manufacturing">Manufacturing</option>
           <option value="sales">Sales</option>
           <option value="logistics">Logistics</option>
           <option value="finance">Finance</option>
           <option value="quality">Quality</option>
           <option value="hr">Human Resources</option>
           <option value="all">All Domains</option>
         </select>
         <span class="sync-status" id="sync-status"></span>
       </div>

       <!-- Criteria display (markdown rendered) -->
       <div class="criteria-display" id="criteria-display" style="display:block;"></div>

       <!-- Criteria edit mode (hidden by default) -->
       <div class="criteria-edit-mode" id="criteria-edit" style="display:none;">
         <textarea id="criteria-textarea" placeholder="Enter markdown..."></textarea>
       </div>

       <!-- Action buttons -->
       <div class="focus-area-actions">
         <button class="btn-edit" id="btn-edit-focus">✏️ Edit Criteria</button>
         <button class="btn-save" id="btn-save-focus" style="display:none;">💾 Save Changes</button>
         <button class="btn-cancel" id="btn-cancel-focus" style="display:none;">✕ Cancel</button>
       </div>

       <!-- Last updated time -->
       <div class="focus-area-footer">
         <small id="last-updated">Last updated: --</small>
       </div>
     </div>
   </div>
   ```

3. Ensure the panel is styled with consistent card design (matching other panels on the page)

---

### UI-002: CSS Styling (1 hour)

**File:** `datahaven-web/public/css/planning.css`

**What to implement:**

1. **Card styling:**
   ```css
   .focus-area-panel {
     background: var(--card-bg, #fff);
     border: 1px solid var(--card-border, #ddd);
     border-radius: 8px;
     padding: 20px;
     margin-bottom: 24px;
     box-shadow: 0 1px 3px rgba(0,0,0,0.1);
   }
   ```

2. **Header styling:**
   ```css
   .focus-area-header {
     display: flex;
     justify-content: space-between;
     align-items: center;
     margin-bottom: 16px;
     border-bottom: 1px solid var(--divider, #eee);
     padding-bottom: 12px;
   }

   .focus-area-header h3 {
     margin: 0;
     font-size: 1rem;
     font-weight: 600;
   }
   ```

3. **Domain selector styling:**
   ```css
   .domain-selector {
     display: flex;
     align-items: center;
     gap: 12px;
     margin-bottom: 16px;
   }

   .domain-selector label {
     font-weight: 600;
     min-width: 80px;
   }

   .domain-selector select {
     flex: 1;
     padding: 8px 12px;
     border: 1px solid var(--input-border, #ccc);
     border-radius: 4px;
     font-size: 0.95rem;
   }

   .sync-status {
     font-size: 0.85rem;
     color: var(--text-muted, #666);
     margin-left: 8px;
   }
   ```

4. **Criteria display styling:**
   ```css
   .criteria-display {
     background: var(--bg-subtle, #f9f9f9);
     border: 1px solid var(--divider, #eee);
     border-radius: 4px;
     padding: 12px;
     margin-bottom: 16px;
     max-height: 300px;
     overflow-y: auto;
     font-size: 0.95rem;
     line-height: 1.5;
   }

   .criteria-display ul {
     margin: 8px 0;
     padding-left: 20px;
   }

   .criteria-display strong {
     font-weight: 600;
   }
   ```

5. **Criteria edit mode styling:**
   ```css
   .criteria-edit-mode {
     margin-bottom: 16px;
   }

   #criteria-textarea {
     width: 100%;
     min-height: 150px;
     padding: 12px;
     border: 1px solid var(--input-border, #ccc);
     border-radius: 4px;
     font-family: 'Courier New', monospace;
     font-size: 0.9rem;
     resize: vertical;
   }
   ```

6. **Button styling:**
   ```css
   .focus-area-actions {
     display: flex;
     gap: 8px;
     margin-bottom: 12px;
   }

   .btn-edit, .btn-save, .btn-cancel {
     padding: 8px 16px;
     border: 1px solid var(--btn-border, #ccc);
     background: var(--btn-bg, #f5f5f5);
     border-radius: 4px;
     cursor: pointer;
     font-size: 0.9rem;
     transition: all 0.2s ease;
   }

   .btn-save {
     background: var(--success-bg, #e6ffed);
     border-color: var(--success, #28a745);
     color: var(--success, #28a745);
   }

   .btn-cancel {
     background: var(--danger-bg, #ffe6e6);
     border-color: var(--danger, #dc3545);
     color: var(--danger, #dc3545);
   }

   .btn-edit:hover, .btn-save:hover, .btn-cancel:hover {
     opacity: 0.8;
   }
   ```

7. **Responsive design:**
   ```css
   @media (max-width: 768px) {
     .focus-area-panel {
       display: none; /* Hide on mobile — show "Desktop required" message instead */
     }

     .focus-area-mobile-notice {
       display: block;
       background: var(--warning-bg, #fff3cd);
       border: 1px solid var(--warning, #ffc107);
       padding: 12px;
       border-radius: 4px;
       text-align: center;
       color: var(--warning, #ffc107);
     }
   }
   ```

---

### UI-003: JavaScript Logic (2-3 hours)

**File:** `datahaven-web/public/js/planning-focus.js` (CREATE NEW) or extend existing `planning.js`

**What to implement:**

1. **Page initialization:**
   ```javascript
   async function initFocusAreaPanel() {
     await loadFocusArea();
     attachEventListeners();
   }

   document.addEventListener('DOMContentLoaded', initFocusAreaPanel);
   ```

2. **Load focus area data from API:**
   ```javascript
   async function loadFocusArea() {
     try {
       const response = await fetch('/api/planning/domain-focus', {
         headers: {
           'Authorization': `Bearer ${getAuthToken()}`
         }
       });

       if (response.status === 401) {
         console.warn('Auth token missing or invalid');
         return;
       }

       if (!response.ok) {
         throw new Error(`HTTP ${response.status}`);
       }

       const data = await response.json();
       renderFocusArea(data);
     } catch (error) {
       console.error('Failed to load focus area:', error);
       showToast('Failed to load focus area', 'error');
     }
   }
   ```

3. **Render focus area:**
   ```javascript
   function renderFocusArea(data) {
     // Set domain selector value
     document.getElementById('domain-select').value = data.domain;

     // Render criteria as HTML from markdown
     const criteriaHtml = renderMarkdown(data.criteria);
     document.getElementById('criteria-display').innerHTML = criteriaHtml;

     // Update last updated time
     const updatedAt = new Date(data.updated_at);
     document.getElementById('last-updated').textContent =
       `Last updated: ${updatedAt.toLocaleString()}`;
   }
   ```

4. **Render markdown to HTML:**
   ```javascript
   function renderMarkdown(markdown) {
     if (typeof marked === 'undefined') {
       console.warn('marked.js not loaded');
       return '<p>(Markdown not available)</p>';
     }

     // marked.js needs to be included via CDN or npm
     return marked.parse(markdown);
   }
   ```

5. **Domain dropdown change handler:**
   ```javascript
   document.getElementById('domain-select').addEventListener('change', async (e) => {
     const newDomain = e.target.value;

     try {
       const response = await fetch('/api/planning/domain-focus', {
         method: 'PUT',
         headers: {
           'Content-Type': 'application/json',
           'Authorization': `Bearer ${getAuthToken()}`
         },
         body: JSON.stringify({ domain: newDomain })
       });

       if (response.status === 429) {
         showToast('Too many requests. Please wait.', 'error');
         return;
       }

       if (!response.ok) {
         const error = await response.json();
         throw new Error(error.error || `HTTP ${response.status}`);
       }

       const data = await response.json();
       renderFocusArea(data);
       showToast('Domain updated', 'success');
     } catch (error) {
       console.error('Failed to update domain:', error);
       showToast(`Failed to update domain: ${error.message}`, 'error');
       // Revert dropdown to current value
       loadFocusArea();
     }
   });
   ```

6. **Edit button handler:**
   ```javascript
   document.getElementById('btn-edit-focus').addEventListener('click', () => {
     // Hide display, show edit mode
     document.getElementById('criteria-display').style.display = 'none';
     document.getElementById('criteria-edit').style.display = 'block';

     // Populate textarea with current criteria
     const currentCriteria = document.getElementById('criteria-display').textContent;
     document.getElementById('criteria-textarea').value = currentCriteria;

     // Hide edit button, show save/cancel
     document.getElementById('btn-edit-focus').style.display = 'none';
     document.getElementById('btn-save-focus').style.display = 'inline';
     document.getElementById('btn-cancel-focus').style.display = 'inline';
   });
   ```

7. **Save button handler:**
   ```javascript
   document.getElementById('btn-save-focus').addEventListener('click', async () => {
     const newCriteria = document.getElementById('criteria-textarea').value;

     try {
       const response = await fetch('/api/planning/domain-focus', {
         method: 'PUT',
         headers: {
           'Content-Type': 'application/json',
           'Authorization': `Bearer ${getAuthToken()}`
         },
         body: JSON.stringify({ criteria: newCriteria })
       });

       if (response.status === 429) {
         showToast('Too many requests. Please wait.', 'error');
         return;
       }

       if (!response.ok) {
         const error = await response.json();
         throw new Error(error.error || `HTTP ${response.status}`);
       }

       const data = await response.json();
       renderFocusArea(data);
       toggleEditMode(false);
       showToast('Criteria updated', 'success');
     } catch (error) {
       console.error('Failed to save criteria:', error);
       showToast(`Failed to save criteria: ${error.message}`, 'error');
     }
   });
   ```

8. **Cancel button handler:**
   ```javascript
   document.getElementById('btn-cancel-focus').addEventListener('click', () => {
     toggleEditMode(false);
   });
   ```

9. **Helper function for edit mode toggle:**
   ```javascript
   function toggleEditMode(editing) {
     if (editing) {
       document.getElementById('criteria-display').style.display = 'none';
       document.getElementById('criteria-edit').style.display = 'block';
       document.getElementById('btn-edit-focus').style.display = 'none';
       document.getElementById('btn-save-focus').style.display = 'inline';
       document.getElementById('btn-cancel-focus').style.display = 'inline';
     } else {
       document.getElementById('criteria-display').style.display = 'block';
       document.getElementById('criteria-edit').style.display = 'none';
       document.getElementById('btn-edit-focus').style.display = 'inline';
       document.getElementById('btn-save-focus').style.display = 'none';
       document.getElementById('btn-cancel-focus').style.display = 'none';
     }
   }
   ```

10. **Sync button handler:**
    ```javascript
    document.getElementById('btn-sync-focus').addEventListener('click', async () => {
      const btn = document.getElementById('btn-sync-focus');
      btn.disabled = true;
      btn.textContent = '⏳ Syncing...';

      try {
        await loadFocusArea();
        showToast('Synced with server', 'success');
      } catch (error) {
        showToast('Sync failed', 'error');
      } finally {
        btn.disabled = false;
        btn.textContent = '🔄 Sync';
      }
    });
    ```

11. **Toast notification helper:**
    ```javascript
    function showToast(message, type = 'info') {
      // Implementation depends on existing toast system in your app
      // If none exists, create a simple overlay div
      const toast = document.createElement('div');
      toast.className = `toast toast-${type}`;
      toast.textContent = message;
      toast.style.cssText = `
        position: fixed;
        bottom: 16px;
        right: 16px;
        padding: 12px 16px;
        background: ${type === 'success' ? '#28a745' : type === 'error' ? '#dc3545' : '#17a2b8'};
        color: white;
        border-radius: 4px;
        z-index: 10000;
        animation: slideIn 0.3s ease;
      `;
      document.body.appendChild(toast);
      setTimeout(() => toast.remove(), 3000);
    }
    ```

---

### UI-004: Include marked.js Library (30 min)

**File:** `datahaven-web/public/planning.html`

**What to implement:**

1. Add marked.js CDN link to the `<head>` section:
   ```html
   <script src="https://cdn.jsdelivr.net/npm/marked/marked.min.js"></script>
   ```

   OR if using npm:
   ```bash
   npm install marked
   ```
   And import in your JS:
   ```javascript
   import { marked } from 'marked';
   ```

2. Verify marked.js is loaded before calling `renderMarkdown()`

---

## Testing Requirements

### Manual Integration Tests (1 hour)

**Test scenarios:**

1. ✅ Page loads → Focus Area Panel visible above Pipeline Overview
2. ✅ Panel displays current domain (e.g., "manufacturing")
3. ✅ Panel displays criteria as formatted markdown (bold, bullets)
4. ✅ Domain dropdown selection → API PUT call → page updates
5. ✅ Edit button → shows textarea with current criteria
6. ✅ Save button → API PUT call → criteria updates
7. ✅ Cancel button → hides textarea without saving
8. ✅ Sync button → API GET call → refreshes from server
9. ✅ Invalid auth → error toast message
10. ✅ Rate limit exceeded (11+ saves in 1 min) → 429 error toast
11. ✅ Mobile view (<768px) → panel hidden or "Desktop required" message
12. ✅ XSS attempt in criteria (manual): `<script>alert('xss')</script>` → not executed

---

## Success Criteria

- ✅ Panel appears at top of Planning page with correct styling
- ✅ Domain selector working (changes reflected immediately)
- ✅ Criteria display shows formatted markdown
- ✅ Edit mode toggle working (button state changes)
- ✅ Save/cancel buttons persist/discard changes
- ✅ Sync button refreshes from server
- ✅ Toast notifications appear for success/error
- ✅ Rate limiting respected (no spam on save)
- ✅ Mobile responsive (hidden on <768px)
- ✅ No JavaScript errors in browser console
- ✅ Auth token properly sent in all requests

---

## Important Notes

**API dependency:** Backend API (MSG-BACKEND-048) must be implemented first.
- You can start UI work in parallel, but testing requires API to be ready

**marked.js:** Used for markdown → HTML rendering. Lightweight and widely used.

**Auth token:** Use existing `getAuthToken()` function or token storage from your app.

**Existing toast system:** If the app has a toast/notification system, use that instead of creating new one.

---

## Definition of Done

- [ ] HTML structure added to `planning.html`
- [ ] CSS styling complete in `planning.css`
- [ ] JavaScript logic implemented in `planning-focus.js` or extended `planning.js`
- [ ] marked.js library included (CDN or npm)
- [ ] All button handlers working
- [ ] API integration tested (mock with Postman first if needed)
- [ ] Toast notifications working
- [ ] Mobile responsive (hidden on small screens)
- [ ] No console errors
- [ ] Manual tests passing (all 12 scenarios above)
- [ ] Outbox message written when complete

---

**Estimated completion:** 3-4 hours for UI implementation + 1 hour testing = **4-5 hours total**

When complete, write your DONE message to `terminals/frontend/outbox/YYYY-MM-DD_NNN_<slug>-done.md`
