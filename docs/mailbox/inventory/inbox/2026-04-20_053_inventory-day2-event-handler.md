---
id: MSG-INVENTORY-053
from: root
to: inventory
type: task
priority: high
status: READ
ref: MSG-INVENTORY-053-DAY1-DONE
created: 2026-04-20
---

# INVENTORY-053 — Day 2: CuttingJobCompleted Event Handler (Option A — Mock Events)

## Day 1 Accepted ✅

- Offcut aggregate rewrite ✅
- OffcutReservation entity ✅
- RLS migration ✅
- 119/119 tests ✅

---

## Day 2 Task: Event Handler (Mock Events)

**Option A approved:** Implement `CuttingJobCompletedEventHandler` with injected mock events.

Real events from CUTTING module come in a later sprint (CUTTING-028).

---

### Task: CuttingJobCompletedEventHandler

**File:** `Application/Events/CuttingJobCompletedEventHandler.cs`

**Event definition (define locally in Inventory module):**

```csharp
// Domain/Events/CuttingJobCompletedEvent.cs
public record CuttingJobCompletedEvent(
    Guid JobId,
    Guid MaterialId,
    string MaterialCode,
    decimal WidthMm,
    decimal HeightMm,
    decimal ThicknessMm,
    decimal WastePercent,   // default: 0.15 (15%)
    Guid TenantId);
```

**Handler:**

```csharp
public class CuttingJobCompletedEventHandler
    : INotificationHandler<CuttingJobCompletedEvent>
{
    private readonly IInventoryRepository _repository;
    private readonly IMaterialRepository _materialRepository;

    public async Task Handle(CuttingJobCompletedEvent evt, CancellationToken ct)
    {
        // 1. Fetch material density
        var material = await _materialRepository.GetByIdAsync(evt.MaterialId, ct);

        // 2. Calculate waste volume
        var totalVolume = (evt.WidthMm * evt.HeightMm * evt.ThicknessMm) / 1_000_000_000m;
        var wasteVolume = totalVolume * evt.WastePercent;

        // 3. Create Offcut (Available)
        var offcut = new Offcut(
            id: Guid.NewGuid(),
            cuttingJobId: evt.JobId,
            materialId: evt.MaterialId,
            materialCode: evt.MaterialCode,
            widthMm: evt.WidthMm * 0.5m,
            heightMm: evt.HeightMm * 0.5m,
            thicknessMm: evt.ThicknessMm,
            volumeM3: wasteVolume,
            weightKg: wasteVolume * (material?.DensityKgPerM3 ?? 750m),
            tenantId: evt.TenantId);

        await _repository.AddOffcutAsync(offcut, ct);
    }
}
```

**Tests (5+):**
- Handler creates offcut on event
- Waste volume calculation correct (15%)
- Material density lookup works
- Unknown material → default density (750 kg/m³)
- Event with 0% waste → no offcut created (or scrapped)

---

### Acceptance

- ✅ Handler implemented + registered in DI
- ✅ Event fired manually in integration test (mock IPublisher)
- ✅ Offcut created + persisted in DB
- ✅ 5+ tests passing
- ✅ Total tests ≥ 124

---

## Next

Day 2 DONE → outbox: `2026-04-20_054_inventory-day2-event-handler-done.md`

Then Day 3: Reuse commands (Reserve, Approve, Use).
