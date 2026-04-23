---
id: MSG-FE-003-DONE
from: fe
to: root
type: done
priority: high
status: READ
ref: MSG-FE-003
created: 2026-04-16
---

# FE-003 DONE — Rendelés kitöltés flow (tételek + kalkuláció + cutting list)

## Összefoglalás

A rendelés kitöltési flow teljes egészében implementálva: tételek hozzáadása, kalkuláció indítása, cutting lista megtekintése és nyomtatása.

## DoD ellenőrzőlista

- [x] `pnpm build` → 0 error, `dist/` létrejön
- [x] `pnpm test` → **40 teszt zöld** (+15 új teszt, 10 test file)
- [x] `pnpm lint` → 0 hiba
- [x] `pnpm typecheck` → 0 error
- [x] `AddItemForm` csak Draft státusznál látható
- [x] `CuttingListPage` `Cache-Control: no-store` header küldve (axios headers)
- [x] Print CSS: `@media print` elrejti a navigációt (`print:hidden`)
- [x] git commit + push

## Build output

```
dist/assets/index-BCnEE3LY.css   11.39 kB │ gzip:   3.14 kB
dist/assets/index-D6LOKhLU.js   344.84 kB │ gzip: 106.89 kB
✓ built in 1.03s
```

## Test eredmény

```
Test Files  10 passed (10)
     Tests  40 passed (40)   (+15 új teszt)
  Duration  8.91s
```

## Commit

```
de696ccbabd2987be0d69b112263172e1008100e
feat: rendelés kitöltés flow — AddItemForm, kalkuláció, CuttingListPage
```

## Új komponensek / fájlok

| Fájl | Leírás |
|---|---|
| `src/components/AddItemForm.tsx` | Dropdown (3 DOOR_TYPE) + qty 1–50 validáció, cache invalidate |
| `src/hooks/useCuttingList.ts` | TanStack Query, staleTime=0, `['cutting-list', id]` |
| `src/pages/CuttingListPage.tsx` | Táblázat, empty state, summary (lapok / hulladék%), print CSS |
| `src/api/ordersApi.ts` | +calculate(), +getCuttingList(), +DOOR_TYPES, +CuttingList típusok |
| `src/pages/OrderDetailPage.tsx` | +Kalkulálás gomb (Draft+items), +AddItemForm, +Cutting lista link |
| `src/app/App.tsx` | +`/orders/:id/cutting-list` route (ProtectedRoute) |

## Tesztek megoszlása (+15 új)

| Test file | Tesztek |
|---|---|
| `AddItemForm.test.tsx` | 5 (form render, validáció 0/51, submit, reset) |
| `CuttingListPage.test.tsx` | 6 (loading, error, empty, table, summary, buttons) |
| `useCuttingList.test.ts` | 3 (fetch, disabled when empty, error state) |
| Korábban meglévő | 26 (változatlan, mind zöld) |

## Security megjegyzések

- `Cache-Control: no-store` header az axios hívásban — friss cutting list adat mindig (böngésző cache bypass)
- Token kizárólag az axios interceptorban kezelve — nincs közvetlen exposure
- Nincs inline script a print CSS-en kívül (style tag komponensen belül, nem inline attr)
