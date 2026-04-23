# SpaceOS Modules Cutting Planning Architecture v4

> **Cutting Planning teljes architektúra spec — Sessions B+C**

| Meta | Érték |
|---|---|
| **Fájl** | `docs/architecture/SpaceOS_Modules_Cutting_Planning_Architecture_v4.md` |
| **Verzió** | v4.0 |
| **Státusz** | DRAFT — Gábor jóváhagyásra vár (OQ-1..OQ-7) |
| **Dátum** | 2026-04-20 |
| **Szerző** | Architect terminál |
| **Scope** | CuttingPlan FSM redesign, DaySlot, PriorityProfile, ICapacityModel, IReworkPolicy, PanelReservation |
| **Prerrekvizitumok** | CUTTING-026/027/028 DEPLOYED, Contracts v1.2.0 (ReserveAsync) DEPLOYED |
| **Forrás** | `SpaceOS_Growth_Strategy_v1.md` Section 4 (Q1–Q14), codebase audit 2026-04-20 |
| **Cél** | Cutting terminál közvetlenül implementálhat ebből a spec-ből |
| **Golden Rules érintettek** | #1 (Data→Rules→Geometry), #2 (Modular Monolith), #3 (Immutability), #5 (Walking Skeleton) |

---

## 1. Jelenlegi állapot vs. célkép

### 1.1 Létezik (CUTTING-026/027/028 — DEPLOYED)

| Komponens | Implementáció | Fájl | Megjegyzés |
|---|---|---|---|
| `CuttingPlan` aggregate | 4 string status: Draft/Approved/InProgress/Closed | `Domain/Aggregates/CuttingPlan.cs` | Nincs FSM guard, `UpdateStatus()` bármilyen irányba engedi |
| `DailyPlan` child entity | Fix 8h kapacitás, nincs lokális állapot | `Domain/Aggregates/DailyPlan.cs` | Nem value object, entity FK-val |
| `CuttingJob` child entity | 5 string status: Pending/InProgress/Cut/QC/Delivered | `Domain/Aggregates/CuttingJob.cs` | `MarkAsCut()` részleges FSM |
| `IPlanningStrategy` + 4 impl | MaxCut/FIFO/Priority/Custom | `Application/Strategies/` | Kapacitás-alapú yield (nem geometria) |
| `IPlanningStrategyFactory` | StrategyId→implementáció mapping | `Application/Strategies/` | |
| 6 HTTP endpoint | POST/GET/PUT planning, GET daily, PUT complete | `Api/Endpoints/CuttingPlanningEndpoints.cs` | ManufacturerOnly RBAC |
| `CuttingJobCompletedEvent` | HTTP event bus → Inventory | `Infrastructure/Events/` | CUTTING-028 |
| PlanDays range | 7–90 | `CuttingPlan.Create()` | Fix range, nem tenant-config |
| DB migration | `20260419000001_AddCuttingPlanAggregate` | 3 tábla: CuttingPlans, DailyPlans, CuttingJobs | RLS a CuttingPlans táblán |
| Tesztek | 184+ passing (77 cutting-specific) | `tests/` | String assert-ok a status-okon |

### 1.2 Hiányzik (Sessions B+C — ez a spec)

| Komponens | Prioritás | Session |
|---|---|---|
| FSM redesign: Draft→Published→Frozen→Closed | Kritikus | B |
| DaySlot value object lokális FSM-mel (Open/Locked/Closed) | Kritikus | B |
| Extension points: PartnerId, SourceChannel, ProfileSnapshotId, ReworkPolicyOverride | Magas | B |
| PlanDays range átalakítás: 1..14 default, 1..30 tenant-config | Közepes | B |
| PanelReservation aggregate (IInventoryProvider.ReserveAsync) | Magas | B |
| PriorityProfile entity + 2 preset | Magas | C |
| ICapacityModel + AreaCapacityModel | Magas | C |
| IReworkPolicy + WarnAndApplyPolicy | Magas | C |
| PriorityScoringService (profile-aware) | Közepes | C |
| OffcutFirstStrategy (inventory-aware) | Közepes | C |
| LockDaySlot handler + cron auto-lock | Magas | C |
| Publish/Freeze/Close command handlerek | Kritikus | B+C |

---

## 2. FSM Redesign döntés

### 2.1 Probléma

A jelenlegi `CuttingPlan.UpdateStatus()` 4 string értéket fogad el: `Draft`, `Approved`, `InProgress`, `Closed`. A Growth Strategy Q3 döntése 4 új állapotot definiál: `Draft`, `Published`, `Frozen`, `Closed`. Az átfedés részleges:

| Jelenlegi | Új | Szemantika különbség |
|---|---|---|
| `Draft` | `Draft` | Azonos — szerkeszthető |
| `Approved` | `Published` | Approved = jóváhagy, Published = operator látható + profil snapshot |
| `InProgress` | `Frozen` | InProgress = munka folyamatban, Frozen = DaySlot-ok zárolva, nesting véglegesítve |
| `Closed` | `Closed` | Közel azonos — archivált |

### 2.2 Opciók elemzése

| Opció | Leírás | Előny | Hátrány |
|---|---|---|---|
| **A) Clean migration** | DB migration: UPDATE Status SET 'Published' WHERE 'Approved', SET 'Frozen' WHERE 'InProgress'. Enum csere a domain-ben. | Tiszta slate, nincs legacy debt | Deployed production adatokat módosít. E2E tesztek törnek. |
| **B) Két párhuzamos mező** | Megtartjuk a régi `Status` string-et, új `PlanState` enum oszlopot adunk hozzá | Nincs breaking change | Két igazságforrás, melyik az érvényes? Zavaros. |
| **C) Új `CuttingPlanStatus` enum + migrációs mapping** | Új enum definíció a domain-ben. Migration: ALTER COLUMN TYPE + USING CASE expression. | Típusbiztos enum. Egyetlen igazságforrás. Migráció atomikus. | Enum ordinálok gondos tervezést igényelnek. |

### 2.3 Döntés: Opció C — Új `CuttingPlanStatus` enum + migrációs mapping

**Indoklás:**

