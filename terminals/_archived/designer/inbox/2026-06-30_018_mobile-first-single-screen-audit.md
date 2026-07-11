---
id: MSG-DESIGNER-018
from: root
to: designer
type: task
priority: high
status: READ
model: haiku
created: 2026-06-30
acknowledged_by: designer
acknowledged_at: 2026-06-30T23:59:59Z
processed_sessions: 7
deliverables_complete: 1
quality_score: 5.4/10
audit_type: mobile_first_single_screen
content_hash: 64ea82617247a0e9063d7268efbff60e4dd0074d54372812b25638060c6d3c73
---

# Mobile-First & Single-Screen Focus Audit — Datahaven Dashboard

## Új Koordinációs Szerepkör

A Designer terminál mostantól **koordinálja a vizuális megjelenést és UX minőséget**. Ez a feladat az első koordinációs audit.

## Audit Területek

### 1. Mobile-First Ellenőrzés

Vizsgáld meg a 4 Datahaven oldalt mobil nézetben:

| Oldal | URL | Mit ellenőrizz |
|-------|-----|----------------|
| Dashboard | `/` | KPI card-ok egymás alatt? Touch target 44px? |
| Kanban | `/kanban.html` | Drag-drop működik touch-csal? Swimlane váltó? |
| Planning | `/planning.html` | Focus panel responsive? Pipeline lépések elérhetők? |
| Projects | `/projects.html` | Gantt scrollozható? Projekt kártyák elérhetők? |

**Ellenőrizd:**
- [ ] Gombok és linkek minimum 44×44px touch target
- [ ] Egykezes használhatóság (fontos elemek hüvelykujj zónában)
- [ ] Swipe gesture támogatás ahol releváns
- [ ] Horizontal scroll kerülése (kivéve Gantt)

### 2. Single-Screen Focus Audit

Minden oldalon:
- **Mi az aktuális feladat?** — Csak az ehhez szükséges elemek jelennek meg?
- **Van-e felesleges elem?** — Ami elvonja a figyelmet?
- **Progresszív felfedés** — Részletek elrejtve, kattintásra megjelennek?

**Audit kérdések:**
- [ ] Dashboard: A terminál státusz az első pillantásra egyértelmű?
- [ ] Kanban: Egy kártya mozgatása egyszerű és egyértelmű?
- [ ] Planning: A pipeline lépések vizuálisan elkülönülnek?
- [ ] Projects: A Gantt timeline olvasható?

### 3. Desktop vs Mobile Különbségek

- [ ] Desktop verzió **bővíti** a mobilt (több info, de nem duplikál)
- [ ] Sidebar/panel layout megfelelő desktopra
- [ ] Responsive breakpointok: 480px (mobile), 768px (tablet), 1200px (desktop)

## Output

Készíts egy **UX Audit Report**-ot:

```markdown
# UX Audit Report — Datahaven Dashboard

## Mobile-First Értékelés

| Oldal | Touch Target | Egykezes | Swipe | Értékelés |
|-------|--------------|----------|-------|-----------|
| Dashboard | ✅/⚠️/❌ | ✅/⚠️/❌ | ✅/⚠️/❌ | jó/javítandó |

## Single-Screen Focus Értékelés

| Oldal | Felesleges Elem | Progresszív Felfedés | Értékelés |
|-------|-----------------|----------------------|-----------|
| Dashboard | ... | ... | ... |

## Prioritás Lista

### P1 — Kritikus (azonnal javítandó)
1. [Probléma és javítási javaslat]

### P2 — Fontos (következő sprintben)
1. ...

### P3 — Nice-to-have
1. ...

## Összefoglaló

[1-2 mondatos összefoglaló]
```

## Referenciák

- **CSS fájlok:** `datahaven-web/public/css/*.css`
- **HTML oldalak:** `datahaven-web/public/*.html`
- **Design System (kész):** `terminals/designer/outbox/2026-06-30_014_datahaven-design-system-done.md`
- **JoineryTech UI minták:** `docs/joinerytech/ui.jsx` (MobileBottomNav, touch patterns)

## Időkeret

**45 perc**
