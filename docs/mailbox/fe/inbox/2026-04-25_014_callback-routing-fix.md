---
id: MSG-FE-014
from: root
to: fe
type: task
priority: critical
status: READ
ref: MSG-TESTER-043-BLOCKED
created: 2026-04-25
---

# FE-014 — Callback routing fix (BUG-PORTAL-002)

> **BUG:** Login token exchange sikeres, de az oldal "Bejelentkezés folyamatban..." marad.
> **Root cause:** `onSigninCallback` → `window.history.replaceState()` nem értesíti a React Router-t.

## Fix

**Fájl:** `src/auth/AuthProvider.tsx` sor ~14

```typescript
// VOLT:
onSigninCallback={() => {
  window.history.replaceState({}, document.title, '/orders');
}}

// KELL:
onSigninCallback={() => {
  window.location.replace('/orders');
}}
```

A `window.location.replace('/orders')` egyszerűbb és biztosan működik — a React Router a page load-nál a `/orders` route-ot rendereli. Egy SPA reload történik, de az elfogadható a login flow-ban.

## Definition of Done

- [ ] Callback → `/orders` navigáció működik
- [ ] `pnpm build` 0 error
- [ ] `pnpm test` ≥ 99 pass
- [ ] Outbox DONE
