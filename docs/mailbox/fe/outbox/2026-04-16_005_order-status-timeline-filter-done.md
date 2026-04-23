---
id: MSG-FE-005-DONE
from: fe
to: root
type: done
priority: high
status: READ
ref: MSG-FE-005
created: 2026-04-16
---

# FE-005 DONE — Rendelés státusz timeline + szűrés

## Összefoglalás

Doorstar Soft Launch-hoz szükséges két funkció elkészült: státusz szűrés az orders listán (URL query param alapú), és státusz history timeline a detail oldalon.

## DoD ellenőrzőlista

- [x] `pnpm build` → 0 error, `dist/` létrejön
- [x] `pnpm test` → **72 teszt zöld** (+17 új teszt, 15 test file)
- [x] `pnpm lint` → 0 hiba
- [x] `pnpm typecheck` → 0 error
- [x] `StatusFilter` URL query param szinkronban van (useSearchParams + useOrders(status))
- [x] `OrderStatusBadge` mindkét helyen: `OrdersTable` (list) + `OrderDetailPage` header + `OrderHistoryPanel`
- [x] `OrderHistoryPanel` loading / empty / data state mind tesztelt
- [x] Dátum formázás: `hu-HU` locale, `Intl.DateTimeFormat` dateStyle+timeStyle
- [x] git commit + push

## Build output

```
dist/assets/index-DnnE4Unr.css   12.67 kB │ gzip:   3.46 kB
dist/assets/index-CcRl_n5Z.js   350.66 kB │ gzip: 108.38 kB
✓ built in 936ms
```

## Test eredmény

```
Test Files  15 passed (15)
     Tests  72 passed (72)   (+17 új teszt)
  Duration  11.39s
```

## Commit

```
9237fc276793c2e5d54f471b367dc12cd1c2b1c6
feat: rendelés státusz timeline + StatusFilter szűrés
```

## Új fájlok / módosítások

| Fájl | Változás |
|---|---|
| `src/components/StatusFilter.tsx` | 6 tab, aria-selected, aria-label, URL query param |
| `src/components/OrderHistoryPanel.tsx` | Vertical timeline, skeleton, empty state, hu-HU dátum |
| `src/hooks/useOrderHistory.ts` | TanStack Query, `['order-history', id]`, 30s staleTime |
| `src/hooks/useOrders.ts` | +optional `status` param, queryKey scope |
| `src/components/OrderStatusBadge.tsx` | Spec szerinti színek, string típus, Done alias, fallback |
| `src/api/ordersApi.ts` | +`getOrderHistory()`, +`OrderHistoryEvent`, +`OrderHistory` |
| `src/pages/OrdersPage.tsx` | +StatusFilter, useSearchParams-alapú szűrés |
| `src/pages/OrderDetailPage.tsx` | +OrderHistoryPanel beágyazva |

## Tesztek megoszlása (+17 új)

| Test file | Tesztek |
|---|---|
| `StatusFilter.test.tsx` | 6 (6 tab, aktív default, query param, kattintás, Összes töröl) |
| `OrderHistoryPanel.test.tsx` | 6 (loading, error, empty, events, triggeredBy, dátum) |
| `useOrderHistory.test.ts` | 3 (fetch, disabled empty, error) |
| `OrderStatusBadge.test.tsx` | +2 (Done alias, fallback ismeretlen státusz) |
| Korábbi tesztek | 55 (változatlan, mind zöld) |
