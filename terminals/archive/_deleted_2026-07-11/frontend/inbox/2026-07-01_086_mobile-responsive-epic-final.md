---
completed: 2026-07-01
id: MSG-FRONTEND-086
from: conductor
to: frontend
type: task
priority: high
status: READ
injected: 2026-07-01
model: sonnet
ref: EPIC-DATAHAVEN-UI
epic_id: EPIC-DATAHAVEN-UI
checkpoint_id: CP-MOBILE
created: 2026-07-01
content_hash: 7c66b89db36bb1ae19d8c684c79a5e828f9a5e215a1e54429c3c082e86c123d0
---

# Mobile-First Responsive Grid — EPIC-DATAHAVEN-UI Final Checkpoint

> **Checkpoint:** CP-MOBILE — Mobile Responsive Complete
> **Epic Status:** 4/5 checkpoints done, ez az utolsó!

## Kontextus

Az EPIC-DATAHAVEN-UI eddig elkészült komponensei:
- ✅ CP-BENTO: Bento Grid Layout (MSG-FRONTEND-064)
- ✅ CP-KPI: KPI Card System (MSG-FRONTEND-083)
- ✅ CP-COST: Cost Budget Widget (MSG-FRONTEND-084)
- ✅ CP-REALTIME: Real-time Metrics (MSG-FRONTEND-067)
- ⏳ **CP-MOBILE: Mobile Responsive** ← Ez a feladat!

## Feladat

Touch-optimalizált responsive layout minden Datahaven Dashboard oldalra, swipe gesture-ökkel és mobile navigation pattern-nel.

### Probléma

A jelenlegi Dashboard desktop-first, mobil eszközökön nehézkes:
- Terminal cards nem stack-elnek mobile-on
- Kanban swimlane-ek túl keskenyek touch-ra
- Gantt timeline nem scrollozható horizontálisan
- Navigation menu nem collapse-ol mobile-on
- Card actions túl kicsik ujjal megnyomni

## Implementálandó Komponensek

### 1. Responsive Breakpoints

```css
/* Mobile first approach */
@media (min-width: 640px)  { /* sm - tablet portrait */ }
@media (min-width: 768px)  { /* md - tablet landscape */ }
@media (min-width: 1024px) { /* lg - desktop */ }
@media (min-width: 1280px) { /* xl - large desktop */ }
```

**Alkalmazás minden oldalra:**
- `datahaven-web/client/src/pages/DashboardPage.tsx`
- `datahaven-web/public/kanban.html` + `kanban.css`
- `datahaven-web/public/planning.html` + `planning.css`
- `datahaven-web/public/projects.html` + `projects.css`

### 2. Dashboard Page Mobile Layout

**File:** `datahaven-web/client/src/pages/DashboardPage.tsx`

**Changes needed:**
- Terminal cards grid → stack vertically on mobile
- KPI Strip: 6 cols desktop → 3 tablet → 2 mobile
- Bento Grid: 2-col → 1-col on mobile
- System Health widget: full-width on mobile
- Cost Budget widget: full-width on mobile

**CSS additions:**
```css
@media (max-width: 768px) {
  .dashboard-grid {
    grid-template-columns: 1fr; /* single column */
  }

  .kpi-strip {
    grid-template-columns: repeat(2, 1fr); /* 2 cols */
  }

  .terminal-card {
    width: 100%;
  }
}

@media (max-width: 480px) {
  .kpi-strip {
    grid-template-columns: 1fr; /* 1 col */
  }
}
```

### 3. Kanban Page Mobile Layout

**Files:**
- `datahaven-web/public/kanban.html`
- `datahaven-web/public/css/kanban.css`

**Changes needed:**
- Discovery/Delivery tracks → horizontal scroll
- Swimlane cards: full-width mobile
- Swipe left/right: navigate tracks
- Touch target size >= 44px

**CSS additions:**
```css
@media (max-width: 768px) {
  .kanban-board {
    overflow-x: auto;
    -webkit-overflow-scrolling: touch;
  }

  .swimlane {
    min-width: 280px; /* stack cards */
  }

  .kanban-card {
    width: 100%;
    min-height: 44px; /* touch target */
  }
}
```

### 4. Planning Page Mobile Layout

**Files:**
- `datahaven-web/public/planning.html`
- `datahaven-web/public/css/planning.css`

**Changes needed:**
- Focus Area Panel: collapsible accordion
- Idea/Selected/Debate tabs: horizontal scroll
- Workflow Mermaid: pinch-to-zoom

**CSS additions:**
```css
@media (max-width: 768px) {
  .focus-panel {
    position: static; /* not sticky */
    max-height: 200px; /* collapsible */
  }

  .planning-tabs {
    overflow-x: auto;
    -webkit-overflow-scrolling: touch;
  }

  .mermaid-diagram {
    touch-action: pinch-zoom;
  }
}
```

### 5. Projects Page Mobile Layout

**Files:**
- `datahaven-web/public/projects.html`
- `datahaven-web/public/css/projects.css`

**Changes needed:**
- Gantt chart: horizontal scroll + pinch-to-zoom
- Timeline legend: collapsible
- Project cards: full-width

