---
id: MSG-KC03
from: architect
to: portal
type: task
priority: P0
date: 2026-04-09
sprint: "Keycloak IdP Integration — Portal"
effort: "~3 nap (Nap 7-9 a végrehajtási sorrendben)"
---

# Keycloak IdP Integration — Portal

## Kontextus

Ref: `/opt/spaceos/docs/SpaceOS_Keycloak_IdP_Architecture_v4.md`

A Portal (React/Vite, `apps/joinerytech`) a username/password login helyett Keycloak OIDC Authorization Code + PKCE flow-ra vált. Tokenek memory-only tárolás (nem localStorage). Auth store teljesen újraírandó.

**Függőség:** Keycloak realm fut (Infra Track A), Orchestrator `/bff/api/auth/me` kész (Track C, Nap 5-6).

---

## T1 — PKCE Auth Module (Nap 7)

### Fájl: `packages/@spaceos/api-client/src/auth/keycloak.ts` — ÚJ

```typescript
const KC_REALM_URL = import.meta.env.VITE_KC_REALM_URL;
const KC_CLIENT_ID = import.meta.env.VITE_KC_CLIENT_ID;

function base64UrlEncode(buffer: Uint8Array): string {
  return btoa(String.fromCharCode(...buffer))
    .replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

function generateRandom(length: number): string {
  const array = new Uint8Array(length);
  crypto.getRandomValues(array);
  return base64UrlEncode(array);
}

async function generateCodeChallenge(verifier: string): Promise<string> {
  const digest = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(verifier));
  return base64UrlEncode(new Uint8Array(digest));
}

export async function redirectToLogin(): Promise<void> {
  const codeVerifier = generateRandom(32);
  const codeChallenge = await generateCodeChallenge(codeVerifier);
  const state = generateRandom(16);   // SEC-01: CSRF protection
  const nonce = generateRandom(16);   // SEC-02: token replay protection

  sessionStorage.setItem('pkce_verifier', codeVerifier);
  sessionStorage.setItem('oauth_state', state);
  sessionStorage.setItem('oauth_nonce', nonce);

  const params = new URLSearchParams({
    client_id: KC_CLIENT_ID,
    response_type: 'code',
    scope: 'openid profile email',
    redirect_uri: `${window.location.origin}/callback`,
    code_challenge: codeChallenge,
    code_challenge_method: 'S256',
    state,
    nonce
  });
  window.location.href = `${KC_REALM_URL}/protocol/openid-connect/auth?${params}`;
}

export async function handleCallback(searchParams: URLSearchParams): Promise<TokenResponse> {
  const code = searchParams.get('code');
  const returnedState = searchParams.get('state');
  const error = searchParams.get('error');

  if (error) throw new AuthError(`Keycloak error: ${error}`, 'keycloak_error');
  if (!code) throw new AuthError('Missing authorization code', 'missing_code');

  // SEC-01: state validation
  const storedState = sessionStorage.getItem('oauth_state');
  if (!storedState || storedState !== returnedState) {
    sessionStorage.removeItem('pkce_verifier');
    sessionStorage.removeItem('oauth_state');
    sessionStorage.removeItem('oauth_nonce');
    throw new AuthError('State mismatch — possible CSRF', 'state_mismatch');
  }

  const codeVerifier = sessionStorage.getItem('pkce_verifier');
  const storedNonce = sessionStorage.getItem('oauth_nonce');
  sessionStorage.removeItem('pkce_verifier');
  sessionStorage.removeItem('oauth_state');
  sessionStorage.removeItem('oauth_nonce');

  if (!codeVerifier) throw new AuthError('Missing PKCE verifier', 'missing_verifier');

  const tokens = await exchangeCodeWithRetry(code, codeVerifier);

  // SEC-02: nonce validation
  if (storedNonce && tokens.id_token) {
    const idPayload = parseJwtPayload(tokens.id_token);
    if (idPayload.nonce !== storedNonce)
      throw new AuthError('Nonce mismatch — possible token replay', 'nonce_mismatch');
  }
  return tokens;
}

async function exchangeCodeWithRetry(
  code: string, codeVerifier: string, attempt = 0
): Promise<TokenResponse> {
  try {
    return await exchangeCode(code, codeVerifier);
  } catch (err) {
    if (attempt === 0) {  // BE-05: retry once after 1s
      await new Promise(r => setTimeout(r, 1000));
      return exchangeCodeWithRetry(code, codeVerifier, 1);
    }
    throw err;
  }
}

async function exchangeCode(code: string, codeVerifier: string): Promise<TokenResponse> {
  const res = await fetch(`${KC_REALM_URL}/protocol/openid-connect/token`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: new URLSearchParams({
      grant_type: 'authorization_code',
      client_id: KC_CLIENT_ID,
      code,
      redirect_uri: `${window.location.origin}/callback`,
      code_verifier: codeVerifier
    })
  });
  if (!res.ok) {
    const body = await res.text().catch(() => '');
    throw new AuthError(`Token exchange failed: ${res.status} ${body}`, 'exchange_failed');
  }
  return res.json();
}

export async function refreshAccessToken(refreshToken: string): Promise<TokenResponse> {
  const res = await fetch(`${KC_REALM_URL}/protocol/openid-connect/token`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: new URLSearchParams({
      grant_type: 'refresh_token',
      client_id: KC_CLIENT_ID,
      refresh_token: refreshToken
    })
  });
  if (!res.ok) throw new AuthError('Token refresh failed', 'refresh_failed');
  return res.json();
}

export function logoutUrl(idTokenHint: string): string {
  const params = new URLSearchParams({
    post_logout_redirect_uri: window.location.origin,
    id_token_hint: idTokenHint
  });
  return `${KC_REALM_URL}/protocol/openid-connect/logout?${params}`;
}

function parseJwtPayload(token: string): Record<string, any> {
  const base64 = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
  return JSON.parse(atob(base64));
}

export class AuthError extends Error {
  constructor(message: string, public code: string) {
    super(message);
    this.name = 'AuthError';
  }
}

export interface TokenResponse {
  access_token: string;
  refresh_token: string;
  id_token: string;
  expires_in: number;
  token_type: string;
}
```

