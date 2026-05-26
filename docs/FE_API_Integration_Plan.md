# FE API Integrációs Terv

**Készült:** 2026-05-26  
**Státusz:** DRAFT  
**Cél:** A meglévő, mock adatokkal működő frontend oldalakat bekötni az élő backend API-kba, hogy a rendszer valódi adatokkal működjön.

---

## 1. Jelenlegi helyzet

### Mi van kész

| Réteg | Állapot |
|---|---|
| Auth (Keycloak OIDC PKCE) | ✅ Működik — token minden API hívásba kerül |
| `useApi<T>` hook | ✅ Kész — Bearer token, loading/error state, refetch |
| `API_BASE` konstansok | ✅ Definiálva (`/api`, `/joinery`, `/cutting`, `/inventory`, `/procurement`, `/ai`) |
| nginx proxy | ✅ Konfigurálva — minden `/api/*`, `/joinery/*` stb. path proxyzva a backend-re |
| UI oldalak (13+) | ✅ Kész — teljesen mock adatokkal |
| `TenantInfoBar` | ⚠️ Részleges — hardcoded `DEMO_TENANT_ID` string-et küld, nem JWT claim-et |

### Mi hiányzik

Az összes oldal (`DashboardPage`, `OrdersPage`, `ProductionPage` stb.) a `src/mocks/data.ts` fájl statikus adatait használja. Az API hívás nincs bekötve egyetlen oldalba sem.

---

## 2. Backend API referencia

### Kernel (`/api/*` → port 5000)

| Endpoint | Metódus | Leírás |
|---|---|---|
| `GET /api/dashboard/stats` | GET | Összesített KPI-k (tenant, facility, workstation, flow-epic számlálók) |
| `GET /api/tenants/{id}` | GET | Tenant adatok (név, tier) |
| `GET /api/tenants/{id}/facilities` | GET | Telephely lista |
| `GET /api/facilities/{id}/flow-epics` | GET | Rendelések/projektek listája (WorkflowPage) |
| `GET /api/facilities/{id}/work-stations` | GET | Géppark (SettingsPage → Géppark fül) |
| `GET /api/flow-epics/{id}` | GET | Egy rendelés részletei |
| `POST /api/flow-epics` | POST | Új rendelés létrehozása (NewOrderDrawer) |
| `PUT /api/flow-epics/{id}/advance-stage` | PUT | Rendelés léptetése következő státuszba |
| `GET /api/audit-events` | GET | Audit napló (SettingsPage → Audit fül) |
| `GET /api/stages` / `GET /api/stage-chains` | GET | Gyártási lépések konfigurációja |
| `GET /api/work-stations` | GET | Összes gép |

### Joinery (`/joinery/*` → port 5002)

| Endpoint | Metódus | Leírás |
|---|---|---|
| `GET /api/orders` | GET | Ajtó rendelések listája (OrdersPage) |
| `POST /api/orders` | POST | Új ajtó rendelés |
| `GET /api/gyartasilap/{id}` | GET | Gyártási lap PDF adat |
| `GET /api/anyaglista/{id}` | GET | Anyaglista |

### Cutting (`/cutting/*` → port 5005)

| Endpoint | Metódus | Leírás |
|---|---|---|
| `GET /api/cutting/planning` | GET | Vágótervek listája (ProductionPage) |
| `GET /api/cutting/executions` | GET | Futó vágóterv végrehajtások (ShopFloorPage) |
| `GET /api/cutting/analytics` | GET | Hulladék, kihasználtság statisztika (AnalyticsPage) |
| `GET /api/cutting/adapters` | GET | Gép adapterek |

### Inventory (`/inventory/*` → port 5004)

| Endpoint | Metódus | Leírás |
|---|---|---|
| `GET /api/inventory` | GET | Készlet lista (InventoryPage) |
| `GET /api/inventory/offcuts` | GET | Maradék lapok (OffcutsPanel) |

### Procurement (`/procurement/*` → port 5006)

| Endpoint | Metódus | Leírás |
|---|---|---|
| `GET /api/procurement` | GET | Aktív PO-k + szállítók (ProcurementPage) |

---

## 3. Tenant ID probléma — JWT claim

Jelenleg a `TenantInfoBar` hardcoded `'doorstar-kft'` stringet küld. Ez helytelen — a tenant ID-t a Keycloak JWT tokenből kell olvasni.

**Megoldás:** A Keycloak token `profile` claim-jéből vagy egyedi claim-ből olvasni a tenant ID-t.

```typescript
// AuthContext.tsx kiegészítés
const tenantId = user?.profile?.['tenant_id'] as string | undefined

// Vagy: a /api/tenants/me endpoint ha létezik
```

**Feladat a FE-nek:** Keycloak admin-ban ellenőrizni, hogy a `portal-app` client mapper hozzáadja-e a `tenant_id` claim-et, és ha igen, azt kiolvasni `user.profile`.

---

## 4. Integrációs sorrend (prioritás)

### Fázis 1 — Auth + Tenant alap (1-2 nap)

**Cél:** A bejelentkezett felhasználó tenant adata mindig valódi legyen.

1. `TenantInfoBar` — JWT claim-ből tenantId, majd `GET /api/tenants/{id}` → valódi név megjelenítése
2. `AuthContext` kiegészítés — `tenantId` és `facilityId` exportálása a context-ből (Keycloak claim-ből)

### Fázis 2 — Dashboard és Orders (2-3 nap)

**Cél:** A két legfontosabb oldal élő adatokkal.

