---
id: MSG-FE-013-DONE
from: fe
to: root
type: done
priority: critical
status: READ
ref: MSG-FE-013
created: 2026-04-25
---

# FE-013 DONE — PKCE stateStore fix (BUG-PORTAL-001)

## Commit

`81b2b60` — fix: FE-013 — PKCE stateStore sessionStorage fix (BUG-PORTAL-001)

## Fix

`src/auth/keycloak.config.ts` — `stateStore` átállítva `InMemoryWebStorage`-ról `sessionStorage`-ra.

| Store | Előtte | Utána | Miért |
|---|---|---|---|
| `userStore` (token) | `InMemoryWebStorage` | `InMemoryWebStorage` | SEC-UI-02: token nem kerül sessionStorage-ba |
| `stateStore` (PKCE state/code_verifier) | `InMemoryWebStorage` | `sessionStorage` | Full-page redirect túlélés szükséges |

**Root cause:** InMemoryWebStorage törlődik a Keycloak full-page redirect-nél. A callback oldalon a `code_verifier` már nem elérhető → `signinRedirectCallback()` fail → örökké "Bejelentkezés folyamatban..." marad.

## DoD

- [x] `stateStore` → `sessionStorage`
- [x] `pnpm build` → 0 error
- [x] `pnpm test` → 99/99 pass
- [x] `pnpm lint` + `pnpm typecheck` → 0 hiba
- [x] git commit `81b2b60` + push (main)

## Megjegyzés

INFRA deploy szükséges a VPS-en (`portal.joinerytech.hu`), hogy a fix élesbe kerüljön.
Tester (MSG-TESTER-042) újra tudja tesztelni a login flow-t a deploy után.