---

## T2 — AuthStore teljes csere (Nap 7)

### Fájl: `packages/@spaceos/api-client/src/stores/authStore.ts` — TELJES CSERE

```typescript
import { create } from 'zustand';
import { refreshAccessToken } from '../auth/keycloak';
import { createSpaceOsClient } from '../client';

export interface TenantInfo {
  tenantId: string;
  tenantType: string;
  enabledModules: string[];
  brandSkin: string;
}

export interface UserInfo {
  sub: string;
  email: string;
  name: string;
  tenants: TenantInfo[];
  activeTenantId: string | null;
  roles: string[];
}

interface AuthState {
  // MEMORY ONLY — no localStorage/sessionStorage persist
  accessToken: string | null;
  refreshToken: string | null;
  idToken: string | null;
  user: UserInfo | null;
  isAuthenticated: boolean;

  setTokens: (access: string, refresh: string, id: string) => void;
  clearTokens: () => void;
  fetchMe: () => Promise<void>;
  tryRefresh: () => Promise<boolean>;
}

export const useAuthStore = create<AuthState>((set, get) => ({
  accessToken: null, refreshToken: null, idToken: null,
  user: null, isAuthenticated: false,

  setTokens: (access, refresh, id) =>
    set({ accessToken: access, refreshToken: refresh, idToken: id, isAuthenticated: true }),

  clearTokens: () =>
    set({ accessToken: null, refreshToken: null, idToken: null, user: null, isAuthenticated: false }),

  fetchMe: async () => {
    const { accessToken } = get();
    if (!accessToken) return;
    const client = createSpaceOsClient(() => accessToken);
    const res = await fetch(`${client.baseUrl}/bff/api/auth/me`, {
      headers: { Authorization: `Bearer ${accessToken}` }
    });
    if (!res.ok) return;
    const data: UserInfo = await res.json();
    set({ user: data });
  },

  tryRefresh: async () => {
    const { refreshToken } = get();
    if (!refreshToken) return false;
    try {
      const tokens = await refreshAccessToken(refreshToken);
      set({
        accessToken: tokens.access_token, refreshToken: tokens.refresh_token,
        idToken: tokens.id_token, isAuthenticated: true
      });
      return true;
    } catch {
      get().clearTokens();
      return false;
    }
  }
}));
```

---

## T3 — CallbackPage + ProtectedRoute + Route changes (Nap 7-8)

### Fájl: `apps/joinerytech/src/pages/CallbackPage.tsx` — ÚJ