1. `DashboardPage` — `GET /api/dashboard/stats` → KPI kártyák feltöltése valódi számokkal
   - A statikus `SPARKS`, `machines`, `ORDERS` lista maradhat fallback-ként amíg a backend adatok nem fedik le teljesen
2. `OrdersPage` — `GET /api/orders` (Joinery modul) → rendelés lista
   - `NewOrderDrawer` → `POST /api/orders` → mentés után lista refresh
3. `WorkflowPage` — `GET /api/facilities/{id}/flow-epics` → kanban kártyák

### Fázis 3 — Gyártás és Készlet (3-4 nap)

1. `ProductionPage` (Cutting tab) — `GET /api/cutting/planning` → vágótervek
2. `ShopFloorPage` — `GET /api/cutting/executions` → futó feladatok
3. `InventoryPage` — `GET /api/inventory` → anyag készlet
4. `OffcutsPanel` — `GET /api/inventory/offcuts` → maradék lapok

### Fázis 4 — Beszerzés és Analytics (2 nap)

1. `ProcurementPage` — `GET /api/procurement` → szállítók + PO-k
2. `AnalyticsPage` — `GET /api/cutting/analytics` → hulladék, OEE, kapacitás

### Fázis 5 — Settings (2-3 nap)

1. `SettingsPage` → Telephely fül — `GET /api/tenants/{id}/facilities`
2. `SettingsPage` → Géppark fül — `GET /api/facilities/{id}/work-stations`
3. `SettingsPage` → Audit fül — `GET /api/audit-events`
4. `SettingsPage` → Felhasználók fül — Keycloak admin API (külön döntés szükséges)

---

## 5. Implementációs minta

Minden oldal az alábbi sablon szerint épül fel. A mock adat **fallback** marad ameddig az API nem fed le mindent — nem törölni, hanem `data ?? MOCK_DATA` logika.

```typescript
// Példa: DashboardPage.tsx
import { useEffect } from 'react'
import { useApi, API_BASE } from '../hooks/useApi'
import { ORDERS } from '../mocks/data'  // fallback marad

interface DashboardStats {
  tenantCount: number
  facilityCount: number
  workStationCount: number
  flowEpicCount: number
  auditEventCount: number
}

export function DashboardPage() {
  const { data: stats, refetch } = useApi<DashboardStats>(
    `${API_BASE.kernel}/dashboard/stats`
  )

  useEffect(() => { refetch() }, [refetch])

  // KPI értékek: API adat ha van, egyébként mock
  const ordersToday = stats?.flowEpicCount?.toString() ?? '12'
  // ...
}
```

**Fontos szabályok:**
- `useApi` `url === null` → nem hív semmit → conditional fetch-hez használható (pl. amíg nincs tenantId)
- Minden hiba silent fallback mock adatra — a felhasználó ne lásson error state-et a prototípus fázisban
- `useEffect(() => { refetch() }, [refetch])` — minden oldal mount-kor fetchel

---

## 6. useApi hook kiegészítés (szükséges)

A jelenlegi `useApi` csak GET-et tud jól. POST/PUT esetén a `refetch` nem automatikus.

**Szükséges bővítés:** `useMutation` hook a write műveletekhez:

```typescript
// src/hooks/useMutation.ts
export function useMutation<T, B>(url: string, method: 'POST' | 'PUT' | 'DELETE') {
  const { token } = useAuth()
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const mutate = useCallback(async (body?: B): Promise<T | null> => {
    if (!token) return null
    setIsLoading(true)
    try {
      const res = await fetch(url, {
        method,
        headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
        body: body ? JSON.stringify(body) : undefined,
      })
      if (!res.ok) throw new Error(`HTTP ${res.status}`)
      return await res.json() as T
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

## 7. nginx ellenőrzés

A `/api/*` proxy route-ok már be vannak állítva az nginx configban. Ellenőrizni kell:

```bash
# VPS-en
grep -A3 "location /api" /etc/nginx/sites-available/joinerytech
grep -A3 "location /joinery" /etc/nginx/sites-available/joinerytech
grep -A3 "location /cutting" /etc/nginx/sites-available/joinerytech
grep -A3 "location /inventory" /etc/nginx/sites-available/joinerytech
grep -A3 "location /procurement" /etc/nginx/sites-available/joinerytech
```

Ha valamelyik hiányzik, a `Codebase_Status.md` `Operátori teendők` szerint még nincs konfigurálva — ezeket pótolni kell mielőtt az API hívások élnek.

---

## 8. Tesztelési stratégia

Minden API bekötés után:

1. **Böngésző DevTools Network tab** — ellenőrizni hogy a kérés elmegy, 200-at kap, és Bearer token van a headerben
2. **Meglévő 229 unit test** — `pnpm test` — ezek mock-ot tesztelnek, nem kell módosítani
3. **Manuális smoke test** — bejelentkezés → az oldal valódi adatot mutat-e

---

## 9. Függőségek és blokkolók

| Blokkoló | Megoldás |
|---|---|
| `tenant_id` claim hiányzik a JWT-ből | Keycloak admin → spaceos realm → portal-app client → Client Scopes → mapper hozzáadása |
| nginx `/cutting/*`, `/inventory/*` stb. route-ok hiányoznak | Codebase_Status.md szerint még nincs konfigurálva — operátori feladat |
| Backend service-ek futnak? | `systemctl status spaceos-*` VPS-en |

---

## 10. Következő lépés (javaslat)

1. Ellenőrizni az nginx proxy route-okat (`/cutting`, `/inventory`, `/procurement`)
2. Ellenőrizni a Keycloak `tenant_id` mapper konfigurációt
3. FE terminálnak kiadni a **Fázis 1** feladatot (TenantInfoBar + AuthContext kiegészítés)
