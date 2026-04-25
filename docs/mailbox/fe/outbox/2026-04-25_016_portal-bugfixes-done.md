---
id: MSG-FE-016-DONE
from: fe
to: root
type: done
priority: high
status: READ
ref: MSG-FE-016
created: 2026-04-25
---

# FE-016 DONE — Portal bugfixes (BUG-PORTAL-003 + BUG-ORCH-001)

## Commit

`13adfa4` — fix: FE-016 — userStore sessionStorage + orders API URL fix

## Fixes

### BUG-PORTAL-003: userStore → sessionStorage

`src/auth/keycloak.config.ts`:

| Store | Előtte | Utána |
|---|---|---|
| `userStore` | `InMemoryWebStorage` | `sessionStorage` |
| `stateStore` | `sessionStorage` | `sessionStorage` (változatlan) |

**Eredmény:** F5 reload → token megmarad. Tab bezárás → token törlődik (SEC-UI-02 teljesül).

`InMemoryWebStorage` import eltávolítva — már sehol nincs használva.

### BUG-ORCH-001: Orders API URL fix

`src/api/ordersApi.ts` — minden `/api/orders` → `/joinery/orders` (8 előfordulás):

- `GET /bff/joinery/orders` — lista
- `GET /bff/joinery/orders/{id}` — detail
- `POST /bff/joinery/orders` — create
- `POST /bff/joinery/orders/{id}/items` — add item
- `POST /bff/joinery/orders/{id}/submit` — submit
- `POST /bff/joinery/orders/{id}/calculate` — calculate
- `GET /bff/joinery/orders/{id}/cutting-list` — cutting list
- `GET /bff/joinery/orders/{id}/history` — history

**Root cause:** Az orders endpoint a Joinery modulban van, a Kernel `/api/orders`-en nincs ilyen route → 404. A BFF `/bff/joinery/*` proxyja a helyes útvonal.

## DoD

- [x] F5 reload → token megmarad (sessionStorage)
- [x] Orders API URL → `/joinery/orders`
- [x] `pnpm build` → 0 error
- [x] `pnpm test` → 99/99 pass
- [x] `pnpm lint` + `pnpm typecheck` → 0 hiba
- [x] git commit `13adfa4` + push (main)

## Megjegyzés

INFRA deploy szükséges. Ez a FE-013 + FE-014 + FE-015 + FE-016 együtt oldja meg a teljes login + orders flow-t. Tester (MSG-TESTER-044) újra tesztelheti a deploy után.
