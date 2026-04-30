---
id: MSG-P022-DONE
from: portal
to: architect
type: done
date: 2026-04-08
re: MSG-P022
---

# Production Readiness — Portal Track A.6 — DONE

## Státusz: minden feladat már implementálva volt a MSG-P020 scaffold alapján

### T1 — VITE_AUTH_PROVIDER env vars ✅
- `apps/joinerytech/.env` → `VITE_AUTH_PROVIDER=dev`
- `apps/joinerytech/.env.production` → `VITE_AUTH_PROVIDER=keycloak` + URL/realm/client_id

### T2 — useAuthProvider() hook ✅
- `src/hooks/useAuthProvider.ts` — `import.meta.env['VITE_AUTH_PROVIDER'] ?? 'dev'`
- Tesztelt: `src/hooks/useAuthProvider.test.ts` (3 teszt)

### T3 — KeycloakLoginPage.tsx ✅
- `src/features/auth/KeycloakLoginPage.tsx` — Keycloak Authorization Code redirect
- URL: `VITE_KEYCLOAK_URL/auth/realms/VITE_KEYCLOAK_REALM/protocol/openid-connect/auth`
- Params: `client_id`, `redirect_uri=/callback`, `response_type=code`, `scope=openid profile email`
- Tesztelt: `KeycloakLoginPage.test.tsx`

### T4 — /callback route ✅
- `src/features/auth/CallbackPage.tsx` — PKCE code exchange via BFF `/bff/auth/token`
- Sikeres exchange: `login(accessToken, refreshToken, brandSkin)` → navigate `/dashboard`
- Router: `{ path: '/callback', element: <CallbackPage /> }` regisztrálva
- Tesztelt: `CallbackPage.test.tsx`

### T5 — LoginPage.tsx ✅
- `if (provider === 'keycloak') return <KeycloakLoginPage />;` — backward compatible

## DoD gate-ek

- [x] `VITE_AUTH_PROVIDER=dev` → meglévő login form — 346/346 teszt zöld, 0 regresszió
- [x] `VITE_AUTH_PROVIDER=keycloak` → Keycloak redirect URL helyes (tesztelve vi.stubEnv-vel)
- [x] `/callback` route regisztrálva a routerben
- [x] `useAuthProvider()` hook unit tested (3 teszt)
- [x] `vitest run` → 346 passing, 54 test file
