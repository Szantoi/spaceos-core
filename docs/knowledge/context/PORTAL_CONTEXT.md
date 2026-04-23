# Portal Terminal — Hidegindítási Kontextus

> Stack: React 18, Vite, TypeScript, Tailwind CSS, Zustand
> Repo: `/opt/spaceos/design-portal` (Turborepo monorepo)
> Apps: `apps/joinerytech`, `apps/asztalostech`
> Packages: `packages/@spaceos/api-client`

---

## Felelősség

- Brand-specifikus UI (joinerytech.hu / asztalostech.hu)
- Keycloak PKCE OIDC flow (Authorization Code + PKCE S256)
- Token management (memory-only, nincs localStorage)
- BFF `/bff/auth/me` hívás → tenant kontextus
- Spatial canvas UI
- Dashboard, Facility, FlowEpic, Workstation nézetek

---

## Jelenlegi állapot (2026-04-20)

| Metrika | Érték |
|---------|-------|
| Unit tesztek | **323 pass** (app: 308 + packages: 15) |
| VPS státusz | `DEPLOYED` — static files Nginx-en |
| Dist path | `/opt/spaceos/design-portal/apps/joinerytech/dist/` |
| Build | `0 TypeScript error · turbo build OK` |

---

## Auth flow (Keycloak PKCE)

```
Felhasználó → Portal /dashboard
  → nincs token → ProtectedRoute → redirectToLogin()
  → Keycloak /auth/realms/spaceos/protocol/openid-connect/auth?...&code_challenge=S256
  → Keycloak login form
  → Callback /callback?code=...&state=...
  → CallbackPage.tsx: handleCallback() → PKCE exchange
  → /bff/auth/me → tenantId, tenants[], brandSkin
  → authStore.setTokens() → navigate /dashboard
```

**Env vars:**
```
VITE_KC_REALM_URL=https://joinerytech.hu/auth/realms/spaceos
VITE_KC_CLIENT_ID=portal-app
```

---

## Kritikus kód helyek

| Komponens | Helyszín | Fontosság |
|-----------|----------|-----------|
| `packages/@spaceos/api-client/src/auth/keycloak.ts` | PKCE implementáció | state + nonce validáció (SEC-01, SEC-02) |
| `packages/@spaceos/api-client/src/stores/authStore.ts` | Auth state | Memory-only (nincs persist!) |
| `apps/joinerytech/src/features/auth/CallbackPage.tsx` | OIDC callback | handleCallback → setTokens → fetchMe |
| `apps/joinerytech/src/features/auth/ProtectedRoute.tsx` | Route guard | redirectToLogin() hívás |
| `apps/joinerytech/src/api/client.ts` | HTTP client | 401 interceptor → tryRefresh() |

---

## Token tárolás

**Memory-only** — `authStore` Zustand store, `persist: false`. Token page refresh után elvész → újra Keycloak redirect.

**Szándékos:** localStorage token tárolás XSS sebezhetőséget jelent.

---

## Build + deploy

```bash
cd /opt/spaceos/design-portal
npm run build  # Turborepo

# Nginx static deploy:
# Apps dist-je automatikusan:
# /opt/spaceos/design-portal/apps/joinerytech/dist/ → joinerytech.hu /
# /opt/spaceos/design-portal/apps/asztalostech/dist/ → asztalostech.hu /
```

---

## Nemrég lezárt bugok (Sprint Soft Launch → Bug Fix)

| Bug | Fix | Commit |
|-----|-----|--------|
| BUG-005 Chat 422 (üres assistant content) | Portal SSE URL fix (`/bff/chat/stream`) + chunk format fix | Portal terminál |
| BUG-009 Orders error handling | Hibakezelés fejlesztés | — |
| BUG-010 Logout client_id | Keycloak logout endpoint javítás | — |
| BUG-013 Mobile sidebar (375px) | Fixed position overlay + responsive classes | — |
| BUG-015 Browser back auth | localStorage auth persistence + popstate | — |
| BUG-016 Logout parameter | Logout paraméter helyes | — |
| PORTAL-010 Inventory bevételezés UI | Anyag + vastagság szeparáció, auto m², dátum validáció | — |
| PORTAL-012 Nesting panel UX | Loading spinner + error messages | — |

### Chat SSE endpoint

Portal-nak `/bff/chat/stream`-et kell hívnia (nem `/bff/chat` JSON endpoint-ot)!
SSE chunk format: `{ type: 'text', text: '...' }` (nem `text_delta`)

---

## Doorstar Portal (spaceos-doorstar-portal)

Különálló React app: `/opt/spaceos/spaceos-doorstar-portal/`
- 306 pass · 55 API endpoint TypeScript kliens
- `@spaceos/api-client` TS types (PORTAL-013)
- portal.joinerytech.hu LIVE ✅

---

## Ismert tech debt

1. **asztalostech brand** — UI nem tér el jelentősen joinerytech-től (Phase 3C+ scope)
2. **PKCE flow E2E lefedettség** — a 28-keycloak-auth teszt DAG-ot használ, nem PKCE-t
3. **BUG-014** (404 UX) — pending

---

## Indítás előtt

1. `npm run build → 0 error`
2. `npm test → 323 pass, 0 fail`
3. `VITE_KC_REALM_URL` production URL-re állítva?
