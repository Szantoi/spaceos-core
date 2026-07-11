---
id: IDEA-2026-06-30-004
title: "Mobile-First Responsive Grid with Touch Gestures"
category: ux
priority: medium
effort: medium
domain: manufacturing
created: 2026-06-30
---

## Összefoglaló

Touch-optimalizált responsive layout minden Datahaven oldalra, swipe gesture-ökkel és mobile navigation pattern-nel.

## Probléma

A jelenlegi Dashboard desktop-first, mobil eszközökön nehézkes:
- Kanban swimlane-ek túl keskenyek
- Gantt timeline nem scrollozható horizontálisan touch-csal
- Navigation menu nem collapse-ol mobile-on
- Card actions túl kicsik ujjal megnyomni

## Megoldás

**Responsive Breakpoints:**
```css
/* Mobile first approach */
@media (min-width: 640px)  { /* sm */ }
@media (min-width: 768px)  { /* md */ }
@media (min-width: 1024px) { /* lg */ }
@media (min-width: 1280px) { /* xl */ }
```

**Mobile Layout (< 768px):**
1. **Dashboard page:**
   - Terminal cards stack vertically
   - 1 terminal = 1 card (full width)
   - Swipe left/right to switch terminal detail view

2. **Kanban page:**
   - Horizontal scroll swimlanes
   - Swipe left/right to navigate Discovery ↔ Delivery
   - Card full-width in mobile view

3. **Planning page:**
   - Ideas/Selected/Debate/Consensus tabs → horizontal scroll
   - Focus Panel collapsible (accordion)
   - Workflow tab Mermaid graph pinch-to-zoom

4. **Projects page:**
   - Gantt chart horizontal scroll + pinch-zoom
   - Timeline legend collapsible

**Touch Gestures:**
- Swipe left/right: Navigate views
- Long-press: Open card actions menu
- Pinch-to-zoom: Gantt/Mermaid graphs
- Pull-to-refresh: Refresh data

**Navigation:**
- Hamburger menu (mobile < 768px)
- Bottom tab bar (iOS/Android pattern)
- Sticky header with breadcrumbs

## Acceptance Criteria

- [ ] Mobile breakpoints CSS implementálva
- [ ] Dashboard terminal cards stack mobile-on
- [ ] Kanban horizontal scroll működik
- [ ] Swipe left/right navigation működik
- [ ] Long-press card actions menu működik
- [ ] Pinch-to-zoom Mermaid/Gantt működik
- [ ] Hamburger menu működik mobile-on
- [ ] Touch target size >= 44px (Apple HIG)
- [ ] Tested: iOS Safari, Android Chrome
