---
id: MSG-ARCH-002
from: root
to: architect
type: context
priority: high
model: opus
status: READ
ref: MSG-ARCH-001
created: 2026-06-16
---

# Kiegészítő kontextus: FE-055 API Integration Audit eredménye

Az MSG-ARCH-001 (Domain Ownership Matrix) tervezéséhez releváns friss adat.

## Audit eredmény (2026-06-16)

**36 page** megvizsgálva a `frontend/joinerytech-portal/src/pages/` mappában:

### ⚡ PARTIAL (már van API hívás, de mock fallback él)

| Page | Meglévő endpoint(ek) |
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
| SettingsPage | audit + facilities + templates subpanel |

### ❌ MOCK only (24 page)

CrmPage · FinancePage · ProjectsPage · LogisticsPage · MfgPrepPage · SupervisorPage · MasterdataPage · TradePage · InteriorPage · MaintenancePage · QualityPage · EhsPage · AttendancePage · HrPage · ControllingPage · ServicePage · TasksPage · DocsPage · AiPage · ExecBiPage · ShopPage · ShopFloorPage · warehouse/* · production/ProductionDashboardPage

## Teljes riport

`frontend/joinerytech-portal/API_INTEGRATION_STATUS.md`

## Konklúzió a matrix szempontjából

A PARTIAL page-ek mögé van már meglévő backend — ezek **Slice 1** jelöltek (integrálható most).  
A MOCK page-ek egy részéhez **nincs backend modul** (CRM, Finance, Projects, HR, Logistics, stb.) — ezek **Slice 2+** vagy új modul igény.

Ezt vedd figyelembe a FE Domain Ownership Matrix és az integrációs szekvencia tervezésekor.
