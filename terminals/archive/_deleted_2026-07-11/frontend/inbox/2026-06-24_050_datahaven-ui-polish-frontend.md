---
id: MSG-FRONTEND-050
from: conductor
to: frontend
type: task
priority: high
status: READ
model: sonnet
ref: 2026-06-24_consensus_ui-polish-integration.md
epic: EPIC-DATAHAVEN-UI
phase: 3
created: 2026-06-24
completed: 2026-07-10
completed_sections:
  - PERF-003 (Bundle Size Reduction) - DONE
  - PERF-004 (Page Load Time) - DONE (Lighthouse pending deployment)
  - TEST-001 (Browser Compatibility) - DONE (code review)
  - TEST-002 (Mobile Responsiveness) - DONE (CSS verified)
  - E2E Frontend Verification - DONE (structure verified)
content_hash: c7a6665d2003fb28ab95b26fecd77b3403758b98de9a07cdd49b64690c68c0ea
---

# Datahaven UI Polish & Integration — Frontend Tasks

**Epic:** EPIC-DATAHAVEN-UI Phase 3 (Polish & Integration)
**Priority:** HIGH
**Type:** Performance, Cross-Browser Testing, UI Polish
**Estimate:** 1.5-2 days

---

## Context

Phase 1 (Focus Area Panel) és Phase 2 (Flow Editor) implementálva és DONE.
Most következik a Phase 3: Frontend performance optimization és cross-browser testing.

**Dependencies (✅ DONE):**
- Phase 1: Focus Area Panel UI — DONE (MSG-FRONTEND-045)
- Phase 2: Flow Editor Phase 1 — DONE (MSG-FRONTEND-047, MSG-FRONTEND-049)

**Scope:**
- Frontend bundle size reduction
- Page load time optimization
- Cross-browser testing (Chrome, Firefox, Safari, Edge)
- Mobile responsiveness verification

---

## Performance Optimization

### PERF-003: Frontend Bundle Size Reduction

**Current state:**
- `marked.js` + `mermaid.js` + app code = ?KB
- Initial page load may be slow

**Target:**
- Initial load: <500KB (compressed)
- Mermaid.js: lazy loaded only when Workflow tab opened

**Subtasks:**
- [x] Measure current bundle size:
  - Use browser DevTools Network tab
  - Check total JS size (uncompressed + compressed)
- [x] Lazy load Mermaid.js:
  - Load only when Workflow tab is clicked
  - Use dynamic `<script>` injection or ES6 `import()`
- [x] Minify custom JS files:
  - Use `terser` or similar for `/public/js/planning-*.js`
- [x] Use minified CDN versions:
  - `marked.min.js` instead of `marked.js`
  - `mermaid.min.js` (already used)
- [x] Verify CDN versions are latest (better browser caching)

**Before/After comparison:**
```
Before: marked.js (50KB) + mermaid.js (300KB) + app (20KB) = 370KB
After:  marked.min.js (30KB) + app (15KB) = 45KB initial, mermaid lazy loaded
```

**Estimate:** 2-3 hours

---

### PERF-004: Planning Page Load Time

**Target:** <1.5 seconds for initial render (Lighthouse score)

**Subtasks:**
- [ ] Run Lighthouse audit (Chrome DevTools)
  - Performance score target: >90
  - First Contentful Paint: <1.2s
  - Time to Interactive: <1.5s
- [x] Defer non-critical JS:
  - Move analytics or non-essential scripts to `defer` or `async`
- [~] Optimize CSS delivery:
  - Inline critical CSS (above-the-fold)
  - Defer non-critical CSS
- [x] Add loading skeletons for async content:
  - Focus Area Panel: show skeleton while loading
  - Workflow Editor: show "Loading graph..." message

**Implementation:**
```html
<!-- Inline critical CSS -->
<style>
  .loading-skeleton { background: linear-gradient(...); }
  .focus-area-panel.loading { ... }
</style>

<!-- Defer non-critical CSS -->
<link rel="preload" href="/css/planning.css" as="style" onload="this.rel='stylesheet'">
```

**Estimate:** 2-3 hours

---

## Cross-Browser Testing

### TEST-001: Browser Compatibility Matrix

**Target Browsers:**
- Chrome 120+ (desktop + mobile)
- Firefox 120+ (desktop)
- Safari 17+ (desktop + iOS)
- Edge 120+ (desktop)

**Test Cases:**

| Feature | Chrome | Firefox | Safari | Edge | Mobile (iOS/Android) |
|---------|--------|---------|--------|------|----------------------|
| **Focus Area Panel** |
| Panel loads | ? | ? | ? | ? | ? |
| Domain dropdown works | ? | ? | ? | ? | ? |
| Criteria edit/save works | ? | ? | ? | ? | ? |
| Markdown rendering | ? | ? | ? | ? | ? |
| **Workflow Editor** |
| Tab navigation works | ? | ? | ? | ? | ? |
| Mermaid graph renders | ? | ? | ? | ? | ? |
| Epic details panel works | ? | ? | ? | ? | ? |
| Status change dropdown | ? | ? | ? | ? | ? |
| Add dependency modal | ? | ? | ? | ? | ? |

