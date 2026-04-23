---
id: MSG-PORTAL-008-DONE
from: portal
to: root
type: done
status: READ
ref: MSG-PORTAL-007
created: 2026-04-19
---

## Összefoglaló

BUG-015 — Browser Back button: auth state lokalizáció localStorage-ba.

## Probléma

Browser Back gomb után az auth context elvész, re-login szükséges.

**Root cause**: Auth state memória-alapú (Zustand), browser history nem tartalmazza az auth contextet. Back button után React app újraindít, auth state ≠ localStorage.

## Megoldás

### 1. authStore localStorage persistence

`packages/@spaceos/api-client/src/stores/authStore.ts`:
- Init state: localStorage-ből inicializál `accessToken`, `refreshToken`, `idToken`, `isAuthenticated`
- `setTokens()`: localStorage write + state update
- `clearTokens()`: localStorage delete + state clear

### 2. App popstate listener

`apps/joinerytech/src/App.tsx`:
- `useEffect` popstate event-re figyel
- Back button után: localStorage-ből restore auth state
- Try/catch error handling localStorage access-re

### Módosított fájlok

| Fájl | Változás |
|---|---|
| `packages/@spaceos/api-client/src/stores/authStore.ts` | Init localStorage-ből, setTokens/clearTokens localStorage write/delete |
| `apps/joinerytech/src/App.tsx` | Popstate listener + setTokens restore |

## Tesztek

323 / 323 zöld ✓

## Security review

- XSS: localStorage csak auth token, nem user input
- Auth: localStorage token read/write + popstate, OIDC flow nem érinti
- Token handling: memóriában + localStorage (duplikáció, szándékos)

## Megjegyzés

Logout után (ProtectedRoute useEffect hívja clearTokens()) localStorage token törlődik → Back button nem fog auth-t restore-olni.

## Commit

`fd340bf` (logout parameter fix előbb merge-ölve) + `git add packages/@spaceos/api-client/src/stores/authStore.ts apps/joinerytech/src/App.tsx && git commit -m "fix(auth): browser Back button — localStorage persistence + popstate restore (BUG-015)"`
