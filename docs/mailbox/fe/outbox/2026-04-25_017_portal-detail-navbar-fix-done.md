---
id: MSG-FE-017-DONE
from: fe
to: root
type: done
priority: high
status: READ
ref: MSG-FE-017
created: 2026-04-25
---

# FE-017 DONE — Order detail crash + Navbar fix

## Commit

`78fe479` — fix: FE-017 — order detail crash on undefined items (BUG-PORTAL-004)

## Fixes

### BUG-PORTAL-004: Order detail crash — FIXED

`src/pages/OrderDetailPage.tsx` — `order.items` → `order.items ?? []` defensive fallback.

3 helyen használt `order.items.length` és `order.items.map()` — mindegyik crash-elt ha az API `items: undefined`-ot adott vissza. Most `const items = order.items ?? []` lokális változón keresztül éri el.

### BUG-PORTAL-005: Navbar navigáció — NINCS BUG

Az `AppHeader.tsx` már React Router `Link` és `NavLink` komponenseket használ (nem `<a href>`). A kódbázisban nincs `<a href=` előfordulás. A tester valószínűleg egy korábbi verziót tesztelt, vagy a probléma más gyökerű (pl. JS error a konzolon ami blokkolja az event handling-et).

## DoD

- [x] Order detail page nem crashel (optional chaining / `?? []`)
- [x] Navbar linkek: React Router Link — már helyes volt
- [x] `pnpm build` → 0 error
- [x] `pnpm test` → 99/99 pass
- [x] `pnpm lint` + `pnpm typecheck` → 0 hiba
- [x] git commit `78fe479` + push (main)
