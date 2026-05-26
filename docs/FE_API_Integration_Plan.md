# FE API Integrációs Terv

**Készült:** 2026-05-26  
**Frissítve:** 2026-05-26 — valódi endpoint audit alapján  
**Státusz:** AKTÍV  
**Cél:** A meglévő, mock adatokkal működő frontend oldalakat bekötni az élő backend API-kba.

---

## 1. Jelenlegi helyzet (aktuális állapot)

### Infrastruktúra — MINDEN FUT

| Komponens | Státusz | Megjegyzés |
|---|---|---|
| nginx proxy | ✅ Konfigurálva | `/api/`, `/joinery/`, `/inventory/`, `/cutting/`, `/procurement/`, `/abstractions/`, `/ai/`, `/auth/`, `/minio/` mind be van állítva |
| spaceos-kernel (5000) | ✅ RUNNING | 3+ hete folyamatosan fut |
| spaceos-joinery (5002) | ✅ RUNNING | |
| spaceos-abstractions (5003) | ✅ RUNNING | |
| spaceos-inventory (5004) | ✅ RUNNING | |
| spaceos-cutting-svc (5005) | ✅ RUNNING | |
| spaceos-procurement (5006) | ✅ RUNNING | |
| Keycloak (8080) | ✅ RUNNING | |

### Frontend — mi kész, mi hiányzik

| Réteg | Állapot |
|---|---|
| Auth (Keycloak OIDC PKCE) | ✅ Működik — Bearer token minden API hívásba kerül |
| `useApi<T>` hook | ✅ Kész — `url`, `token`, `isLoading`, `error`, `refetch` |
| `API_BASE` konstansok | ✅ Definiálva (`/api`, `/joinery`, `/cutting`, `/inventory`, `/procurement`, `/abstractions`, `/ai`) |
| `useMutation` hook | ❌ Hiányzik — POST/PUT műveletekhez kell |
| UI oldalak (13+) | ✅ Kész — de 100% mock adatokkal |
| Tenant/Facility kontextus | ❌ Hiányzik — `TenantInfoBar` hardcoded `'doorstar-kft'` stringet küld |

### Legfontosabb blokkoló

Az összes értelmes API hívás **`facilityId` (GUID)**-t igényel. A `/api/facilities/{facilityId}/flow-epics`, `/api/facilities/{facilityId}/work-stations` stb. mind facility-scopedek. A FE-nek login után azonnal meg kell szereznie:
1. `tenantId` — JWT claim-ből VAGY `GET /api/tenants` első eleméből
2. `facilityId` — `GET /api/tenants/{tenantId}/facilities` első eleméből

---

## 2. Valódi backend endpointok (audit alapján)

### Kernel (`/api/` → 5000)

| Endpoint | Módszer | Leírás | FE oldal |
|---|---|---|---|
| `GET /api/dashboard/stats` | GET | KPI összesítő (tenant/facility/workstation/flowEpic számlálók) | DashboardPage |
| `GET /api/tenants` | GET | Tenant lista | AuthContext bootstrap |
| `GET /api/tenants/{id}` | GET | Tenant részletek (név, tier) | SettingsPage → Cég |
| `GET /api/tenants/{id}/facilities` | GET | Telephely lista | SettingsPage → Telephely, bootstrap |
| `POST /api/tenants/{id}/facilities` | POST | Új telephely | SettingsPage |
| `GET /api/facilities/{id}/flow-epics` | GET | Rendelések/projektek listája | WorkflowPage, OrdersPage |
| `POST /api/facilities/{id}/flow-epics` | POST | Új rendelés/projekt létrehozása | NewOrderDrawer |
| `GET /api/flow-epics/{id}` | GET | Egy rendelés részletei | — |
| `PUT /api/flow-epics/{id}/title` | PUT | Átnevezés | WorkflowPage |
| `PUT /api/flow-epics/{id}/start` | PUT | Indítás (FSM léptetés) | WorkflowPage |
| `PUT /api/flow-epics/{id}/delegate` | PUT | Delegálás | WorkflowPage |
| `PUT /api/flow-epics/{id}/close` | PUT | Lezárás | WorkflowPage |
| `GET /api/facilities/{id}/work-stations` | GET | Géppark listája | SettingsPage → Géppark |
| `POST /api/facilities/{id}/work-stations` | POST | Új gép | SettingsPage |
| `GET /api/audit-events` | GET | Audit napló | SettingsPage → Audit |
| `GET /api/stages` | GET | Gyártási lépésdefiníciók | SettingsPage → Munkafolyamat |
| `GET /api/stage-chains` | GET | Stage chain sablonok | SettingsPage → Munkafolyamat |

