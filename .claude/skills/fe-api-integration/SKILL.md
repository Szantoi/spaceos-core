# Skill: FE API Integration

## When to invoke
Use `/fe-api-integration` when connecting a frontend page/component to a backend API endpoint. Covers the full pattern: hook setup, type mapping, mock fallback, and test compatibility.

## Context

This skill is specific to the **joinerytech-portal** frontend:
- Stack: React 18 + TypeScript + Vite + Vitest + Tailwind
- Auth: Keycloak OIDC PKCE — `useAuth()` provides `token`, `facilityId`, `tenantId`
- API hook: `useApi<T>(url)` + `refetch()` pattern (no auto-fetch)
- API base URLs: `API_BASE` from `src/hooks/useApi.ts`
- Mock fallback: always keep mock data as fallback for tests and offline

## Standard Pattern

### 1. Define API response type
```typescript
interface ApiXxx {
  id: string
  // ... fields from backend DTO
}
```

### 2. Fetch with useApi
```typescript
const { data: apiData, refetch } = useApi<ApiXxx[]>(
  `${API_BASE.kernel}/some-endpoint?pageSize=50`
)
useEffect(() => { refetch() }, []) // eslint-disable-line react-hooks/exhaustive-deps
```

If URL depends on facilityId/tenantId:
```typescript
const { facilityId } = useAuth()
const { data, refetch } = useApi<T>(
  facilityId ? `${API_BASE.kernel}/facilities/${facilityId}/...` : null
)
useEffect(() => { if (facilityId) refetch() }, [facilityId]) // eslint-disable-line
```

### 3. Map API → FE types
```typescript
function apiToFe(item: ApiXxx): FeXxx {
  return {
    id: item.id.slice(0, 13).toUpperCase(), // short display ID
    title: item.title,
    status: STATUS_MAP[item.status] ?? 'draft',
    // fill missing fields with safe defaults
    customer: '—',
    due: '',
  }
}
```

### 4. Graceful fallback
```typescript
const displayItems = apiData?.items?.map(apiToFe) ?? MOCK_DATA
```
Or for array responses:
```typescript
const displayItems = apiData && apiData.length > 0
  ? apiData.map(apiToFe)
  : MOCK_DATA
```

### 5. Parallel fetch (no list endpoint)
When there's no list endpoint but known keys exist, use `fetchAll<T>`:
```typescript
import { fetchAll, API_BASE } from '../hooks/useApi'
const { token } = useAuth()
const [results, setResults] = useState<ApiXxx[] | null>(null)
useEffect(() => {
  if (!token) return
  const urls = KNOWN_TYPES.map(t => `${API_BASE.inventory}/api/inventory/stock?materialType=${encodeURIComponent(t)}`)
  fetchAll<ApiXxx>(urls, token).then(data => {
    const valid = data.filter((r): r is ApiXxx => r !== null)
    if (valid.length > 0) setResults(valid)
  })
}, [token])
```

## Status Mapping Pattern
```typescript
const STATUS_MAP: Record<string, string> = {
  Submitted: 'planned',   // grey
  Approved:  'running',   // teal
  Delivered: 'done',      // emerald
  Cancelled: 'draft',     // grey
  Discovery: 'sales',
  Delivery:  'production',
  ClosedDone:'delivery',
  Draft:     'draft',
  Running:   'running',
  Done:      'done',
}
```

## Filter E2E Test Data
```typescript
function isRealSupplier(name: string): boolean {
  return !name.startsWith('E2E') && name !== 'E2E-PROBE' && !name.includes('<script>')
}
function isRealFacility(name: string): boolean {
  return !name.startsWith('E2E') && !name.match(/^Fac\d/) && !name.match(/^Fac-/)
}
```

## Test Compatibility
Tests use `token: 'mock-token'` from the auth mock. Since `useApi` only fetches when a token exists, the useEffect fires but the fetch fails silently → component shows mock data. Tests see the initial render, so mock fallback keeps tests green.

**Never change tests to assert on API data** — tests should validate the mock-fallback rendering path.

If a test asserts on a column header or label that changed due to API mapping (e.g., "Anyag" → "Összeg"), update the test string to match the new label.

## API Base URLs (nginx proxy)
```typescript
export const API_BASE = {
  kernel:      '/api',          // port 5000
  joinery:     '/joinery',      // port 5002
  inventory:   '/inventory',    // port 5004
  cutting:     '/cutting',      // port 5005
  procurement: '/procurement',  // port 5006
}
```

## Completed Integrations (as of 2026-05-26)
| Page/Component | Endpoint | Notes |
|---|---|---|
| DashboardPage | GET /api/dashboard/stats | flowEpicCount + facility/workstation stats |
| WorkflowPage | GET /api/facilities/{id}/flow-epics | apiEpicToFe() mapper |
| ProductionPage | GET /cutting/api/cutting/plans | PLAN_STATUS_MAP, mock nesting fallback |
| ProcurementPage | GET /procurement/api/procurement/orders + /suppliers | E2E filter, PO_STATUS_MAP |
| AnalyticsPage | GET /cutting/api/cutting/waste | falls back to 7.1% if no executions |
| InventoryPage | GET /inventory/api/inventory/stock (×7) | fetchAll parallel, known material types |
| AuditPanel | GET /api/audit-events?pageSize=50 | hash prefix, ts format |
| FacilitiesPanel | GET /api/tenants/{id}/facilities | E2E filter + mock-merge for rich fields |
| MiniKanbanStrip | GET /api/facilities/{id}/flow-epics | PHASE_TO_STAGE count |
| MachineParkPanel | GET /api/tools/workstations?pageSize=50 | WS_STATUS_MAP, mock fallback for rich fields |
| DesignPage (dashboard) | GET /abstractions/api/modules/templates | template count stat; full editor stays mock |
| DashboardPage (recent orders) | GET /joinery/api/orders?pageSize=5 | reuses ORDER_STATUS_MAP from OrdersPage |

## Abstractions Service Auth Fix (2026-05-26)
The abstractions service (`/etc/spaceos/abstractions.env`) had wrong Keycloak authority path.
Wrong: `Jwt__Authority=http://127.0.0.1:8080/realms/spaceos` (missing `/auth/`)
Fixed: `Jwt__Authority=http://127.0.0.1:8080/auth/realms/spaceos`
This caused `invalid_token: The issuer '...' is invalid` on every request.
All other services use the `/auth/realms/...` path — the abstractions service was the only outlier.
