---
id: MSG-O036-RESP
from: orchestrator
to: architect
type: response
ref: MSG-O036
status: DONE
date: 2026-04-09
sprint: "Doorstar Pilot Onboarding"
---

# Doorstar Onboarding Response — Orchestrator Mediation Route (ADR-010)

## T1 — env.ts bővítés

```typescript
JOINERY_BASE_URL: z.string().url().default('http://127.0.0.1:5002'),
```

## T2 — `src/routes/joinery.route.ts` (pass-through proxy)

| Method | BFF Path | Joinery Path |
|--------|----------|-------------|
| GET | `/bff/joinery/orders` | `/api/orders` |
| GET | `/bff/joinery/orders/:id` | `/api/orders/:id` |
| GET | `/bff/joinery/orders/:id/cutting-list` | `/api/orders/:id/cutting-list` |
| GET | `/bff/joinery/orders/:id/process-plan` | `/api/orders/:id/process-plan` |
| GET | `/bff/joinery/orders/:id/hardware-list` | `/api/orders/:id/hardware-list` |
| POST | `/bff/joinery/orders/:id/items` | `/api/orders/:id/items` |
| POST | `/bff/joinery/orders/:id/submit` | `/api/orders/:id/submit` |

Minden route: `requireAuth`, UUID validáció `:id`-n, JWT forward, `timeout: 10_000`.

## T3 — `src/routes/doorOrder.route.ts` (ADR-010 saga)

`POST /bff/door-orders`:
1. ① POST Kernel `/api/flow-epics` → `{ flowEpicId }`
2. ② POST Joinery `/api/orders` `{ flowEpicId, ...body }` → `{ orderId }`
3. Ha ② fail → `archiveEpicWithRetry()` (3× exponential backoff: 100ms/500ms/2000ms)
4. Ha archive is fail → `[CRITICAL]` log (`OrphanEpicCleanupJob` feldolgozza)

Zod validáció: `projectId`, `projectName`, `clientName`, `items` (min 1 elem).

## T4 — `src/index.ts` regisztráció

```typescript
app.use('/bff/door-orders', proxyLimiter, doorOrderRouter);
app.use('/bff/joinery',     proxyLimiter, joineryRouter);
```

## T5 — Tesztek (`src/routes/doorOrder.route.test.ts`)

- `Kernel ✅ + Joinery ✅ → 201 { orderId, flowEpicId }` ✅
- `Joinery fail → archive epic called (3× retry), returns 502` ✅
- `missing required field → 422` ✅
- `no Authorization → 401` ✅

## SEC-01 (saga compensation) ✅

`archiveEpicWithRetry()` 3× retry, exponential backoff — `[CRITICAL]` log orphan epic esetén.

## SEC-04 (timeout) ✅

Minden axios hívás: `timeout: 10_000`.

## Build + teszt

```
npm run build → 0 TypeScript error
npm test      → 168 passed (21 test files)
```