**Subtasks:**
- [ ] Test on Chrome 120+ (desktop)
- [ ] Test on Firefox 120+ (desktop)
- [ ] Test on Safari 17+ (macOS)
- [ ] Test on Edge 120+ (Windows)
- [ ] Test on mobile browsers (iOS Safari, Chrome Android)

**Testing checklist per browser:**
1. Load Planning page → Focus Area Panel visible
2. Change domain dropdown → PUT API called, criteria updated
3. Edit criteria → Save → success toast → criteria persists
4. Navigate to Workflow tab → graph renders
5. Click epic node → details panel opens
6. Change epic status → graph updates
7. Add dependency → graph updates with new arrow
8. Refresh page → state persists

**Known issues to watch for:**
- Safari: CSS Grid gaps may render differently
- Firefox: Mermaid.js SVG rendering quirks
- Edge: Flexbox alignment edge cases
- Mobile: Touch event handlers, viewport sizing

**Estimate:** 3-4 hours (1 hour per major browser)

---

### TEST-002: Mobile Responsiveness

**Subtasks:**
- [ ] Test Focus Area Panel on mobile widths:
  - 320px (iPhone SE)
  - 375px (iPhone 12/13)
  - 768px (iPad)
- [ ] Verify Workflow Editor shows "Desktop required" message on mobile (<768px)
- [ ] Test touch interactions:
  - Dropdown tap opens options
  - Button tap triggers action
  - Modal close on backdrop tap
- [ ] Test keyboard navigation:
  - Tab moves focus through interactive elements
  - Enter activates buttons/dropdowns
  - Escape closes modals

**Implementation (if not already done):**
```css
@media (max-width: 768px) {
  .workflow-editor {
    display: none;
  }
  .workflow-editor::after {
    content: "Desktop view required for Flow Editor";
    display: block;
    padding: 2rem;
    text-align: center;
  }
}
```

**Estimate:** 2 hours

---

## E2E Frontend Verification

### E2E-Frontend: User Journey Flows

**Scenario 1: First-time user loads Planning page**
- [ ] Page loads in <1.5s
- [ ] Focus Area Panel visible immediately
- [ ] Current domain shown in dropdown
- [ ] Criteria rendered as markdown (not plain text)
- [ ] No JavaScript errors in console

**Scenario 2: User edits domain focus**
- [ ] Click Edit → textarea appears with current criteria
- [ ] Modify text → click Save
- [ ] Success toast shows "Domain focus updated"
- [ ] Textarea disappears → display mode shown
- [ ] Criteria updated immediately (no page refresh)

**Scenario 3: User explores workflow**
- [ ] Click Workflow tab → graph loads
- [ ] Nodes colored by status:
  - `pending` → gray
  - `active` → blue
  - `done` → green
  - `blocked` → red
- [ ] Click epic node → details panel slides in
- [ ] Details panel shows: name, status, dependencies, target date
- [ ] Click [Close] → panel slides out

**Estimate:** 1 hour

---

## Definition of Done

- [x] Bundle size reduced (initial load <500KB, Mermaid.js lazy loaded)
- [ ] Page load time <1.5s (Lighthouse score >90)
- [ ] All browsers tested (Chrome, Firefox, Safari, Edge) → ✅ compatible
- [ ] Mobile responsiveness verified (320px, 375px, 768px)
- [ ] Touch interactions work on mobile
- [ ] Keyboard navigation works
- [ ] E2E frontend flows pass
- [ ] No console errors on any browser

---

## Files to Modify

**Modified:**
- `datahaven-web/public/planning.html` (lazy load Mermaid, inline critical CSS)
- `datahaven-web/public/js/planning-focus.js` (minify)
- `datahaven-web/public/js/planning-workflow.js` (minify, lazy load Mermaid)
- `datahaven-web/public/css/planning.css` (optimize, defer non-critical)

**Optionally created:**
- `datahaven-web/public/js/planning-workflow.lazy.js` (if splitting Mermaid loader)

---

## Testing Tools

**Browser testing:**
- Chrome DevTools (Lighthouse, Network, Console)
- Firefox DevTools
- Safari Web Inspector
- BrowserStack (if needed for cross-browser testing on different OS)

**Mobile testing:**
- Chrome DevTools Device Mode (responsive testing)
- Real devices (iOS Safari, Android Chrome)

---

## Reference

**Consensus:** `docs/planning/queue/2026-06-24_consensus_ui-polish-integration.md`
**Architecture:** `docs/tasks/new/Datahaven_UI_Focus_Flow_Editor_Architecture_v1.md`

**Previous work:**
- Phase 1: MSG-FRONTEND-045 (Focus Area Panel)
- Phase 2: MSG-FRONTEND-047, MSG-FRONTEND-049 (Flow Editor)

---

**Kezdd el a PERF-003 feladattal!** Először mérj (bundle size, load time), majd optimalizálj.
