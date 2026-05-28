---
id: MSG-PROCUREMENT-013
from: root
to: procurement
type: task
priority: high
status: UNREAD
ref: MSG-PROCUREMENT-012
created: 2026-05-28
---

# Track D + E unblocked — Inventory-059 DONE

Az Inventory terminál teljesítette a Procurement v2 blokkolókat (MSG-INVENTORY-059-DONE).
**Track D és Track E most elindítható.**

## Ami kész az Inventory oldalon

### `POST /inventory/internal/inbound` (Track E célpontja)
- URL: `http://127.0.0.1:5004/inventory/internal/inbound`
- Auth: `Authorization: Bearer {SPACEOS_INTERNAL_SECRET}` + `X-SpaceOS-TenantId`
- Idempotency: `(TenantId, DeliveryLineId)` compound key — duplikátum → 200 `reason: duplicate`
- Permanens: 422 (orphan materialCode) → `Failed` azonnal
- Tranziens: 503 → retry

### `POST /procurement/internal/from-reorder-alert` (Track E — te implementálod)
A Procurement receiver, amit az Inventory worker hív. Az Inventory worker:
- URL: `http://127.0.0.1:5006/procurement/internal/from-reorder-alert`
- Header: `Authorization: Bearer {SPACEOS_INTERNAL_SECRET}` + `X-SpaceOS-TenantId`
- Payload: `{ tenantId, materialCode, currentStock, reorderPoint, suggestedQuantity, preferredSupplierId, unitOfMeasure, alertedAt }`

## Root döntés — Polly csomag

Az Inventory manual retry+circuit-breaker implementációt adott be (Polly nem szerepelt az engedélyezett listán). A manual megoldás ekvivalens viselkedésű — **elfogadva**. A Procurement terminálra ugyanez vonatkozik: ha a spec Polly-t ír, de az approved packages listán nincs rajta, manual implementáció is elfogadható. Ha mégis hozzá szeretnéd adni, jelezd BLOCKED üzenetben — root dönt.

## Amit most csinálj

1. **Track D** — `procurement_outbox` worker: Polly helyett manual retry+circuit-breaker is OK
2. **Track E** — `from-reorder-alert` receiver implementáció

A többi track (A, B, C, F, G, H) párhuzamosan haladhat ezekkel.

Ha a Track D/E implementáció közben bármilyen kérdés a receiver kontrakt kapcsán: olvasd el az Inventory commit `b5453c3`-at referenciának.
