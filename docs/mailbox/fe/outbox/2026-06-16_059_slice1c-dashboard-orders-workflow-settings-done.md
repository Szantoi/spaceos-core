---
id: MSG-FE-059-DONE
from: fe
to: root
type: done
priority: high
status: READ
ref: MSG-FE-059
created: 2026-06-16
---

# FE-059 DONE — Slice 1C: Dashboard + Orders + Workflow + Settings + Analytics mock-mentes

## Összefoglalás

5 oldal mock fallback eltávolítva. Build zöld, 725/725 teszt zöld (+1 új teszt az AnalyticsPage EndpointPending bannerhez).

## Oldalak — mit változtattunk

### 1. DashboardPage ✅ mock-mentes
- `SPARKS` import törölve a `../mocks/data`-ból
- Az összes sparkline adat (`SPARKS.ordersToday`, `.inProduction`, `.stockAlerts`, `.wasteRate`, `.oee`, `.capacity`) → `[]` üres tömbre cserélve
- `I18N` megtartva (fordítások, nem adatfallback)
- `recentOrders = apiOrdersPage?.items?.map(apiOrderToFe) ?? []` — már korábban tiszta volt
- `stats.*` → `?? '—'` — már korábban tiszta volt

### 2. OrdersPage ✅ mock-mentes (nem kellett változtatni)
- Már korábban tiszta: `displayOrders = apiPage?.items?.map(apiOrderToFe) ?? []`
- `I18N` fordítás import marad (nem adatfallback)
- Tényleges mock adat fallback nem volt a kódban

### 3. WorkflowPage ✅ mock-mentes
- `STAGES` import törölve a `../mocks/extra`-ból
- `STAGES` konstans inline beillesztve a fájlba (kanban oszlop konfiguráció: sales/survey/production/delivery/install)
- `displayEpics = apiEpics ?? epics` ahol `epics = []` — már korábban tiszta volt
- API hívás `GET /api/facilities/{facilityId}/flow-epics?pageSize=50` érintetlen

### 4. SettingsPage ✅ EndpointPending-re cserélve
- `MockDataNotice` komponens → `EndpointPending` komponensre cserélve (lokális definíció)
- Roles tab: `EndpointPending endpoint="GET /api/roles [?]"`
- Partners tab: `EndpointPending endpoint="GET /api/partners [?]"`
- Workflow tab: `EndpointPending endpoint="GET /api/workflow/stages [?]"`
- Működő integrációk érintetlen: UsersPanel, TemplatesPanel, AuditPanel, FacilitiesPanel, MachineParkPanel, CatalogPanel

### 5. AnalyticsPage ✅ mock-mentes + EndpointPending
- `SPARKS` import törölve a `../mocks/data`-ból
- Sparkline adatok → `[] as number[]`
- `machineRows` statikus tömb törölve → EndpointPending banner hozzáadva
- Waste endpoint `GET /cutting/api/cutting/waste` → érintetlen, már API-hoz kötött
- `I18N` megtartva (fordítások)

## Nyitott [?] endpointok

| Endpoint | Státusz |
|---|---|
| `GET /api/roles` | [?] Kernel RBAC roles endpoint nem létezik |
| `GET /api/partners` | [?] Partner management endpoint nem létezik |
| `GET /api/workflow/stages` | [?] Stage chain config endpoint nem létezik |
| `GET /cutting/api/cutting/waste/by-machine` | [?] Gép-szintű hulladék breakdown nem létezik |
| Sparkline history endpoints | [?] Trend adatok semmilyen servicenél nem léteznek |

## Build + Tesztek

- `pnpm build` — ✅ zöld (0 error)
- `pnpm test --run` — ✅ 725/725 zöld (+1 AnalyticsPage EndpointPending teszt)
- Tesztszám: 724 → 725 (+1)