1. **Típusbiztonság**: String-alapú status a #1 tech debt forrás a jelenlegi kódbázisban.
2. **FSM guard**: Enum + explicit transition method-ok (`Publish()/Freeze()/Close()`) a domain aggregáton biztosítják az érvényes átmeneteket.
3. **Backward compatibility**: A DB migration atomikus CASE expression-nel mappeli a régi értékeket. `Approved`→`Published`, `InProgress`→`Frozen`.
4. **Teszt impact**: A tesztek TÖRNEK — ez szándékos. A régi tesztek az architektúrálisan hibás `UpdateStatus()`-t tesztelték. Az új tesztek az FSM transition-öket tesztelik.

**Migration SQL:**

```sql
-- Step 1: Add temporary column with new enum values
ALTER TABLE spaceos_cutting."CuttingPlans"
  ADD COLUMN "StatusEnum" integer NOT NULL DEFAULT 0;

-- Step 2: Map old string values to enum ordinals
UPDATE spaceos_cutting."CuttingPlans" SET "StatusEnum" = CASE
  WHEN "Status" = 'Draft' THEN 0
  WHEN "Status" = 'Approved' THEN 1   -- maps to Published
  WHEN "Status" = 'InProgress' THEN 2 -- maps to Frozen
  WHEN "Status" = 'Closed' THEN 3
  ELSE 0
END;

-- Step 3: Drop old string column, rename new
ALTER TABLE spaceos_cutting."CuttingPlans" DROP COLUMN "Status";
ALTER TABLE spaceos_cutting."CuttingPlans" RENAME COLUMN "StatusEnum" TO "Status";
```

**Kockázat csökkentés:** A migration `Down()` method-ja visszaállítja string-re — rollback lehetséges.

---

## 3. Domain model redesign — Döntések D-41…D-60

### D-41: `CuttingPlanStatus` enum

```csharp
namespace SpaceOS.Modules.Cutting.Domain.Enums;

/// <summary>
/// Global FSM for the multi-day cutting plan lifecycle.
/// Transitions: Draft → Published → Frozen → Closed
/// Terminal state: Closed (immutable after this point).
/// </summary>
public enum CuttingPlanStatus
{
    /// <summary>Being configured. Profile, strategy, jobs editable.</summary>
    Draft = 0,

    /// <summary>Visible to operators. Profile snapshot taken. No profile changes allowed.</summary>
    Published = 1,

    /// <summary>DaySlots locked, nesting finalized. Only job completion allowed.</summary>
    Frozen = 2,

    /// <summary>All jobs completed/archived. Immutable. No further changes.</summary>
    Closed = 3
}
```

**Migration impact:** `CuttingPlanConfiguration.cs` módosul: `.HasConversion<int>()` az enum-hoz. A Status oszlop típusa `varchar(20)` → `integer`.

---

### D-42: `CuttingPlan` aggregate kibővítés

```csharp
namespace SpaceOS.Modules.Cutting.Domain.Aggregates;

public class CuttingPlan : AggregateRoot
{
    // --- Létező mezők (változatlan) ---
    public Guid Id { get; private set; }
    public DateTime PlanDate { get; private set; }
    public int PlanDays { get; private set; }
    public string StrategyId { get; private set; } = string.Empty;
    public Guid TenantId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // --- MÓDOSÍTOTT: string → enum ---
    public CuttingPlanStatus Status { get; private set; } = CuttingPlanStatus.Draft;

    // --- ÚJ mezők (Session B extension points) ---
    public Guid? PartnerId { get; private set; }                      // D-43
    public SourceChannel SourceChannel { get; private set; }          // D-44
        = SourceChannel.Direct;
    public Guid? ProfileSnapshotId { get; private set; }              // D-45
    public string? ReworkPolicyOverride { get; private set; }         // D-46
    public Guid? PriorityProfileId { get; private set; }             // D-47

    // --- Child entity kollekció: DailyPlan → DaySlot ---
    private readonly List<DaySlot> _daySlots = new();
    public IReadOnlyList<DaySlot> DaySlots => _daySlots.AsReadOnly();

    // --- FSM transition methodok ---

    /// <summary>Draft → Published. Takes profile snapshot, makes plan visible to operators.</summary>
    public void Publish(Guid profileSnapshotId)
    {
        GuardTransition(CuttingPlanStatus.Draft, CuttingPlanStatus.Published);
        ProfileSnapshotId = profileSnapshotId;
        Status = CuttingPlanStatus.Published;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Published → Frozen. Locks all DaySlots, finalizes nesting.</summary>
    public void Freeze()
    {
        GuardTransition(CuttingPlanStatus.Published, CuttingPlanStatus.Frozen);
        foreach (var slot in _daySlots)
            slot.Lock();
        Status = CuttingPlanStatus.Frozen;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Frozen → Closed. All jobs must be in terminal state.</summary>
    public void Close()
    {
        GuardTransition(CuttingPlanStatus.Frozen, CuttingPlanStatus.Closed);
        foreach (var slot in _daySlots)
            slot.CloseSlot();
        Status = CuttingPlanStatus.Closed;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Updates rework policy override (Draft only).</summary>
    public void SetReworkPolicyOverride(string? policyId)
    {
        if (Status != CuttingPlanStatus.Draft)
            throw new InvalidOperationException("ReworkPolicyOverride can only be set in Draft status.");
        ReworkPolicyOverride = policyId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Links to a PriorityProfile (Draft only).</summary>
    public void AssignProfile(Guid profileId)
    {
        if (Status != CuttingPlanStatus.Draft)
            throw new InvalidOperationException("Profile can only be assigned in Draft status.");
        PriorityProfileId = profileId;
        UpdatedAt = DateTime.UtcNow;
    }

    private void GuardTransition(CuttingPlanStatus from, CuttingPlanStatus to)
    {
        if (Status != from)
            throw new InvalidOperationException(
                $"Cannot transition from {Status} to {to}. Expected current status: {from}.");
    }

    // Factory: Create módosítva (PlanDays range 1..30, opcionális extension params)
    public static CuttingPlan Create(
        Guid tenantId, DateTime planDate, int planDays, string strategyId,
        Guid? partnerId = null,
        SourceChannel sourceChannel = SourceChannel.Direct)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId required.", nameof(tenantId));
        if (planDays < 1 || planDays > 30)  // D-48: 1..30 range (volt 7..90)
            throw new ArgumentException("PlanDays must be 1-30.", nameof(planDays));
        if (string.IsNullOrWhiteSpace(strategyId))
            throw new ArgumentException("StrategyId required.", nameof(strategyId));
        if (planDate.Date < DateTime.UtcNow.Date)
            throw new ArgumentException("PlanDate must be >= today.", nameof(planDate));

        var plan = new CuttingPlan
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PlanDate = DateTime.SpecifyKind(planDate.Date, DateTimeKind.Utc),
            PlanDays = planDays,
            Status = CuttingPlanStatus.Draft,
            StrategyId = strategyId,
            PartnerId = partnerId,
            SourceChannel = sourceChannel,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        for (int i = 0; i < planDays; i++)
            plan._daySlots.Add(DaySlot.Create(plan.Id, plan.PlanDate.AddDays(i)));

        return plan;
    }

    // TÖRÖLVE: UpdateStatus(string) — helyette: Publish(), Freeze(), Close()
}
```

