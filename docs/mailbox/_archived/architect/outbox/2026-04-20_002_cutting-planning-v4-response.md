---
id: MSG-ARCH-002-RESPONSE
from: architect
to: root
type: response
priority: high
status: READ
ref: MSG-ARCH-002
created: 2026-04-20
---

# ARCH-002 — Cutting Planning v4: Sessions B+C arch spec — KÉSZ

## Kérdés összefoglalója

Root kérte a Cutting Planning v4 teljes architektúra specifikációját (Sessions B+C), amely lefedi a `CuttingPlan` aggregate FSM-jét, `DaySlot` value objectet, `ICapacityModel` és `IReworkPolicy` interfészeket, `PriorityProfile` entityt, valamint a kapcsolatot a meglévő CUTTING-026/027/028/030 kódbázissal és az implementációs sorrendet.

---

## Elemzés

### Meglévő kódbázis állapota (Session A — Phase 1 lightweight)

| Fájl | Megjegyzés |
|---|---|
| `CuttingPlan.cs` | Status string (`Draft/Approved/InProgress/Closed`), PlanDays 7-90, nincs extension point |
| `DailyPlan.cs` | Egyszerű entity, AvailableCapacity=8h fixed, nincs lokális FSM |
| `CuttingJob.cs` | DailyPlanId FK, string Priority/Status, `MarkAsCut()` részleges FSM |
| `IPlanningStrategy.cs` | 4 implementáció (MaxCut/FIFO/Priority/Custom), `DailyPlan` típust használ |
| `NestingService.cs` | FFDH algoritmus beágyazva, nem cserélhető |

**Phase 1 hiányosságai (Sessions B+C oldják meg):**
- Nincs typed FSM a CuttingPlan-en (string status törékeny)
- Nincs capacity model absztrakció (8h hardcoded)
- Nincs rework policy (mi történik ha job nem fér el)
- Nincs tenant-szintű PriorityProfile konfiguráció
- Nincs PanelReservation lifecycle (Inventory kapcsolat hiányzik)
- Nincs DaySlot auto-lock mechanizmus

---

## Döntési opciók (főbb dilemmák)

### D-41: CuttingPlanStatus enum — melyik megközelítés?

| Opció | Leírás | Előny | Hátrány | **Döntés** |
|---|---|---|---|---|
| A | String megtartása | 0 migrációs kockázat | Törékeny, runtime hiba lehetséges | ❌ |
| B | Párhuzamos int/string | Kompatibilis | Dupla igazság-forrás | ❌ |
| **C** | **Typed enum + CASE migration** | **Erős típusosság, egyértelmű FSM** | **CASE SQL migráció kell** | **✅ VÁLASZTOTT** |

**Mapping:** `Draft`→0, `Approved`→1 (→Published), `InProgress`→2 (→Frozen), `Closed`→3

### D-46: DaySlot — value object vagy entity?

| Opció | Döntés | Indok |
|---|---|---|
| Value Object | ❌ | CuttingJob FK referenciát tart rá — identity kell |
| **Entity** | **✅** | EF Core lifecycle, saját Id, DaySlotStatus lokális FSM |

### D-55: PanelReservation — duplikálás vagy referencia?

| Opció | Döntés | Indok |
|---|---|---|
| Inventory aggregate duplikálás | ❌ | Modul határ megsértése |
| **InventoryReservationId referencia** | **✅** | Csak az Id tárolódik, Inventory marad az igazság-forrás |

---

## Javasolt spec

**Teljes spec fájl:** `docs/architecture/SpaceOS_Modules_Cutting_Planning_Architecture_v4.md`

### CuttingPlan aggregate FSM (D-41..D-43)

```
Draft(0) ──Publish(profileSnapshotId)──▶ Published(1)
Published(1) ──Freeze()──▶ Frozen(2)
Frozen(2) ──Close()──▶ Closed(3)
```

**Invariánsok:**
- `Publish()`: legalább 1 DaySlot kell, ProfileSnapshotId ≠ null
- `Freeze()`: csak Published-ből, legalább 1 Open DaySlot kell
- `Close()`: csak Frozen-ből, minden DaySlot Locked/Closed kell

### DaySlot entity (D-46..D-49)