> ⚠️ **Nem létezik:** `POST /api/flow-epics` (helyette: `POST /api/facilities/{id}/flow-epics`), `PUT /api/flow-epics/{id}/advance-stage` (helyette: `/start`, `/delegate`, `/close`), `GET /api/work-stations` (all — csak by-id)

### Joinery (`/joinery/` → 5002, belső prefix `/api/orders`)

| Endpoint | Módszer | Leírás | FE oldal |
|---|---|---|---|
| `GET /joinery/api/orders` | GET | Ajtó rendelések listája | OrdersPage (Sales) |
| `GET /joinery/api/orders/{id}` | GET | Egy rendelés részletei | — |
| `POST /joinery/api/orders` | POST | Új ajtó rendelés | NewOrderDrawer |
| `POST /joinery/api/orders/{id}/items` | POST | Tételek hozzáadása | — |
| `POST /joinery/api/orders/{id}/calculate` | POST | Kalkuláció indítása | DesignPage |
| `GET /joinery/api/orders/{id}/cutting-list` | GET | Szabász lista | ProductionPage |
| `GET /joinery/api/orders/{id}/process-plan` | GET | Gyártási folyamat | ProductionPage |
| `GET /joinery/api/orders/{id}/hardware-list` | GET | Vasalat lista | — |
| `GET /joinery/api/orders/{id}/material-req` | GET | Anyagszükséglet | InventoryPage |
| `POST /joinery/api/orders/{id}/submit` | POST | Véglegesítés | — |
| `GET /joinery/api/orders/{id}/manufacturing-sheet` | GET | Gyártási lap | ProductionPage |

### Inventory (`/inventory/` → 5004, belső prefix `/api/inventory`)

| Endpoint | Módszer | Leírás | FE oldal |
|---|---|---|---|
| `GET /inventory/api/inventory/stock` | GET | Készlet lista | InventoryPage → Anyagok |
| `GET /inventory/api/inventory/offcuts` | GET | Maradékok (egyszerű) | OffcutsPanel |
| `GET /inventory/api/inventory/offcuts/` | GET | Maradékok (részletes lista) | OffcutsPanel |
| `GET /inventory/api/inventory/offcuts/stats/summary` | GET | Maradék statisztika | InventoryPage KPI |
| `POST /inventory/api/inventory/movements/consumption` | POST | Anyagkivét rögzítése | MovementsPage |
| `POST /inventory/api/inventory/movements/inbound` | POST | Bevét rögzítése | MovementsPage |
| `POST /inventory/api/inventory/movements/offcut` | POST | Maradék rögzítése | MovementsPage |
| `GET /inventory/api/inventory/trend` | GET | Felhasználási trend | AnalyticsPage |
| `GET /inventory/api/inventory/reservations` | GET | Aktív foglalások | — |

> ⚠️ **Nem létezik:** `GET /api/inventory` — a valódi endpoint `GET /inventory/api/inventory/stock`. Az nginx `/inventory/` prefix hozzáadja a path-t.

### Cutting (`/cutting/` → 5005, belső prefix `/api/cutting`)

