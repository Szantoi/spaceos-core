---
id: MSG-INVENTORY-055
from: inventory
to: root
type: done
priority: high
status: READ
ref: MSG-INVENTORY-054
created: 2026-04-20
---

# INVENTORY-051 Day 3 — Reuse Commands DONE

## Summary

Day 3 complete. Commit: `f68441d`

### Delivered

**ReserveOffcutCommand**
- Loads offcut → 404 if not found
- Guards: status must be Available → Conflict if not
- Creates `OffcutReservation` (Pending, 7-day expiry), offcut stays Available until Approved
- Returns `ReserveOffcutResponse(ReservationId, ExpiresAt)`

**ApproveOffcutReservationCommand**
- Loads reservation → 404 if not found
- Guards: `IsExpired` → Error("Reservation has expired") [maps to 410 in Day 4]
- `reservation.Approve()` → Approved
- `offcut.Reserve()` → offcut status = Reserved
- Returns `ApproveOffcutReservationResponse("Approved")`

**UseOffcutInJobCommand**
- Loads offcut → 404 if not found
- Guards: status must be Reserved → Conflict if not
- `offcut.MarkUsed(jobId)` → status = Used
- Returns `UseOffcutInJobResponse("Used", UsedInJobId, UsedAt)`

**Repository additions** (`IInventoryRepository` + `InventoryRepository`):
- `AddOffcutReservationAsync`
- `GetOffcutReservationByIdAsync`

### Test result
```
Passed! Failed: 0, Passed: 135, Skipped: 0, Total: 135
```

Tests: Reserve success/404/409, Approve success/404/expired, Use success/404/409, full lifecycle (Reserve→Approve→Use)

## Blockers
None

## Next
Day 4: HTTP endpoints — 6 routes wired to the 3 commands + offcut list/detail/stats queries
