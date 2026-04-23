---
id: MSG-CUTTING-032-DONE
from: cutting
to: root
type: done
status: READ
ref: MSG-CUTTING-032
created: 2026-04-20
---

## Összefoglaló

CUTTING-032 DaySlot entity refactor végrehajtva — a `DailyPlan` entity felváltva `DaySlot`-tal, teljes FSM-mel és kapacitásmodellel.

### Domain változások

- `Domain/Enums/DaySlotStatus.cs` — **LÉTREHOZVA** (Open=0, Locked=1, Closed=2)
- `Domain/Entities/DaySlot.cs` — **LÉTREHOZVA** (FSM: Lock(), CloseSlot(), Result AddJob())
  - `SlotDate: DateOnly`, `CapacityHours: decimal`, `UsedCapacityHours: decimal` (stored)
  - `AddJob` visszaad `Result` — kapacitás és FK ellenőrzéssel
- `Domain/Aggregates/DailyPlan.cs` — **TÖRÖLVE** (felváltotta DaySlot)
- `Domain/Aggregates/CuttingJob.cs` — `DailyPlanId` → `DaySlotId`
- `Domain/Aggregates/CuttingPlan.cs` — `_dailyPlans/DailyPlans` → `_daySlots/DaySlots`, `DaySlot.Create(planId, DateOnly)`

### Application változások

- `Strategies/IPlanningStrategy.cs` — `IEnumerable<DailyPlan> dailyPlans` → `IEnumerable<DaySlot> daySlots`
- Mind a 4 strategy (`MaxCut`, `FIFO`, `Priority`, `Custom`) — `AvailableCapacity/AllocatedCapacity` → `CapacityHours/UsedCapacityHours`, `d.Date` → `d.SlotDate.ToDateTime(...)`, `plan.DailyPlans` → `plan.DaySlots`
- `CreateCuttingPlanCommandHandler.cs` — seed jobs `d.SlotDate.ToDateTime(...)`, `job.DaySlotId`, `slot.AddJob(job)` Result kezelve
- `GetCuttingPlanQueryHandler.cs` — `d.SlotDate.ToString("yyyy-MM-dd")`, `d.CapacityHours`, `d.UsedCapacityHours`

### Infrastructure változások

- `Configurations/DaySlotConfiguration.cs` — **LÉTREHOZVA** (tábla: "DaySlots", DateOnly→date, Status int, CapacityHours, UsedCapacityHours)
- `Configurations/DailyPlanConfiguration.cs` — **TÖRÖLVE**
- `Configurations/CuttingJobConfiguration.cs` — `DailyPlanId` → `DaySlotId`
- `Configurations/CuttingPlanConfiguration.cs` — `HasMany(DaySlots)`
- `Persistence/CuttingDbContext.cs` — `DailyPlans` → `DaySlots` (DbSet)
- `Repositories/CuttingRepository.cs` — `Include(p => p.DaySlots).ThenInclude(d => d.Jobs)`

### Migration

- `20260420075638_AddDaySlotRefactorDailyPlan.cs` — EF Core által generált
  - DropFK + DropTable DailyPlans
  - RenameColumn CuttingJobs.DailyPlanId → DaySlotId (EF detektálta a rename-t)
  - CreateTable DaySlots (SlotDate date, Status int, CapacityHours, UsedCapacityHours)
  - AddFK DaySlots → CuttingPlans, CuttingJobs.DaySlotId → DaySlots

### Teszt változások

- `Domain/DailyPlanTests.cs` — **TÖRÖLVE** (8 teszt)
- `Domain/DaySlotTests.cs` — **LÉTREHOZVA** (19 teszt, FSM + kapacitás + validáció)
- `Domain/CuttingPlanTests.cs` — DailyPlan → DaySlot referenciák frissítve
- `Domain/CuttingPlanRepositoryTests.cs` — `DailyPlans` → `DaySlots`
- Minden strategy test (MaxCut, FIFO, Priority, Custom) — `DailyPlanId` → `DaySlotId`, `d.Date` → `d.SlotDate.ToDateTime(...)`

## Tesztek

- `dotnet build` → **0 error, 0 warning** ✅
- `dotnet test` → **196/196 pass** ✅ (205+10 contracts összesen)

## Security review

- Nincs security-releváns változás (domain refactor)
- RLS érintett táblák: `DaySlots` tenant-en keresztül `CuttingPlans` via FK
- Nincs új public endpoint

## Kockázatok / kérdések

- `DaySlot.AddJob` jelenleg nem ellenőrzi a `Status`-t — Locked/Closed állapotban is fogad jobokat (a task spec nem zárta ki ezt; v1.5-ben lehet szigorítani)
- A migration DropTable+CreateTable jellegű (nem Rename) — ez dev/staging környezetben elfogadható, nincs éles adat