| Endpoint | Módszer | Leírás | FE oldal |
|---|---|---|---|
| `GET /cutting/api/cutting/planning/` | GET | Szabászati tervek listája | ProductionPage |
| `POST /cutting/api/cutting/planning/` | POST | Új vágóterv | ProductionPage |
| `GET /cutting/api/cutting/planning/{id}` | GET | Egy vágóterv részletei | ProductionPage |
| `GET /cutting/api/cutting/plans` | GET | Napi tervek | ProductionPage |
| `GET /cutting/api/cutting/plans/{date}` | GET | Adott nap tervei | ProductionPage |
| `POST /cutting/api/cutting/sheets` | POST | Vágólap beküldése | ShopFloorPage |
| `GET /cutting/api/cutting/sheets/{id}/status` | GET | Végrehajtás státusza | ShopFloorPage |
| `GET /cutting/api/cutting/sheets/{id}/nesting` | GET | Nesting eredmény | ShopFloorPage |
| `GET /cutting/api/cutting/waste` | GET | Hulladék riport | AnalyticsPage |
| `GET /cutting/api/cutting/adapters/health` | GET | Gép adapter állapot | ProductionPage |

> ⚠️ **Nem létezik:** `GET /api/cutting/executions` (futó feladatok listája), `GET /api/cutting/analytics` — ezek helyett a waste report + planning használandó.

### Procurement (`/procurement/` → 5006, belső prefix `/api/procurement`)

| Endpoint | Módszer | Leírás | FE oldal |
|---|---|---|---|
| `GET /procurement/api/procurement/orders` | GET | Aktív PO lista | ProcurementPage |
| `POST /procurement/api/procurement/orders` | POST | Új PO | ProcurementPage |
| `GET /procurement/api/procurement/orders/{id}` | GET | PO részletei | — |
| `GET /procurement/api/procurement/suppliers` | GET | Szállító lista | ProcurementPage |
| `POST /procurement/api/procurement/suppliers` | POST | Új szállító | ProcurementPage |
| `GET /procurement/api/procurement/prices` | GET | Szállítói árak | ProcurementPage |
| `POST /procurement/api/procurement/deliveries` | POST | Szállítás rögzítése | — |

> ⚠️ **Nem létezik:** `GET /api/procurement` egyetlen endpoint — külön szállítók és rendelések endpointok vannak.

### Abstractions (`/abstractions/` → 5003)

| Endpoint | Módszer | Leírás | FE oldal |
|---|---|---|---|
| `GET /abstractions/api/product-templates` | GET | Sablonlista | DesignPage |
| `GET /abstractions/api/product-templates/{id}` | GET | Sablon részletei | DesignPage |
| `POST /abstractions/api/product-templates` | POST | Új sablon | DesignPage |
| `GET /abstractions/api/product-templates/{id}/graph` | GET | Komponens gráf | DesignPage |
| `POST /abstractions/api/product-templates/{id}/calculate` | POST | Kalkuláció | DesignPage |
| `GET /abstractions/api/product-templates/{id}/cutting-list` | GET | Szabász lista | DesignPage |
| `GET /abstractions/api/product-templates/{id}/cnc-plan` | GET | CNC terv | DesignPage |

---

## 3. Tenant/Facility bootstrap — az első és legfontosabb feladat

A FE-nek login után egyszer le kell kérdeznie és kontextusba tennie a tenant + facility adatokat. Ezek nélkül az API hívások 90%-a nem tud futni.

```typescript
// src/auth/AuthContext.tsx kiegészítés — TenantContext

interface TenantContext {
  tenantId: string | null
  facilityId: string | null
  tenantName: string | null
}

// Login után:
// 1. GET /api/tenants → lista, first item → tenantId
// 2. GET /api/tenants/{tenantId}/facilities → lista, first item → facilityId
// 3. Ezeket context-ben tárolni, minden hook-ba átadni
```

**Keycloak claim opció:** Ha a Keycloak `portal-app` client mapperbe be van állítva `tenant_id` és `facility_id` claim, akkor ezek kiolvashatók `user.profile['tenant_id']`-ből is — gyorsabb, nem igényel API hívást.

**Döntés szükséges:** Keycloak claim vagy API bootstrap? (Javaslat: API bootstrap, mert rugalmasabb — nem kell Keycloak admin módosítás)

---

## 4. `useApi` hook — hiányosságok és javítások

### Jelenlegi állapot
A hook `refetch` funkciót ad vissza, de **NEM hív automatikusan** — minden komponensnek kell `useEffect`:

```typescript
const { data, refetch } = useApi<T>('/api/...')
useEffect(() => { refetch() }, []) // refetch-et NEM szabad a dep array-be tenni (végtelen loop)
```