```csharp
public sealed class DaySlot : Entity<Guid>
{
    public DateOnly SlotDate { get; private set; }
    public DaySlotStatus Status { get; private set; }  // Open/Locked/Closed
    public decimal CapacityHours { get; private set; }
    public decimal UsedCapacityHours { get; private set; }
    public IReadOnlyList<CuttingJob> Jobs => _jobs.AsReadOnly();

    // FSM transitions
    public void Lock() { /* Open → Locked */ }
    public void CloseSlot() { /* Locked → Closed */ }
    public Result AddJob(CuttingJob job, ICapacityModel capacityModel) { /* kapacitás ellenőrzés */ }
}
```

### ICapacityModel interfész (D-50..D-51)

```csharp
public interface ICapacityModel
{
    string ModelId { get; }
    decimal ComputeCapacityHours(DaySlot slot);
    decimal ComputeJobCost(CuttingJob job);
    bool HasCapacity(DaySlot slot, CuttingJob job);
}

// v1 implementáció: AreaCapacityModel (2.5 m²/h)
// v2 placeholder: MachineCapacityModel
// v3 placeholder: HybridCapacityModel
```

### IReworkPolicy interfész (D-52..D-53)

```csharp
public interface IReworkPolicy
{
    string PolicyId { get; }
    ReworkDecision Evaluate(CuttingJob job, DaySlot targetSlot);
    void Apply(CuttingJob job, IReadOnlyList<DaySlot> availableSlots);
}

// v1 implementáció: WarnAndApplyPolicy (következő szabad slot)
// v2 placeholder: StrictBlockPolicy
// v3 placeholder: ManagerApprovalPolicy
```

### PriorityProfile entity (D-54)

```csharp
public sealed class PriorityProfile : AggregateRoot<Guid>
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; }
    public bool IsDefault { get; private set; }
    public string CapacityModelId { get; private set; }   // FK → ICapacityModel
    public string ReworkPolicyId { get; private set; }    // FK → IReworkPolicy
    public string PlanningStrategyId { get; private set; }
    public IReadOnlyList<PriorityRule> Rules { get; }     // ordered list
}
```

**Presetek (seed):** `Manufacturer` (Area+FIFO+Warn) és `PanelCutter` (Area+Priority+Warn)

### Implementációs sorrend — Root feladatkiosztás

#### Session B (~10 nap, 6 task)

| Task | Feladat | Előfeltétel |
|---|---|---|
| CUTTING-030 | DB migration + CuttingPlanStatus enum | Contracts 1.3.0 ✅ |
| CUTTING-031 | DaySlot entity (DailyPlan refactor) | CUTTING-030 |
| CUTTING-032 | ICapacityModel interfész + AreaCapacityModel | CUTTING-031 |
| CUTTING-033 | IReworkPolicy interfész + WarnAndApplyPolicy | CUTTING-032 |
| CUTTING-034 | PriorityProfile aggregate + seed presets | CUTTING-033 |
| CUTTING-035 | CuttingPlan FSM átírás (Publish/Freeze/Close) | CUTTING-034 |

#### Session C (~10 nap, 6 task)

| Task | Feladat | Előfeltétel |
|---|---|---|
| CUTTING-036 | PanelReservation aggregate (Inventory referencia) | CUTTING-035 |
| CUTTING-037 | DaySlotAutoLockWorker (BackgroundService, 15 perc) | CUTTING-036 |
| CUTTING-038 | GetNestingResultQueryHandler → INestingStrategy | CUTTING-029 ✅ |
| CUTTING-039 | IPlanningStrategy refactor (DaySlot típus) | CUTTING-031 |
| CUTTING-040 | Integration tesztek (Session B+C lefedettség) | CUTTING-035..039 |
| CUTTING-041 | API endpoint frissítések (új FSM tükrözése) | CUTTING-040 |

**Blokkoló feltételek:**
- CUTTING-030 blokkol: OQ-1 (StatusMapping döntés) + Contracts 1.3.0 DONE
- CUTTING-036 blokkol: OQ-4 (PanelReservation Reserve/Release API) + Inventory szinkron
- CUTTING-037 blokkol: OQ-5 (AutoLock scope: csak múlt, vagy jövő is?)

---

## Kockázatok / nyitott kérdések Gábornak

Az alábbi **7 nyitott kérdés** megválaszolása szükséges mielőtt Root kiadja CUTTING-030..034 feladatokat:

### OQ-1 — StatusMapping migrációs döntés ⚡ BLOKKOLÓ

**Kérdés:** A meglévő DB-ben `Approved` értékek → `Published(1)`-re vagy maradjon `Approved(1)` az enum értékneve?

