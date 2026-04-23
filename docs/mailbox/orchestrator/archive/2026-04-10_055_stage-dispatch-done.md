---
id: MSG-ORCHESTRATOR-055-DONE
from: orchestrator
to: root
type: response
status: DONE
ref: MSG-ORCHESTRATOR-055
created: 2026-04-10
---

# MSG-ORCHESTRATOR-055 — Stage Dispatch Route: DONE

## Összefoglaló

Stage dispatch route implementálva, TTL cache-sel, tenant izolációval és hibakezeléssel.

### Változott fájlok

| Fájl | Változás |
|---|---|
| `src/routes/stageDispatch.route.ts` | ÚJ — stage dispatch router, TTL cache, StageError |
| `src/routes/stageDispatch.route.test.ts` | ÚJ — 7 teszt |
| `src/index.ts` | Import + mount: `/bff/stages` proxyLimiter + stageDispatchRouter |
| `docs/nginx.conf` | ÚJ location: `/bff/stages/` explicit Authorization + X-SpaceOS-Brand forward |

### Architektúra

```
GET/POST/... /bff/stages/:stageCode/<path>
  ↓ proxyLimiter
  ↓ requireAuth (JWT)
  ↓ resolveStageEndpoint():
      cache hit (TTL 5 min) → return cached URL
      cache miss → kernelClient.get('/api/stages', { stageCode })
                 → items[0].moduleEndpoint → cache → return
  ↓ axios({ method, url: endpoint/path, Authorization, X-SpaceOS-Brand })
  → 200 / error forwarding
```

**Cache key:** `${tenantId}:${stageCode}` — tenant-enként izolált  
**TTL:** 5 perc, no invalidation (BE-03)  
**Stage not found (items empty):** 404  
**moduleEndpoint null:** 502  

## Tesztek

- **176 passed / 0 failed** (169 baseline + 7 új)
- `src/routes/stageDispatch.route.test.ts`:
  1. Cache miss → Kernel API hívás → proxy 200
  2. Cache hit → Kernel API NEM hívódik (2. kérés)
  3. TTL lejárat → friss Kernel API hívás (`vi.useFakeTimers()`)
  4. Ismeretlen stageCode → 404
  5. moduleEndpoint null → 502
  6. Tenant A vs Tenant B cache izoláció (külön cache key)
  7. Modul API hiba → upstream státusz forward (422)

## Security review

- **Auth:** `requireAuth` minden stage route-on (JWT RS256)
- **Rate limiting:** `proxyLimiter` alkalmazva
- **Header forward:** Authorization + X-SpaceOS-Brand — más headerek nem forwarded
- **Tenant izoláció:** cache key tartalmaz tenantId-t → cross-tenant cache contamination kizárva
- **Error leakage:** StageError üzenet user-facing, de nem tartalmaz belső detail-t
- **OWASP:** nincs path traversal (targetPath a params-ból, nem user inputból raw)

## Megjegyzés

- Fájl neve `stageDispatch.route.ts` (a codebase `*.route.ts` konvencióját követi)
- A DoD-ban szereplő `stageDispatch.ts` vs `.route.ts` eltérés a project konvencióból fakad
- Nginx `/bff/stages/` location hozzáadva — a meglévő `/bff/` már lefedi, de explicit konfig a header forward miatt szükséges
