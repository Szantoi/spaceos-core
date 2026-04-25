---
id: MSG-FE-015-DONE
from: fe
to: root
type: done
priority: critical
status: READ
ref: MSG-FE-015
created: 2026-04-25
---

# FE-015 DONE — Callback SPA navigáció fix (BUG-PORTAL-002 v2)

## Commit

`b0ada10` — fix: FE-015 — callback SPA navigate fix (BUG-PORTAL-002 v2)

## Fix

**Root cause:** `window.location.replace('/orders')` (FE-014) page reload-ot okoz → `InMemoryWebStorage` token törlődik → `/login` redirect → végtelen login loop.

**Megoldás — 2 fájl:**

| Fájl | Előtte | Utána |
|---|---|---|
| `AuthProvider.tsx` `onSigninCallback` | `window.location.replace('/orders')` | `window.history.replaceState(...)` — csak URL cleanup, nincs navigáció |
| `CallbackPage.tsx` | Statikus "Bejelentkezés folyamatban..." | `useAuth()` + `useNavigate()` → ha `isAuthenticated` → `navigate('/orders', { replace: true })` |

**Flow:** Keycloak redirect → `/callback?code=...&state=...` → `OidcAuthProvider` token exchange → `onSigninCallback` URL cleanup → `CallbackPage` `useEffect` detektálja `isAuthenticated` → React Router navigate `/orders` → token megmarad InMemoryWebStorage-ban.

## DoD

- [x] `onSigninCallback` nem okoz page reload
- [x] CallbackPage → React Router navigate ha authenticated
- [x] Auth error megjelenítés CallbackPage-en
- [x] `pnpm build` → 0 error
- [x] `pnpm test` → 99/99 pass
- [x] `pnpm lint` + `pnpm typecheck` → 0 hiba
- [x] git commit `b0ada10` + push (main)

## BUG-PORTAL-002 teljes fix lánc

| Commit | Fix | Probléma |
|---|---|---|
| `81b2b60` FE-013 | `stateStore` → `sessionStorage` | PKCE code_verifier elvész redirect-nél |
| `190e42c` FE-014 | `replaceState` → `location.replace` | React Router nem értesül az URL változásról |
| `b0ada10` FE-015 | `location.replace` → React Router navigate | Page reload törli a token-t |

INFRA deploy szükséges. Tester újra tesztelheti a deploy után.
