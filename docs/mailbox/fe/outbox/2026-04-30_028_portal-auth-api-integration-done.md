---
id: MSG-FE-028-DONE
from: fe
to: root
type: done
priority: high
status: READ
ref: MSG-FE-028
created: 2026-04-30
---

# FE-028 DONE — Keycloak OIDC + API integráció

## Commit

`7e4c58b` — `feat: Keycloak OIDC + API integráció — 189 teszt, 0 build hiba`

## Definition of Done ✅

- [x] Keycloak login/logout PKCE flow (`oidc-client-ts`)
- [x] Token megjelenik az API hívásokban (`Authorization: Bearer`)
- [x] `TenantInfoBar` — valódi API adatot jelenít meg fallback-kel
- [x] `/w/*` route-ok `RequireAuth` wrappel védve
- [x] `/` route anonymous marad
- [x] `/callback` route létezik (`CallbackPage`)
- [x] `pnpm build` → 0 error
- [x] `pnpm test` → **189/189 pass** (179 meglévő + 10 új)
- [x] `pnpm lint` → 0 error

## Megvalósított struktúra

```
src/auth/
  authConfig.ts       OIDC konfig: authority, client_id, PKCE, InMemoryWebStorage
  AuthContext.tsx     AuthProvider + useAuth hook (UserManager, events)
  CallbackPage.tsx    OIDC redirect callback → /w/dashboard
  RequireAuth.tsx     Route guard — unauthenticated → login() + redirect /
  index.ts            Barrel export
  __mocks__/          AuthContext mock tesztekhez
  __tests__/          useAuth (6), RequireAuth (3), CallbackPage (1) tesztek
```

## API hook

```typescript
// src/hooks/useApi.ts
// - Typed generics, nincs any
// - Bearer token automatikusan minden kéréshez
// - url === null → nem hív semmit (conditional fetch)
// - Fallback: displayData = apiData ?? DEMO_DATA
```

## Route védelem

```
/                     → anonymous (HomeScreen, login gombbal)
/callback             → CallbackPage (OIDC redirect handler)
/w/shopfloor          → RequireAuth → ShopFloorPage
/w/*                  → RequireAuth → WorldShell + child routes
```

## Security review ✅

- `stateStore: InMemoryWebStorage` — XSS-safe (nem sessionStorage/localStorage)
- Token csak `Authorization: Bearer` headerben — sosem logolva
- Nincs `console.log(token)`, nincs `localStorage.setItem('token', ...)`
- Nincs `dangerouslySetInnerHTML`
- TypeScript strict — nincs `any`
- Test izolálás: `oidc-client-ts` globálisan mock-olva → nincs valódi Keycloak hívás tesztben

## Teszt összesítő

| Terület | Tesztek |
|---|---|
| Meglévő (FE-027) | 179 |
| Auth (useAuth, RequireAuth, Callback) | 10 |
| **Összesen** | **189/189 pass** |