**FONTOS változások a létező kódbázishoz képest:**
1. `UpdateStatus(string)` **TÖRÖLVE** — az `UpdateCuttingPlanStatusCommand` és handler is törlésre kerül
2. `DailyPlans` kollekció `DaySlots`-ra neveződik át (D-49)
3. `PlanDays` range 7..90 → 1..30 (D-48)
4. `Create()` factory kibővül opcionális `partnerId` és `sourceChannel` paraméterekkel

---

### D-43: PartnerId extension point

| Mező | Típus | Default | DB | Migráció |
|---|---|---|---|---|
| `PartnerId` | `Guid?` | `NULL` | `uuid NULL` | ADD COLUMN |

Használat: PartnerTier (v2) attribúciós mező. V1-ben mindig NULL.

---

### D-44: SourceChannel enum

```csharp
namespace SpaceOS.Modules.Cutting.Domain.Enums;

public enum SourceChannel
{
    Direct = 0,    // Tenant user via Portal
    FreeTier = 1,  // Anonymous workspace (v1.5)
    Partner = 2,   // B2B2C embedded (v2)
    Api = 3        // ERP integration (v3)
}
```

**Megjegyzés:** Ha a Contracts v1.3.0 (ARCH-001) már definiálja ezt a `Shared/` namespace-ben, a Cutting modul AZT használja, nem definiálja újra. Ellenőrizze a terminal a Contracts csomagot implementálás előtt.

| Mező | Típus | Default | DB | Migráció |
|---|---|---|---|---|
| `SourceChannel` | `int` (enum) | `0` (Direct) | `integer NOT NULL DEFAULT 0` | ADD COLUMN |

---

### D-45: ProfileSnapshotId

| Mező | Típus | Default | DB | Migráció |
|---|---|---|---|---|
| `ProfileSnapshotId` | `Guid?` | `NULL` | `uuid NULL` | ADD COLUMN |

