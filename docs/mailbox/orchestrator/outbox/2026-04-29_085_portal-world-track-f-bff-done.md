---
id: MSG-ORCH-085-DONE
from: orchestrator
to: root
type: done
priority: high
status: READ
ref: MSG-ORCH-085
created: 2026-04-29
---

# ORCH-085 — Portal World Track F: BFF Routes DONE

## Összefoglaló

### Day 1 — Contract freeze + dependency

- `opossum` + `@types/opossum` telepítve (v9.0.0 / v8.1.9)
- CONTRACT_ISSUES audit lefuttatva: **0 frontend caller** hívja `/bff/abstractions/*`-t a `spaceos-doorstar-portal`-ban — konzisztens a tervdok BFF-01 döntésével
- Audit eredmény: `CONTRACT_ISSUES.md`

### Day 2-3 — 14 új route (+ 3 meglévő pass-through a kernelProxy-n)

#### Session aggregátor (`me.route.ts`)
```
GET /bff/api/me/session    — KC JWT claims + Kernel tenant fetch + enabledModules
                            degraded fallback ha Kernel nem elérhető (JWT claims only)
GET /bff/api/me/home-state — proxy → Kernel /api/me/home-state
```
`/bff/api/me` a kernelProxy **ELŐTT** mountolva → intercept ok

#### Meglévő proxy (változatlan, kernelProxy-n át)
```
GET /bff/api/tenant  → Kernel (pass-through)
GET /bff/api/audit   → Kernel (pass-through)
GET /bff/api/users   → Kernel (pass-through)
```

#### Manufacturing proxy (`manufacturing.route.ts`) — opossum circuit breaker
```
GET  /bff/manufacturing/orders
GET  /bff/manufacturing/orders/:id
POST /bff/manufacturing/edge-banding/*
POST /bff/manufacturing/cnc/*
GET  /bff/manufacturing/tasks/:id/full
```
Circuit breaker config: `timeout=10s, errorThreshold=50%, resetTimeout=30s, volumeThreshold=5`
→ Circuit OPEN → `503 { error: { code: 'SERVICE_UNAVAILABLE', service: 'manufacturing' } }`

#### Shop Floor (`shopfloor.route.ts`)
```
POST /bff/shopfloor/pin/login   — Zod validate → Kernel forward, dev fallback ha Kernel down
POST /bff/shopfloor/pin/logout  — Kernel forward, graceful ha Kernel down
GET  /bff/shopfloor/tasks       — Manufacturing proxy (circuit breaker) + requireAuth
GET  /bff/shopfloor/task/:id/status — Manufacturing proxy (circuit breaker) + requireAuth
```

### Day 4 — Health bővítés

```
GET /bff/health — Manufacturing (5007) ping hozzáadva
  { orchestrator: 'ok', kernel: 'ok'|'unreachable', manufacturing: 'ok'|'unreachable', ... }
  HTTP 200 ha mind OK, HTTP 207 ha bármelyik unreachable
```

### Env bővítés
- `MANUFACTURING_BASE_URL=http://127.0.0.1:5007` — `env.ts` + `.env.example`

### Middleware chain
- Error normalizer: meglévő `errorHandler` middleware — minden route `next(err)` → egységes `{ error: string }` shape
- Circuit breaker: opossum BFF-11 — Manufacturing route-okon
- Rate limit: `proxyLimiter` a `/bff/manufacturing/*` és `/bff/shopfloor/*` mountokon

## Tesztek

- `npm run build` → **0 TS error** ✅
- `npm test` → **254/254 pass** (227 → 254, +27 új) ✅

Új tesztek:
| File | Tesztek |
|---|---|
| `me.route.test.ts` | 6 (session happy, degraded, 401; home-state happy, 404, 401) |
| `manufacturing.route.test.ts` | 8 (orders, orders/:id happy+404, edge-banding, cnc, tasks/full, circuit breaker 503, 401) |
| `shopfloor.route.test.ts` | 11 (pin login happy+fallback+422+pin-short, logout happy+fallback, tasks happy+401+503, task status happy+401) |
| `health.route.test.ts` | +2 (manufacturing ok, manufacturing unreachable 207) |

## Security review

- [x] `requireAuth` — `/bff/api/me/*`, `/bff/manufacturing/*`, shopfloor task route-okon
- [x] PIN login — Zod validáció (machineId min 1, pin 4-8 char)
- [x] Circuit breaker — Manufacturing timeout/failure → 503 immediate (nem 10s timeout)
- [x] JWT forwarding — user token forwardolva upstream-re (nem újragenerálva)
- [x] Nincs secret a logban
- [x] MANUFACTURING_BASE_URL env-ből (nem hardcoded)
- [x] `X-SpaceOS-Brand` header forwarding — manufacturing route-okon

## Kockázatok / kérdések

1. **Shop Floor machine token (v2):** A `/bff/shopfloor/tasks` és `/task/:id/status` jelenleg KC JWT-vel védett (`requireAuth`). A tervdok BFF-managed machine token-t ír (SEC-FE-10). Ez v2 feature — PIN login → BFF által kiállított opaque token. v1-ben KC JWT is elfogadható.
2. **enabledModules forrás:** `/me/session` a Kernel tenant endpoint-tól veszi az `enabledModules` listát. Ha a Kernel sémájában ez más mezőnéven szerepel, a mapping frissítendő.
3. **Manufacturing OpenAPI snapshot:** a tervdok BFF-02 szerint csak akkor commitolható, ha a Manufacturing service production candidate. Jelen commit-ban nincs snapshot — ez helyes.

## Commit

`e959c3d` — pushed to `origin/develop`

## DoD ellenőrzés

- [x] opossum installálva (v9.0.0)
- [x] CONTRACT_ISSUES audit (`/bff/abstractions/*`) → 0 caller
- [x] 14 új BFF route (+ 3 meglévő kernelProxy pass-through = 17 összesen)
- [x] Circuit breaker (opossum) — Manufacturing + ShopFloor task route-okon
- [x] `/bff/health` Manufacturing ping
- [x] `npm run build` 0 error
- [x] `npm test` 254/254 pass (≥ 247 ✅)
- [x] Outbox DONE
