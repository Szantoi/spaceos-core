---
id: MSG-FE-006-DONE
from: fe
to: root
type: done
priority: high
status: READ
ref: MSG-FE-006
created: 2026-04-16
---

# FE-006 DONE — Production Readiness: ErrorBoundary + 404 + Profil oldal

## Összefoglalás

Doorstar Soft Launch MVP utolsó feladatcsomagja kész. Fehér képernyő helyett barátságos hibaoldal, 404, profil és kijelentkezés — minden él.

## DoD ellenőrzőlista

- [x] `pnpm build` → 0 error, `dist/` létrejön
- [x] `pnpm test` → **87 teszt zöld** (+15 új teszt, 18 test file)
- [x] `pnpm lint` → 0 hiba
- [x] `pnpm typecheck` → 0 error
- [x] `ErrorBoundary` az App root körül (minden route védi)
- [x] `NotFoundPage` catch-all `*` route-on aktív (ProtectedRoute-on kívül is)
- [x] `ProfilePage` ProtectedRoute mögött, `/profile` route-on
- [x] Kijelentkezés gomb: `userManager.signoutRedirect()` (oidc-client-ts PKCE flow)
- [x] `AppHeader` minden védett oldalon (Rendelések + Profil link)
- [x] git commit + push

## Build output

```
dist/assets/index-CRp5PkT8.css   13.35 kB │ gzip:   3.58 kB
dist/assets/index-BbOmw1_l.js   356.55 kB │ gzip: 109.79 kB
✓ built in 1.13s
```

## Test eredmény

```
Test Files  18 passed (18)
     Tests  87 passed (87)   (+15 új teszt)
  Duration  13.46s
```

## Commit

```
9b6bd61800b424a3a44ff5da813faae77004baf0
feat: Production Readiness — ErrorBoundary, NotFoundPage, ProfilePage
```

## Új fájlok

| Fájl | Leírás |
|---|---|
| `src/components/ErrorBoundary.tsx` | React class, getDerivedStateFromError, resetErrorBoundary, ErrorFallback |
| `src/components/AppHeader.tsx` | Brand link, Rendelések + Profil NavLink, `print:hidden` |
| `src/pages/NotFoundPage.tsx` | 404, "Az oldal nem található", vissza link /orders-ra |
| `src/hooks/useProfile.ts` | GET /bff/auth/me, 5 perc staleTime, UserProfile interface |
| `src/pages/ProfilePage.tsx` | Avatar initials, név/email/tenant, role badge-ek, signoutRedirect |
| `src/App.tsx` | ErrorBoundary wrap, NotFoundPage route, ProfilePage route |
| `src/components/ProtectedRoute.tsx` | +AppHeader minden védett oldal fölé |

## Tesztek megoszlása (+15 új)

| Test file | Tesztek |
|---|---|
| `ErrorBoundary.test.tsx` | 5 (children OK, error → fallback, custom fallback, reset, console.error) |
| `NotFoundPage.test.tsx` | 3 (404 text, heading, back link href) |
| `ProfilePage.test.tsx` | 7 (loading, error, name/email, roles, logout, tenant, avatar initials) |
| Korábbi tesztek | 72 (változatlan, mind zöld) |

## Soft Launch MVP állapot

Az összes FE-001–FE-006 feladat DONE. A teljes user journey él:

```
/login → OIDC PKCE → /callback → /orders (lista + StatusFilter)
  → [+ Új rendelés] → CreateOrderModal → /orders/:id
  → AddItemForm → Kalkulálás → /orders/:id/cutting-list → print
  → OrderHistoryPanel (státusz timeline)
  → /profile → kijelentkezés

ErrorBoundary: minden hiba elegánsan elkapva
NotFoundPage: minden ismeretlen route → 404
```
