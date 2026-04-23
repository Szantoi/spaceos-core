---
id: MSG-CUTTING-034
from: root
to: cutting
type: task
priority: high
status: READ
ref: MSG-CUTTING-032
created: 2026-04-20
---

# CUTTING-034 — ICapacityModel interfész + AreaCapacityModel

> **Skill:** `/spaceos-terminal` szerint dolgozz — inbox → build → test → outbox DONE
> **Arch spec:** `docs/architecture/SpaceOS_Modules_Cutting_Planning_Architecture_v4.md` (D-50..D-51)
> **Timeline:** ~1 nap
> **Blokkoló feloldva:** CUTTING-032 ✅ DONE

---

## Kontextus

A `DaySlot.AddJob()` jelenleg nem kapacitásmodell-független — a kapacitáslogikát absztrakcióba kell szervezni. Az `ICapacityModel` interfész lehetővé teszi, hogy v2-ben `MachineCapacityModel` is pluggolható legyen.

**OQ-3 döntés (jóváhagyva):** `AreaCapacityModel` v1-ben 8h default, tenant-szinten konfigurálható.

---

## Feladatok

### 1. ICapacityModel interfész

**Fájl:** `Domain/Interfaces/ICapacityModel.cs`

```csharp
namespace SpaceOS.Modules.Cutting.Domain.Interfaces;

public interface ICapacityModel
{
    string ModelId { get; }
    decimal ComputeCapacityHours(DaySlot slot);
    decimal ComputeJobCost(CuttingJob job);
    bool HasCapacity(DaySlot slot, CuttingJob job);
}
```

### 2. AreaCapacityModel implementáció

**Fájl:** `Domain/Services/AreaCapacityModel.cs`

```csharp
public sealed class AreaCapacityModel : ICapacityModel
{
    public string ModelId => "area-v1";

    private readonly decimal _throughputM2PerHour;  // default: 2.5

    public AreaCapacityModel(decimal throughputM2PerHour = 2.5m)
        => _throughputM2PerHour = throughputM2PerHour;

    public decimal ComputeCapacityHours(DaySlot slot) => slot.CapacityHours;

    public decimal ComputeJobCost(CuttingJob job)
        => (job.WidthMm * job.HeightMm / 1_000_000m) / _throughputM2PerHour;

    public bool HasCapacity(DaySlot slot, CuttingJob job)
        => slot.UsedCapacityHours + ComputeJobCost(job) <= slot.CapacityHours;
}
```

### 3. DaySlot.AddJob frissítése

A jelenlegi `AddJob(CuttingJob job)` nem ellenőriz státuszt — frissíteni kell:

```csharp
public Result AddJob(CuttingJob job, ICapacityModel capacityModel)
{
    if (Status != DaySlotStatus.Open)
        return Result.Failure("DaySlot is not Open.");
    if (!capacityModel.HasCapacity(this, job))
        return Result.Failure("Insufficient capacity.");
    _jobs.Add(job);
    UsedCapacityHours += capacityModel.ComputeJobCost(job);
    return Result.Success();
}
```

### 4. DI regisztráció

```csharp
services.AddSingleton<ICapacityModel, AreaCapacityModel>();
```

### 5. Érintett handlerek frissítése

`CreateCuttingPlanCommandHandler` és planning strategy-k ahol `AddJob` hívás van — frissíteni `ICapacityModel` paraméterrel (DI-ból injektálva).

---

## Tesztek

- `AreaCapacityModelTests.cs` (ÚJ): `ComputeJobCost`, `HasCapacity` igaz/hamis, `ModelId` ellenőrzés — min. 8 teszt
- `DaySlotTests.cs` frissítés: `AddJob` statuszt is ellenőriz (Locked/Closed esetén failure), kapacitástúllépés esetén failure
- Meglévő tesztek mind zölden

---

## Definition of Done

- [ ] `ICapacityModel` interfész létrehozva
- [ ] `AreaCapacityModel` implementáció létrehozva
- [ ] `DaySlot.AddJob` Status + kapacitás ellenőrzéssel
- [ ] DI regisztrálva
- [ ] `dotnet ef migrations add` ha szükséges (schema változás esetén)
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` mind zöld (196+)
- [ ] Outbox DONE üzenet küldve
