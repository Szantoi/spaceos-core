---
completed: 2026-07-02
id: MSG-EXPLORER-001
from: conductor
to: explorer
type: task
priority: high
status: INJECTED
injected: 2026-07-03
injected: 2026-07-01
model: haiku
created: 2026-07-01
ref: MSG-CONDUCTOR-060
---

# Datahaven UI — Következő Modul Kutatás

## Kontextus

A Datahaven Dashboard fejlesztés során **8 új UI ötlet** érkezett (2026-06-30). Ezek mind különböző modulokat/feature-öket javasolnak a JoineryTech UI-hoz.

**Root stratégiai irányelvei:**
- **Haiku first** — költséghatékonyság prioritás
- **Egy modulra koncentrálva** — ne dolgozzon egyszerre több területen
- **Pontos tervezés előbb** — kutatás → tervezés → implementáció

## Feladat

Elemezd a `docs/planning/ideas/2026-06-30_*.md` fájlokban található **8 UI ötletet**, és határozd meg:

1. **Dependency Map** — Melyik ötlet függ melyik másiktól? (pl. Bento Grid → KPI Card System)
2. **Következő Logikus Modul** — A Bento Grid (MSG-FRONTEND-064, folyamatban) után melyik feature-t érdemes implementálni?
3. **Előfeltételek** — Van-e hiányzó backend API, library vagy design resource ami blokkol?
4. **Implementációs Sorrend Javaslat** — 3-4 feature-t prioritizálj (reasoning-gel)

## 8 Planning Idea Lista

```
2026-06-30_001_dashboard-kpi-card-system.md
2026-06-30_001_mermaid-flow-editor-interactive.md
2026-06-30_002_kanban-realtime-feedback.md
2026-06-30_002_realtime-metrics-dashboard.md
2026-06-30_003_dark-first-bento-layout.md         ← FOLYAMATBAN (MSG-FRONTEND-064)
2026-06-30_003_kanban-quick-actions-inline.md
2026-06-30_004_mobile-responsive-grid-touch.md
2026-06-30_005_cost-budget-tracker-widget.md
```

## Kutatási Irányelvek

1. **Dependency elemzés:**
   - Olvasd el mindegyik idea frontmatter-jét (priority, effort, domain)
   - Nézd meg melyik említi a másikat (pl. "building on Bento Grid")
   - Rajzolj egy függőségi gráfot (ASCII vagy Mermaid)

2. **Következő modul javaslat:**
   - **NE** választasz olyan feature-t ami:
     - Backend API-t igényel ami nincs kész
     - Túl komplex (effort: high)
     - Low priority
   - **Preferáld** azokat amik:
     - Medium/High priority ÉS medium effort
     - Újrafelhasználható komponenseket építenek (KPI Card, Real-time SSE, stb.)
     - Horizontal value — több oldalon is használható (Dashboard + Kanban + Projects)

3. **Előfeltételek check:**
   - Backend API-k: `/api/dashboard`, `/api/kanban`, `/api/projects` endpointok elérhetőek?
   - Design resources: Van-e Figma/spec a feature-höz vagy Librarian-nal kell koordinálni?
   - Library-k: Új NPM dependency szükséges? (pl. chart library, drag-drop)

4. **Implementációs sorrend:**
   - 1. feature: Bento Grid után logikus folytatás (pl. KPI Card System → Bento Grid-re építve)
   - 2. feature: Horizontal value (pl. Real-time SSE → minden board használhatja)
   - 3. feature: Nice-to-have de nem blokkoló (pl. Mobile responsive touch gestures)

## Kimenet (OUTBOX-ba)

Készíts egy **összefoglaló riportot**:

```markdown
# Datahaven UI — Következő Modul Kutatási Riport

## Dependency Map

[ASCII vagy Mermaid diagram]

## Következő Modul Javaslat

**Feature:** [név]
**Reasoning:** [2-3 mondat miért ez a legjobb]
**Előfeltételek:** [lista: backend API, design spec, library]
**Becsült effort:** [days]

## Implementációs Sorrend (Top 3)

1. **[Feature 1]** — [reasoning]
2. **[Feature 2]** — [reasoning]
3. **[Feature 3]** — [reasoning]

## Blokkolt Feature-ök

- **[Feature X]** — BLOCKER: [backend API/design spec missing]
```

## Acceptance Criteria

- [ ] Mind a 8 planning idea elolvasva
- [ ] Dependency map elkészítve (ASCII diagram)
- [ ] Következő modul kiválasztva (reasoning-gel)
- [ ] Előfeltételek listázva (backend API, design, library)
- [ ] Implementációs sorrend javaslat (top 3)
- [ ] Blokkolt feature-ök listázva
- [ ] OUTBOX-ba riport küldve

## Időkeret

**1-2 óra** (haiku model, lightweight kutatás)

---

**Conductor note:** Ez egy gyors kutatási feladat, NE mélyülj el iparági best practice-ekben vagy konkurens megoldásokban. Csak a 8 idea elemzése kell + dependency map + next module javaslat.

---

## Completion Report
*2026-07-02T21:46:51.205Z*

### Summary
Datahaven UI next module research complete - 8 ideas analyzed, KPI Card System recommended as next module after Bento Grid

### Implementation Details
Analyzed all 8 UI ideas from 2026-06-30. Dependency map created (Bento Grid → KPI Cards, SSE → Real-time features). TOP 3 implementation order: (1) KPI Card System - natural Bento continuation, horizontal value, no blockers; (2) Cost Budget Tracker Widget - quick win, small effort; (3) Real-time Metrics Dashboard - SSE foundation. Blockers flagged: Mermaid Flow Editor (LARGE effort), Kanban Real-time (needs SSE first). All acceptance criteria met.

### Files Changed
- `terminals/explorer/outbox/2026-07-02_046_datahaven-ui-next-module-research-done.md`

