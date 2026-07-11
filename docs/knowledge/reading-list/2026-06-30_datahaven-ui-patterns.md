# Datahaven UI Patterns — Reading List

> **Frontend terminál olvasnivaló** — Top 5 forrás a Datahaven UI v2 fejlesztéshez
>
> Kurátor: Librarian | Forrás: Explorer UX research (2026-06-30)

**Created:** 2026-06-30
**For:** Frontend terminal
**Context:** Datahaven Dashboard v2 UX pattern implementation

---

## 📚 TOP 5 FORRÁS

### 1. Grafana Dashboard Best Practices

**URL:** https://grafana.com/grafana/dashboards/

**Miért releváns a SpaceOS-nek:**
- **KPI card strip pattern** — Sticky header strip, 4-6 metrics "above the fold"
- **Real-time updates** — SSE/WebSocket integration best practices
- **Status-based coloring** — Green/orange/red semantic colors (production-ready patterns)

**Datahaven alkalmazás:**
- Dashboard oldal KPI strip: Active Terminals, Inbox Queue, Pipeline Health
- Real-time refresh (2-3 sec interval)
- Production dashboard grid layout (Bento grid inspiration)

**Kulcs tanulságok:**
- 4-6 KPI optimal (többnél elvész a fókusz)
- Sticky header (mindig látható, scroll-on-stable)
- Card onClick = drilldown capability (detail view)

---

### 2. LogRocket Drag-Drop UX Examples

**URL:** https://blog.logrocket.com/ux-design/drag-and-drop-ui-examples/

**Miért releváns a SpaceOS-nek:**
- **Drag-drop visual feedback** — Opacity change, shadow increase, drop zone highlight
- **Animation easing** — 200ms cubic-bezier (smooth, not jarring)
- **Mobile optimization** — Long-press = drag start (500ms threshold)

**Datahaven alkalmazás:**
- Kanban board dual-track (Discovery + Delivery swimlanes)
- Card drag-drop (idea → selected → debate → queue)
- Real-time collaboration (WebSocket sync, optimistic updates)

**Kulcs tanulságok:**
- Immediate visual feedback (user knows action registered)
- 60 FPS drag performance (hardware-accelerated CSS)
- Conflict resolution tooltip ("User X is moving this card")

**Technikai stack javasolt:**
- React: dnd-kit library (NOT react-dnd, deprecated)
- CSS: `transition: all 200ms cubic-bezier(0.4, 0, 0.2, 1)`
- WebSocket: broadcast `card:moved` event to all clients

---

### 3. SaaS Dashboard Design Examples 2026

**URL:** https://www.925studios.co/blog/saas-dashboard-design-examples-2026

**Miért releváns a SpaceOS-nek:**
- **Bento Grid layout** — Aszimmetrikus card grid (CSS Grid 12 column)
- **Dark-first design** — Dark theme first, light theme adapted (NOT inverse)
- **Data-dense patterns** — Progressive disclosure, expandable rows

**Datahaven alkalmazás:**
- Full dashboard redesign: KPI Strip + Kanban + Gantt + Sidebar + Alerts
- CSS Grid 12 column system (responsive breakpoints: 1200px, 768px, 480px)
- Dark theme default (`#1a1d23` bg, `#e5e7eb` text, WCAG AA contrast)

**Kulcs tanulságok:**
- Bento grid = flexible, asymmetric layout (not boring 2-column)
- Dark-first: design for dark, then adapt to light (preserves intent)
- Progressive disclosure: Show summary, expand on demand (reduce cognitive load)

---

### 4. Jira Kanban Board Patterns

**URL:** https://titanapps.io/blog/jira-kanban-board

**Miért releváns a SpaceOS-nek:**
- **Swimlane organization** — Multiple swimlanes (Discovery + Delivery tracks)
- **Card structure** — Title + Assignee + Status badge + Priority
- **Mobile-first UX** — Tabbed view (swap tracks), touch-optimized buttons (44×44px)

**Datahaven alkalmazás:**
- Dual-track Kanban: Discovery (Ideas → Queue) + Delivery (7 terminals)
- Mobile experience: Tabbed view (swipe to switch), long-press drag
- Card detail modal: Long-press or tap card (full task metadata)

**Kulcs tanulságok:**
- Swimlane clarity: Max 2 tracks (Discovery + Delivery), more = confusion
- Card density: 3-5 cards per column optimal (scroll if more)
- Status badge: Icon + color (✅/⚠️/❌), not text alone (a11y)

---

### 5. Dark Mode UI Design Trends 2026

**URL:** https://midrocket.com/en/guides/ui-design-trends-2026/

**Miért releváns a SpaceOS-nek:**
- **Dark-first approach** — 80%+ mobile users enable dark mode by default
- **Eye strain reduction** — Near-black (`#1a1d23`) > pure black (#000), soft shadows
- **Color palette** — Neon accents (blues, greens) for highlights, muted text (`#9ca3af`)

**Datahaven alkalmazás:**
- Dark theme as default (light theme as alternate)
- WCAG AA compliance: Text contrast ≥ 4.5:1, AAA goal ≥ 7:1 for KPIs
- Status colors: Green (`#10b981`), Orange (`#f59e0b`), Red (`#ef4444`)

**Kulcs tanulságok:**
- Pure black (#000) = too harsh, use near-black (`#1a1d23`)
- Text: Light gray (`#e5e7eb`), NOT pure white (eye strain)
- Minimal animation (only on user interaction, no autoplaying loops)

---

## 🎯 OLVASÁSI SORREND (Frontend terminálnak)

### Week 1: Dashboard KPI Card
1. **Grafana Dashboard Best Practices** (30 perc olvasás)
2. DATAHAVEN_UI_PATTERNS.md — Pattern #1 (KPI Card System)
3. Implementáció: `KPICard.tsx` component + SSE endpoint

### Week 2: Kanban Drag-Drop
1. **LogRocket Drag-Drop UX** (20 perc olvasás)
2. **Jira Kanban Board Patterns** (15 perc olvasás)
3. DATAHAVEN_UI_PATTERNS.md — Pattern #2 (Kanban Real-Time)
4. Implementáció: dnd-kit integration + WebSocket sync

### Week 3: Dark-First Bento Grid
1. **SaaS Dashboard Design 2026** (25 perc olvasás)
2. **Dark Mode UI Trends 2026** (15 perc olvasás)
3. DATAHAVEN_UI_PATTERNS.md — Pattern #3 (Bento Grid)
4. Implementáció: CSS Grid layout + dark theme colors

---

## 🔗 BONUS FORRÁSOK (opcionális mélyülés)

| Terület | URL | Időigény |
|---------|-----|----------|
| **Gantt Chart Best Practices** | [asana.com/resources](https://asana.com/resources/gantt-chart-basics) | 20 perc |
| **Mobile-First Kanban 2026** | [any.do/blog](https://www.any.do/blog/top-kanban-boards-for-mobile-first-workflows-in-2026/) | 15 perc |
| **WCAG 2.1 Guidelines** | [w3.org/WAI/WCAG21](https://www.w3.org/WAI/WCAG21/quickref/) | 30 perc |
| **dnd-kit Documentation** | [dndkit.com](https://dndkit.com/) | 1 óra |

---

**Last updated:** 2026-06-30
**Next update:** Új UX pattern research után (2026 Q3)
**Maintained by:** Librarian (knowledge curator)
