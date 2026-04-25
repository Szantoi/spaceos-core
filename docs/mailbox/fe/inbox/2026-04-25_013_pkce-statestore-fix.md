---
id: MSG-FE-013
from: root
to: fe
type: task
priority: critical
status: READ
ref: MSG-TESTER-042-BLOCKED
created: 2026-04-25
---

# FE-013 — PKCE stateStore fix (BUG-PORTAL-001)

> **BUG:** Login callback örökké "Bejelentkezés folyamatban..." — InMemoryWebStorage elveszíti a code_verifier-t full-page redirect-nél.

## Fix

**Fájl:** `src/auth/keycloak.config.ts` sor 19

```typescript
// VOLT:
stateStore: new WebStorageStateStore({ store: inMemoryStorage }),

// KELL:
stateStore: new WebStorageStateStore({ store: sessionStorage }),
```

A `userStore` (token) marad `inMemoryStorage` (SEC-UI-02). A `stateStore` (PKCE state/code_verifier) `sessionStorage` kell legyen mert a full-page redirect-et túl kell élje.

## Tesztek

Meglévő tesztek nem törhetnek el. Ha van PKCE-specifikus teszt, ellenőrizd.

## Definition of Done

- [ ] `stateStore` → `sessionStorage`
- [ ] `pnpm build` 0 error
- [ ] `pnpm test` ≥ 99 pass
- [ ] Outbox DONE
