---
id: MSG-INVENTORY-053
from: inventory
to: root
type: done
priority: high
status: READ
ref: MSG-INVENTORY-052
created: 2026-04-20
---

# INVENTORY-051 Day 1 — Domain + EF Migration DONE

## Summary

Day 1 complete. Commit: `c022043`

### Delivered
- **`OffcutStatus` enum extended**: Added `Reserved=1`, `Scrapped=4` (kept `Waste=3` for backward compat)
- **`OffcutReservationStatus` enum** (new): `Pending / Approved / Cancelled`
- **`Offcut` aggregate rewritten**: new fields `MaterialCode`, `ThicknessMm`, `VolumeM3`, `WeightKg`, `CuttingJobId?`, `UsedAt?`, `UsedInJobId?`; new transitions `Reserve()`, `CancelReservation()`, `MarkUsed(Guid jobId)`, `Scrap()`; legacy overloads preserved
- **`OffcutReservation` aggregate** (new): FSM `Pending → Approved / Cancelled`, 7-day expiry, guard validations
- **EF config updated**: `OffcutConfiguration` + new `OffcutReservationConfiguration`
- **Migration `20260420000004_AddOffcutTrackingV2`**: columns, indexes, RLS ENABLE+FORCE+POLICY on Offcuts (was missing), CREATE TABLE OffcutReservations + RLS
- **ModelSnapshot updated**
- **20 new domain unit tests** in `OffcutV2Tests.cs`

### Test result
```
Passed! Failed: 0, Passed: 119, Skipped: 0, Total: 119
```

## Blockers
None

## Option Selected
**Option A** (mock events) for Day 2 — proceeding unblocked.

## Next
Day 2: `CuttingJobCompletedEventHandler` stub with local mock event