### Szükséges: `useMutation` hook

```typescript
// src/hooks/useMutation.ts — hiányzik, kell
export function useMutation<TResult, TBody = unknown>(
  url: string,
  method: 'POST' | 'PUT' | 'DELETE' = 'POST'
) {
  const { token } = useAuth()
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const mutate = useCallback(async (body?: TBody): Promise<TResult | null> => {
    if (!token) return null
    setIsLoading(true)
    try {
      const res = await fetch(url, {
        method,
        headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
        body: body ? JSON.stringify(body) : undefined,
      })
      if (!res.ok) throw new Error(`HTTP ${res.status}`)
      return await res.json() as TResult
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Hiba')
      return null
    } finally {
      setIsLoading(false)
    }
  }, [url, token, method])

  return { mutate, isLoading, error }
}
```

---

## 5. Integrációs fázisok

### Fázis 0 — Bootstrap (blokkol mindent) ⚡

**Cél:** Tenant + facility kontextus minden komponensbe elérhetővé tétele.

1. `AuthContext.tsx` kiegészítés: login után `GET /api/tenants` → `GET /api/tenants/{id}/facilities` → `tenantId` + `facilityId` a contextbe
2. `useMutation` hook megírása
3. `useApi` auto-fetch pattern dokumentálva (`useEffect(() => { refetch() }, [])`)

**Fájlok:** `src/auth/AuthContext.tsx`, `src/hooks/useMutation.ts`

---

### Fázis 1 — Dashboard KPI-k

**Cél:** A DashboardPage valódi számokat mutasson.

1. `GET /api/dashboard/stats` → KPI kártyák (tenant/facility/workstation/flowEpic count)
2. A mock `SPARKS` sparkline adatok maradnak fallback-ként

**Fájlok:** `src/pages/DashboardPage.tsx`

---

### Fázis 2 — Rendelések és Workflow

**Cél:** A core üzleti folyamat élő adatokkal.

1. `GET /api/facilities/{facilityId}/flow-epics` → WorkflowPage kanban kártyák
2. `GET /joinery/api/orders` → Sales → Rendelések lista
3. `POST /api/facilities/{facilityId}/flow-epics` + `POST /joinery/api/orders` → NewOrderDrawer

**Fájlok:** `src/pages/WorkflowPage.tsx`, `src/pages/SalesPage.tsx`, `src/components/orders/NewOrderDrawer.tsx`

---

### Fázis 3 — Gyártás

**Cél:** ProductionPage valódi vágótervekkel.

1. `GET /cutting/api/cutting/planning/` → vágóterv lista (Szabászat tab)
2. `GET /cutting/api/cutting/plans/{date}` → napi terv
3. `GET /cutting/api/cutting/adapters/health` → gép adapter státusz
4. ShopFloor: `GET /cutting/api/cutting/sheets/{id}/status` — de ez sheet-szintű, nem lista. **GAP:** Futó feladatok listája nincs — decision szükséges (mock maradhat vagy Kernel flow-epics alapján)

**Fájlok:** `src/pages/ProductionPage.tsx`, `src/pages/production/ProductionDashboardPage.tsx`

---

### Fázis 4 — Készlet és Mozgások

**Cél:** InventoryPage, OffcutsPanel, MovementsPage élő adatokkal.

1. `GET /inventory/api/inventory/stock` → InventoryPage → Anyagok tab
2. `GET /inventory/api/inventory/offcuts/` → OffcutsPanel
3. `GET /inventory/api/inventory/offcuts/stats/summary` → KPI kártyák

**Fájlok:** `src/pages/InventoryPage.tsx`, `src/components/orders/OffcutsPanel.tsx`, `src/pages/warehouse/MovementsPage.tsx`

---

### Fázis 5 — Beszerzés

**Cél:** ProcurementPage valódi PO-k és szállítók.

1. `GET /procurement/api/procurement/orders` → aktív PO lista
2. `GET /procurement/api/procurement/suppliers` → szállítók
3. `POST /procurement/api/procurement/orders` → Új PO gomb

**Fájlok:** `src/pages/ProcurementPage.tsx`