Beállítódik: `Publish()` transition-kor. Utána immutable (Golden Rule #3).

---

### D-46: ReworkPolicyOverride

| Mező | Típus | Default | DB | Migráció |
|---|---|---|---|---|
| `ReworkPolicyOverride` | `string?` | `NULL` | `varchar(50) NULL` | ADD COLUMN |

Érték: `IReworkPolicy` implementáció azonosítója (pl. `"WarnAndApply"`, `"AutoReNest"`). Ha NULL, a PriorityProfile default policy-ja érvényes.

---

### D-47: PriorityProfileId (FK)

| Mező | Típus | Default | DB | Migráció |
|---|---|---|---|---|
| `PriorityProfileId` | `Guid?` | `NULL` | `uuid NULL REFERENCES PriorityProfiles(Id)` | ADD COLUMN + FK |

---

### D-48: PlanDays range változás

| Régi | Új | Indoklás |
|---|---|---|
| 7..90 | 1..30 | Growth Strategy Q8/Q13: N-day rolling, 1..14 default cap, 1..30 tenant-config max |

**Tenant config kulcs:** `cutting.plan.horizon_max_days` (default: 14). A `Create()` factory NEM olvassa a configot — a hívó Application handler felelős a validálásért.

---

### D-49: DailyPlan → DaySlot entitás (lokális FSM-mel)

**Döntés:** A `DaySlot` marad **entity** (nem value object), mert:
1. Saját lifecycle-ja van (Open→Locked→Closed)
2. A `CuttingJob` FK-val hivatkozik rá (`DaySlotId`)
3. Az EF Core change tracker követni tudja
4. Saját azonosítója van (`DaySlot.Id`)

```csharp
namespace SpaceOS.Modules.Cutting.Domain.Aggregates;

/// <summary>
/// Child entity of CuttingPlan. Represents one day's cutting work slot.
/// Local FSM: Open → Locked → Closed (independent of global plan status).
/// </summary>
public class DaySlot
{
    public Guid Id { get; private set; }
    public Guid CuttingPlanId { get; private set; }
    public DateTime Date { get; private set; }
    public decimal AvailableHours { get; private set; }  // renamed from AvailableCapacity
    public DaySlotStatus LocalState { get; private set; } = DaySlotStatus.Open;

    private readonly List<CuttingJob> _jobs = new();
    public IReadOnlyList<CuttingJob> Jobs => _jobs.AsReadOnly();

    private DaySlot() { }

    public static DaySlot Create(Guid cuttingPlanId, DateTime date, decimal availableHours = 8m)
        => new DaySlot
        {
            Id = Guid.NewGuid(),
            CuttingPlanId = cuttingPlanId,
            Date = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc),
            AvailableHours = availableHours,
            LocalState = DaySlotStatus.Open
        };

    public decimal AllocatedHours => _jobs.Sum(j => j.EstimatedTimeHours);
    public decimal UtilizationPercent =>
        AvailableHours > 0 ? Math.Round(AllocatedHours / AvailableHours * 100m, 2) : 0;

    /// <summary>Open → Locked. Manual operator action or cron auto-lock. Idempotent.</summary>
    public void Lock()
    {
        if (LocalState == DaySlotStatus.Closed)
            throw new InvalidOperationException($"DaySlot {Id} is Closed and cannot be locked.");
        LocalState = DaySlotStatus.Locked; // idempotent (Locked→Locked is fine)
    }

    /// <summary>Locked → Closed. Called when all jobs are completed. Idempotent.</summary>
    public void CloseSlot()
    {
        if (LocalState == DaySlotStatus.Closed) return; // idempotent
        LocalState = DaySlotStatus.Closed;
    }

    public void AddJob(CuttingJob job)
    {
        if (LocalState != DaySlotStatus.Open)
            throw new InvalidOperationException($"DaySlot {Id} is {LocalState}. Cannot add jobs.");
        if (job.DaySlotId != Id)
            throw new InvalidOperationException("Job belongs to a different DaySlot.");
        _jobs.Add(job);
    }
}
```

---

### D-50: `DaySlotStatus` enum

```csharp
namespace SpaceOS.Modules.Cutting.Domain.Enums;

/// <summary>Local FSM for a single day slot within a CuttingPlan.</summary>
public enum DaySlotStatus
{
    /// <summary>Accepting jobs. Editable.</summary>
    Open = 0,

    /// <summary>Day's work committed. No new jobs. Nesting finalized.</summary>
    Locked = 1,

    /// <summary>All jobs completed. Archived.</summary>
    Closed = 2
}
```

---

### D-51: `CuttingJob` módosítások

```csharp
public class CuttingJob
{
    // --- MÓDOSÍTOTT: DailyPlanId → DaySlotId ---
    public Guid DaySlotId { get; private set; }  // volt DailyPlanId

    // --- ÚJ ---
    public decimal? PriorityScore { get; private set; }  // D-57: PriorityScoringService output

    // --- Létező mezők változatlanul ---
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public DateTime ScheduledDate { get; private set; }
    public string Priority { get; private set; } = "Normal";
    public decimal EstimatedTimeHours { get; private set; }
    public string Status { get; private set; } = "Pending";  // marad string v1-ben (OQ-7)

    public static CuttingJob Create(
        Guid daySlotId, Guid orderId, DateTime scheduledDate,
        string priority, decimal estimatedTimeHours)
    { /* ... validation unchanged ... */ }

    public void SetPriorityScore(decimal score) { PriorityScore = score; }

    // MarkAsCut() — változatlan
}
```

**Migration impact:** `CuttingJobs` táblában a `DailyPlanId` oszlop átnevezendő `DaySlotId`-re. A `DailyPlans` tábla `DaySlots`-ra neveződik át.

---

### D-52: `PriorityProfile` entitás

```csharp
namespace SpaceOS.Modules.Cutting.Domain.Aggregates;

/// <summary>
/// Tenant-level configuration tying together a scheduling strategy,
/// capacity model, and rework policy. Reusable across CuttingPlans.
/// </summary>
public class PriorityProfile
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string StrategyId { get; private set; } = string.Empty;      // "maxcut-v1"|"fifo"|"priority"
    public string CapacityModelId { get; private set; } = string.Empty; // "area"|"machine-hour"|"hybrid"
    public string ReworkPolicyId { get; private set; } = string.Empty;  // "warn-and-apply"|"auto-renest"
    public bool IsPreset { get; private set; }                          // true = system preset, not deletable
    public decimal BaseWeight { get; private set; } = 1.0m;            // PriorityScoringService multiplier
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private PriorityProfile() { }

    public static PriorityProfile Create(
        Guid tenantId, string name, string strategyId,
        string capacityModelId, string reworkPolicyId,
        bool isPreset = false, decimal baseWeight = 1.0m)
    {
        return new PriorityProfile
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            StrategyId = strategyId,
            CapacityModelId = capacityModelId,
            ReworkPolicyId = reworkPolicyId,
            IsPreset = isPreset,
            BaseWeight = baseWeight,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
```

---

### D-53: `ProfilePresets.cs` seed data

```csharp
namespace SpaceOS.Modules.Cutting.Domain.Profiles;

public static class ProfilePresets
{
    /// <summary>Manufacturer preset: MaxCut strategy, AreaCapacity, WarnAndApply rework.</summary>
    public static PriorityProfile Manufacturer(Guid tenantId) =>
        PriorityProfile.Create(
            tenantId, "Manufacturer", "maxcut-v1", "area", "warn-and-apply",
            isPreset: true, baseWeight: 1.0m);

    /// <summary>PanelCutter preset: FIFO strategy, AreaCapacity, WarnAndApply rework.</summary>
    public static PriorityProfile PanelCutter(Guid tenantId) =>
        PriorityProfile.Create(
            tenantId, "PanelCutter", "fifo", "area", "warn-and-apply",
            isPreset: true, baseWeight: 1.0m);
}
```

**Seed mechanizmus:** A migration NEM seed-el. A `CreateCuttingPlanCommandHandler`-ben `EnsurePresetsExist()` ellenőrzés: ha a tenant-nek nincs egyetlen `PriorityProfile` sem, automatikusan létrehozza a 2 presetet.

---

### D-54: `ICapacityModel` domain service

```csharp
namespace SpaceOS.Modules.Cutting.Domain.Services.Capacity;

public interface ICapacityModel
{
    string ModelId { get; }
    CapacityBudget ComputeBudget(IEnumerable<DaySlot> daySlots, PriorityProfile profile);
}

public sealed record CapacityBudget(
    decimal TotalHours,
    decimal TotalAreaM2,
    decimal UtilizationTarget);
```

```csharp
/// <summary>v1: budget = sum(DaySlot.AvailableHours * area conversion factor 2.5 m²/h)</summary>
public sealed class AreaCapacityModel : ICapacityModel
{
    public string ModelId => "area";
    private const decimal DefaultAreaPerHour = 2.5m;  // OQ-4: Doorstar-specifikus, tenant-config-ból felülírható

    public CapacityBudget ComputeBudget(IEnumerable<DaySlot> daySlots, PriorityProfile profile)
    {
        var totalHours = daySlots.Sum(s => s.AvailableHours);
        return new CapacityBudget(totalHours, totalHours * DefaultAreaPerHour, UtilizationTarget: 91m);
    }
}

// V2 placeholder-ek:
public sealed class MachineHourCapacityModel : ICapacityModel
{
    public string ModelId => "machine-hour";
    public CapacityBudget ComputeBudget(IEnumerable<DaySlot> daySlots, PriorityProfile profile)
        => throw new NotImplementedException("MachineHourCapacityModel is a v2 placeholder.");
}

public sealed class HybridCapacityModel : ICapacityModel
{
    public string ModelId => "hybrid";
    public CapacityBudget ComputeBudget(IEnumerable<DaySlot> daySlots, PriorityProfile profile)
        => throw new NotImplementedException("HybridCapacityModel is a v2 placeholder.");
}
```

**DI regisztráció:** `ICapacityModelFactory` pattern (hasonlóan az `IPlanningStrategyFactory`-hoz):

```csharp
public interface ICapacityModelFactory { ICapacityModel Resolve(string modelId); }
```

---

### D-55: `IReworkPolicy` domain service

```csharp
namespace SpaceOS.Modules.Cutting.Domain.Services.Rework;

public interface IReworkPolicy
{
    string PolicyId { get; }
    Task<ReworkResult> EvaluateAsync(
        CuttingPlan plan,
        IEnumerable<CuttingJob> changedJobs,
        CancellationToken ct);
}

public sealed record ReworkResult(
    bool ReworkNeeded,
    ReworkAction Action,
    string? Message,
    IReadOnlyList<Guid>? AffectedJobIds);

public enum ReworkAction { None = 0, WarnAndApply = 1, AutoReNest = 2, Block = 3 }
```

```csharp
/// <summary>v1 default: warns operator, applies changes anyway, logs rework event.</summary>
public sealed class WarnAndApplyPolicy : IReworkPolicy
{
    public string PolicyId => "warn-and-apply";

    public Task<ReworkResult> EvaluateAsync(CuttingPlan plan, IEnumerable<CuttingJob> changedJobs, CancellationToken ct)
    {
        var jobs = changedJobs.ToList();
        if (!jobs.Any())
            return Task.FromResult(new ReworkResult(false, ReworkAction.None, null, null));

        return Task.FromResult(new ReworkResult(
            ReworkNeeded: true,
            Action: ReworkAction.WarnAndApply,
            Message: $"Rework applied: {jobs.Count} job(s) changed. Operator notified.",
            AffectedJobIds: jobs.Select(j => j.Id).ToList()));
    }
}

// V2 placeholder-ek:
public sealed class AutoReNestPolicy : IReworkPolicy
{
    public string PolicyId => "auto-renest";
    public Task<ReworkResult> EvaluateAsync(...) => throw new NotImplementedException("v2 placeholder.");
}

public sealed class LockAfterPublishedPolicy : IReworkPolicy
{
    public string PolicyId => "lock-after-published";
    public Task<ReworkResult> EvaluateAsync(...) => throw new NotImplementedException("v2 placeholder.");
}
```

**DI regisztráció:** `IReworkPolicyFactory` (same pattern):

```csharp
public interface IReworkPolicyFactory { IReworkPolicy Resolve(string policyId); }
```

---

### D-56: `PanelReservation` aggregate

```csharp
namespace SpaceOS.Modules.Cutting.Domain.Aggregates;

/// <summary>
/// Local reference to a soft stock reservation held in Inventory module.
/// CorrelationId = CuttingPlan.Id. TTL default: 24h (OQ-5: fix v1-ben).
/// NOT a duplicate of Inventory's Reservation — only stores the reference ID.
/// </summary>
public class PanelReservation
{
    public Guid Id { get; private set; }
    public Guid CuttingPlanId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid InventoryReservationId { get; private set; }
    public DateTime ReservedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public PanelReservationStatus Status { get; private set; }

    private PanelReservation() { }

    public static PanelReservation Create(
        Guid cuttingPlanId, Guid tenantId, Guid inventoryReservationId, TimeSpan ttl)
        => new PanelReservation
        {
            Id = Guid.NewGuid(),
            CuttingPlanId = cuttingPlanId,
            TenantId = tenantId,
            InventoryReservationId = inventoryReservationId,
            ReservedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(ttl),
            Status = PanelReservationStatus.Active
        };

    public void Release()
    {
        if (Status != PanelReservationStatus.Active) return; // idempotent
        Status = PanelReservationStatus.Released;
    }

    public void MarkConsumed()
    {
        if (Status != PanelReservationStatus.Active)
            throw new InvalidOperationException($"Cannot consume reservation in {Status} state.");
        Status = PanelReservationStatus.Consumed;
    }
}

public enum PanelReservationStatus { Active = 0, Released = 1, Consumed = 2, Expired = 3 }
```

**Fontos:** A `PanelReservation` NEM duplikálja az Inventory modul Reservation aggregate-jét. A Cutting modul csak az `InventoryReservationId`-t tárolja:
1. Idempotency: ne hívja újra a `ReserveAsync`-et ugyanarra a plan-re
2. `Close()` transition-kor tudja mit kell `ReleaseReservationAsync`-kel felszabadítani
3. `Freeze()` transition-kor a rezervációt `MarkConsumed()`-ra állítja

---

### D-57: `PriorityScoringService`

```csharp
namespace SpaceOS.Modules.Cutting.Domain.Services;

/// <summary>Profile-aware priority scoring. Score = profile.BaseWeight × PriorityRank + deadline_factor</summary>
public sealed class PriorityScoringService
{
    private static readonly Dictionary<string, int> PriorityRank = new()
    {
        ["Urgent"] = 1, ["High"] = 2, ["Normal"] = 3, ["Low"] = 4
    };

    public IReadOnlyList<CuttingJob> AssignScores(
        PriorityProfile profile, IEnumerable<CuttingJob> jobs, DateTime planDate)
    {
        var result = new List<CuttingJob>();
        foreach (var job in jobs)
        {
            var rank = PriorityRank.GetValueOrDefault(job.Priority, 99);
            var daysUntilDue = Math.Max(0, (job.ScheduledDate - planDate).TotalDays);
            var deadlineFactor = daysUntilDue <= 1 ? -2m : daysUntilDue <= 3 ? -1m : 0m;
            job.SetPriorityScore(profile.BaseWeight * rank + deadlineFactor);
            result.Add(job);
        }
        return result;
    }
}
```

---

### D-58: `OffcutFirstStrategy`

```csharp
namespace SpaceOS.Modules.Cutting.Domain.Services;

/// <summary>
/// Inventory-aware scheduling helper. Checks offcuts before full panels.
/// Delegates to IInventoryProvider. Tenant config: cutting.offcut.min_width_mm (default 400).
/// </summary>
public sealed class OffcutFirstStrategy
{
    private readonly IInventoryProvider _inventory;
    private readonly decimal _minWidthMm;
    private readonly decimal _minHeightMm;

    public OffcutFirstStrategy(
        IInventoryProvider inventory,
        decimal minWidthMm = 400m,   // Q6: tenant-config
        decimal minHeightMm = 400m)
    {
        _inventory = inventory;
        _minWidthMm = minWidthMm;
        _minHeightMm = minHeightMm;
    }

    public async Task<IReadOnlyList<StockItemDto>> GetAvailableOffcutsAsync(
        string materialCode, CancellationToken ct)
    {
        var result = await _inventory.GetUsableOffcutsAsync(
            materialCode, _minWidthMm, _minHeightMm, ct);
        return result.IsSuccess ? result.Value : Array.Empty<StockItemDto>();
    }
}
```

**Megjegyzés:** `OffcutFirstStrategy` NEM `IPlanningStrategy` implementáció. Segítő service, amit a `PublishCuttingPlanCommandHandler` hív meg a scheduling előtt.

---

### D-59: `IPlanningStrategy` típuscsere

```csharp
public interface IPlanningStrategy
{
    Task<IEnumerable<CuttingJob>> ScheduleJobsAsync(
        IEnumerable<CuttingJob> unscheduledJobs,
        IEnumerable<DaySlot> daySlots,      // volt: IEnumerable<DailyPlan>
        CancellationToken ct);

    decimal CalculateYield(
        CuttingPlan plan,
        IEnumerable<DaySlot> daySlots);     // volt: IEnumerable<DailyPlan>

    string GetLabel();
    Task<PlanningValidationResult> ValidateAsync(CuttingPlan plan, CancellationToken ct);
}
```

Mind a 4 implementáció (MaxCut, FIFO, Priority, Custom) frissítendő a `DaySlot` típusra. A logika nem változik.

---

### D-60: `ICuttingRepository` bővítés

Új repository metódusok (a meglévők változatlanok):

```csharp
// ÚJ
Task<DaySlot?> GetDaySlotTrackedAsync(Guid daySlotId, CancellationToken ct = default);
Task<IReadOnlyList<DaySlot>> GetUnlockedPastDaySlotsAsync(DateTime beforeDate, CancellationToken ct = default);
Task AddPriorityProfileAsync(PriorityProfile profile, CancellationToken ct = default);
Task<PriorityProfile?> GetPriorityProfileByIdAsync(Guid profileId, CancellationToken ct = default);
Task<IReadOnlyList<PriorityProfile>> GetProfilesByTenantAsync(CancellationToken ct = default);
Task AddPanelReservationAsync(PanelReservation reservation, CancellationToken ct = default);
Task<PanelReservation?> GetReservationByCuttingPlanIdAsync(Guid cuttingPlanId, CancellationToken ct = default);
```

---

## 4. Application layer kiegészítések

### 4.1 Új command handlerek

| Handler | Trigger | Bemenet | Kimenet | Domain hatások |
|---|---|---|---|---|
| `PublishCuttingPlanCommandHandler` | PUT endpoint | `PlanId` | `Result<Unit>` | `plan.Publish(snapshotId)`, `ReserveAsync()` Inventory-n |
| `FreezeCuttingPlanCommandHandler` | PUT endpoint | `PlanId` | `Result<Unit>` | `plan.Freeze()`, reservation `MarkConsumed()` |
| `CloseCuttingPlanCommandHandler` | PUT endpoint | `PlanId` | `Result<Unit>` | `plan.Close()`, `ReleaseReservationAsync()` ha nem consumed |
| `LockDaySlotCommandHandler` | PUT endpoint + cron | `DaySlotId` | `Result<Unit>` | `daySlot.Lock()` |
| `AssignProfileCommandHandler` | PUT endpoint | `PlanId, ProfileId` | `Result<Unit>` | `plan.AssignProfile(profileId)` |
| `ReNestCommandHandler` | POST endpoint | `PlanId` | `Result<ReworkResult>` | `IReworkPolicy.EvaluateAsync` + opcionális `INestingStrategy` újrafuttatás |

### 4.2 Törölt handler

| Handler | Ok |
|---|---|
| `UpdateCuttingPlanStatusCommandHandler` | Helyette: Publish/Freeze/Close külön handlerek. A generikus status-setter az FSM-et kerüli meg. |

### 4.3 Módosított handler

| Handler | Változás |
|---|---|
| `CreateCuttingPlanCommandHandler` | `PlanDays` validáció 1..30, opcionális `partnerId`/`sourceChannel` paraméter, opcionális `profileId` assignment, `EnsurePresetsExist()` hívás |

### 4.4 Új API endpointok

A `CuttingPlanningEndpoints.cs` bővül, a générikus PUT endpoint törlődik:

```csharp
// ÚJ FSM transition endpointok
group.MapPut("/{planId:guid}/publish",  PublishCuttingPlan);
group.MapPut("/{planId:guid}/freeze",   FreezeCuttingPlan);
group.MapPut("/{planId:guid}/close",    CloseCuttingPlan);

// DaySlot management
group.MapPut("/dayslots/{daySlotId:guid}/lock", LockDaySlot);

// Profile management
group.MapPut("/{planId:guid}/profile", AssignProfile);
group.MapGet("/profiles",              GetProfiles);

// Rework
group.MapPost("/{planId:guid}/renest", ReNest);

// TÖRÖLVE: group.MapPut("/{planId:guid}", UpdateCuttingPlan)
```

### 4.5 Nesting az FSM lifecycle-ban

```
Draft ──────────────────── Published ──────────── Frozen ──────── Closed
  │                           │                     │               │
  │ IPlanningStrategy         │ ★ NESTING TRIGGER   │ Nesting FINAL │
  │ .ScheduleJobsAsync()      │ INestingStrategy    │ Immutable     │
  │ (kapacitás scheduling)    │ .ComputeAsync()     │               │
  │                           │ IInventoryProvider  │               │
  │                           │ .ReserveAsync()     │               │
```

**Publish transition-kor** triggerelődik az `INestingStrategy` (CUTTING-029 NuGet):
1. `PublishCuttingPlanCommandHandler` meghívja `INestingStrategy.ComputeAsync()` minden DaySlot-ra
2. Nesting eredmények tárolódnak
3. `IInventoryProvider.ReserveAsync()` meghívódik a kiszámolt panel-szükséglettel
4. `ProfileSnapshotId` beállítódik

### 4.6 DaySlot auto-lock cron worker

```csharp
namespace SpaceOS.Modules.Cutting.Infrastructure.Workers;

/// <summary>
/// Background worker that auto-locks DaySlots whose date < today.
/// Runs every 15 minutes via PeriodicTimer.
/// </summary>
public sealed class DaySlotAutoLockWorker : BackgroundService
{
    // IServiceScopeFactory → scope → ICuttingRepository
    // GetUnlockedPastDaySlotsAsync(DateTime.UtcNow.Date)
    // foreach: daySlot.Lock()
    // SaveChangesAsync
}
```

**Miért BackgroundService és nem Hangfire?** Egyszerűség — a SpaceOS nem használ Hangfire-t.

---

## 5. DB migrációs terv

### 5.1 Migráció neve

```
20260420000001_CuttingPlanningV4
```

### 5.2 Tábla változások

#### Módosított táblák

**`CuttingPlans` tábla:**

| Oszlop | Változás | Típus |
|---|---|---|
| `Status` | `varchar(20)` → `integer` (enum) | CASE mapping: Approved→1, InProgress→2 |
| `PartnerId` | ÚJ | `uuid NULL` |
| `SourceChannel` | ÚJ | `integer NOT NULL DEFAULT 0` |
| `ProfileSnapshotId` | ÚJ | `uuid NULL` |
| `ReworkPolicyOverride` | ÚJ | `varchar(50) NULL` |
| `PriorityProfileId` | ÚJ | `uuid NULL REFERENCES PriorityProfiles(Id)` |

**`DailyPlans` tábla → `DaySlots`:**

| Változás | |
|---|---|
| Tábla neve | RENAME: `DailyPlans` → `DaySlots` |
| `AvailableCapacity` | RENAME → `AvailableHours` |
| `LocalState` | ÚJ: `integer NOT NULL DEFAULT 0` |

**`CuttingJobs` tábla:**

| Oszlop | Változás |
|---|---|
| `DailyPlanId` | RENAME → `DaySlotId` (FK átirányítás) |
| `PriorityScore` | ÚJ: `numeric(8,2) NULL` |

#### Új táblák

**`PriorityProfiles`:**

```sql
CREATE TABLE spaceos_cutting."PriorityProfiles" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "TenantId" uuid NOT NULL,
    "Name" varchar(100) NOT NULL,
    "StrategyId" varchar(50) NOT NULL,
    "CapacityModelId" varchar(50) NOT NULL,
    "ReworkPolicyId" varchar(50) NOT NULL,
    "IsPreset" boolean NOT NULL DEFAULT false,
    "BaseWeight" numeric(8,2) NOT NULL DEFAULT 1.0,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL
);

ALTER TABLE spaceos_cutting."PriorityProfiles" ENABLE ROW LEVEL SECURITY;
ALTER TABLE spaceos_cutting."PriorityProfiles" FORCE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON spaceos_cutting."PriorityProfiles"
    USING ("TenantId" = current_setting('app.current_tenant_id')::uuid);
CREATE INDEX "IX_PriorityProfiles_TenantId" ON spaceos_cutting."PriorityProfiles" ("TenantId");
```

**`PanelReservations`:**

```sql
CREATE TABLE spaceos_cutting."PanelReservations" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "CuttingPlanId" uuid NOT NULL REFERENCES spaceos_cutting."CuttingPlans"("Id"),
    "TenantId" uuid NOT NULL,
    "InventoryReservationId" uuid NOT NULL,
    "ReservedAt" timestamp with time zone NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "Status" integer NOT NULL DEFAULT 0
);

ALTER TABLE spaceos_cutting."PanelReservations" ENABLE ROW LEVEL SECURITY;
ALTER TABLE spaceos_cutting."PanelReservations" FORCE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON spaceos_cutting."PanelReservations"
    USING ("TenantId" = current_setting('app.current_tenant_id')::uuid);
CREATE UNIQUE INDEX "IX_PanelReservations_CuttingPlanId_Active"
    ON spaceos_cutting."PanelReservations" ("CuttingPlanId")
    WHERE "Status" = 0;  -- csak egy aktív rezerváció per plan
```

### 5.3 DEFAULT PRIVILEGES

```sql
GRANT ALL ON TABLE spaceos_cutting."PriorityProfiles" TO spaceos_cutting_user;
GRANT ALL ON TABLE spaceos_cutting."PanelReservations" TO spaceos_cutting_user;
```

---

## 6. Implementációs sorrend (Root terminal task-ok)

### 6.1 Task bontás (12 task, 20 nap)

| Task ID | Tartalom | Függőség | Session | Nap |
|---|---|---|---|---|
| **CUTTING-030** | CuttingPlan FSM redesign + DaySlot entity + Strategy típuscsere | — | B | 3.0 |
| **CUTTING-031** | Extension points + PlanDays range + SourceChannel | CUTTING-030 | B | 1.5 |
| **CUTTING-032** | PanelReservation + IInventoryProvider.ReserveAsync integráció | CUTTING-030 | B | 1.5 |
| **CUTTING-033** | Application handlerek: Publish/Freeze/Close + API endpointok | CUTTING-030 | B | 2.5 |
| **CUTTING-034** | DB migration `20260420000001_CuttingPlanningV4` + EF config | CUTTING-030..033 | B | 1.5 |
| **CUTTING-035** | PriorityProfile entity + ProfilePresets + seed logika | — | C | 2.0 |
| **CUTTING-036** | ICapacityModel + AreaCapacityModel + v2 placeholder-ek | CUTTING-035 | C | 1.0 |
| **CUTTING-037** | IReworkPolicy + WarnAndApplyPolicy + v2 placeholder-ek | CUTTING-035 | C | 1.0 |
| **CUTTING-038** | PriorityScoringService + OffcutFirstStrategy | CUTTING-035, 036 | C | 1.5 |
| **CUTTING-039** | LockDaySlotHandler + DaySlotAutoLockWorker | CUTTING-030 | C | 1.0 |
| **CUTTING-040** | ReNestCommandHandler + AssignProfileCommandHandler | CUTTING-035, 037 | C | 1.0 |
| **CUTTING-041** | Integrált tesztek (Session B+C teljes lefedettség) | CUTTING-030..040 | C | 2.5 |
| **Session B subtotal** | | | | **10.0** |
| **Session C subtotal** | | | | **10.0** |
| **TOTAL** | | | | **20.0 nap** |

### 6.2 CUTTING-030 részletes scope (első task)

- `CuttingPlanStatus` enum létrehozása (D-41)
- `DaySlotStatus` enum létrehozása (D-50)
- `CuttingPlan.cs` átírása: string Status → enum, `UpdateStatus()` törlése, `Publish()/Freeze()/Close()` hozzáadása (D-42)
- `DailyPlan.cs` → `DaySlot.cs` átalakítva lokális FSM-mel (D-49)
- `CuttingJob.cs`: `DailyPlanId` → `DaySlotId` (D-51)
- `IPlanningStrategy` + 4 impl: `DailyPlan` → `DaySlot` típus csere (D-59)
- **Létező tesztek frissítése**: minden string `"Approved"`/`"InProgress"` assert → enum értékekre; `UpdateStatus()` hívások → `Publish()/Freeze()` hívásokra
- DoD: `dotnet test` pass, minden régi `UpdateStatus` teszt átírva FSM tesztekre

---

## 7. Nyitott kérdések Gábornak

| # | Kérdés | Kontextus | Javaslatom |
|---|---|---|---|
| **OQ-1** | A `PlanDays` range változás (7..90 → 1..30) van-e production-ban meglévő 14+ napos CuttingPlan? | Ellenőrzés: `SELECT MAX("PlanDays") FROM spaceos_cutting."CuttingPlans"` | Valószínűleg nincs (fresh Doorstar pilot). |
| **OQ-2** | A `DailyPlans` tábla átnevezése `DaySlots`-ra breaking change az Orchestrator BFF-ben? | Az Orch proxy REST API-n keresztül kommunikál, nem DB szinten. | Nem breaking. |
| **OQ-3** | PriorityProfile preset seed: mikor történjen? | Opciók: A) Migration-ben. B) Handler-ben első CuttingPlan létrehozásakor. C) Tenant onboarding eventkor. | **B) Handler-ben** — nincs Kernel függőség. |
| **OQ-4** | `AreaCapacityModel.DefaultAreaPerHour = 2.5m` helyes-e? (8h = 20 m²/nap) | Doorstar referencia szükséges. | Tenant-config-ból felülírható → ask Doorstar. |
| **OQ-5** | ReserveAsync TTL: 24h fix, vagy tenant config? | Growth Strategy Q1: "TTL 24h". | **24h fix v1-ben**, tenant config v2-ben. |
| **OQ-6** | A `PUT /{planId}` (UpdateCuttingPlan) endpoint törölhető? | Portal esetleg használja-e? | **Törölni** — Portal még nem hasznalja produktivan. |
| **OQ-7** | `CuttingJob.Status` maradjon string, vagy enum-ra váltson? | A CuttingJob FSM komplex (5 állapot), az átírás nem sürgős. | **V1-ben maradjon string.** Külön sprint a CuttingJob FSM enum-osítására. |

