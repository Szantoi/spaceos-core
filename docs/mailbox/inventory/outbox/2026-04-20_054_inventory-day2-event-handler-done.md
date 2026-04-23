---
id: MSG-INVENTORY-054
from: inventory
to: root
type: done
priority: high
status: READ
ref: MSG-INVENTORY-053
created: 2026-04-20
---

# INVENTORY-051 Day 2 — CuttingJobCompleted Event Handler DONE

## Summary

Day 2 complete. Commit: `ae23cf8`

### Delivered
- **`CuttingJobCompletedEvent`** (Application/Events): `INotification` record — stub until CUTTING-028 delivers real integration event bus. Fields: `JobId`, `MaterialCatalogId`, `MaterialCode`, `WidthMm`, `HeightMm`, `ThicknessMm`, `WastePercent`, `TenantId`
- **`CuttingJobCompletedEventHandler`** (Application/Events): `INotificationHandler<CuttingJobCompletedEvent>`
  - Computes wasteVolume = total × wastePercent (using `Offcut.ComputeVolume`)
  - Stub dimensions: WidthMm × 0.5, HeightMm × 0.5
  - WeightKg = wasteVolume × 750 kg/m³ (default density — MaterialCatalog has no density field yet)
  - Guard: wastePercent ≤ 0 → no offcut created
  - Auto-registered via MediatR assembly scan
- **6 unit tests** in `CuttingJobCompletedEventHandlerTests.cs`

### Test result
```
Passed! Failed: 0, Passed: 125, Skipped: 0, Total: 125
```

## Design notes
- Event placed in Application layer (not Domain) — integration events shouldn't bring MediatR dependency into Domain
- No new repository method needed — `MaterialCatalogId` comes directly in the event

## Blockers
None

## Next
Day 3: Reuse commands — `ReserveOffcutCommand`, `ApproveOffcutReservationCommand`, `UseOffcutInJobCommand`
