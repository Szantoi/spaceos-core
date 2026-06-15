---
id: 2026-06-15_047_projektek-logisztika-done
from: fe
to: root
type: done
priority: high
status: READ
ref_inbox: 2026-06-15_047_projektek-logisztika.md
date: 2026-06-15
---

# DONE — FE-047 Projektek + Logisztika világ implementálása

## Commit

`89da031` — feat: FE-047 Projektek + Logisztika világ implementálása

## Elvégzett munka

### ProjectsPage.tsx (új fájl)
- `ProjectStatusPill` — státusz badge (draft/active/install/done/on_hold)
- `ProjectDetailSlideOver` — projekt részletek: szakág-függőségek (TRADE_META + DEP_STATUS_META), tételek összesítővel, install readiness badge, note
- `ProjectCard` — kártya: név, ügyfél, tervező, státusz pill, install readiness indikátor, szakág chip-ek, határidő + összeg
- `ProjectDashboard` — KPI kártyák (aktív projektek, lejárt határidő, átlagos fedezet), aktív projektek mini-lista
- `ProjectList` — projekt lista 5 státusz szűrővel (Összes/Vázlat/Folyamatban/Beépítésre kész/Lezárva/Felfüggesztve)
- `ProjectKanban` — 4 Kanban oszlop (Tervezett/Aktív/Beépítésre kész/Lezárva) kattintható kártyákkal
- `ProjectsWorldPage` — router: dash/list/kanban

### LogisticsPage.tsx (új fájl)
- `ShipStatusPill` — fuvar státusz badge (12 lehetséges állapot)
- `ShipTypeBadge` — fuvar típus badge (delivery/pickup/survey)
- `ShipStepper` — FSM lépés-jelző (típusonként különböző sorrendek, install flag figyelembe vételével)
- `ShipmentDetailSlideOver` — fuvar részletek: státusz + stepper, alapadatok, jármű/brigád, helyszín, rakomány, eseménynapló, átadás-átvétel (fotók, jegyzőkönyv, hiánytételek)
- `ShipmentRow` — fuvar sor: ügyfél, ID + áruleírás, dátum/időablak, jármű, státusz pill, delegáció jelzés
- `LogisticsDashboard` — KPI kártyák (mai túrák, úton, beosztásra vár, hiánytétel), mai túrák + következő fuvarok panel
- `ShipmentListPage` — kiszállítás/felmérés ill. beszállítás lista, keresés, státusz szűrő
- `LogisticsWorldPage` — router: dash/outgoing/incoming

### Mock adatok
- `src/mocks/projects.ts`: 6 projekt (draft/active/install/done/on_hold), TRADE_META, DEP_STATUS_META, PROJECT_STATUS_META
- `src/mocks/logistics.ts`: 7 fuvar seed (delivery×4, pickup×1, survey×1, delegált×1), SHIP_STATUS_META, SHIP_TYPE_META, SHIP_STEPS

### Navigáció + routing
- `worlds.ts`: projects (violet, folder ikon) + logistics (cyan, truck ikon), WORLD_ORDER 8→10
- `types/index.ts`: WorldKey `| 'projects' | 'logistics'`
- `HomeScreen.tsx`: Admin + Designer role-okhoz projects + logistics hozzáadva
- `App.tsx`: `/w/projects`, `/w/projects/:screen`, `/w/logistics`, `/w/logistics/:screen`
- `WorldShell.tsx`: violet + cyan bejegyzések az ACCENT_MAP-be
- `Icon.tsx`: folder, truck, route ikonok

## Tesztek

| Fájl | Tesztek |
|---|---|
| `ProjectsPage.test.tsx` | 16 teszt — dashboard, lista szűrő, kanban, SlideOver |
| `LogisticsPage.test.tsx` | 17 teszt — dashboard, kiszállítás/beszállítás lista, SlideOver, hiánytétel |
| **Összesen** | **455 teszt — mind zöld** |

```
Test Files  54 passed (54)
Tests       455 passed (455)
```

## Elfogadási kritériumok

- [x] Projektek + Logisztika világ megjelenik a Home-on (Admin/Designer role)
- [x] Projekt Kanban renderel mock adattal (6 projekt, 4 oszlop)
- [x] Szállítás lista renderel státusz pill-ekkel (7 fuvar, delivery/pickup/survey)
- [x] `pnpm build` hiba nélkül
- [x] `pnpm test` zöld (455 teszt, +33 az előző 422-höz képest)

## Megjegyzés

FE-B terminál (HR + Kontrolling) érintett fájljait (HrPage, ControllingPage, hr.ts, controlling.ts) nem módosítottam.
Közösen érintett fájlok (App.tsx, worlds.ts, HomeScreen.tsx) — nem konfliktusos módosítások.