---

## 8. Kockázati mátrix

| Kockázat | Súlyosság | Valószínűség | Migráció |
|---|---|---|---|
| Migration felboritja a deployed adatokat | Magas | Alacsony | VPS-en DB backup migration előtt. Down() tesztelve. Testcontainers integration teszt. |
| IPlanningStrategy típuscsere törli a 4 implementáció tesztjeit | Közepes | Magas | Ez szándékos. CUTTING-030-ban frissülnek a tesztek. |
| IInventoryProvider.ReserveAsync nem elérhető (Inventory service leáll) | Magas | Alacsony | `PublishCuttingPlanCommandHandler` kezeli: ha ReserveAsync fail → Publish MEGHIÚSUL, plan Draft marad. |
| DaySlotAutoLockWorker race condition | Közepes | Alacsony | `Lock()` metódus idempotens. Többszöri futtatás nem okoz problémát. |

---

## 9. ADR-ek (létrehozandó)

| ADR | Cím | Döntés |
|---|---|---|
| **ADR-034** | CuttingPlanStatus string → enum migráció | Opció C: új enum + CASE migráció |
| **ADR-035** | DailyPlan → DaySlot entity (nem value object) | Entity marad FK-k és lifecycle miatt |
| **ADR-036** | `UpdateStatus()` törlése, explicit FSM transition methodok | Publish/Freeze/Close különböző methodok, nincs generikus setter |
| **ADR-037** | PanelReservation helyi referencia (nem duplikálás) | Cutting csak `InventoryReservationId`-t tárol |

