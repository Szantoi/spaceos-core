---
id: MSG-FE-056
from: root
to: fe
type: task
priority: high
status: READ
ref: MSG-ARCH-001-DONE
created: 2026-06-16
---

# FE-CORE Slice 1A — Mock fallback eltávolítás

## Kontextus

Az FE Domain Ownership Matrix (v1) alapján a FE-A terminál megkapja a **FE-CORE domain**-t:
Dashboard · Orders · Workflow · Production · Analytics · Design · Settings · Supervisor

## Feladat

**Slice 1A: Mock fallback eltávolítás** a meglévő PARTIAL page-ekből.

A pattern minden oldalon azonos:
```typescript
// VOLT (mock fallback):
const data = apiData ?? MOCK_DATA

// LESZ (real API only):
const data = apiData
// + proper loading state + error state, ha még nincs
```

### Prioritás sorrend

| # | Page | Mock konstans(ok) eltávolítandó | Backend endpoint |
|---|---|---|---|
| 1 | `OrdersPage.tsx` | `ORDERS` | `GET /joinery/api/orders?pageSize=50` |
| 2 | `WorkflowPage.tsx` | `FLOW_EPICS` | `GET /kernel/facilities/{id}/flow-epics?pageSize=50` |
| 3 | `DashboardPage.tsx` | `ORDERS`, `STATS_FALLBACK` | `GET /kernel/dashboard/stats` + `/joinery/api/orders?pageSize=5` |
| 4 | `ProductionPage.tsx` | `CUTTING_PLANS` | `GET /cutting/api/cutting/plans` |
| 5 | `AnalyticsPage.tsx` | `WASTE_DATA_FALLBACK` | `GET /cutting/api/cutting/waste` |
| 6 | `SupervisorPage.tsx` | — | `GET /api/tools/workstations` (Kernel, kész!) |
| 7 | `ProductionDashboardPage.tsx` | shopfloor mock-ok | meglévő `/cutting/*` + `/joinery/*` |

### Slice 1B feladatok (ugyanebben a taskban)

| # | Page | Feladat |
|---|---|---|
| 8 | `DesignPage.tsx` | Template wizard teljes API bekötés: `PUT /abstractions/api/modules/templates/{id}` + `POST /abstractions/api/modules/calculate` |
| 9 | `SettingsPage.tsx` | Roles/Partners/Stages szekciók: "Hamarosan" / "Nincs backend" badge UI, hogy ne mutassanak mock adatot |

## Elvárások

- Minden érintett page-en: loading skeleton (ha nincs), error state (ha nincs)
- Mock import-ok törlése ahol nincs más felhasználójuk
- Ha egy endpoint 404-et vagy 500-at ad → error state, NEM mock fallback
- A meglévő tesztek mind zöldek maradjanak
- Ahol a backend endpoint path bizonytalan → `[?]` komment és outbox-ban jelezd

## DoD

- Minden fent felsorolt page: mock fallback eltávolítva
- Build zöld (`npm run build`)
- Tesztek zöldek (`npm run test`)
- Outbox: `MSG-FE-056-DONE` — liste: melyik page-en mi változott, melyik endpoint path-ok lettek verifikálva vs [?]

## Skill / agent

Használd a `/spaceos-terminal` skillt. Sub-agent engedélyezett a page-enkénti munkához.
