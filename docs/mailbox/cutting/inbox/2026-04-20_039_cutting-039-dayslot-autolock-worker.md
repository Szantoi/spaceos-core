---
id: MSG-CUTTING-039
from: root
to: cutting
type: task
priority: high
status: READ
ref: MSG-CUTTING-038
created: 2026-04-20
---

# CUTTING-039 — DaySlotAutoLockWorker (BackgroundService)

> **Skill:** `/spaceos-terminal` szerint dolgozz — inbox → build → test → outbox DONE
> **Arch spec:** `docs/architecture/SpaceOS_Modules_Cutting_Planning_Architecture_v4.md` (Session C, OQ-5 döntés)
> **Timeline:** ~0.5 nap
> **Blokkoló:** CUTTING-038 ✅ DONE

---

## Kontextus

OQ-5 döntés (jóváhagyva): Az AutoLock worker **csak múltbeli slotokat** zárolja (`SlotDate < DateTime.Today`). Jelenlegi és jövőbeli slotokat nem érint — a gyártás közbeni automatikus zárás nem kívánatos viselkedés.

A `DaySlot` FSM (`Open → Locked`) már implementálva van (CUTTING-032). Ez a task az automatikus, time-based triggert adja hozzá.

---

## Feladatok

### 1. DaySlotAutoLockWorker

**Fájl:** `Infrastructure/Workers/DaySlotAutoLockWorker.cs`

```csharp
public sealed class DaySlotAutoLockWorker : BackgroundService
{
    // 15 perces futási intervallum
    // Lekérdezi az összes Open státuszú DaySlotot ahol SlotDate < DateTime.Today
    // Minden ilyen slotra meghívja a DaySlot.Lock() metódust (ha nem létezik: Lock() = Locked státusz)
    // Mentés repository-n keresztül
    // Structured logging: hány slot lett lezárva, hány hiba
}
```

**Regisztráció:** `ServiceCollectionExtensions.cs`-ben:
```csharp
services.AddHostedService<DaySlotAutoLockWorker>();
```

### 2. DaySlot.Lock() metódus

Ha a `DaySlot` entitáson még nincs `Lock()` FSM metódus (csak státusz-set volt eddig), adj hozzá:

```csharp
// Open → Locked
public Result Lock()
{
    if (Status == DaySlotStatus.Locked) return Result.Success(); // idempotens
    if (Status == DaySlotStatus.Closed)
        return Result.Failure("Closed DaySlot cannot be locked.");
    Status = DaySlotStatus.Locked;
    return Result.Success();
}
```

### 3. IDaySlotRepository kiegészítés

```csharp
// Új metódus:
Task<IReadOnlyList<DaySlot>> GetOpenSlotsBeforeDateAsync(DateTime date, CancellationToken ct);
```

Implementáció: `WHERE Status = 'Open' AND SlotDate < @date` — multi-tenant safe (RLS érvényes).

---

## Tesztek

- `DaySlotAutoLockWorkerTests.cs` (ÚJ): min. 4 teszt
  - Múltbeli Open slotok zárolódnak
  - Jelenlegi/jövőbeli slotok nem zárolódnak
  - Closed slot nem módosul (idempotens)
  - Locked slot nem módosul (idempotens)
- `DaySlotTests.cs` frissítés: `Lock()` metódus tesztek (ha új metódus)
- Meglévő 259 teszt mind zölden

---

## Definition of Done

- [ ] `DaySlotAutoLockWorker` BackgroundService, 15 perces intervallum, csak `SlotDate < DateTime.Today`
- [ ] `DaySlot.Lock()` FSM metódus (idempotens)
- [ ] `IDaySlotRepository.GetOpenSlotsBeforeDateAsync()` + implementáció
- [ ] DI regisztráció (`AddHostedService`)
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` mind zöld (259+)
- [ ] Outbox DONE üzenet küldve
