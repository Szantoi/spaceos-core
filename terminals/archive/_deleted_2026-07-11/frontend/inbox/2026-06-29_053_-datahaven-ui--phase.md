---
id: MSG-FRONTEND-053
from: mcp-server
to: frontend
type: task
priority: high
status: READ
created: 2026-06-29
model: sonnet
content_hash: f7d2902aef52b3e5c0e764d7c70b86a7370fee28425184f64f3757a1c3e130f8
---

## Datahaven UI — Phase 1, Task 3: Frontend Focus Area Panel HTML + JS Display

**Status:** ACTIVE (parallel with Backend Tasks 1-2)
**Estimated time:** 2 hours
**Ref:** docs/tasks/active/Datahaven_UI_Focus_Flow_Editor_Architecture_v1.md (sections 1, 6.2)

### Task
Implement the Focus Area Panel UI on the Planning page:
1. Add HTML markup to `planning.html` (new section at top)
2. Create `public/js/planning-focus.js` for interactivity
3. Implement domain dropdown (7 options)
4. Display criteria as rendered markdown
5. Show sync status indicator
6. Call GET /api/planning/domain-focus on page load

### Requirements

**Placement:** Planning page (`planning.html`), ABOVE the current pipeline overview section

**HTML Structure** (add before `#pipeline-overview` div):
```html
<div id="focus-area-panel" class="focus-area-panel">
  <div class="focus-area-header">
    <h2>Focus Area</h2>
    <button id="sync-btn" class="btn-sync">🔄 Sync</button>
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
    <div class="criteria-display" id="criteria-display">
      <!-- Markdown rendered here -->
    </div>
    <div class="sync-indicator" id="sync-indicator">
      Last sync: Never
    </div>
  </div>
</div>
```

**JavaScript Logic** (`public/js/planning-focus.js`):
1. On page load: `GET /api/planning/domain-focus`
2. Parse response JSON: `{ domain, criteria, updated_at }`
3. Set dropdown to `domain` value
4. Render `criteria` markdown as HTML (use `marked.js` library)
5. Update sync indicator: "Last sync: 2 minutes ago"
6. Add event listener to domain dropdown (not yet active — wait for Task 4)
7. Add event listener to Sync button → fetch fresh data

**Markdown Rendering:**
- Use existing `marked.js` library (already in codebase)
- Config: `{ breaks: true, gfm: true }` (GitHub-flavored markdown)
- Example input: `- **User value**: Helps users do X`
- Expected output: `<ul><li><strong>User value</strong>: Helps users do X</li></ul>`

**Error Handling:**
```javascript
GET /api/planning/domain-focus
  ✅ 200 → Parse JSON, render
  ❌ 401 → Show "Authentication failed"
  ❌ 500 → Show "Failed to load domain focus"
```

**CSS Classes** (already defined in styles.css, section 6.2):
- `.focus-area-panel` — main container
- `.focus-area-header` — title + sync button
- `.focus-area-body` — content area
- `.domain-selector` — dropdown wrapper
- `.criteria-display` — markdown rendered area
- `.sync-indicator` — "Last sync: X" text

### Implementation Checklist
1. Create `public/js/planning-focus.js`
2. Add HTML div to `planning.html` (above pipeline section)
3. Implement `loadFocusArea()` function
4. Implement markdown rendering with `marked.js`
5. Implement sync button click handler
6. Update sync indicator timestamp
7. Add error toast notifications (use existing toast library)
8. Test in browser: load page, see dropdown + criteria display
9. Test dropdown change (UI only, don't save yet)

### Files to Modify
- `datahaven-web/public/planning.html` — add HTML section
- `datahaven-web/public/js/planning-focus.js` — NEW file
- Check existing styles apply (no new CSS needed for Task 3)

### Acceptance Criteria
- ✅ HTML added to planning.html (above pipeline section)
- ✅ planning-focus.js created with loadFocusArea() function
- ✅ GET /api/planning/domain-focus called on page load
- ✅ Domain dropdown populated from API response
- ✅ Criteria rendered as HTML markdown
- ✅ Sync button works (re-fetches from API)
- ✅ Sync indicator shows timestamp
- ✅ Error toast shown if API fails
- ✅ Manual browser test: load planning.html, see panel with data
- ✅ Code merged to main

**Next:** After this lands, dispatch Task 4 (edit mode + save + CSS polish).

**Note:** Focus Area Panel CSS is already defined in `public/css/styles.css` (section 6.2). If any classes are missing, add them to that file.
