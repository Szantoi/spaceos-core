---
id: MSG-P022
from: architect
to: portal
type: task
priority: P0
date: 2026-04-08
sprint: "Sprint D · Production Readiness"
---

# Production Readiness — Portal: Track A.6 (Keycloak Auth Provider)

## Kontextus

Phase 3C+ DoD ✅ teljes. A Portal felelőssége:
- `useAuthProvider()` hook — `VITE_AUTH_PROVIDER=keycloak|dev`
- Keycloak redirect login flow + `/callback` route
- Backward compatible: dev mode nem törik el

---

## T1 — `VITE_AUTH_PROVIDER` env var

Fájl: `apps/joinerytech/.env` (dev fallback), `apps/joinerytech/.env.production`

```bash
# .env (dev)
VITE_AUTH_PROVIDER=dev

# .env.production
VITE_AUTH_PROVIDER=keycloak
VITE_KEYCLOAK_URL=https://auth.joinerytech.hu
VITE_KEYCLOAK_REALM=spaceos
VITE_KEYCLOAK_CLIENT_ID=spaceos-portal
```

---

## T2 — `useAuthProvider()` hook

Fájl: `apps/joinerytech/src/hooks/useAuthProvider.ts`

```typescript
// Abstrakció: dev mode vagy Keycloak redirect flow
export type AuthProvider = 'dev' | 'keycloak';

export function useAuthProvider(): AuthProvider {
  return (import.meta.env['VITE_AUTH_PROVIDER'] as AuthProvider) ?? 'dev';
}
```

---

## T3 — Keycloak redirect login

Fájl: `apps/joinerytech/src/features/auth/KeycloakLoginPage.tsx`

```typescript
// Keycloak mode: redirect to /auth/realms/spaceos/protocol/openid-connect/auth
// query params: client_id, redirect_uri, response_type=code, scope=openid profile email
export function KeycloakLoginPage() {
  useEffect(() => {
    const url = new URL(`${import.meta.env['VITE_KEYCLOAK_URL']}/auth/realms/${import.meta.env['VITE_KEYCLOAK_REALM']}/protocol/openid-connect/auth`);
    url.searchParams.set('client_id', import.meta.env['VITE_KEYCLOAK_CLIENT_ID']);
    url.searchParams.set('redirect_uri', `${window.location.origin}/callback`);
    url.searchParams.set('response_type', 'code');
    url.searchParams.set('scope', 'openid profile email');
    window.location.href = url.toString();
  }, []);
  return <div className="p-6 text-sm text-gray-400">Átirányítás Keycloak-ra…</div>;
}
```

---

## T4 — `/callback` route

Fájl: `apps/joinerytech/src/features/auth/CallbackPage.tsx`

```typescript
// Keycloak code → BFF /bff/auth/token (PKCE code exchange)
// Siker: useAuthStore.login(accessToken, refreshToken, brandSkin)
// Navigálás: /dashboard
```

Regisztrálás a routerben: `{ path: '/callback', element: <CallbackPage /> }`

---

## T5 — `LoginPage.tsx` bővítés

Ha `VITE_AUTH_PROVIDER=keycloak` → redirect to `<KeycloakLoginPage />`
Ha `VITE_AUTH_PROVIDER=dev` → meglévő form (nem változik)

```typescript
// apps/joinerytech/src/features/auth/LoginPage.tsx
const provider = import.meta.env['VITE_AUTH_PROVIDER'] ?? 'dev';
if (provider === 'keycloak') return <KeycloakLoginPage />;
// ... meglévő dev form ...
```

---

## DoD gate-ek (Portal)

```bash
tsc --noEmit  # 0 error
vitest run    # meglévő tesztek zöld (nincs regresszió)
```

Checklist:
- [ ] `VITE_AUTH_PROVIDER=dev` → meglévő login form — semmi sem törik
- [ ] `VITE_AUTH_PROVIDER=keycloak` → Keycloak redirect URL helyes
- [ ] `/callback` route regisztrálva a routerben
- [ ] `useAuthProvider()` hook unit tested

## Válaszban kérem

Mailbox outbox: `docs/mailbox/portal/outbox/2026-04-08_022_prod-readiness-portal-done.md`
