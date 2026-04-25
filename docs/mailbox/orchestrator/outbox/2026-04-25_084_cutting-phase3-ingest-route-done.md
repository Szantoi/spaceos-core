---
id: MSG-ORCH-084-DONE
from: orchestrator
to: root
type: done
priority: high
status: READ
ref: MSG-ORCH-084
created: 2026-04-25
---

# ORCH-084 — Cutting Phase 3: Order Ingest BFF route DONE

## Összefoglaló

Új orchestrációs route a `cutting.route.ts`-ben:

```
POST /bff/cutting/planning/:planId/add-order/:orderId
```

### Flow

1. **Joinery fetch**: `GET ${JOINERY_BASE_URL}/api/orders/${orderId}` — user JWT forwarding
2. **Transform**: JoineryDoorItem → CuttingIngestItem mapping
   - `doorWidth/wallOpeningWidth/width` → `widthMm`
   - `doorHeight/wallOpeningHeight/height` → `heightMm`
   - `doorThickness/wallOpeningThickness/thickness` → `thicknessMm`
   - `material/doorType` → `material`
   - `grainDirection` → `grainDirection` (default: `'none'`)
   - `sorszam/name` → `label`
3. **Cutting ingest**: `POST ${CUTTING_BASE_URL}/internal/ingest-order`
   - Body: `{ orderId, planId, tenantId, items }`
   - Header: `X-SpaceOS-Internal: true`
4. **Response**: `{ jobsCreated: N }`

### Implementáció részletek

- `tenantId` a JWT `tid` claimből (requireAuth által kiolvasva)
- UUID validáció `planId` és `orderId` paraméterekre
- Joinery 404 → BFF 404 (order not found)
- Upstream hiba → BFF forward (axios error handler)
- Route a catch-all proxy ELŐTT regisztrálva (specifikus route elsőbbség)

## Tesztek

- `npm run build` → 0 TS error ✅
- `npm test` → **227/227 zöld** (224 + 3 új) ✅

Új tesztek (`cutting.route.test.ts`):
1. Happy path: Joinery fetch → transform → Cutting ingest → 200
2. Joinery order not found → 404
3. Missing JWT → 401

## Security review

- [x] `requireAuth` — index.ts mount szintjén alkalmazva
- [x] JWT `tid` claim ellenőrzés — tenantId kiolvasás
- [x] UUID validáció — planId és orderId
- [x] `X-SpaceOS-Internal: true` — Cutting internal endpoint felé
- [x] Joinery JWT forwarding — user saját tokenje
- [x] Timeout — 10s mindkét hívásra
- [x] Nincs secret a logban

## Commit

`900063f` — pushed to `origin/develop`

## Kockázatok / kérdések

Nincsenek.

## DoD

- [x] `POST /bff/cutting/planning/:planId/add-order/:orderId` route
- [x] Joinery fetch → transform → Cutting ingest flow
- [x] `npm run build` 0 error
- [x] `npm test` ≥ 227 pass
- [x] Commit + push
