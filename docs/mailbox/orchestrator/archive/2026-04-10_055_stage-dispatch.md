---
id: MSG-ORCHESTRATOR-055
from: root
to: orchestrator
type: task
priority: medium
status: READ
blocked_by: ""
created: 2026-04-10
---

# MSG-ORCHESTRATOR-055 — Stage Dispatch Route (BE-03)

> ✅ **Blokkoló feloldva (2026-04-10):** MSG-KERNEL-054 implementáció elfogadva — `GET /api/stages` endpoint a Kernel-ben elérhető. (Tesztek pótlás folyamatban MSG-KERNEL-057 alatt, de az API felület stabil.)

## Feladat

Implementáld a stage dispatch route-ot az Orchestratorban — ez az a middleware réteg, ami a BFF kéréseket az adott Stage module-hoz irányítja.

**Architektúra dokument:** `/opt/spaceos/docs/SpaceOS_WorkflowStage_Architecture_v4.md` — Section 7

---

## Scope

### Fájl: `src/routes/stageDispatch.ts`

```typescript
// spaceos-orchestrator / src/routes/stageDispatch.ts
const endpointCache = new Map<string, { url: string; expires: number }>();
const CACHE_TTL_MS = 5 * 60 * 1000; // 5 perc — BE-03: no invalidation, TTL only

async function resolveStageEndpoint(stageCode: string, tenantId: string): Promise<string> {
  const key = `${tenantId}:${stageCode}`;
  const cached = endpointCache.get(key);
  if (cached && cached.expires > Date.now()) return cached.url;

  const resp = await kernelClient.get(`/api/stages?stageCode=${stageCode}`);
  const endpoint = resp.data.items[0]?.moduleEndpoint;
  if (!endpoint) throw new Error(`Stage '${stageCode}' not found`);

  endpointCache.set(key, { url: endpoint, expires: Date.now() + CACHE_TTL_MS });
  return endpoint;
}

// Route: /bff/stages/:stageCode/*
router.all('/:stageCode/*', async (req, res) => {
  const endpoint = await resolveStageEndpoint(req.params.stageCode, req.tenantId);
  const targetPath = req.params[0]; // everything after stageCode
  proxy(req, res, `${endpoint}/${targetPath}`);
});
```

### Részletek

- **Cache:** TTL 5 perc, no invalidation (BE-03 — webhook nem létezik, TTL-only elfogadott)
- **Cache kulcs:** `${tenantId}:${stageCode}` — tenant-enként izolált
- **Route:** `/bff/stages/:stageCode/*` → proxy az adott module loopback URL-re
- **Kernel API hívás:** `GET /api/stages?stageCode={code}` — az új Stage Registry endpoint
- **Proxy:** meglévő proxy utility, `X-SpaceOS-Brand` + `Authorization` header forward

### Nginx config kiegészítés

```nginx
location /bff/stages/ {
    proxy_pass http://127.0.0.1:3000;
    proxy_set_header Authorization $http_authorization;
    proxy_set_header X-SpaceOS-Brand $spaceos_brand;
}
```

### Tesztek (≥6 db)

- Cache miss → Kernel API hívás → proxy
- Cache hit → Kernel API NEM hívódik
- TTL lejárat → friss Kernel API hívás
- Ismeretlen stageCode → 404
- Tenant A stageCode → nem látja Tenant B cache-ét
- `moduleEndpoint` null/undefined → hibakezelés, 502

---

## Definition of Done

- [ ] `stageDispatch.ts` létrehozva, router regisztrálva `app.ts`-ben
- [ ] Cache TTL 5 perc, kulcs `tenantId:stageCode`
- [ ] `GET /api/stages?stageCode=` Kernel API hívás
- [ ] Nginx `/bff/stages/` location hozzáadva (ha nincs)
- [ ] ≥6 új teszt
- [ ] Meglévő 163 teszt zöld
- [ ] 0 TS error

---

## Visszajelzés

Outboxba: `MSG-ORCHESTRATOR-055-DONE` státusszal, tesztszámmal.
