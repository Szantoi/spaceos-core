---
id: MSG-FRONTEND-034
from: conductor
to: frontend
type: task
priority: high
status: READ
model: sonnet
ref: Datahaven_UI_Focus_Flow_Editor_Architecture_v1
created: 2026-06-24
content_hash: a6c8b3ae520b67561bf0c527cbbee7ae0fc49ac6752ef01d4b8df9fa1a1b9f46
---

# Focus Area Panel - HTML Structure & CSS Styling

## Context

The Datahaven Planning page needs a new Focus Area Panel to display the planning domain configuration. This task implements the UI structure and styling (no API integration yet).

**Architecture Doc:** `/opt/spaceos/docs/tasks/new/Datahaven_UI_Focus_Flow_Editor_Architecture_v1.md` (sections 1.2, 6.2)

## Task (<2h)

Add HTML structure and CSS styling for the Focus Area Panel on the Planning page.

### Implementation Steps

1. **Add HTML to planning.html** (placement: above pipeline overview):
   ```html
   <!-- Focus Area Panel -->
   <div class="focus-area-panel">
     <div class="focus-area-header">
       <h3>Focus Area</h3>
       <button class="btn-sync" title="Sync with server">🔄 Sync</button>
     </div>
     <div class="focus-area-body">
       <div class="domain-selector">
         <label>Domain:</label>
         <select id="domain-select">
           <option value="manufacturing">manufacturing</option>
           <option value="sales">sales</option>
           <option value="logistics">logistics</option>
           <option value="finance">finance</option>
           <option value="quality">quality</option>
           <option value="hr">hr</option>
           <option value="all">all</option>
         </select>
       </div>
       <div class="criteria-section">
         <label>Criteria:</label>
         <div id="criteria-display" class="criteria-display">
           <!-- Markdown rendered here (mock for now) -->
           <ul>
             <li><strong>Felhasználói érték:</strong> Milyen funkció...</li>
             <li><strong>Backend kapcsolhatóság:</strong> Van-e már...</li>
           </ul>
         </div>
       </div>
       <div class="focus-area-actions" style="display:none;">
         <button class="btn-edit">Edit Criteria</button>
         <button class="btn-save" style="display:none;">Save Changes</button>
       </div>
     </div>
   </div>
   ```

2. **Add CSS styles** (append to `public/css/planning.css`):
   - Use design system from architecture doc section 6.2
   - Match existing Datahaven dark theme
   - Card-based layout with border-radius: 12px

3. **Placement**: Insert after the page header, before the pipeline overview section

4. **Verify responsive design**:
   - Desktop (>1024px): Full width, criteria 300px max-height
   - Tablet (768-1024px): Same layout, criteria 200px max-height
   - Mobile (<768px): Domain dropdown full width, criteria 150px max-height

### CSS Classes to Add

Reference architecture doc section 6.2 for the complete CSS. Key classes:

```css
.focus-area-panel { /* Card container */ }
.focus-area-header { /* Header with sync button */ }
.focus-area-body { /* Main content area */ }
.domain-selector { /* Domain dropdown row */ }
.criteria-display { /* Criteria list container */ }
.focus-area-actions { /* Button row */ }
.btn-edit, .btn-save { /* Action buttons */ }
```

### Color Palette (from existing styles.css)

- `--bg-card: #242b33` — Panel background
- `--border-color: #2f3336` — Borders
- `--text-primary: #e7e9ea` — Main text
- `--accent-blue: #1d9bf0` — Buttons, links

### Acceptance Criteria

- [ ] Focus Area Panel appears on Planning page
- [ ] Panel is positioned above the pipeline overview
- [ ] Domain dropdown shows all 7 options
- [ ] Criteria section displays mock markdown (bullets)
- [ ] Styling matches existing Datahaven dark theme
- [ ] Responsive layout works on desktop/tablet/mobile
- [ ] No JavaScript yet (static HTML/CSS only)

### File Locations

- **HTML file**: `datahaven-web/public/planning.html`
- **CSS file**: `datahaven-web/public/css/planning.css`

## Estimate

1.5-2 hours (HTML structure + CSS styling)

## Next Step

After this is done, the next task will be implementing the JavaScript to connect this UI to the API endpoint (GET /api/planning/domain-focus).

## Notes

- This task is **independent** of the backend API (use mock data for now)
- Focus on layout and visual design
- Backend is working on the API endpoint in parallel (MSG-BACKEND-045)
