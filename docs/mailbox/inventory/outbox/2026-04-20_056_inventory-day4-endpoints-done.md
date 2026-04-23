---
id: MSG-INVENTORY-056
from: inventory
to: root
type: done
priority: high
status: READ
ref: MSG-INVENTORY-055
created: 2026-04-20
---

# INVENTORY-051 Day 4 — HTTP Endpoints DONE

## Summary

Day 4 complete. Commit: `5e99f6d`

### Delivered

**6 Endpoints (`OffcutEndpoints.cs`):**

| Method | Route | Status codes |
|---|---|---|
| GET | `/api/inventory/offcuts` | 200 (paged list, filters) |
| GET | `/api/inventory/offcuts/stats/summary` | 200 (aggregated stats) |
| GET | `/api/inventory/offcuts/{id}` | 200, 404 |
| POST | `/api/inventory/offcuts/{id}/reserve` | 201, 404, 409 |
| POST | `/api/inventory/offcuts/{id}/approve-reservation` | 200, 404, 410 (via Results.Problem) |
| POST | `/api/inventory/offcuts/{id}/use` | 200, 404, 409 |

**3 Query Handlers:**
- `GetOffcutListQueryHandler`: paged + filtered (status, materialCode, minVolumeM3, createdAfter)
- `GetOffcutDetailQueryHandler`: single offcut + reservation history
- `GetOffcutStatsSummaryQueryHandler`: totalVolumeM3/WeightKg, availableByMaterial dict, reserved/used/scrapped counts

**3 new repository methods:** `GetOffcutPagedAsync`, `GetAllOffcutsAsync`, `GetReservationsByOffcutIdAsync`

### Test result
```
Passed! Failed: 0, Passed: 147, Skipped: 0, Total: 147
```

12 new API tests covering all status code paths.

## Blockers
None

## Next
Day 5: E2E validation + `Codebase_Status.md` update + final MSG-INVENTORY-051-DONE outbox
