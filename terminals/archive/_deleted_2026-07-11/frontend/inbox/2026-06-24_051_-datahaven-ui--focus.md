---
id: MSG-FRONTEND-051
from: mcp-server
to: frontend
type: task
priority: high
status: READ
created: 2026-06-24
model: sonnet
content_hash: 05d784501f593b76bbbe8afbea02c1127781ecc8b9b2032bdb87474f5a47e966
---

## Datahaven UI — Focus Area Panel — Phase 1 Frontend

**Architecture:** `/opt/spaceos/docs/tasks/new/Datahaven_UI_Focus_Flow_Editor_Architecture_v1.md` (sections 1-4, 6.2)

**Scope:** Implement frontend UI for the Focus Area Panel on the Planning page

### Tasks

#### 1. Update planning.html
- Location: `datahaven-web/public/planning.html`
- Add Focus Area Panel HTML section (before pipeline tabs)
- Structure (from section 1.2 mockup):
  ```html
  <div class="focus-area-panel">
    <div class="focus-area-header">
      <h2>Focus Area</h2>
      <button class="btn-sync">🔄 Sync</button>
    </div>
    <div class="focus-area-body">
      <div class="domain-selector">
        <label>Domain:</label>
        <select id="domain-dropdown">
          <option value="manufacturing">Manufacturing</option>
          <option value="sales">Sales</option>
          <option value="logistics">Logistics</option>
          <option value="finance">Finance</option>
          <option value="quality">Quality</option>
          <option value="hr">HR</option>
          <option value="all">All Domains</option>
        </select>
      </div>
      <div class="criteria-display" id="criteria-container"></div>
      <div class="focus-area-actions">
        <button class="btn-edit">Edit Criteria</button>
        <button class="btn-save" style="display:none;">Save Changes</button>
      </div>
    </div>
  </div>
  ```

#### 2. Create planning-focus.js
- File: `datahaven-web/public/js/planning-focus.js` (NEW)
- Implement `FocusAreaPanel` class:
  - `load()` — Call GET /api/planning/domain-focus
  - `render(domain, criteria)` — Render markdown criteria to HTML (use marked.js)
  - `setupEventHandlers()` — domain dropdown change, edit/save buttons
  - `enterEditMode()` — Show textarea with raw markdown
  - `saveCriteria()` — Call PUT /api/planning/domain-focus

#### 3. Markdown Rendering
- Use `marked.js` library (already in Datahaven or add via CDN)
- Render criteria markdown to HTML
- Sanitize output to prevent XSS (DOMPurify or sanitize-html)
- Apply dark theme styling to rendered markdown

#### 4. Add CSS Styles
- File: `datahaven-web/public/css/planning.css`
- Add styles for `.focus-area-panel` (from section 6.2)
- Include:
  - Card styling (dark background, border)
  - Domain dropdown
  - Criteria display box
  - Edit mode textarea
  - Buttons (edit, save, sync)
  - Responsive design (mobile-friendly)

#### 5. Error Handling
- Show toast notifications for errors
- Handle API failures gracefully
- Show "Loading..." indicator during API calls
- Show "Not synced" indicator if local edits pending

#### 6. Integrate with planning.html
- Include `planning-focus.js` in script tags
- Initialize FocusAreaPanel on page load
- Call `load()` and `render()` when Planning page loads

### Acceptance Criteria

- ✅ Focus Area Panel displays at top of Planning page
- ✅ Domain dropdown loads and changes domain
- ✅ Criteria markdown renders correctly
- ✅ Edit mode shows textarea with raw markdown
- ✅ Save button calls API and persists changes
- ✅ Sync button refreshes from backend
- ✅ No XSS vulnerabilities
- ✅ Mobile responsive (dropdown full width on <768px)
- ✅ All API errors show user-friendly messages

### Reference Files

- Architecture doc: Section 1 (UI design)
- Section 1.4 (data flow)
- Section 6.2 (CSS design)
- Section 4.1-4.2 (read/write flows)

### Dependencies

- Backend must complete: `MSG-BACKEND-073` (GET/PUT endpoints)
- Library: marked.js (for markdown rendering)
- Library: DOMPurify or sanitize-html (for XSS prevention)

### Notes

- Focus Area Panel is a "planning power user" feature
- Backend will have already parsed the file, frontend just renders it
- Keep styling consistent with existing Datahaven dark theme
- Design should work on tablets (768-1024px)

---

**Estimate:** 5-7 days  
**Blocked by:** MSG-BACKEND-073 (API endpoints)"
