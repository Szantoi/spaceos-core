---
id: MSG-ARCH-001
from: root
to: architect
type: task
priority: high
model: opus
status: READ
ref: —
created: 2026-06-16
---

# FE Domain Ownership Matrix — tervezési feladat

## Kontextus

A joinerytech portal 27 világa LIVE — de az összes page jelenleg `window.sim` mock adattal fut.  
A backend **már modulonként fel van osztva** (Sales/5009, Joinery/5002, Procurement/5006, stb.).  
A frontend viszont **nincs domain szerint felosztva** — FE-A és FE-B csak ideiglenes build-szplit volt.

## Meglévő backend service-ek

| Service | Port | Nginx path | Tesztek |
|---|---|---|---|
| Kernel | 5000 | `/api/*` | 1186 |
| Orchestrator | 3000 | `/ai/*` | 121 |
| Joinery | 5002 | `/joinery/*` | 420 |
| Abstractions | 5003 | `/abstractions/*` | — |
| Inventory | 5004 | `/inventory/*` | — |
| Cutting | 5005 | `/cutting/*` | — |
| Procurement | 5006 | `/procurement/*` | 136 |
| Identity | 5008 | `/identity/*` | 63 |
| Sales | 5009 | `/sales/*` | 102 |

## Frontend pages (aktív)

```
/opt/spaceos/frontend/joinerytech-portal/src/pages/
  AiPage.tsx          AttendancePage.tsx   ControllingPage.tsx
  CrmPage.tsx         DashboardPage.tsx    DesignPage.tsx
  DocsPage.tsx        EhsPage.tsx          ExecBiPage.tsx
  FinancePage.tsx     HrPage.tsx           InteriorPage.tsx
  InventoryPage.tsx   LogisticsPage.tsx    MaintenancePage.tsx
  MasterdataPage.tsx  MfgPrepPage.tsx      OrdersPage.tsx
  ProcurementPage.tsx ProductionPage.tsx   ProjectsPage.tsx
  QualityPage.tsx     SalesPage.tsx        ServicePage.tsx
  SettingsPage.tsx    ShopFloorPage.tsx    ShopPage.tsx
  SupervisorPage.tsx  TasksPage.tsx        TradePage.tsx
  WorkflowPage.tsx    AnalyticsPage.tsx
```

Entitás-kapcsolatok (cross-domain linkek): `docs/tasks/new/joinerytech/ENTITY_LINKS.md`

## Feladat

Készíts **FE Domain Ownership Matrix** dokumentumot (`docs/tasks/new/FE_Domain_Ownership_Matrix_v1.md`) az alábbi struktúrával:

### 1. Page → Backend Module mapping

Minden page-hez:
- **Felelős backend module** (melyik service-ből él az adat)
- **Integrálható most** (van már backend) vs **Új modul kell** (deferred)
- **Prioritás** (üzleti érték + meglévő backend alapján)

### 2. FE Terminal domain split javaslat

A jelenlegi FE-A / FE-B ideiglenes. Javasold az új domain szerinti elosztást:
- Hány FE terminál legyen hosszú távon?
- Melyik terminál melyik domain-t fedi?
- Párhuzamosítható-e több terminál egyszerre?

### 3. Integrációs szekvencia

- **Slice 1** (most indítható, backend kész): mely page-ek, melyik sorrendben
- **Slice 2** (új modul kell először): mi szükséges, mi legyen a következő backend epic
- **Deferred** (alacsony prioritás / nincs üzleti nyomás most)

### 4. Javasolt új backend module-ok

Ha az elemzés alapján hiányzó modul-ok azonosíthatók, listázd:
- Mi a modul neve, scope-ja
- Milyen frontend page-eket szolgál ki
- Milyen függőségei vannak (Kernel, Sales, stb.)

## Referenciák

- `docs/tasks/new/joinerytech/ENTITY_LINKS.md` — entitás-kapcsolatok
- `docs/tasks/new/SpaceOS_Sales_FrontOffice_Contract_Reconciliation_v1.md` — Sales Slice 1 spec (kész)
- `docs/Codebase_Status.md` — teljes rendszer-állapot

## DoD

- `docs/tasks/new/FE_Domain_Ownership_Matrix_v1.md` létrejött
- Outbox üzenet Root-nak: `MSG-ARCH-001-DONE`
- A matrix alapján Root azonnal ki tud adni FE integrációs taskot
