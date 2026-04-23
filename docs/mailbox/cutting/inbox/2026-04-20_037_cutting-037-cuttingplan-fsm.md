---
id: MSG-CUTTING-037
from: root
to: cutting
type: task
priority: high
status: READ
ref: MSG-CUTTING-036
created: 2026-04-20
---

# CUTTING-037 — CuttingPlan FSM (Publish / Freeze / Close)

> **Skill:** `/spaceos-terminal` szerint dolgozz — inbox → build → test → outbox DONE
> **Arch spec:** `docs/architecture/SpaceOS_Modules_Cutting_Planning_Architecture_v4.md` (D-41..D-43, CuttingPlan aggregate FSM)
> **Timeline:** ~1.5 nap
> **Blokkoló feloldva:** CUTTING-036 ✅ DONE — **Session B utolsó task**

---

## Kontextus

A `CuttingPlan` jelenleg `Draft` státuszból csak `UpdateStatus()` hívással mozdul — nincs valódi FSM. Az arch spec három átmenetet definiál: `Publish`, `Freeze`, `Close`, mindegyik saját invariánsokkal.

---

## Feladatok

### 1. CuttingPlan FSM metódusok

**Fájl:** `Domain/Aggregates/CuttingPlan.cs`

```csharp
// Draft(0) → Published(1)
public Result Publish(Guid profileSnapshotId)
{
    if (Status != CuttingPlanStatus.Draft)
        return Result.Failure("Only Draft plans can be published.");
    if (!DaySlots.Any())
        return Result.Failure("Plan must have at least one DaySlot.");
    if (profileSnapshotId == Guid.Empty)
        return Result.Failure("ProfileSnapshotId is required.");
    ProfileSnapshotId = profileSnapshotId;
    Status = CuttingPlanStatus.Published;
    return Result.Success();
}

// Published(1) → Frozen(2)
public Result Freeze()
{
    if (Status != CuttingPlanStatus.Published)
        return Result.Failure("Only Published plans can be frozen.");
    if (!DaySlots.Any(s => s.Status == DaySlotStatus.Open))
        return Result.Failure("Plan must have at least one Open DaySlot.");
    Status = CuttingPlanStatus.Frozen;
    return Result.Success();
}

// Frozen(2) → Closed(3)
public Result Close()
{
    if (Status != CuttingPlanStatus.Frozen)
        return Result.Failure("Only Frozen plans can be closed.");
    if (DaySlots.Any(s => s.Status == DaySlotStatus.Open))
        return Result.Failure("All DaySlots must be Locked or Closed before closing plan.");
    Status = CuttingPlanStatus.Closed;
    return Result.Success();
}
```

### 2. ProfileSnapshotId mező

```csharp
public Guid? ProfileSnapshotId { get; private set; }
```

EF konfigurációban opcionális (nullable Guid). Migration szükséges.

### 3. Command handlerek

- `PublishCuttingPlanCommandHandler` — `POST /api/cutting/plans/{id}/publish`
- `FreezeCuttingPlanCommandHandler` — `POST /api/cutting/plans/{id}/freeze`
- `CloseCuttingPlanCommandHandler` — `POST /api/cutting/plans/{id}/close`

Request body `PublishCuttingPlanRequest(Guid ProfileSnapshotId)`.

### 4. UpdateStatus eltávolítása / deprecated

Az `UpdateStatus(CuttingPlanStatus)` metódus → `[Obsolete]` attribútum (v1.4.0-ban törlendő). Az FSM metódusok veszik át.

### 5. Migration

```bash
dotnet ef migrations add AddCuttingPlanProfileSnapshotId \
  --project SpaceOS.Modules.Cutting.Infrastructure \
  --startup-project SpaceOS.Modules.Cutting.Api
```

---

## Tesztek

- `CuttingPlanTests.cs` frissítés: FSM átmenetek tesztelése
  - `Publish` Draft-ból sikerül, Published-ből hibát ad
  - `Publish` üres DaySlots esetén hibát ad
  - `Publish` üres profileSnapshotId esetén hibát ad
  - `Freeze` Published-ből sikerül, Draft-ból hibát ad
  - `Freeze` ha nincs Open slot → hibát ad
  - `Close` Frozen-ból sikerül, Published-ből hibát ad
  - `Close` ha van Open slot → hibát ad
  - min. 10 új FSM teszt
- Meglévő 233 teszt mind zölden

---

## Definition of Done

- [ ] `Publish()`, `Freeze()`, `Close()` FSM metódusok implementálva invariánsokkal
- [ ] `ProfileSnapshotId` mező + migration (`dotnet ef migrations add`)
- [ ] 3 új command handler + endpoint
- [ ] `UpdateStatus` → `[Obsolete]`
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` mind zöld (233+)
- [ ] Outbox DONE üzenet küldve

---

## Megjegyzés: Session B lezárása

Ez az utolsó Session B task. DONE elfogadása után Root értékeli a Session C indíthatóságát (CUTTING-038..042, PanelReservation + AutoLock + E2E).