---

## 10. Érintett fájlok (reference)

| Fájl | Változás |
|---|---|
| `Domain/Aggregates/CuttingPlan.cs` | Enum FSM, új mezők, `UpdateStatus()` törölve |
| `Domain/Aggregates/DailyPlan.cs` | → `DaySlot.cs` átnevezve, lokális FSM hozzáadva |
| `Domain/Aggregates/CuttingJob.cs` | `DailyPlanId` → `DaySlotId`, `PriorityScore` mező |
| `Application/Strategies/IPlanningStrategy.cs` | `DailyPlan` → `DaySlot` típuscsere |
| `Application/Strategies/MaxCutStrategy.cs` | Típuscsere |
| `Application/Strategies/FIFOStrategy.cs` | Típuscsere |
| `Application/Strategies/PriorityStrategy.cs` | Típuscsere |
| `Application/Strategies/CustomStrategy.cs` | Típuscsere |
| `Application/Commands/UpdateCuttingPlanStatus/` | **TÖRÖLVE** |
| `Application/Commands/CreateCuttingPlan/CreateCuttingPlanCommandHandler.cs` | PlanDays 1..30, extension params |
| `Api/Endpoints/CuttingPlanningEndpoints.cs` | Új FSM endpointok, generikus PUT törölve |
| `Infrastructure/Migrations/20260420000001_CuttingPlanningV4.cs` | ÚJ |
| `Infrastructure/Persistence/Configurations/CuttingPlanConfiguration.cs` | Enum mapping, új oszlopok |
| `Infrastructure/Workers/DaySlotAutoLockWorker.cs` | ÚJ |

---

*SpaceOS Cutting Planning Architecture v4.0 — 2026-04-20*
*Sessions B+C implementációs spec — 20 nap, 12 terminal task (CUTTING-030..041)*
