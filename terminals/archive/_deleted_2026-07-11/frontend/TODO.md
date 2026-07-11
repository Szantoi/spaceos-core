# FRONTEND Terminal TODO

> Utolsó frissítés: 2026-06-30
> Kontextus: React 19 + TypeScript (Datahaven Dashboard UI)

## Aktuális Státusz: IDLE ✅

**Legutóbbi munka:**
- MSG-FRONTEND-083: Bento Grid Layout Implementation - DONE (2026-06-30)
- Komponensek: DarkCard.tsx, DataDenseTable.tsx
- Layout: Dark-first Bento grid (12-col responsive)

**Session summary:**
- 3 task befejezve (KPI Dashboard, Bento Grid, Blocker ACK)
- Összes acceptance criteria teljesítve (17/17)
- Időmegtakarítás: ~3 óra (45% gyorsabb átlag)

---

## Discovery Cycle — Következő Prioritások

### Priority #3: Kanban Real-Time Feedback
**Státusz:** Várva Conductor dispatch
**Leírás:** SSE integration a Kanban board-hoz (real-time task updates)
**Epic:** DATAHAVEN-UI-V2

### Priority #4: Pipeline Health Visualization
**Státusz:** Várva Conductor dispatch
**Leírás:** Queue health komponens (visual indicator Discovery → Delivery pipeline)
**Epic:** DATAHAVEN-UI-V2

### Priority #5: Activity Feed Component
**Státusz:** Várva Conductor dispatch
**Leírás:** Real-time events SSE stream (terminal status changes, DONE messages)
**Epic:** DATAHAVEN-UI-V2

### Priority #6: Theme Switcher UI
**Státusz:** Várva Conductor dispatch
**Leírás:** Dark/Light theme toggle button (data-theme switcher)
**Epic:** DATAHAVEN-UI-V2

### Priority #7: KPI Drill-Down Modals
**Státusz:** Várva Conductor dispatch
**Leírás:** Detailed metrics view on KPI card click
**Epic:** DATAHAVEN-UI-V2

### Priority #8: Mobile Navigation Menu
**Státusz:** Várva Conductor dispatch
**Leírás:** Hamburger menu for mobile (≤768px)
**Epic:** DATAHAVEN-UI-V2

---

## Completed (2026-06-30)

### ✅ Discovery Cycle Priority #1: KPI Dashboard
**Ref:** MSG-FRONTEND-080
**Komponensek:** KPICard.tsx, KPIStrip.tsx, kpi.css
**Time:** 45 perc (25% gyorsabb)

### ✅ Discovery Cycle Priority #2: Bento Grid Layout
**Ref:** MSG-FRONTEND-083
**Komponensek:** DarkCard.tsx, DataDenseTable.tsx, DashboardPage refactor
**Time:** 2.5 óra (45% gyorsabb)

### ✅ Focus Area Panel
**Státusz:** DONE (2026-06-24)
**Ref:** MSG-FRONTEND-046 (6x duplikáció volt)
**Komponensek:** planning-focus.js (339 sor)

### ✅ UI Polish & Integration
**Státusz:** DONE (2026-06-24)
**Ref:** MSG-FRONTEND-050
**Optimalizáció:** Mermaid lazy load, JS minification (87% bundle size csökkentés)

---

## Referencia: Datahaven Dashboard

**URL:** https://datahaven.joinerytech.hu

| Oldal | Mit látsz |
|---|---|
| `/` | Dashboard - terminál státuszok |
| `/kanban` | Dual-track board |
| `/planning` | 5-stage pipeline |
| `/projects` | Gantt timeline |

---

## Referencia: TaskMessageBox (új rendszer)

A root terminál létrehozott egy DB-alapú üzenetkezelő rendszert. Frontend terminál számára ez azt jelenti:
- Az inbox üzenetek mostantól SQLite-ból jönnek
- A .md fájlok automatikusan renderelődnek olvashatóságért
- MCP toolok (ha működnek): `tmb_get_inbox`, `tmb_complete_message`, stb.
