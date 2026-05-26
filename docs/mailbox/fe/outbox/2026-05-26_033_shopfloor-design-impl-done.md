---
id: MSG-FE-033-DONE
from: fe
to: root
type: done
priority: high
status: READ
ref: MSG-FE-033
created: 2026-05-26
---

# FE-033 DONE — Üzem (ShopFloor) design igazítás + orphaned route cleanup

## Státusz: KÉSZ

## Elvégzett munkák

### `src/pages/ShopFloorPage.tsx`
- Háttér: `bg-[#0a0f1a]` → `bg-stone-900`
- Header: `bg-stone-900 border-stone-800` → `bg-stone-800 border-stone-700`
- Kijelentkezés gomb: `bg-stone-800` → `bg-stone-700`
- "Vissza a portálra" gomb hozzáadva a headerbe (mindig látható, `navigate('/')`)
- `useNavigate` import hozzáadva
- PinStage cím: "Shopfloor" → "Bejelentkezés"
- PIN törlés gomb: "C" → "Töröl" + stílus frissítve (`text-[12px] uppercase tracking-wide`)

### `src/App.tsx`
- `OrdersPage` import eltávolítva (nem volt más helyen használva)
- `/w/orders`, `/w/workflow`, `/w/analytics` orphaned route-ok törölve
  - Ezek a screen-ek a ProductionWorldPage-ben érhetők el: `/w/production/:screen`

### Tesztek frissítve
- `ShopFloorPage.test.tsx`: MemoryRouter wrapper, "Shopfloor" → "Bejelentkezés", "C" → "Töröl", `bg-[#0a0f1a]` → `bg-stone-900`, új "Vissza a portálra" teszt
- `App.test.tsx`: "Shopfloor" → "Bejelentkezés"

## Tesztek
- **244 teszt, 0 fail** (volt 243, +1 új: Vissza a portálra gomb)
- `pnpm build` → 0 error
- `pnpm lint` → 0 új hiba
