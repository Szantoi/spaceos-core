---
id: MSG-FE-047
from: root
to: fe
type: task
priority: high
status: READ
ref: MSG-FE-046
created: 2026-06-15
---

# FE-047 — Projektek + Logisztika világ implementálása

## Kontextus

FE-046 (CRM + Finance) elfogadva ✅ — 422 teszt zöld, commit `9070dc0`. Folytatás jön.

**Skill:** `/spaceos-terminal`  
**Sub-agent:** engedélyezett (verifier agent futtatáshoz)

## Prototípus fájlok (referencia)

```
/opt/spaceos/docs/tasks/new/joinerytech/
  page-projects.jsx         — Projekt lista + Kanban board + Gantt nézet
  page-projects-board.jsx   — Projekt Kanban (fázis oszlopok + task kártyák)
  page-projects-epic.jsx    — Epic detail + task breakdown
  page-proj-assembly.jsx    — Összeszereléssel kapcsolatos projekt nézet
  page-logistics.jsx        — Logisztika: szállítások, fuvarok, nyomkövetés
  page-logistics-2.jsx      — Logisztika detail SlideOver-ek
  data-logistics.js         — Logisztika store (mock: shipments, carriers, routes)
```

## Amit implementálni kell

### Projektek világ

**1. ProjectsPage (`src/pages/ProjectsPage.tsx`)**
- Projekt lista kártyás nézettel (Projekt neve + Ügyfél + Fázis + Határidő + Fedezet-sáv)
- Kanban board (fázis-oszlopok: Tervezett → Aktív → QC → Lezárt)
- Dashboard KPI: Aktív projektek / Lejárt határidő / Átlagos fedezet
- Stone + indigo/violet akcent — prototípus design language

**2. Navigáció**
- `projects` világ, violet akcent, ikon: `folder`
- Router: `/w/projects`, `/w/projects/:screen`
- HomeScreen kártya

**3. Mock adatok (`src/mocks/projects.ts`)**
- 5-6 projekt (tervezett, aktív, QC, lezárt fázisok mindegyikéből legyen)
- Minden projekthez: ügyfél, határidő, terv/tény fedezet, fázis

**4. SlideOver-ek**
- ProjectDetail SlideOver (feladatlista, fázis FSM gomb, fedezet bontás)
- NewProjectSheet (Ügyfél, típus, határidő, felelős)

### Logisztika világ

**1. LogisticsPage (`src/pages/LogisticsPage.tsx`)**
- Szállítások lista (státusz pill: planned/in-transit/delivered/failed)
- Fuvar KPI-k: Ma szállítandó / Késő / Teljesített
- Nyomkövetési mini-map helyett: státusz timeline (tervezett → felvéve → úton → átadva)

**2. Navigáció**
- `logistics` világ, cyan akcent, ikon: `truck`
- Router + HomeScreen kártya

**3. Mock adatok (`src/mocks/logistics.ts`)**
- A prototípus `data-logistics.js` alapján: 6-8 szállítás különböző státuszokkal
- Fuvarozó neve, rendszám, tervezett/tényleges átadás dátuma

**4. SlideOver-ek**
- ShipmentDetail (státusz timeline + tételek + fuvarozó adatok)
- NewShipmentSheet (Rendelésszám, fuvarozó, tervezett dátum)

## Tesztek

- `ProjectsPage.test.tsx` — lista render, Kanban render, SlideOver
- `LogisticsPage.test.tsx` — lista render, státusz filter, SlideOver
- `pnpm test` 422 → ~455 (kb. +30 teszt, minimum +1/komponens)

## DONE feltételek

- [ ] Projektek + Logisztika világ megjelenik a Home-on
- [ ] Projekt Kanban renderel mock adattal
- [ ] Szállítás lista renderel státusz pill-ekkel
- [ ] `pnpm build` hiba nélkül
- [ ] `pnpm test` zöld (422-nél több)

## Koordináció

FE-B terminál (fe2) a HR + Kontrolling világon dolgozik ugyanabban a branch-ben.

- **Ne módosítsd:** `src/pages/HrPage.tsx`, `src/pages/ControllingPage.tsx`, `src/mocks/hr.ts`, `src/mocks/controlling.ts`
- **Közösen érintheted:** `src/App.tsx`, `src/mocks/worlds.ts`, `src/components/layout/HomeScreen.tsx`
