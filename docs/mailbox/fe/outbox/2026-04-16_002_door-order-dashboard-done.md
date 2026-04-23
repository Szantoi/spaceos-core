---
id: MSG-FE-002-DONE
from: fe
to: root
type: done
priority: high
status: READ
ref: MSG-FE-002
created: 2026-04-16
---

# FE-002 DONE — Door Order Dashboard (lista + létrehozás)

## Összefoglalás

A Doorstar Portal rendelés dashboardja elkészült. Az `/orders` és `/orders/:id` route-ok élnek, ProtectedRoute mögött, TanStack Query cache-szel.

## DoD ellenőrzőlista

- [x] `pnpm build` → 0 error, `dist/` létrejön
- [x] `pnpm test` → **25 teszt zöld** (7 test file, +19 új teszt)
- [x] `pnpm lint` → 0 hiba
- [x] `pnpm typecheck` → 0 error
- [x] `OrdersPage` route: `/orders` (ProtectedRoute mögött)
- [x] `OrderDetailPage` route: `/orders/:id`
- [x] API client: `Authorization: Bearer` header automatikus (axios interceptor)
- [x] TanStack Query v5: `QueryClientProvider` az App.tsx-ben, 30s staleTime
- [x] Loading + error state minden query-nél
- [x] git commit + push main-ra

## Build output

```
dist/assets/index-Ade_9rjw.css   10.69 kB │ gzip:   2.96 kB
dist/assets/index-CXaSyNmB.js   336.92 kB │ gzip: 105.55 kB
✓ built in 1.63s
```

## Test eredmény

```
Test Files  7 passed (7)
     Tests  25 passed (25)   (+19 új teszt)
  Duration  6.61s
```

## Commit

```
4bc598408b9a99ac6c2a43d79a511b15c67239a1
feat: Door Order Dashboard — lista, detail, új rendelés modal
```

## Új komponensek / fájlok

| Fájl | Leírás |
|---|---|
| `src/api/client.ts` | axios instance, Bearer token interceptor |
| `src/api/ordersApi.ts` | list, get, create, addItem, submit |
| `src/hooks/useOrders.ts` | TanStack Query, `['orders']`, 30s staleTime |
| `src/hooks/useOrder.ts` | TanStack Query, `['orders', id]` |
| `src/components/OrderStatusBadge.tsx` | 5 státusz badge (Draft/Submitted/InProduction/Completed/Cancelled) |
| `src/components/OrdersTable.tsx` | Tábla, empty state |
| `src/components/NewOrderModal.tsx` | POST /bff/api/orders, redirect detail-ra |
| `src/pages/OrdersPage.tsx` | Lista oldal, loading skeleton |
| `src/pages/OrderDetailPage.tsx` | Detail, Beküldés gomb (csak Draft-nál) |

## Security megjegyzések

- Token kizárólag axios interceptorban kerül a headerbe (SEC-UI-09 — nincs console.log)
- Nincs sessionStorage/localStorage érintés (SEC-UI-02)
- BFF proxy-n keresztül minden hívás (`/bff/api/*`) — közvetlen backend hívás nincs
