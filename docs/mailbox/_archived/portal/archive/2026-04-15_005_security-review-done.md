---
id: MSG-PORTAL-005-DONE
from: portal
to: root
type: response
priority: high
status: READ
ref: MSG-PORTAL-005
created: 2026-04-15
---

# MSG-PORTAL-005-DONE — Security Review (Q2 Pre-launch)

## Összefoglaló

Teljes biztonsági önellenőrzés elvégezve. **Kritikus kódhibát nem találtam.** Nincs BLOCKED állapot.
1 MEDIUM találat (Axios CVE) VPS operátori beavatkozást igényel. Részletek alább.

---

## 1. Authentication (PKCE OAuth flow)

| Ellenőrzés | Eredmény | Megjegyzés |
|---|---|---|
| `state` + `nonce` validálva callback-ben | ✅ PASS | `keycloak.ts:63–85` — mindkettő ellenőrzött, mismatch → `AuthError` |
| PKCE S256 helyesen implementálva | ✅ PASS | `crypto.subtle.digest('SHA-256', ...)` + base64url encode |
| Token csak memóriában (nem localStorage) | ✅ PASS | `authStore.ts:2` — "Memory-only". PKCE paraméterek sessionStorage-ban vannak, de csak az auth flow idejére (redirect előtt set, callback után remove) |
| Refresh race condition kezelve | ✅ PASS | `_refreshInFlight` dedup implementálva (R-17 fix, Sprint 5) |
| Logout: token törlés + Keycloak session | ✅ PASS | `clearTokens()` + `window.location.href = logoutUrl(idToken)` — Keycloak OIDC logout endpoint |

**Megjegyzés:** `store/CLAUDE.md` elavult dokumentáció — "persisted to localStorage" szöveg maradt, de a `auth.store.ts` fájl törölve lett (commit `bccf2bf`). Tényleges store: memory-only `useAuthStore` az api-client csomagban. Alacsony kockázat (csak dokumentáció), de frissítendő.

---

## 2. XSS

| Ellenőrzés | Eredmény | Megjegyzés |
|---|---|---|
| `dangerouslySetInnerHTML` | ✅ PASS | Nincs az egész kódbázisban |
| URL paraméterek DOM-ba kerülnek? | ✅ PASS | `?error=...` → `handleCallback(params)` mock-olható függvény, soha nem `.innerHTML`-be |
| React Router redirect — relatív URL? | ✅ PASS | Minden `Navigate` relatív (`to="/"`) |

---

## 3. Route Protection

| Ellenőrzés | Eredmény | Megjegyzés |
|---|---|---|
| Minden védett oldal `ProtectedRoute` mögött | ✅ PASS | Router: `{path: '/', element: <ProtectedRoute />}` → összes app route child |
| Unauthenticated → auth flow | ✅ PASS | `ProtectedRoute.tsx` — `!isAuthenticated` → `redirectToLogin()` |
| `/callback` elérhető bejelentkezett usernek? | ⚠️ LOW | `/callback` nincs `ProtectedRoute` mögött. Exploitálhatóság alacsony: sessionStorage PKCE state nélkül a token exchange sikertelen, a meglévő session nem kerül veszélybe. |

---

## 4. Token Handling

| Ellenőrzés | Eredmény | Megjegyzés |
|---|---|---|
| Access token csak HTTPS-en | ✅ PASS | Production `baseURL: '/bff'` relatív URL — nginx HTTPS-en fut |
| Token expiry kliens oldalon | ⚠️ LOW | Nincs proaktív expiry check. Reaktív: 401 → `tryRefresh()`. Ez elfogadható pattern. |
| Authorization header csak ismert originre | ✅ PASS | `apiClient` `baseURL: '/bff'` (same-origin) — third-party hívásokhoz nem megy a Bearer token. Keycloak token exchange saját `fetch` hívásokban, Authorization header nélkül. |

---

## 5. Dependency Security

### npm audit eredmény

```
6 vulnerabilities (5 moderate, 1 critical)
```

**CRITICAL — Axios 1.14.0:**
- `GHSA-3p68-rc4w-qgx5` — NO_PROXY Hostname Normalization Bypass → SSRF
- `GHSA-fvcv-3m26-pcqx` — Unrestricted Cloud Metadata Exfiltration via Header Injection Chain
- Range: `<=1.14.0`, fix: `1.15.0`

**Kontextuális értékelés:** Az SSRF CVE Node.js server-side kontextusban kritikus. Ez a kódbázis **böngésző-only** Axios klienst használ (`/bff` relatív URL, CORS által védett). Browser kontextusban az exploitálhatóság **alacsony-közepes**.

**Kategória: MEDIUM** (npm audit: critical, tényleges browser kockázat: közepes)

**Fix szükséges, de blokkolt:** `packages/@spaceos/api-client/node_modules/` root tulajdonban van. Megoldás:
```bash
sudo chown -R gabor:gabor /opt/spaceos/design-portal/packages/@spaceos/api-client/node_modules/
pnpm update axios@1.15.0 -r
```
→ **VPS Operator beavatkozást igényel.**

---

## 6. Build / Info Leak

| Ellenőrzés | Eredmény | Megjegyzés |
|---|---|---|
| Source map production build-ben | ✅ PASS | `vite.config.ts`: `build: { sourcemap: false }` |
| API URL-ek a bundle-ben | ✅ PASS | Csak `/bff` relative prefix — nincs hardcoded belső URL |
| `console.*` production bundle-ben | ⚠️ LOW | 4 db: security log (`[Security]`), ErrorBoundary stack trace log, rate limit warn, brand router warn — mindegyik megfontolt, de `componentStack` leakeli a React fa struktúrát a devtools-ban |
| `.env` fájlok | ✅ PASS | `.env.production` csak realm URL + client ID (nem titkos, publikus OIDC paraméter) |

---

## 7. OWASP Top 10 (frontend scope)

| | Ellenőrzés | Eredmény |
|---|---|---|
| A3 | Injection — eval/Function/innerHTML | ✅ PASS — egyik sem található |
| A5 | Security Misconfiguration — dev mód production-ban | ⚠️ LOW — `useAuthProvider` default='dev', **de ez a hook nincs sehol meghívva** a production kódban (dead code) |
| A7 | XSS — React JSX escaping | ✅ PASS — React automatikus escaping, nincs bypass |
| A1 | Broken Access Control — Bearer token minden API híváson | ✅ PASS — `client.ts` request interceptor minden `/bff` kéréshez hozzáadja |

---

## Összesített találatok

| Súlyosság | Szám | Részlet |
|---|---|---|
| 🔴 Kritikus | 0 | — |
| 🟠 Közepes | 1 | Axios CVE (VPS Operator fix szükséges) |
| 🟡 Alacsony | 4 | `/callback` no auth guard · token expiry nincs · useAuthProvider dead code · ErrorBoundary componentStack log |

**Status: DONE** — nincs kritikus találat, nincs BLOCKED.

## Javasolt követő lépések (nem blokkoló)

1. **VPS Operator:** `sudo chown` + `pnpm update axios@1.15.0` — Axios CVE fix
2. `/callback` route: `isAuthenticated` check + redirect `/`-re ha már be van jelentkezve (ALACSONY prioritás)
3. `store/CLAUDE.md` frissítése (elavult localStorage dokumentáció)
