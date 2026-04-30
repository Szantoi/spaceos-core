---
id: MSG-PORTAL-004-DONE
from: portal
to: root
type: response
priority: high
status: READ
ref: MSG-PORTAL-004
created: 2026-04-15
---

# MSG-PORTAL-004-DONE — Sprint 5: Test Coverage — PKCE auth + refresh race

## Összefoglaló

| Fájl | Változás |
|---|---|
| `apps/joinerytech/src/features/auth/CallbackPage.tsx` | `console.error('[Security]', ...)` hozzáadva state_mismatch/nonce_mismatch esetén |
| `apps/joinerytech/src/features/auth/CallbackPage.test.tsx` | +5 OAuth hibaeset teszt |
| `packages/@spaceos/api-client/src/stores/authStore.ts` | `_refreshInFlight` modul-szintű dedup — concurrent tryRefresh → 1 hálózati kérés |
| `packages/@spaceos/api-client/src/stores/authStore.concurrent.test.ts` | ÚJ — 3 concurrent/dedup teszt |
| `packages/@spaceos/api-client/vitest.config.ts` | coverage threshold: `src/auth/**` + `src/stores/**` → 80% branch |
| `apps/joinerytech/vitest.config.ts` | coverage threshold: `src/features/auth/**` → 80% branch |

## Tesztek

| Csomag | Előtte | Utána | Változás |
|---|---|---|---|
| `apps/joinerytech` | 276 | **281** | +5 (CallbackPage OAuth hibák) |
| `packages/@spaceos/api-client` | 15 | **18** | +3 (concurrent dedup) |
| **Összesen** | **291** | **299** | **+8** |

Minden teszt zöld. DoD ≥ 291 teljesítve (299).

**Diagnosztikai megjegyzés:** az api-client csomag lokális `node_modules`-ban vitest **3.2.4** van (root: 1.6.1). A `npm test` (amit Turbo hív) a lokális binaryt használja — ez helyes és működik. A `npx vitest run` parancs a root 1.6.1-et találta, ami inkompatibilis volt → ez pre-existing probléma, nem új.

## 5 CallbackPage OAuth hibaeset (DoD teljesítve)

| # | Eset | Mock kód | Ellenőrzött viselkedés |
|---|---|---|---|
| 1 | `?error=access_denied` | `keycloak_error` | "Bejelentkezés sikertelen" + "Vissza" gomb |
| 2 | state_mismatch (CSRF) | `state_mismatch` | "Biztonsági hiba" + `console.error('[Security]', ...)` |
| 3 | `code_verifier` hiányzik | `missing_verifier` | Hibaoldal, nem fagy |
| 4 | Keycloak 400 | `exchange_failed` | Hibaoldal + `handleCallback` pontosan 1× hívva |
| 5 | Keycloak 500 | `exchange_failed` | Hibaoldal + "Vissza" (retry) gomb |

## Concurrent refresh dedup (3 teszt)

- 2 párhuzamos `tryRefresh()` → 1 hálózati kérés (deduplicated) ✓
- 3 párhuzamos `tryRefresh()` → 1 hálózati kérés ✓
- Egymás utáni (sequential) refreshek → 2 hálózati kérés (helyes) ✓

## Security review

- XSS: nincs `dangerouslySetInnerHTML`
- Auth: minden védett oldal `ProtectedRoute` mögött
- Token: csak memóriában (Zustand, nem localStorage)
- Security logging: `console.error('[Security]', err.code)` state/nonce mismatch esetén
- Input: user input nem kerül HTML-be

## Kockázatok / kérdések

Nincs. Minden DoD-pont teljesítve.
