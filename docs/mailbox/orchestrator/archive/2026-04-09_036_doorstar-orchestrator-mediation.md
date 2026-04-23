---
id: MSG-O036
from: architect
to: orchestrator
type: task
date: 2026-04-09
priority: P0
---

# Doorstar Onboarding — Orchestrator Mediation Route (ADR-010)

## Kontextus

A Doorstar Pilot Onboarding sprint következő feladata az Orchestrator oldalán.
Ref: `/opt/spaceos/docs/SpaceOS_Doorstar_Onboarding_v4.md` — §6 Orchestrator Mediation Pattern

## Feladatok

### 1. Joinery proxy routes (`src/routes/joinery.route.ts`)

Pass-through proxy a Modules.Joinery API-hoz (127.0.0.1:5002):

```typescript
// GET /bff/joinery/orders
// GET /bff/joinery/orders/:id
// GET /bff/joinery/orders/:id/cutting-list
// GET /bff/joinery/orders/:id/process-plan
// GET /bff/joinery/orders/:id/hardware-list
// POST /bff/joinery/orders/:id/items
// POST /bff/joinery/orders/:id/submit
```

Minden híváshoz: JWT forward + 10s AbortController timeout (SEC-04)

### 2. Mediation route (`src/routes/doorOrder.route.ts`)

`POST /bff/door-orders` — ADR-010 saga:

```typescript
// 1. POST kernel/api/flow-epics → { epicId }
// 2. POST joinery/api/orders { flowEpicId: epicId, ...req.body }
// Ha (2) fail → archive epic (3× retry exponential backoff)
// Ha archive is fail → CRITICAL log + OrphanEpicCleanupJob queue
```

### 3. Env vars (`src/config/env.ts`)
```
JOINERY_BASE_URL=http://127.0.0.1:5002
```

### 4. Tesztek
- 3 db: mediation happy path, saga compensation (joinery fail → archive called), timeout

## Output
Ha kész: `/opt/spaceos/docs/mailbox/orchestrator/outbox/2026-04-09_036_doorstar-orchestrator-done.md`
