---
id: MSG-INVENTORY-055
from: root
to: inventory
type: task
priority: high
status: READ
ref: MSG-INVENTORY-055-DAY3-DONE
created: 2026-04-20
---

# INVENTORY-055 — Day 4: HTTP Endpoints (6 routes)

## Day 3 Accepted ✅

- ReserveOffcutCommand ✅
- ApproveOffcutReservationCommand ✅
- UseOffcutInJobCommand ✅
- Full lifecycle test (Reserve→Approve→Use) ✅
- 135/135 tests ✅

---

## Day 4 Task: HTTP Endpoints

**File:** `Api/Endpoints/OffcutEndpoints.cs`

Regisztráld a meglévő `MapInventoryEndpoints()` pattern alapján.

---

### Routes

```
GET    /api/inventory/offcuts
       Query: status?, materialCode?, minVolumeM3?, createdAfter?, page=1, pageSize=20
       Auth: ManufacturerOnly
       Response: 200 + { offcuts[], total, page, pageSize }

GET    /api/inventory/offcuts/{offcutId}
       Auth: ManufacturerOnly
       Response: 200 + { offcut, reservationHistory[] }
       Error: 404

POST   /api/inventory/offcuts/{offcutId}/reserve
       Body: { jobId: uuid }
       Auth: ManufacturerOnly
       Response: 201 + { reservationId, expiresAt }
       Errors: 404, 409

POST   /api/inventory/offcuts/{offcutId}/approve-reservation
       Body: { reservationId: uuid }
       Auth: ManufacturerOnly
       Response: 200 + { status: "Approved" }
       Errors: 404, 410 (expired)

POST   /api/inventory/offcuts/{offcutId}/use
       Body: { jobId: uuid }
       Auth: ManufacturerOnly
       Response: 200 + { status: "Used", usedInJobId, usedAt }
       Errors: 404, 409

GET    /api/inventory/offcuts/stats/summary
       Auth: ManufacturerOnly
       Response: 200 + {
         totalAvailableVolumeM3,
         totalAvailableWeightKg,
         availableByMaterial: { "18mm_MDF": { volumeM3, weightKg } },
         reservedCount,
         usedCount,
         scrappedCount
       }
```

**410 expired:** `Results.Problem(statusCode: 410, detail: "Reservation has expired")`

---

### Queries szükségesek (Day 4-en implementáld)

**GetOffcutListQuery** + Handler: paginated list, filterek
**GetOffcutDetailQuery** + Handler: single offcut + reservation history
**GetOffcutStatsSummaryQuery** + Handler: aggregated stats

---

### Tests (10+)

- GET list: 200, filter by status, filter by material
- GET detail: 200, 404
- POST reserve: 201, 404, 409
- POST approve: 200, 404, 410
- POST use: 200, 404, 409
- GET stats: 200 + correct aggregation

---

## Acceptance

- ✅ 6 endpoint implementálva
- ✅ 3 query handler implementálva
- ✅ 10+ API teszt
- ✅ Total tests ≥ 145
- ✅ Build green

---

## Next

Day 4 DONE → outbox: `2026-04-20_056_inventory-day4-endpoints-done.md`

Then Day 5: E2E validation + final MSG-INVENTORY-051-DONE.
