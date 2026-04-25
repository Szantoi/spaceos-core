---
id: MSG-ORCH-084
from: root
to: orchestrator
type: task
priority: high
status: READ
ref: SpaceOS_Cutting_Phase3_Implementation_v1.md
created: 2026-04-25
---

# ORCH-084 — Cutting Phase 3: Order Ingest BFF route

> **Tervdok:** `docs/architecture/SpaceOS_Cutting_Phase3_Implementation_v1.md`
> **Előfeltétel:** CUTTING-043 ✅ (POST /internal/ingest-order LIVE) + CUTTING-044 ✅ (nesting publish)
> **Skill:** `/spaceos-terminal` szerint dolgozz

## Feladat

A Doorstar Portal-ról a Joinery rendelés adatait el kell juttatni a Cutting modulba. Az Orchestrator BFF közvetít:

```
POST /bff/cutting/planning/{planId}/add-order/{orderId}

Flow:
1. BFF: GET /bff/joinery/orders/{orderId} → Joinery order items (JWT forwarded)
2. Transform: Joinery DoorItem → Cutting IngestOrderItem (widthMm, heightMm, material, grainDirection)
3. BFF: POST http://127.0.0.1:5005/internal/ingest-order (X-SpaceOS-Internal: true)
   Body: { orderId, tenantId (from JWT), items: [...] }
4. Response: { jobsCreated: N }
```

### Megjegyzések

- A `tenantId` a JWT-ből jön (a BFF kiolvashatja)
- A Joinery order items response formátumát ellenőrizd: `GET /api/orders/{id}` → items[].width, height, material
- A transform mapping lehet hogy egyszerű 1:1, de a mezőnevek eltérhetnek
- `X-SpaceOS-Internal: true` header a Cutting API felé KÖTELEZŐ

## Tesztek (+3)

1. Happy path: BFF hívás → Joinery fetch → transform → Cutting ingest → 200
2. Joinery order not found → 404
3. Missing JWT → 401

## Definition of Done

- [ ] `POST /bff/cutting/planning/{planId}/add-order/{orderId}` route
- [ ] Joinery fetch → transform → Cutting ingest flow
- [ ] `npm run build` 0 error
- [ ] `npm run test` ≥ 227 pass
- [ ] Outbox DONE
