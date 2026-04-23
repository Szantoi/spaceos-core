---
id: MSG-ORCH-063-DONE
from: orchestrator
to: root
type: done
priority: high
status: READ
ref: MSG-ORCH-063
created: 2026-04-15
---

## Összefoglaló

A KERNEL-070 által bevezetett 3 Ecosystem Actor endpoint a BFF-en keresztül:

**Megállapítás:** Az `index.ts:75`-ön lévő meglévő catch-all `kernelProxy` (`app.use('/bff/api', proxyLimiter, requireAuth, kernelProxy)`) **már teljes egészében kiszolgálja** az összes `/bff/api/*` útvonalat — új production kód nem szükséges.

- `PUT /bff/api/tenants/:id/modules` → Kernel `PUT /api/tenants/:id/modules` ✅
- `GET /bff/api/module-registry/:tenantType` → Kernel `GET /api/module-registry/:tenantType` ✅
- `GET /bff/api/tenants?tenantType=Manufacturer` → query param átmegy (`http-proxy-middleware` alapból forwardol) ✅

**Új fájl:** `src/proxy/ecosystem-actor.proxy.test.ts` — 7 unit teszt a viselkedés dokumentálásához

Commit: `f7ddb37` — branch: `develop` — **pushed to origin**

## Tesztek

- `npm run build` → 0 TS hiba
- `npm test` → **191/191 teszt zöld** (7 új teszt hozzáadva)

```
Test Files  26 passed (26)
Tests  191 passed (191)
```

Tesztelt esetek:
- PUT `/bff/api/tenants/:id/modules` no auth → 401
- PUT `/bff/api/tenants/:id/modules` with auth → 200 (proxied)
- GET `/bff/api/module-registry/Manufacturer` no auth → 401
- GET `/bff/api/module-registry/Manufacturer` with auth → 200 (proxied)
- GET `/bff/api/module-registry/Retailer` with auth → 200 (proxied)
- GET `/bff/api/tenants?tenantType=Manufacturer` no auth → 401
- GET `/bff/api/tenants?tenantType=Manufacturer` with auth → 200 (proxied)

## Security review

- `requireAuth` az összes `/bff/api/*` route előtt tüzel — érintetlen maradt
- Rate limit (`proxyLimiter`) szintén érintetlen
- Query param forwarding: csak auth'd kérések jutnak el a Kernel-ig

## Kockázatok / kérdések

Nincsenek. Az architecture-nek megfelelő megoldás: a generikus proxy-t nem duplikálni dedikált route-okkal.
