---
id: MSG-FRONTEND-060
from: conductor
to: frontend
type: task
priority: high
status: READ
model: haiku
ref: MSG-CONDUCTOR-056
epic: DATAHAVEN-UI-V2
created: 2026-06-30
read: 2026-06-30
content_hash: 6c2549c256d5299aa266ed81cb014e796aff0fd730cd9b704e1aaf030fd8e8b6
---

# Focus Area Panel Component — Datahaven UI Phase 1

Implementáld a Focus Area Panel komponenst a Planning page-re, amely a domain fókuszt jeleníti meg és szerkeszti.

## Context

A Datahaven Planning page (`/planning.html`) kap egy új top section-t: **Focus Area Panel**.

Ez a panel a `/api/planning/domain-focus` API-t használja (Backend MSG-BACKEND-099 task-ja készíti el).

**Architektúra spec:** `/opt/spaceos/docs/tasks/active/Datahaven_UI_Focus_Flow_Editor_Architecture_v1.md`

## Task

Implementáld a következő komponenst a Planning page-re:

### 1. Focus Area Panel HTML

**Lokáció:** `datahaven-web/public/planning.html`

**Pozíció:** A meglévő "Pipeline Overview" section **ELŐTT**, ugyanazon a sávban (top section).

**HTML struktúra:**
```html
<div class="panel focus-area-panel">
  <div class="panel-header">
    <h3>Focus Area</h3>
    <button id="focus-sync-btn" class="btn-icon" title="Sync">🔄</button>
  </div>
  <div class="panel-body">
    <div class="form-group">
      <label for="domain-select">Domain:</label>
      <select id="domain-select" class="form-control">
        <option value="manufacturing">Manufacturing</option>
        <option value="sales">Sales</option>
        <option value="logistics">Logistics</option>
        <option value="finance">Finance</option>
        <option value="quality">Quality</option>
        <option value="hr">HR</option>
        <option value="all">All</option>
      </select>
    </div>

    <div class="form-group">
      <label>Criteria:</label>
      <div id="criteria-display" class="criteria-display"></div>
      <textarea id="criteria-edit" class="form-control criteria-textarea" rows="8" style="display:none;"></textarea>
    </div>

    <div class="panel-actions">
      <button id="edit-criteria-btn" class="btn-secondary">Edit Criteria</button>
      <button id="save-criteria-btn" class="btn-primary" style="display:none;">Save Changes</button>
      <button id="cancel-edit-btn" class="btn-secondary" style="display:none;">Cancel</button>
    </div>
  </div>
</div>
```

### 2. Focus Area Panel JavaScript

**Lokáció:** `datahaven-web/public/js/planning-focus.js` (új fájl)

**Funkciók:**
- `loadFocusArea()` — GET /api/planning/domain-focus → populate UI
- `saveFocusArea()` — PUT /api/planning/domain-focus → update file
- `toggleEditMode()` — Edit/View mode switch
- `renderMarkdown()` — Markdown → HTML (használd a `marked.js` library-t)

**Event handlers:**
- Domain dropdown change → auto-save
- Edit button → show textarea, hide display
- Save button → PUT API call, switch back to view mode
- Cancel button → discard changes, switch back to view mode
- Sync button → reload from API

**Dependencies:**
- `marked.js` library — Markdown rendering (CDN: https://cdn.jsdelivr.net/npm/marked/marked.min.js)

### 3. CSS Styling

**Lokáció:** `datahaven-web/public/css/planning.css`

**Új class-ok:**
```css
.focus-area-panel {
  margin-bottom: 2rem;
}

.criteria-display {
  background: var(--card-bg);
  border: 1px solid var(--border-color);
  border-radius: 4px;
  padding: 1rem;
  min-height: 120px;
  font-size: 0.9rem;
  line-height: 1.6;
}

.criteria-textarea {
  font-family: 'Monaco', 'Menlo', monospace;
  font-size: 0.85rem;
  resize: vertical;
}

.panel-actions {
  display: flex;
  gap: 0.5rem;
  margin-top: 1rem;
}
```

**Színek:** Használd a meglévő Datahaven CSS változókat (`--card-bg`, `--border-color`, `--accent-blue`).

## Acceptance Criteria

- [ ] Focus Area Panel megjelenik a Planning page tetején
- [ ] Domain dropdown működik, 7 opció elérhető
- [ ] Criteria display markdown-ként renderelődik
- [ ] Edit mode működik (textarea megjelenik)
- [ ] Save button frissíti az API-t
- [ ] Sync button újratölti az adatokat
- [ ] Mobile-friendly (responsive grid)
- [ ] Manual test: nyisd meg a `/planning.html` oldalt, válts domain-t, szerkeszd a criteria-t

## Technical Notes

**Meglévő minták:**
- `planning.html` — meglévő panel layout minták
- `planning.css` — meglévő form és button stílusok
- `planning.js` — meglévő API fetch patterns

**Marked.js használat:**
```javascript
// Include in HTML
<script src="https://cdn.jsdelivr.net/npm/marked/marked.min.js"></script>

// Render markdown
const html = marked.parse(criteriaMarkdown);
document.getElementById('criteria-display').innerHTML = html;
```

## BLOCKED BY

⚠️ **Ez a task BLOKKOLT amíg MSG-BACKEND-099 nincs kész!**

A Backend-nek el kell készítenie a `/api/planning/domain-focus` endpoint-okat.

**Javasolt workflow:**
1. Készítsd el a HTML/CSS strukturát most
2. Implementáld a JS függvényeket API mock-kal (hardcoded JSON)
3. Ha Backend DONE → kapcsold össze az éles API-val

## Next Steps

Ha ez kész, a következő Phase 2 task: Flow Editor tab Mermaid graph-al.

**Deadline:** High priority, blokkol további Planning UI fejlesztést!