**Kontextus:** Két éles DB record már `Approved` szöveggel van. A CASE migration átalakítja → int, de az enum neve még kérdés.

**Javaslat:** `Published` — konzisztens a Growth Strategy terminológiával.
**Hatás:** Ha `Approved` marad, változtatni kell a spec-en és a spec-ben hivatkozott FSM docban.

---

### OQ-2 — PriorityProfile: egy tenant több profilt tárolhat?

**Kérdés:** Egy tenant egyszerre 1 vagy N PriorityProfile-t tarthat aktívan?

**Javaslat:** N profil (pl. `Manufacturer` + `PanelCutter`), egy CuttingPlan pontosan 1-re hivatkozik `ProfileSnapshotId` via.
**Hatás:** Ha 1 profil → egyszerűbb seed, de elvész a flexibilitás.

---

### OQ-3 — CapacityModel: 2.5 m²/h konstans v1-re elfogadható?

**Kérdés:** Az `AreaCapacityModel` fix 2.5 m²/h értékkel indul. Ez Doorstar számára reális alap?

**Javaslat:** Igen — konfigurálható legyen tenant-szinten (PriorityProfile-ban tárolt param).
**Hatás:** Ha nem megfelelő, kell egy `MachineCapacityModel` v1-be is.

---

### OQ-4 — PanelReservation: Inventory API szinkron vagy aszinkron? ⚡ BLOKKOLÓ

**Kérdés:** Ha Cutting reservál egy panelt az Inventory-ban — szinkron HTTP call vagy event?

**Kontextus:** Jelenlegi Inventory API: `IInventoryProvider.ReserveAsync()` — szinkron Result<>.
**Javaslat:** Szinkron v1-ben (simple és testable), aszinkron v2-ben saga pattern-nel.
**Hatás:** Ha event → CUTTING-036 timeline megduplázódhat.

---

### OQ-5 — DaySlotAutoLockWorker: mi kerül auto-lock alá?

**Kérdés:** Az AutoLockWorker csak a MÚLTBELI Open slotokat zárolja, vagy a MAI napét is?

**Javaslat:** Csak `SlotDate < DateTime.Today` — mai nap marad nyitott amíg Gábor manuálisan nem zárja.
**Hatás:** Ha ma is auto-lock → production esetén reggelente minden job elveszhet ha nem volt frozen.

---

### OQ-6 — Session C scope: van-e rá Q2 deadline?

**Kérdés:** CUTTING-036..041 (~10 nap) belefér-e a 2026 Q2 Doorstar Soft Launch-ba?

**Kontextus:** Soft Launch: **2026 Q2** (kb. június vége). Session B+C összesen ~20 nap.
**Javaslat:** Session B (CUTTING-030..035) kötelező a Soft Launch-hoz. Session C (036..041) lehet Q3 első feladat.
**Hatás:** Ha Session C Q2-ben kell → párhuzamosítás szükséges (Nesting + AutoLock egyidejűleg).

---

### OQ-7 — PlanningStrategy: melyik legyen a PanelCutter preset defaultja?

**Kérdés:** A `PanelCutter` preset `PlanningStrategyId`-je `MaxCutStrategy` vagy `PriorityStrategy` legyen?

**Kontextus:** PanelCutter (lapszabász) elsősorban anyaghatékonyságra optimalizál.
**Javaslat:** `MaxCutStrategy` — maximális lapkihasználás a célja, nem a határidő-prioritás.
**Hatás:** Ha `PriorityStrategy` → a seed presetek megtévesztők lesznek a lapszabász usereknek.

---

## Következő lépések (Root számára)

1. **OQ-1..OQ-7 válaszok bekérése Gábortól** — CUTTING-030 csak ezután indítható
2. **ABSTRACTIONS-010 DONE-ra várakozás** — Contracts 1.3.0 kell CUTTING-030 előtt
3. **CUTTING-029 DONE-ra várakozás** (párhuzamosan fut) — CUTTING-038 blokkol rá
4. **Session B task dispatch** (CUTTING-030..035) — OQ válaszok + Contracts 1.3.0 DONE után
5. **ADR-034..037 megírása** (Root vagy Architect) — 4 új architekturális döntés dokumentálása

**Spec fájl:** `docs/architecture/SpaceOS_Modules_Cutting_Planning_Architecture_v4.md`
**Státusz:** DRAFT — OQ válaszok után → APPROVED → Root kiadhatja CUTTING-030-at
