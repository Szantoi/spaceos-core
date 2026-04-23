---
id: MSG-PORTAL-005
from: root
to: portal
type: task
priority: high
status: READ
ref: —
created: 2026-04-15
---

# MSG-PORTAL-005 — Security Review (Q2 Pre-launch)

## Feladat

Futtass teljes biztonsági önellenőrzést a Design Portal (React/Vite) kódbázison.
Ez egy **review feladat** — kódot csak ha kritikus sérülékenységet találsz.

## Ellenőrzési területek

### 1. Authentication (PKCE OAuth flow)
- [ ] `state` és `nonce` paraméterek validálva vannak a callback-ben?
- [ ] `code_verifier` / `code_challenge` PKCE flow helyes (S256)?
- [ ] Token csak memóriában tárolódik (nem `localStorage` / `sessionStorage` / `cookie`)?
- [ ] Refresh token race condition kezelve? (`_refreshInFlight` dedup — R-17 fix)
- [ ] Logout: token memory-ból törölve, Keycloak session invalidálva?

### 2. XSS
- [ ] Nincs `dangerouslySetInnerHTML` felhasználói adattal?
- [ ] URL paraméterek (pl. `?error=...`) közvetlenül DOM-ba kerülnek-e?
- [ ] `react-router` redirect: csak relatív URL-ekre?

### 3. Route Protection
- [ ] Minden védett oldal `ProtectedRoute` mögött van?
- [ ] Unauthenticated user automatikusan az auth flow-ba kerül?
- [ ] Az auth callback oldal (`/callback`) nem érhető el bejelentkezett user számára?

### 4. Token Handling
- [ ] Access token csak HTTPS kérésekhez használva?
- [ ] Token expiry ellenőrzés van-e kliens oldalon?
- [ ] Az Authorization header csak az ismert BFF origin-re küldődik (nem third-party)?

### 5. Dependency Security
- [ ] `npm audit` futtatva — kritikus CVE-k?
- [ ] Főbb függőségek (React, Vite, Zustand, Vitest) frissek-e?

### 6. Build / Info Leak
- [ ] A production build tartalmaz-e API URL-eket, belső path-okat a bundle-ben?
- [ ] `console.log` hívások maradtak-e production build-ben?
- [ ] Source map ki van-e kapcsolva production-ban?

### 7. OWASP Top 10 (frontend scope)
- [ ] A3 — Injection: user input nem kerül `eval()`, `Function()`, `innerHTML`-be?
- [ ] A5 — Security Misconfiguration: dev/mock módok kikapcsolvák production-ban?
- [ ] A7 — XSS: React JSX escaping mindenhol érvényes?
- [ ] A1 — Broken Access Control: API hívások mindig Bearer token-nel?

## DoD

- [ ] Minden terület ellenőrizve
- [ ] Talált problémák: kritikus / közepes / alacsony szinten kategorizálva
- [ ] Ha kritikus találat: `status: BLOCKED` → azonnali jelzés
