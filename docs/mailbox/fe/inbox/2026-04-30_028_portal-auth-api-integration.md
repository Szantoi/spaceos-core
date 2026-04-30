---
id: MSG-FE-028
from: root
to: fe
type: task
priority: high
status: READ
ref: MSG-FE-027
created: 2026-04-30
---

# FE-028 — Keycloak Auth + API integráció

> **Cél:** A portál bejelentkezzen Keycloak OIDC-vel, és az API hívások valódi backend adatokat jelenítsenek meg.

## Kontextus

A scaffold kész (FE-027, 179 teszt). Most az auth és API réteg következik.

**Skill:** `/spaceos-terminal` szerint dolgozz, `/senior-frontend` mintákat kövesd
**Használhatsz sub-agent-eket** ha szükséges

## 1. Keycloak OIDC integráció

### Paraméterek
```
Authority:    https://joinerytech.hu/auth/realms/spaceos
Client ID:    portal-app
Grant type:   Authorization Code + PKCE
Redirect URI: https://joinerytech.hu/callback
Post-logout:  https://joinerytech.hu/
```

### Ajánlott csomag
`oidc-client-ts` — PKCE flow, token refresh, silent renew

### useAuth hook kitöltése
- `login()` — redirect to Keycloak
- `logout()` — redirect to Keycloak end_session
- `user` — decoded JWT claims (sub, preferred_username, tenant_id, roles)
- `token` — access_token az API hívásokhoz
- `isAuthenticated` — boolean

### FONTOS biztonsági szabályok
- **InMemoryWebStorage** a stateStore-hoz (NEM sessionStorage — XSS védelem)
- A userStore használhat sessionStorage-t (túléli a redirectet)
- Nincs `console.log(token)` — soha
- Token csak a `Authorization: Bearer` header-ben utazik

### Route védelem
- `/` — anonymous (landing, bárki nézheti)
- `/w/*` — auth required → ha nincs bejelentkezve, redirect to login
- `/callback` — OIDC callback handler

## 2. API integráció

### API base URL-ek (nginx proxy-n keresztül)
```
Kernel:       /api/
Joinery:      /joinery/
Inventory:    /inventory/
Cutting:      /cutting/
Procurement:  /procurement/
Abstractions: /abstractions/
AI/Chat:      /ai/
Auth:         /auth/
```

### useApi hook kitöltése
```typescript
// Minden API híváshoz:
const headers = {
  'Authorization': `Bearer ${token}`,
  'Content-Type': 'application/json'
};
```

### Tesztelhető endpoint-ok (a test-admin user-rel működnek)
```
GET /api/tenants/{tenantId}              → Doorstar Kft. adatok
GET /api/facilities/{facilityId}         → Doorstar Üzem
GET /api/inventory/stock                 → MDF 18mm, 20 panel (port 5004)
```

### Fallback stratégia
```typescript
// Ha az API nem ad vissza adatot, a mock data marad:
const displayData = apiData ?? DEMO_DATA;
```

Tehát a meglévő mock data nem törlődik — az API adat felülírja ha van.

## 3. Tesztek

- Auth mock-olás teszteknél (ne hívjon valódi Keycloak-ot)
- API hook tesztelése mock fetch-csel
- Meglévő 179 teszt zöld marad

## Definition of Done

- [ ] Keycloak login/logout működik (PKCE flow)
- [ ] Token megjelenik az API hívásokban
- [ ] Legalább 1 oldal valódi API adatot jelenít meg (pl. tenant info)
- [ ] `/w/*` route-ok auth-protected
- [ ] `/` route anonymous marad
- [ ] `pnpm build` → 0 error
- [ ] `pnpm test` → ≥ 179 pass (meglévők + újak)
- [ ] Outbox DONE — commit hash, teszt összesítő, security review
