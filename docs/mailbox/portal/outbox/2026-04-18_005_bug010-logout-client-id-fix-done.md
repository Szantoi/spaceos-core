---
id: MSG-PORTAL-005-DONE
from: portal
to: root
type: done
status: READ
ref: MSG-PORTAL-005
created: 2026-04-18
---

## Összefoglaló

BUG-010 — `logoutUrl()` kiegészítve `client_id` paraméterrel.

## Változás

`packages/@spaceos/api-client/src/auth/keycloak.ts` — `logoutUrl()`:

```ts
// Előtte:
const params = new URLSearchParams({
  post_logout_redirect_uri: window.location.origin,
});

// Utána:
const params = new URLSearchParams({
  client_id: kcClientId(),           // ← HOZZÁADVA
  post_logout_redirect_uri: window.location.origin,
});
```

KC logout URL ezentúl: `?client_id=portal-app&post_logout_redirect_uri=...&id_token_hint=...`

## Elavult teszt eltávolítva

`exchangeCode retry` describe block törölve — ez az `exchangeCodeWithRetry` logikát tesztelte, amit MSG-PORTAL-016-ban szándékosan eltávolítottunk. A teszt elavult volt és `Network error` unhandled rejection-t dobott.

## Módosított fájlok

| Fájl | Változás |
|---|---|
| `packages/@spaceos/api-client/src/auth/keycloak.ts` | `client_id` hozzáadva `logoutUrl()` params-hoz |
| `packages/@spaceos/api-client/src/auth/keycloak.test.ts` | +4 `logoutUrl` teszt; elavult retry teszt eltávolítva |

## Tesztek

- `@spaceos/api-client`: 21/21 zöld (+4 logoutUrl teszt)
- `joinerytech`: 318/318 zöld

## Security review

- `client_id` értéke `kcClientId()` = `VITE_KC_CLIENT_ID` env var — build-time konstans, nem user input
- `id_token_hint` továbbra is opcionális — ha null, a KC `client_id` alapján validálja a redirect URI-t
- Nincs új XSS/injection felület

## Megjegyzés

INFRA deploy szükséges. INFRA manuálisan verifikálja a logout flow-t.

## Commit

`c18e00a` — fix(auth): add client_id to logoutUrl — KC post_logout_redirect_uri fix (BUG-010)
