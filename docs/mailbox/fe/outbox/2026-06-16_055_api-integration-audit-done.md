---
id: MSG-FE-055-DONE
from: fe
to: root
type: done
priority: high
status: READ
ref: MSG-FE-055
created: 2026-06-16
---

# FE-055 DONE — API Integration Audit elvégezve

## Összefoglalás

Az audit teljesítve. `API_INTEGRATION_STATUS.md` létrehozva a portal gyökerében.

## Eredmény

**36 page fájl** megvizsgálva:

| Kategória | Szám |
|---|---|
| ⚡ PARTIAL (real API + mock fallback) | 10 |
| ❌ MOCK only | 24 |
| — Nincs adat (auth/landing) | 2 |

## PARTIAL oldalak (már van API bekötés)

| Page | Endpoint(ek) |
|---|---|
| DashboardPage | `/kernel/dashboard/stats`, `/joinery/api/orders` |
| SalesPage | `/sales/api/quotes`, `/sales/api/customers` |
| OrdersPage | `/joinery/api/orders` |
| ProductionPage | `/cutting/api/cutting/plans` |
| AnalyticsPage | `/cutting/api/cutting/waste` |
| WorkflowPage | `/kernel/facilities/{id}/flow-epics` |
| InventoryPage | `/inventory/api/inventory/stock` |
| ProcurementPage | `/procurement/api/procurement/orders`, `/suppliers` |
| DesignPage | `/abstractions/api/modules/templates` |
| SettingsPage | 3 subpanel: audit, facilities, templates |

## Fő megállapítások

1. **Mock-first**: Mind a 27 világ megjelenít valódi UI-t mock adattal — backend bekötés külön feladat
2. **PARTIAL pattern**: `useApi<T>()` hook + `apiData ?? MOCK_DATA` fallback — ha API él, élő adatot mutat
3. **MovementsPage outlier**: Egyetlen oldal inline mock tömbbel (nem külső mocks fájlból)
4. **Prioritás-javaslat**: CRM, Finance, Projects, Logistics — üzleti kritikus, még csak mock

## Fájl

`frontend/joinerytech-portal/API_INTEGRATION_STATUS.md`
