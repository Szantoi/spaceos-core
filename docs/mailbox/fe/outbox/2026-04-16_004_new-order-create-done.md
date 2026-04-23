---
id: MSG-FE-004-DONE
from: fe
to: root
type: done
priority: high
status: READ
ref: MSG-FE-004
created: 2026-04-16
---

# FE-004 DONE — Új rendelés létrehozása (Create Order flow)

## Összefoglalás

A hiányzó láncszem bezárva: az `OrdersPage` „+ Új rendelés" gombja most a `CreateOrderModal`-t nyitja meg (reference + notes mezőkkel), sikeres POST után navigál `/orders/:id`-ra.

## DoD ellenőrzőlista

- [x] `pnpm build` → 0 error, `dist/` létrejön
- [x] `pnpm test` → **55 teszt zöld** (+15 új teszt, 12 test file)
- [x] `pnpm lint` → 0 hiba
- [x] `pnpm typecheck` → 0 error
- [x] `CreateOrderModal` ProtectedRoute-on belül (auto teljesül)
- [x] Modal bezárul create után, navigate `/orders/:id`-ra
- [x] Loading state: gomb disabled + SVG spinner animáció
- [x] Error state: inline `data-testid="api-error"` hibaüzenet, nincs crash
- [x] git commit + push

## Build output

```
dist/assets/index-EZnMKC-F.css   11.77 kB │ gzip:   3.26 kB
dist/assets/index-aFnO1dIA.js   346.68 kB │ gzip: 107.35 kB
✓ built in 1.03s
```

## Test eredmény

```
Test Files  12 passed (12)
     Tests  55 passed (55)   (+15 új teszt)
  Duration  8.80s
```

## Commit

```
f19244f2c00756176423d97bf4adc529781c1dda
feat: CreateOrderModal — új rendelés flow (reference + notes)
```

## Új fájlok

| Fájl | Leírás |
|---|---|
| `src/components/CreateOrderModal.tsx` | Modal: reference (max 100), notes (max 500), spinner, aria-modal, backdrop close |
| `src/hooks/useCreateOrder.ts` | useMutation → createOrder, invalidate ['orders'], navigate, onSuccess cb |
| `src/api/ordersApi.ts` | +`createOrder(CreateOrderInput)`, +`CreateOrderInput` interface |
| `src/pages/OrdersPage.tsx` | NewOrderModal → CreateOrderModal csere |

## Tesztek megoszlása (+15 új)

| Test file | Tesztek |
|---|---|
| `CreateOrderModal.test.tsx` | 8 (render, cancel, backdrop, validation×2, submit, api error, loading) |
| `useCreateOrder.test.ts` | 4 (mutate, navigate, onSuccess cb, error state) |
| `OrdersPage.test.tsx` | +2 (modal nyílik kattintásra, modal bezárul) |
| Korábbi tesztek | 41 (változatlan, mind zöld) |

## Teljes workflow most

```
/orders → [+ Új rendelés] → CreateOrderModal → POST /bff/api/orders
  → navigate /orders/:id → AddItemForm → [Kalkulálás]
  → POST .../calculate → [Cutting lista →] GET .../cutting-list → print
```