**CSS additions:**
```css
@media (max-width: 768px) {
  .gantt-container {
    overflow-x: auto;
    -webkit-overflow-scrolling: touch;
  }

  .gantt-timeline {
    min-width: 800px; /* scroll horizontally */
  }

  .project-card {
    width: 100%;
  }
}
```

### 6. Touch Gestures (Optional — P2)

**React Hook:** `useSwipeGesture.ts`

```typescript
export function useSwipeGesture(onSwipeLeft: () => void, onSwipeRight: () => void) {
  const [touchStart, setTouchStart] = useState(0);
  const [touchEnd, setTouchEnd] = useState(0);

  const handleTouchStart = (e: TouchEvent) => {
    setTouchStart(e.targetTouches[0].clientX);
  };

  const handleTouchMove = (e: TouchEvent) => {
    setTouchEnd(e.targetTouches[0].clientX);
  };

  const handleTouchEnd = () => {
    if (touchStart - touchEnd > 150) onSwipeLeft();
    if (touchStart - touchEnd < -150) onSwipeRight();
  };

  return { handleTouchStart, handleTouchMove, handleTouchEnd };
}
```

**Usage:** Dashboard terminal card swipe navigation

### 7. Mobile Navigation (Optional — P2)

**Hamburger Menu:**
- Collapse nav links on mobile
- Bottom tab bar (iOS/Android pattern)
- Sticky header with breadcrumbs

## Acceptance Criteria

**P1 — Core Responsive (Kötelező):**
- [ ] Mobile breakpoints CSS minden oldalon (Dashboard, Kanban, Planning, Projects)
- [ ] Dashboard terminal cards stack mobile-on (1 col)
- [ ] KPI Strip: 6 → 3 → 2 cols responsive
- [ ] Kanban horizontal scroll működik touch-csal
- [ ] Gantt chart horizontal scroll + touch support
- [ ] Touch target size >= 44px minden interaktív elemen
- [ ] Build sikeres, TypeScript 0 errors

**P2 — Touch Gestures (Optional):**
- [ ] Swipe left/right navigation implementálva
- [ ] Long-press card actions menu
- [ ] Pinch-to-zoom Mermaid/Gantt graphs
- [ ] Hamburger menu mobile-on

**Testing:**
- [ ] Chrome DevTools mobile simulator (375x667 - iPhone SE)
- [ ] Firefox Responsive Design Mode (768x1024 - iPad)
- [ ] Manual test: iOS Safari + Android Chrome (if available)

## Design System Conformance

**CSS Variables használata kötelező:**
```css
/* Spacing */
--space-xs: 4px;
--space-sm: 8px;
--space-md: 16px;
--space-lg: 24px;

/* Breakpoints */
--breakpoint-sm: 640px;
--breakpoint-md: 768px;
--breakpoint-lg: 1024px;
--breakpoint-xl: 1280px;
```

## Files To Modify

**Priority 1 (Core):**
1. `datahaven-web/client/src/pages/DashboardPage.tsx` — React components
2. `datahaven-web/client/src/index.css` — Global responsive styles
3. `datahaven-web/public/css/kanban.css` — Kanban responsive
4. `datahaven-web/public/css/planning.css` — Planning responsive
5. `datahaven-web/public/css/projects.css` — Projects responsive

**Priority 2 (Optional):**
6. `datahaven-web/client/src/hooks/useSwipeGesture.ts` — Touch gesture hook
7. `datahaven-web/client/src/components/MobileNav.tsx` — Hamburger menu

## Next Steps After DONE

Ha ez a task DONE:
1. **Epic complete:** EPIC-DATAHAVEN-UI status → `done` ✅
2. **Checkpoint done:** CP-MOBILE trigger → root + conductor notification
3. **Subscription cleanup:** unsubscribe EPIC-DATAHAVEN-UI watchers
4. **Epic summary:** Conductor készít epic summary report-ot

## References

- Mobile-first CSS: https://developer.mozilla.org/en-US/docs/Web/Progressive_web_apps/Responsive/Mobile_first
- Touch target size: https://web.dev/accessible-tap-targets/
- Swipe gestures: https://developer.mozilla.org/en-US/docs/Web/API/Touch_events
- Apple HIG: https://developer.apple.com/design/human-interface-guidelines/ios

---

**Estimated Time:** 3-4 óra (P1 core responsive)
**Epic Completion:** 5/5 checkpoints ✅

Frontend terminál, ez az EPIC utolsó lépése. Hajrá! 🚀

---

## Completion Report
*2026-07-01T12:05:26.331Z*

### Summary
Mobile-First Responsive Grid implementálva. 4 CSS fájl módosítva (index.css, kanban.css, planning.css, projects.css). Breakpoints: sm(640px), md(768px), lg(1024px), xl(1280px). Touch scroll + pinch-zoom minden oldalon. Build: 0 errors.

### Files Changed
- `datahaven-web/client/src/index.css`
- `datahaven-web/public/css/kanban.css`
- `datahaven-web/public/css/planning.css`
- `datahaven-web/public/css/projects.css`

