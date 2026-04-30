---
id: MSG-KC03-DONE
from: portal
to: architect
type: done
date: 2026-04-09
re: MSG-KC03
---

# MSG-KC03 — Keycloak IdP Integration: DONE

## Teszt összesítő

| Szint | Fájl | Tesztek | Állapot |
|---|---|---|---|
| Package | `packages/@spaceos/api-client/src/auth/keycloak.test.ts` | 10 | ✅ |
| Package | `packages/@spaceos/api-client/src/stores/authStore.test.ts` | 5 | ✅ |
| App | `src/features/auth/CallbackPage.test.tsx` | 4 | ✅ |
| App | `src/features/auth/ProtectedRoute.test.tsx` | 2 | ✅ |
| App | `src/api/tokenRefresh.test.ts` | 8 | ✅ |
| App | `src/components/layout/Topbar.test.tsx` | 4 | ✅ |
| App | Összes többi | ~243 | ✅ |
| **Összesen** | | **276 (app) + 15 (package) = 291** | **0 failure** |

Új tesztek: **≥16** (10 keycloak PKCE + 5 authStore + új CB/PR tesztek)

## Megvalósított feladatok

### T1 — PKCE Auth Module ✅
`packages/@spaceos/api-client/src/auth/keycloak.ts`
- `redirectToLogin()`: PKCE code verifier + challenge generálás, sessionStorage tárolás, Keycloak redirect
- `handleCallback()`: state + nonce validáció (SEC-01, SEC-02), PKCE exchange, retry (BE-05)
- `refreshAccessToken()`: Keycloak direct refresh
- `logoutUrl()`: OIDC RP-initiated logout URL
- `AuthError` osztály `code` property-vel
- Env vars lazy olvasása (vi.stubEnv kompatibilitás miatt)

### T2 — AuthStore teljes csere ✅
`packages/@spaceos/api-client/src/stores/authStore.ts`
- Memory-only tokens (`accessToken`, `refreshToken`, `idToken`) — **nincs persist**
- `UserInfo` + `TenantInfo` shape (tenants, activeTenantId, roles)
- `TenantInfo` tartalmazza: `enabledModules`, `brandSkin`, `allowedHosts` (CabinetOrdersPage kompatibilitás)
- `setTokens(access, refresh, id)`, `clearTokens()`, `fetchMe()`, `tryRefresh()`

### T3 — CallbackPage + ProtectedRoute + Router ✅
- `CallbackPage.tsx`: `handleCallback` → `setTokens` + `fetchMe` → navigate `/`
- `ProtectedRoute.tsx`: `redirectToLogin()` hívás (nem Navigate to /login)
- `LoginErrorPage.tsx`: új hibaoldal retry gombbal
- `router/index.tsx`: `/login` eltávolítva, `/login-error` hozzáadva
- `client.ts`: `tryRefresh()` alapú 401 kezelés, `forceLogout()` → `logoutUrl`/`redirectToLogin`

### T4 — Environment variables ✅
```
.env:            VITE_KC_REALM_URL=http://localhost:8080/realms/spaceos
.env.production: VITE_KC_REALM_URL=https://joinerytech.hu/auth/realms/spaceos
Mindkettő:       VITE_KC_CLIENT_ID=portal-app
```

### T5 — Régi kód eltávolítva ✅
| Törölt fájl | Ok |
|---|---|
| `src/store/auth.store.ts` | `@spaceos/api-client` authStore váltotta fel |
| `src/store/auth.store.test.ts` | authStore.test.ts (package) váltotta fel |
| `src/features/auth/LoginPage.tsx` | PKCE flow nincs username/password form |
| `src/features/auth/LoginPage.test.tsx` | törölt komponens tesztje |
| `src/features/auth/KeycloakLoginPage.tsx` | `redirectToLogin()` váltotta fel |
| `src/features/auth/KeycloakLoginPage.test.tsx` | törölt komponens tesztje |
| `src/api/auth.service.ts` | `/bff/api/auth/token` + `/bff/api/auth/refresh` eltávolítva |
| `src/hooks/useCallbackExchange.ts` | `handleCallback` (package) váltotta fel |

### T6 — Tesztek ✅
Minden fájl, ami `@/store/auth.store`-t importált, frissítve → `@spaceos/api-client`

## DoD gates

```
✅ npm run build → 0 TypeScript error
✅ npm test      → 291 pass, 0 fail (≥16 új teszt)
✅ grep -r "LoginPage\|auth/token\|auth/refresh" src/ → 0 találat
```

## Eltérések az arch doc-tól

1. **fetchMe implementáció**: Az arch doc `createSpaceOsClient(() => accessToken)` + `client.baseUrl` referenciát ír, ami hibás (axios client nem rendelkezik `baseUrl` property-vel). `fetch('/bff/api/auth/me', ...)` közvetlen hívás lett helyette.

2. **allowedHosts a TenantInfo-ban**: Az arch doc nem tartalmazza, de a meglévő `CabinetOrdersPage` megköveteli. Hozzáadva a `TenantInfo` interface-hez.

3. **vitest.config.ts (api-client)**: `environment: 'node'` → `environment: 'jsdom'` módosítás szükséges volt, mert a PKCE tesztek `sessionStorage`/`window` API-kat használnak.

4. **Env var lazy olvasás**: `import.meta.env` konstansok modul-szintű értékelés helyett lazy function-ökként lettek implementálva (`kcRealmUrl()`, `kcClientId()`), hogy `vi.stubEnv` működjön a tesztekben.
