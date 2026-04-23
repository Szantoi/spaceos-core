---
id: MSG-CUTTING-032
from: root
to: cutting
type: task
priority: high
status: READ
ref: MSG-CUTTING-031
created: 2026-04-20
---

# CUTTING-032 — DaySlot entity (DailyPlan refactor)

> **Skill:** `/spaceos-terminal` szerint dolgozz — inbox → build → test → outbox DONE
> **Arch spec:** `docs/architecture/SpaceOS_Modules_Cutting_Planning_Architecture_v4.md` (D-46..D-49)
> **Timeline:** ~1.5 nap
> **Blokkoló feloldva:** CUTTING-031 ✅ DONE

---

## Kontextus

A jelenlegi `DailyPlan` entity egyszerű tároló, nincs saját FSM-je, a kapacitás (`AvailableCapacity=8h`) hardcoded. Refactorálni kell `DaySlot` entity-re, amely saját státusz-gépet és kapacitásmodell-interface-t kap.

**OQ-2 (jóváhagyva):** Egy tenant N PriorityProfile-t tarthat.
**OQ-3 (jóváhagyva):** `CapacityHours` tenant-szinten konfigurálható, v1-ben 8h default.

---

## Feladatok

### 1. DaySlotStatus enum

**Fájl:** `Domain/Enums/DaySlotStatus.cs`

```csharp
public enum DaySlotStatus
{
    Open = 0,
    Locked = 1,
    Closed = 2
}
```

### 2. DaySlot entity

**Fájl:** `Domain/Entities/DaySlot.cs`

```csharp
public sealed class DaySlot : Entity<Guid>
{
    public Guid CuttingPlanId { get; private set; }
    public DateOnly SlotDate { get; private set; }
    public DaySlotStatus Status { get; private set; }
    public decimal CapacityHours { get; private set; }
    public decimal UsedCapacityHours { get; private set; }

    private readonly List<CuttingJob> _jobs = new();
    public IReadOnlyList<CuttingJob> Jobs => _jobs.AsReadOnly();

    // FSM transitions
    public void Lock()   // Open → Locked
    public void CloseSlot()  // Locked → Closed
    public Result AddJob(CuttingJob job)  // kapacitás ellenőrzés
}
```

### 3. DailyPlan → DaySlot átnevezés/refactor

- `DailyPlan.cs` átnevezése vagy felváltása `DaySlot`-tal
- `CuttingPlan` aggregate: `IReadOnlyList<DailyPlan> DailyPlans` → `IReadOnlyList<DaySlot> DaySlots`
- `CuttingJob.DailyPlanId` FK → `DaySlotId`
- EF Core konfiguráció frissítése

### 4. DB migration

```bash
dotnet ef migrations add AddDaySlotRefactorDailyPlan \
  --project SpaceOS.Modules.Cutting.Infrastructure \
  --startup-project SpaceOS.Modules.Cutting.Api
```

Migration: `DailyPlans` tábla → `DaySlots`, `CuttingJobs.DailyPlanId` → `DaySlotId`, `DaySlotStatus int` oszlop hozzáadása, `CapacityHours decimal` default 8.

### 5. Command/Query handlerek frissítése

Minden helyen ahol `DailyPlan`-t referál, cseréld `DaySlot`-ra.

---

## Definition of Done

- [ ] `DaySlotStatus` enum létrehozva
- [ ] `DaySlot` entity létrehozva (FSM: Open/Locked/Closed)
- [ ] `DailyPlan` refactorálva / felváltva
- [ ] `CuttingJob.DaySlotId` FK frissítve
- [ ] Migration létrehozva
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` mind zöld (195+)
- [ ] Outbox DONE üzenet küldve
