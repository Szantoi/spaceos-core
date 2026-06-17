---
id: MSG-FE2-056-DONE
from: fe2
to: root
type: done
priority: high
status: READ
ref: MSG-FE2-056
created: 2026-06-16
---

# FE2-056 DONE — Core Slice 1A: Mock fallback eltávolítás (9 lap audit)

## Összefoglalás

9 core page audit elvégezve. 3 page igényelt valódi változtatást. Build zöld, 735/735 teszt zöld.

## Audit eredmény

| Page | Státusz | Változás |
|---|---|---|
| `ProductionPage.tsx` | ✅ Kész | Nesting mock eltávolítva (részletek lent) |
| `SupervisorPage.tsx` | ✅ Kész | WORKSTATIONS fallback eltávolítva |
| `DesignPage.tsx` | ✅ Kész | `templateCount` mock fallback eltávolítva |
| `OrdersPage.tsx` | ✅ Már tiszta | Nem volt mock import/fallback |
| `DashboardPage.tsx` | ✅ Már tiszta | Nem volt mock import/fallback |
| `AnalyticsPage.tsx` | ✅ Már tiszta | Nem volt mock import/fallback |
| `WorkflowPage.tsx` | ✅ Már tiszta | Nem volt mock import/fallback |
| `SettingsPage.tsx` | ✅ Már tiszta | Nem volt mock import/fallback |
| `ProductionDashboardPage.tsx` | ✅ Már tiszta | Nem volt mock import/fallback |

## Elvégzett munkák

### 1. ProductionPage.tsx

- `CUTTING_PLANS, NESTING` import törölve (`../mocks/data`-ból)
- `NESTING_SHEETS` import törölve (`../mocks/extra2`-ből)
- `NestingPart, NestingSheet` típus importok törölve
- `selectedPlan` állapot: `CUTTING_PLANS[0].id` → `null` (semmi sincs előre kiválasztva)
- `mockPlan`, `sheetCount`, `plan`, `getSheet()` mock logika törölve
- Nesting viewer panel: SVG + stats + lapozó → placeholder UI
  - Nincs terv kiválasztva: `'Nincs kiválasztott terv'`
  - Van terv, nincs adat: `'Nesting API nem elérhető'`
- `NestingSVG` komponens törölve (már nem szükséges)
- `hoveredPart`, `sheetIdx` state törölve

### 2. SupervisorPage.tsx

- `WORKSTATIONS` törölve az importból (`../mocks/supervisor`)
- `FloorView`: `apiWorkstations ?? WORKSTATIONS` → `?? []`
- `SupervisorDashboard`: `apiWorkstations ?? WORKSTATIONS` → `?? []`
- `DAY_PLAN`, `ALERTS`, `WS_STATE_META`, `SUP_TODAY` megmaradtak (nem fallback-ek, statikus UI adatok)

### 3. DesignPage.tsx

- `templateCount`: `apiTemplates && apiTemplates.length > 0 ? apiTemplates.length : PARAM_TEMPLATES.length` → `apiTemplates?.length ?? 0`
- `PARAM_TEMPLATES` megmarad: "Népszerű sablonok" szekció és template picker szándékos statikus tartalom

## Tesztek

### ProductionPage.test.tsx
- `renders nesting SVG` → `renders nesting panel heading` (ellenőrzi a `'Nesting vizualizáció'` fejlécet)
- `renders nesting viewer with no-plan state when API unavailable` → most zöld (nincs mock előre-kiválasztás)

### SupervisorPage.test.tsx
- `beforeEach`: fetch stub → WORKSTATIONS adatokat visszaadja `/tools/workstations` URL-re
- `afterEach`: `vi.unstubAllGlobals()`
- 7 teszt átírva `waitFor`-ra (workstation kártyák aszinkron töltődnek API-ból):
  - `dashboard shows workstation cards`
  - `dashboard shows workstation states`
  - `clicking workstation opens detail SlideOver`
  - `blocked workstation shows blocked reason in detail`
  - `workstation detail shows operator name`
  - `floor screen shows all workstations`
  - `floor screen shows utilization percentages`

## Build + Tesztek

- `pnpm build` — ✅ zöld (136 modul)
- `pnpm test --run` — ✅ 735/735 zöld