---

### Fázis 6 — Analytics

**Cél:** AnalyticsPage valódi metrikák.

1. `GET /cutting/api/cutting/waste` → hulladék % és trend
2. `GET /inventory/api/inventory/trend` → anyag felhasználás trend

**Fájlok:** `src/pages/AnalyticsPage.tsx`

---

### Fázis 7 — Settings

**Cél:** A beállítások oldalak élő adatokkal.

1. `GET /api/tenants/{tenantId}` → Cég tab (név, tier)
2. `GET /api/tenants/{tenantId}/facilities` → Telephely tab
3. `GET /api/facilities/{facilityId}/work-stations` → Géppark tab
4. `GET /api/audit-events` → Audit napló tab
5. `GET /api/stages`, `GET /api/stage-chains` → Munkafolyamat tab
6. Felhasználók tab — Keycloak admin API (külön döntés szükséges — Keycloak Users API-t hívni-e közvetlenül)

**Fájlok:** `src/pages/SettingsPage.tsx`

---

### Fázis 8 — Design (Abstractions + Joinery)

**Cél:** DesignPage valódi sablonokkal és kalkulációkkal.

1. `GET /abstractions/api/product-templates` → sablon lista
2. `GET /abstractions/api/product-templates/{id}` → sablon részletek + paraméterek
3. `POST /abstractions/api/product-templates/{id}/calculate` → kalkuláció
4. `GET /abstractions/api/product-templates/{id}/cutting-list` → szabász lista preview

**Fájlok:** `src/pages/DesignPage.tsx`

---

## 6. Implementációs sablon

```typescript
// Tipikus page komponens minta
import { useEffect } from 'react'
import { useApi, API_BASE } from '../hooks/useApi'
import { useTenant } from '../auth/AuthContext'  // Fázis 0 után elérhető
import { MOCK_FALLBACK } from '../mocks/data'    // Fallback marad

export function ExamplePage() {
  const { facilityId } = useTenant()
  const { data, isLoading, refetch } = useApi<ApiType>(
    facilityId ? `${API_BASE.kernel}/facilities/${facilityId}/flow-epics` : null
  )
  
  useEffect(() => { refetch() }, [])  // dep array-ből KIHAGYNI a refetch-et

  const items = data ?? MOCK_FALLBACK  // API adat vagy mock fallback
  // ...
}
```

**Szabályok:**
- `url === null` → `useApi` nem hív semmit (conditional fetch amíg nincs facilityId)
- Mock fallback marad (`data ?? MOCK_DATA`) — prototípus fázisban nincs error state
- `useEffect(() => { refetch() }, [])` — üres dep array, csak mount-kor fut

---

## 7. Ismert hiányok a backendben (döntés szükséges)

| Hiány | Hatás | Opció |
|---|---|---|
| Futó gépfeladatok listája (ShopFloor) | ShopFloorPage nem tud listát mutatni | (1) Kernel flow-epics alapján szűrni; (2) Mock maradjon; (3) Cutting modul bővítése |
| `GET /joinery/api/orders` query param szűrés? | Ismeretlen — tesztelni kell | Böngésző DevTools |
| `GET /api/dashboard/stats` tenant-scoped-e? | Ha nem, más tenant adatait mutathatja | Kód review szükséges |
| Felhasználó lista | Keycloak admin API vs. Kernel Users | Döntés szükséges |

---

## 8. Tesztelési stratégia

1. **Böngésző DevTools Network tab** — minden API hívás után: státusz, Bearer token jelen van-e, válasz struktúra egyezik-e a FE elvárásával
2. **`pnpm test`** — unit tesztek mock-ot tesztelnek, nem kell módosítani az API bekötés miatt
3. **Manuális smoke test** — login → dashboard valódi számokat mutat → rendelések listája él

---

## 9. Ajánlott következő lépés

**Fázis 0 kiadása az FE terminálnak** — ez blokkol mindent. Két fájl érintett (`AuthContext.tsx`, `useMutation.ts`), 1 napnyi munka. Utána párhuzamosan futhat Dashboard (Fázis 1) és Orders/Workflow (Fázis 2).
