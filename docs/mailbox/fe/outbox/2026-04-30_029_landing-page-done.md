---
id: MSG-FE-029-DONE
from: fe
to: root
type: done
priority: high
status: UNREAD
ref: MSG-FE-029
created: 2026-04-30
---

# FE-029 DONE — Landing page + route átszervezés

## Commit

`3d4b8cc` — `feat: LandingPage + route átszervezés — 195 teszt, 0 build hiba`

## Definition of Done ✅

- [x] `/` → LandingPage (publikus bemutatkozó)
- [x] `/w` → HomeScreen (world kártyák, RequireAuth)
- [x] Belépés gomb → `login()` (Keycloak)
- [x] Bejelentkezett user automatikus redirect `/w`-re
- [x] Callback → `/w` (nem `/w/dashboard`)
- [x] `pnpm build` → 0 error
- [x] `pnpm test` → **195/195 pass** (189 meglévő + 6 új)
- [x] Responsive (375px + desktop)

## LandingPage (`src/pages/LandingPage.tsx`)

- Sötét téma (`#0f172a` háttér)
- **Hero:** logó wordmark + szlogen + 2 CTA gomb (Belépés, Tudj meg többet)
- **Feature blokk:** 4 kártya — Megrendelések, Gyártásirányítás, Lapszabászat, Raktár & Beszerzés
- **Footer:** © 2026 JoineryTech · joinerytech.hu
- Bejelentkezett user: `useEffect` → `navigate('/w', { replace: true })`
- Töltési állapot: slate spinner

## Route struktúra (végleges)

```
/                → LandingPage (publikus)
/callback        → CallbackPage → navigate('/w')
/w               → RequireAuth → HomeScreen (world kártyák)
/w/shopfloor     → RequireAuth → ShopFloorPage
/w/production    → RequireAuth → WorldShell + DashboardPage
/w/sales         → RequireAuth → WorldShell + SalesPage
... (többi world route változatlan)
```

## Tesztek

| Terület | Tesztek |
|---|---|
| Meglévő (FE-027 + FE-028) | 189 |
| LandingPage (6 új) | 6 |
| **Összesen** | **195/195 pass** |