```typescript
import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { handleCallback, AuthError } from '@spaceos/api-client';
import { useAuthStore } from '@spaceos/api-client';

export default function CallbackPage() {
  const navigate = useNavigate();
  const setTokens = useAuthStore((s) => s.setTokens);
  const fetchMe = useAuthStore((s) => s.fetchMe);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    handleCallback(params)
      .then(async (tokens) => {
        setTokens(tokens.access_token, tokens.refresh_token, tokens.id_token);
        await fetchMe();
        navigate('/', { replace: true });
      })
      .catch((err) => {
        if (err instanceof AuthError) {
          if (err.code === 'state_mismatch' || err.code === 'nonce_mismatch')
            setError('Biztonsági hiba. Kérjük próbáld újra a bejelentkezést.');
          else
            setError('Bejelentkezés sikertelen. Kérjük próbáld újra.');
        } else {
          setError('Váratlan hiba történt.');
        }
      });
  }, []);

  if (error) return (
    <div className="flex flex-col items-center justify-center h-screen gap-4">
      <p className="text-red-600">{error}</p>
      <button className="px-4 py-2 bg-primary text-white rounded"
        onClick={() => navigate('/', { replace: true })}>
        Vissza a főoldalra
      </button>
    </div>
  );

  return <div className="flex items-center justify-center h-screen"><p>Bejelentkezés...</p></div>;
}
```

### Fájl: `apps/joinerytech/src/components/ProtectedRoute.tsx` — DIFF

```typescript
import { redirectToLogin } from '@spaceos/api-client';
import { useAuthStore } from '@spaceos/api-client';

export function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  if (!isAuthenticated) {
    redirectToLogin();
    return <div className="flex items-center justify-center h-screen">Átirányítás...</div>;
  }
  return <>{children}</>;
}
```

### Fájl: `apps/joinerytech/src/router.tsx` — DIFF

```typescript
// ADD:
import CallbackPage from './pages/CallbackPage';
import LoginErrorPage from './pages/LoginErrorPage';

// Routes:
{ path: '/callback', element: <CallbackPage /> },
{ path: '/login-error', element: <LoginErrorPage /> },

// REMOVE:
// { path: '/login', element: <LoginPage /> }
```

---

## T4 — Environment variables (Nap 8)

```bash
# apps/joinerytech/.env.production
VITE_KC_REALM_URL=https://joinerytech.hu/auth/realms/spaceos
VITE_KC_CLIENT_ID=portal-app

# apps/joinerytech/.env.development
VITE_KC_REALM_URL=http://localhost:8080/realms/spaceos
VITE_KC_CLIENT_ID=portal-app
```

---

## T5 — Régi kód eltávolítása (Nap 8)

| Fájl / Komponens | Akció |
|---|---|
| `LoginPage.tsx` (username/password form) | Törlés |
| Dev auth API hívások (`/bff/api/auth/token`, `/bff/api/auth/refresh`) | Törlés |
| `authStore` régi login/logout action-ök | Teljes csere (T2) |
| Manual JWT decode (role, brandSkin extraction) | `/bff/api/auth/me` adja vissza |

---

## T6 — Tesztek (Nap 8-9)

**≥16 új teszt** (részletek az arch doc Section 9.3-ban):

```
- redirectToLogin_URL × 3           (URL params: client_id, PKCE, state, nonce)
- handleCallback_Success × 2        (mock fetch → token response → store update)
- handleCallback_StateMismatch × 1  (SEC-01: state mismatch → AuthError)
- handleCallback_NonceMismatch × 1  (SEC-02: nonce mismatch → AuthError)
- handleCallback_MissingCode × 1    (no code param → AuthError)
- exchangeCode_Retry × 1            (first fail → retry after 1s → success, BE-05)
- authStore_SetClear × 2            (setTokens / clearTokens / isAuthenticated)
- authStore_TryRefresh × 2          (mock refresh → new tokens)
- authStore_TryRefresh_Fail × 1     (refresh fail → clearTokens)
- ProtectedRoute × 1                (not authenticated → redirectToLogin called)
- CallbackPage_ErrorDisplay × 1     (Keycloak error param → error message)
```

Nginx CSP update (SEC-03) Nap 9-en: `connect-src 'self' https://joinerytech.hu/auth/ https://asztalostech.hu/auth/`

---

## DoD gates

```bash
cd /opt/spaceos/spaceos-design-portal
npm run build 2>&1 | tail -3      # 0 TypeScript error
npm test 2>&1 | tail -5           # ≥321 meglévő + ≥16 új = ≥337 pass, 0 fail
grep -r "LoginPage\|auth/token\|auth/refresh" src/  # 0 találat (csak /callback és /login-error marad)
```

---

## Output

Ha kész: `mailbox/outbox/2026-04-09_048_portal-keycloak-idp-done.md`

Tartalom:
- Teszt összesítő (meglévő + új)
- Törölt fájlok listája
- Esetleges eltérések az arch doc-tól
