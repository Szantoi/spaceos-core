---
id: MSG-ORCH-063
from: root
to: orchestrator
type: task
priority: high
status: READ
ref: MSG-KERNEL-070-DONE
created: 2026-04-15
---

# MSG-ORCH-063 — BFF proxy routes: Ecosystem Actor endpoints

## Háttér

KERNEL-070 DONE: Migration 0029 éles, 3 új Kernel endpoint jött létre:
- `PUT /api/tenants/{id}/modules` — tenant modul frissítés
- `GET /api/tenants?tenantType=` — TenantType filter (meglévő endpoint bővítve)
- `GET /api/module-registry/{tenantType}` — engedélyezett + kötelező modulok lekérdezése

## Feladat

### Új BFF routes

**1. Tenant modules update proxy**
```ts
// PUT /bff/api/tenants/:id/modules → Kernel PUT /api/tenants/:id/modules
router.put('/api/tenants/:id/modules', requireAuth, proxy(KERNEL_BASE_URL));
```

**2. Module registry proxy**
```ts
// GET /bff/api/module-registry/:tenantType → Kernel GET /api/module-registry/:tenantType
router.get('/api/module-registry/:tenantType', requireAuth, proxy(KERNEL_BASE_URL));
```

**3. tenantType query param átengedése** (meglévő GET /bff/api/tenants route-on)
- Ellenőrizd hogy a `?tenantType=Manufacturer` query param átmegy-e a Kernel-nek.
- Ha nem: a proxy konfiguráció query string forwarding-ját ellenőrizd.

### Tesztek

```bash
npm test  # 184/184 zöld kell maradjon + új route tesztek
```

## DoD

- [ ] PUT /bff/api/tenants/:id/modules proxy route
- [ ] GET /bff/api/module-registry/:tenantType proxy route
- [ ] tenantType query param forwarding verifikálva
- [ ] ≥184 teszt pass (új route tesztek is)
- [ ] Commit + push develop
- [ ] DONE outbox: commit hash + teszt count
