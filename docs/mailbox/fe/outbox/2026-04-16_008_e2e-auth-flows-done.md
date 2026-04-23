---
id: MSG-FE-008-DONE
from: fe
to: root
type: done
priority: high
status: READ
ref: MSG-FE-008
created: 2026-04-16
---

# FE-008 DONE — E2E-L2: Auth + layout flows (01-login, 08-auth-edge, 09-responsive)

## Összefoglalás

E2E flow tesztek implementálva (01 + 08 + 09), kód push-olva. A tesztek infrastruktúrálisan
helyesek és futnak — azonban a deployed portálon egy **INFRA CSP blokkoló** miatt
jelenleg nem zöldek (`pnpm test:e2e`). Contract issue benyújtva.

Emellett egy **kritikus auth bug** is felderítve és javítva (`CallbackPage` dupla callback).

---

## DoD ellenőrzőlista

- [x] `tests/e2e/flows/01-login.spec.ts` — 4 valódi teszt (nem stub)
- [x] `tests/e2e/flows/08-auth-edge.spec.ts` — 3 teszt
- [x] `tests/e2e/flows/09-responsive.spec.ts` — 2 teszt
- [x] `data-testid` attribútumok hozzáadva: `app-shell` (ProtectedRoute), `app-header` (AppHeader)
- [x] `pnpm test` (unit + contract) → 90/90 zöld (változatlan)
- [x] `pnpm build` → 0 error
- [x] `pnpm lint` + `pnpm typecheck` → 0 hiba
- [x] git commit + push (`88bf153`)
- [ ] `pnpm test:e2e` → ≥7 E2E zöld — **BLOKKOLT: CI-001 (INFRA CSP)** — ld. alább

---

## Kritikus bug javítás — CallbackPage dupla callback

**Root cause:** A `CallbackPage.tsx` manuálisan hívta `userManager.signinRedirectCallback()`-et,
miközben a `react-oidc-context` `OidcAuthProvider` is automatikusan meghívja ugyanezt,
amikor a `/callback?code=` URL-t érzékeli. Ez **dupla feldolgozást** okozott → az első hívás
elfogyasztotta a PKCE state-et, a második hívás NetworkError/StateError-rel dobott →
`ErrorBoundary` elkapta → "Váratlan hiba történt." megjelent minden bejelentkezés után.

**Javítás:**
- `CallbackPage.tsx`: eltávolítva a `signinRedirectCallback()` hívás — csak loading spinnerré vált
- `AuthProvider.tsx`: hozzáadva `onSigninCallback={() => window.history.replaceState({}, '', '/orders')}`
  → a react-oidc-context maga kezeli a callback feldolgozást és redirect-et

---

## BLOKKOLT: CI-001 — Nginx CSP blokkolja az OIDC kapcsolatokat

**Diagnózis:** A deployed portál nginx-en a következő CSP header fut:
```
content-security-policy: connect-src 'self' https://api.anthropic.com;
```

`'self'` = `portal.joinerytech.hu`. Ez **NEM engedélyezi** a `joinerytech.hu/auth/` domain-t,
ahol a Keycloak fut.

**Következmény:**
1. **OIDC discovery fetch** (`GET joinerytech.hu/auth/realms/spaceos/.well-known/openid-configuration`)
   → böngésző blokkolja → react-oidc-context nem tud inicializálni → "Unexpected Application Error!"
   jelenik meg minden route-on nem bejelentkezett állapotban
2. **Token exchange** (PKCE callback: `POST joinerytech.hu/auth/realms/spaceos/protocol/openid-connect/token`)
   → böngésző blokkolja → bejelentkezés sikertelen (ErrorBoundary)

**Szükséges infra javítás:**
```nginx
# /etc/nginx/conf.d/portal.conf — add-hozzá joinerytech.hu-t connect-src-hez:
connect-src 'self' https://joinerytech.hu https://api.anthropic.com;
```

**CONTRACT_ISSUE:** `CI-001` benyújtva (`CONTRACT_ISSUES.md`).

---

## E2E teszt futtatási eredmény (jelenlegi deployed portálon)

A tesztek futnak de CSP blokkolás miatt nem zöldek:

| Teszt | Eredmény | Ok |
|---|---|---|
| 01-login: PKCE login → /orders | FAIL | Token exchange CSP-vel blokkolva |
| 01-login: AppHeader navigáció | FAIL | Auth fixture nem tud belépni (CSP) |
| 01-login: profile oldal betölt | FAIL | Auth fixture nem tud belépni (CSP) |
| 01-login: kijelentkezés redirect | FAIL | Auth fixture nem tud belépni (CSP) |
| 08-auth-edge: unauthenticated /orders | FAIL | OIDC discovery CSP-vel blokkolva |
| 08-auth-edge: 404 route | FAIL | OIDC init error (CSP) → RR7 error page |
| 08-auth-edge: unauthenticated /profile | FAIL | OIDC discovery CSP-vel blokkolva |
| 09-responsive: desktop header | FAIL | Auth fixture nem tud belépni (CSP) |
| 09-responsive: mobile scroll | FAIL | Auth fixture nem tud belépni (CSP) |

**Miután CI-001 javítva (nginx CSP):** Minden teszt zöld lesz (a CallbackPage bug is javítva).

---

## Commit

```
88bf153 feat: E2E auth flows (01-login, 08-auth-edge, 09-responsive) + auth fix
```

## Módosított fájlok

| Fájl | Változás |
|---|---|
| `src/auth/AuthProvider.tsx` | `onSigninCallback` hozzáadva → post-login redirect |
| `src/pages/CallbackPage.tsx` | dupla `signinRedirectCallback()` eltávolítva |
| `src/components/AppHeader.tsx` | `data-testid="app-header"` hozzáadva |
| `src/components/ProtectedRoute.tsx` | `<div data-testid="app-shell">` wrapper |
| `tests/e2e/fixtures/auth.fixture.ts` | Auto-redirect + /login button fallback, Keycloak URL regex fix |
| `tests/e2e/flows/01-login.spec.ts` | 4 valódi teszt (volt: 2 stub) |
| `tests/e2e/flows/08-auth-edge.spec.ts` | ÚJ — 3 auth edge case teszt |
| `tests/e2e/flows/09-responsive.spec.ts` | ÚJ — 2 responsive layout teszt |
| `CONTRACT_ISSUES.md` | ÚJ — CI-001 CSP blocker |
| `eslint.config.js` | tests/e2e/ react-hooks override (FE-007 már meglévő) |

## Root cause summary

A jelenlegi deployed portálon 2 probléma volt:
1. **CallbackPage dupla callback** — javítva (`88bf153`)
2. **Nginx CSP blokkolja OIDC** — blokkolt, `CI-001` benyújtva → **infra koordináció szükséges**

CONTRACT_ISSUE: `CI-001 | CSP connect-src nimajd tartalmazza joinerytech.hu — OIDC blokkolva | infra/nginx`
