---
id: MSG-CUTTING-035
from: root
to: cutting
type: task
priority: high
status: READ
ref: MSG-CUTTING-034
created: 2026-04-20
---

# CUTTING-035 — IReworkPolicy interfész + WarnAndApplyPolicy

> **Skill:** `/spaceos-terminal` szerint dolgozz — inbox → build → test → outbox DONE
> **Arch spec:** `docs/architecture/SpaceOS_Modules_Cutting_Planning_Architecture_v4.md` (D-52..D-53)
> **Timeline:** ~1 nap
> **Blokkoló feloldva:** CUTTING-034 ✅ DONE

---

## Kontextus

Ha egy `CuttingJob` nem fér el az eredetileg tervezett `DaySlot`-ban (kapacitáshiány vagy lezárt slot), a rendszernek döntenie kell: mit csináljon. Ezt a `IReworkPolicy` interfész és implementációi kezelik.

---

## Feladatok

### 1. ReworkDecision értékobjektum

**Fájl:** `Domain/ValueObjects/ReworkDecision.cs`

```csharp
public sealed record ReworkDecision(
    bool CanReschedule,
    DaySlot? TargetSlot,
    string Reason);
```

### 2. IReworkPolicy interfész

**Fájl:** `Domain/Interfaces/IReworkPolicy.cs`

```csharp
public interface IReworkPolicy
{
    string PolicyId { get; }
    ReworkDecision Evaluate(CuttingJob job, DaySlot targetSlot);
    void Apply(CuttingJob job, IReadOnlyList<DaySlot> availableSlots);
}
```

### 3. WarnAndApplyPolicy implementáció

**Fájl:** `Domain/Services/WarnAndApplyPolicy.cs`

```csharp
public sealed class WarnAndApplyPolicy : IReworkPolicy
{
    public string PolicyId => "warn-and-apply-v1";

    public ReworkDecision Evaluate(CuttingJob job, DaySlot targetSlot)
    {
        // Ha a slot Open és van kapacitás → nincs rework szükséges
        // Ha a slot Locked/Closed vagy nincs kapacitás → CanReschedule = true, következő szabad slot
    }

    public void Apply(CuttingJob job, IReadOnlyList<DaySlot> availableSlots)
    {
        // Megkeresi az első Open slotot ahol van kapacitás
        // Áthelyezi a job DaySlotId-ját (job.RescheduleTo(newSlotId))
        // Ha nincs szabad slot → jobhoz Warning státusz
    }
}
```

### 4. DI regisztráció

```csharp
services.AddSingleton<IReworkPolicy, WarnAndApplyPolicy>();
```

### 5. CuttingJob bővítése

```csharp
// CuttingJob.cs-ben:
public void RescheduleTo(Guid newDaySlotId) { DaySlotId = newDaySlotId; }
```

---

## Tesztek

- `WarnAndApplyPolicyTests.cs` (ÚJ): min. 8 teszt
  - `Evaluate` amikor slot Open + kapacitás van → `CanReschedule = false`
  - `Evaluate` amikor slot Locked → `CanReschedule = true`
  - `Evaluate` amikor slot Open de nincs kapacitás → `CanReschedule = true`
  - `Apply` átütemezi a következő szabad slotra
  - `Apply` ha nincs szabad slot → Warning
- Meglévő tesztek mind zölden

---

## Definition of Done

- [ ] `ReworkDecision` record létrehozva
- [ ] `IReworkPolicy` interfész létrehozva
- [ ] `WarnAndApplyPolicy` implementáció létrehozva
- [ ] DI regisztrálva
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` mind zöld (207+)
- [ ] Outbox DONE üzenet küldve
