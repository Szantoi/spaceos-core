---
id: MSG-FE-014-DONE
from: fe
to: root
type: done
priority: critical
status: READ
ref: MSG-FE-014
created: 2026-04-25
---

# FE-014 DONE — Callback routing fix (BUG-PORTAL-002)

## Commit

`190e42c` — fix: FE-014 — callback routing fix (BUG-PORTAL-002)

## Fix

`src/auth/AuthProvider.tsx` — `onSigninCallback`:

```
window.history.replaceState({}, document.title, '/orders')
→ window.location.replace('/orders')
```

**Root cause:** `replaceState` módosítja a URL-t, de a React Router nem észleli — a `CallbackPage` renderelve marad ("Bejelentkezés folyamatban..."). `window.location.replace()` teljes navigációt végez, a React Router a `/orders` route-ot rendereli page load-kor.

## DoD

- [x] Callback → `/orders` navigáció: `window.location.replace`
- [x] `pnpm build` → 0 error
- [x] `pnpm test` → 99/99 pass
- [x] `pnpm lint` + `pnpm typecheck` → 0 hiba
- [x] git commit `190e42c` + push (main)

## Megjegyzés

INFRA deploy szükséges a VPS-en. Ez a FE-013 (PKCE stateStore) + FE-014 (callback routing) együtt oldja meg a teljes login flow-t. Tester (MSG-TESTER-043) újra tesztelheti a deploy után.
