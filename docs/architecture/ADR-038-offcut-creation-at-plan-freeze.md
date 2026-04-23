# ADR-038 — Offcut Creation at Plan Freeze

> **Status:** APPROVED — 2026-04-20
> **Architect:** Gábor (Founder) · **Drafted by:** Architect (Claude.ai Desktop) · **Validated by:** Architect (Claude Code, codebase review)
> **Scope:** 3 task: NESTING-001 (NuGet 1.1.0) + INVENTORY-015 (batch endpoint) + CUTTING-042 (handler)
> **Supersedes:** CUTTING-028 offcut stub (WidthMm=0 documented limitation)
> **Related:** `SpaceOS_Q3_Planning_Brief_v1.md` Téma 2 · ARCH-003 (IInventoryCuttingAdapter)
> **Implementation target:** Session C (CUTTING-041) lezárása után

---

## 1. Kontextus

A Cutting → Inventory offcut loop nem zár be. Amikor egy `CuttingJob` befejeződik, az `MarkAsCut()` tüzel, de az Inventory `WidthMm=0, HeightMm=0` méretű Offcut rekordot kap. Az anyaghatékonyság-kimutatás nem működik.

**Gyökérok:** A hulladék téglalapok dimenziói a nesting algoritmusban keletkeznek, de a `PanelAssignment` modell csak összesített `WasteAreaMm2` skalárral rendelkezik — dimenzionált téglalapok listája nincs.

### Codebase állapot (2026-04-20 validálva)

| Komponens | Státusz | Megjegyzés |
|---|---|---|
| `CuttingPlan.Freeze()` FSM átmenet | ✅ DONE | De **nem emel domain eventet** — gap |
| `CuttingPlanFrozen` domain event | ❌ HIÁNYZIK | Szükséges az event handler mintához |
| `PanelAssignment.WastePieces[]` | ❌ HIÁNYZIK | Csak `WasteAreaMm2` skalár van |
| `PlanNestingSnapshot` tábla/repo | ❌ HIÁNYZIK | Freeze-kor kell a nesting adat |
| Inventory `/api/inventory/offcuts/batch` | ❌ HIÁNYZIK | Single-offcut endpoint van, batch nincs |
| `IInventoryCuttingAdapter.RegisterOffcutsAsync` | ❌ HIÁNYZIK | Cutting-specifikus Inventory adapter |

---

## 2. Döntések

| Kérdés | Döntés | Indok |
|---|---|---|
| **Mikor keletkezik az Offcut?** | **Plan Freeze időpontban**, batch-ben | A tervezett hulladék Freeze-kor véglegesül — ez az igazságforrás |
| **Adat forrása** | `PlanNestingSnapshot` tábla | Freeze gyors marad; snapshot a nesting futásakor keletkezik |
| **WastePieces tárolás** | Nesting NuGet 1.1.0 — `WastePiece` model + `PanelAssignment.WastePieces[]` | Guillotine algoritmus természetéből adódik |
| **Inventory hívás** | `IInventoryCuttingAdapter.RegisterOffcutsAsync()` → `POST /api/inventory/offcuts/batch` | Cutting-specifikus, nem shared NuGet contract (ARCH-003 döntés alapján) |
| **Failure semantics** | Best-effort + idempotency key (`plan-{planId}`) + 3x exponential retry | Plan Frozen marad fail esetén is; audit log + operátor alert |
| **Offcut küszöb** | `cutting.offcut.min_width_mm` + `min_height_mm` (default 400mm) | Tenant konfig, küszöb alatti waste → nem regisztrált |
| **`IInventoryReservationAdapter` neve** | Átnevezve: `IInventoryCuttingAdapter` | Reservation + offcut műveletek együtt; NuGet IInventoryProvider váltja a reservation részt (ARCH-003) |

### Elutasított alternatívák

| Alternatíva | Miért nem |
|---|---|
| On-demand nesting újraszámítás Freeze-kor | Doorstar esetén OK (~100-300ms), de snapshot skálázhatóbb és auditálható |
| `MarkAsCut()`-kor Offcut regisztráció | Csak a tényleges offcutot rögzíti — a tervezett vs. tényleges discrepancy külön ADR (ADR-039 jelölt) |
| `WastePieces` csak skalárból becsülve | Pontatlan, nem alkalmas offcut reuse-ra |

---

## 3. Architektúra

### 3.1 Event flow

```
[Cutting] CuttingPlan.Freeze()
    │  Status: Published → Frozen
    │  RaiseDomainEvent(new CuttingPlanFrozen(planId, tenantId, frozenAt))  ← ÚJ
    ▼
[Cutting] FreezePlanCommandHandler
    │  plan.Freeze() → Result.Success
    │  saves plan
    ▼
[Cutting] DomainEventDispatcher
    ▼
[Cutting] RegisterOffcutsOnPlanFrozenHandler   ← ÚJ handler
    │  loads PlanNestingSnapshot (by planId)
    │  filters WastePieces by tenant threshold
    │  builds OffcutBatchRequest
    ▼
[Cutting] IInventoryCuttingAdapter.RegisterOffcutsAsync()  ← ÚJ metódus
    │  HTTP POST /api/inventory/offcuts/batch
    │  Idempotency-Key: plan-{planId}
    │  retry: 3x exponential (100ms → 500ms → 2500ms)
    ▼
[Inventory] POST /api/inventory/offcuts/batch   ← ÚJ endpoint
    │  upsert by (tenantId, sourceType=CuttingPlan, sourceId=planId)
    │  visszaad: IReadOnlyList<OffcutId>
```

### 3.2 Mikor keletkezik a `PlanNestingSnapshot`?

A snapshot a nesting futásakor jön létre — **nem Freeze-kor**. Ez az egyetlen hely ahol a `NestingResult` teljesen ismert.

```
[Cutting] GetNestingResultQueryHandler
    │  futtatja a INestingStrategy-t
    │  menti NestingResult-ot
    │  ÚJ: menti PlanNestingSnapshot-ot is (planId + WastePieces per panel)
```

```csharp
// Új entity — Cutting Infrastructure
public class PlanNestingSnapshot : Entity<Guid>
{
    public Guid PlanId { get; private set; }
    public Guid TenantId { get; private set; }
    public DateTime SnapshotAt { get; private set; }
    // JSON tömb: PanelId, MaterialCode, Thickness, WastePieces[]
    public string WastePiecesJson { get; private set; } = string.Empty;

    public static PlanNestingSnapshot Create(
        Guid planId, Guid tenantId,
        IReadOnlyList<PanelWasteRecord> wastePieces) { ... }
}

public sealed record PanelWasteRecord(
    string PanelId,
    string MaterialCode,
    decimal ThicknessMm,
    IReadOnlyList<WastePieceRecord> Pieces);

public sealed record WastePieceRecord(
    decimal WidthMm,
    decimal HeightMm,
    decimal X,
    decimal Y);
```

### 3.3 Nesting NuGet 1.1.0 — `WastePiece` model

A `GuillotineNestingStrategy` már kiszámítja a szabad téglalapokat (rekurzív particionálás) — csak vissza kell adni őket.

```csharp
// SpaceOS.Nesting.Algorithms/Models/WastePiece.cs  ← ÚJ
public sealed class WastePiece
{
    public decimal X { get; }
    public decimal Y { get; }
    public decimal WidthMm { get; }
    public decimal HeightMm { get; }

    public WastePiece(decimal x, decimal y, decimal widthMm, decimal heightMm)
    {
        X = x; Y = y; WidthMm = widthMm; HeightMm = heightMm;
    }
}

// SpaceOS.Nesting.Algorithms/Models/PanelAssignment.cs  ← MÓDOSÍTÁS
public sealed class PanelAssignment
{
    // ... meglévő properties ...
    public IReadOnlyList<WastePiece> WastePieces { get; }  // ← ÚJ

    public PanelAssignment(
        string panelId, string materialCode,
        decimal panelWidthMm, decimal panelHeightMm,
        IReadOnlyList<PlacedPart> placedParts,
        IReadOnlyList<WastePiece> wastePieces)  // ← ÚJ paraméter
    {
        // ...
        WastePieces = wastePieces;
        WasteAreaMm2 = wastePieces.Sum(w => w.WidthMm * w.HeightMm);  // számított
    }
}
```

### 3.4 `RegisterOffcutsOnPlanFrozenHandler`

```csharp
public sealed class RegisterOffcutsOnPlanFrozenHandler(
    IPlanNestingSnapshotRepository snapshotRepo,
    IInventoryCuttingAdapter inventoryAdapter,
    ITenantConfigProvider config,
    ILogger<RegisterOffcutsOnPlanFrozenHandler> logger)
    : INotificationHandler<CuttingPlanFrozen>
{
    public async Task Handle(CuttingPlanFrozen evt, CancellationToken ct)
    {
        var snapshot = await snapshotRepo.GetByPlanIdAsync(evt.PlanId, ct);
        if (snapshot is null)
        {
            logger.LogWarning("No nesting snapshot for plan {PlanId} — offcuts skipped", evt.PlanId);
            return;  // best-effort
        }

        var minW = await config.GetIntAsync(evt.TenantId, "cutting.offcut.min_width_mm", 400, ct);
        var minH = await config.GetIntAsync(evt.TenantId, "cutting.offcut.min_height_mm", 400, ct);

        var offcuts = snapshot.GetWastePieces()
            .Where(w => w.WidthMm >= minW && w.HeightMm >= minH)
            .Select(w => new OffcutItem(w.MaterialCode, w.ThicknessMm, w.WidthMm, w.HeightMm, w.PanelId))
            .ToList();

        if (offcuts.Count == 0) return;

        var result = await inventoryAdapter.RegisterOffcutsAsync(
            new OffcutBatchRequest(evt.PlanId, evt.TenantId, offcuts, $"plan-{evt.PlanId}"), ct);

        if (!result.IsSuccess)
            logger.LogError("Offcut registration failed for plan {PlanId}: {Errors}",
                evt.PlanId, string.Join(", ", result.Errors));
    }
}
```

---

## 4. Implementációs sorrend — 3 task

### NESTING-001 — `SpaceOS.Nesting.Algorithms` 1.1.0 (~0.5 nap)

**Független task, párhuzamosan futhat CUTTING-041-gyel.**

- `WastePiece` model hozzáadása
- `PanelAssignment.WastePieces[]` property + constructor bővítés
- `GuillotineNestingStrategy`: waste téglalapok visszaadása a rekurzív szabad területekből
- `FfdhNestingStrategy`: egyszerűsített waste számítás (minden sor végén maradék téglalap)
- `MaxRectsNestingStrategy`: placeholder, üres lista
- Tesztek: `WastePieces` nem üres Guillotine esetén, összterület = `WasteAreaMm2`
- NuGet verzió: `1.0.0 → 1.1.0`

### INVENTORY-015 — Batch offcut endpoint (~0.5 nap)

**Feltétel:** NESTING-001 DONE

- `POST /api/inventory/offcuts/batch` endpoint
- `RegisterOffcutBatchCommand` + handler (bulk EF Core insert)
- Idempotency: unique constraint `(TenantId, SourceType, SourceId)` + upsert
- Migration: `20260422_0001_AddOffcutIdempotencyConstraint`
- Tesztek: 201 happy path, 200 idempotent retry, 401 auth

### CUTTING-042 — Handler + snapshot + event (~1 nap)

**Feltétel:** NESTING-001 + INVENTORY-015 DONE

1. `CuttingPlanFrozen` domain event létrehozása
2. `CuttingPlan.Freeze()` bővítése: `RaiseDomainEvent(new CuttingPlanFrozen(...))`
3. `PlanNestingSnapshot` entity + `IPlanNestingSnapshotRepository`
4. Migration: `AddPlanNestingSnapshot` tábla
5. `GetNestingResultQueryHandler` bővítése: snapshot mentése
6. `IInventoryCuttingAdapter.RegisterOffcutsAsync()` metódus + HTTP implementáció
7. `RegisterOffcutsOnPlanFrozenHandler` MediatR handler
8. DI regisztráció
9. Tesztek (≥8 új): happy path, küszöb filter, snapshot hiányzik, idempotency, cross-tenant guard

**Teljes becsült effort:** ~2 nap (0.5 + 0.5 + 1)

---

## 5. Definition of Done

- [ ] `SpaceOS.Nesting.Algorithms` 1.1.0: `WastePiece` + `WastePieces[]` + algoritmus update
- [ ] `POST /api/inventory/offcuts/batch` endpoint + idempotency constraint
- [ ] `CuttingPlanFrozen` domain event + `Freeze()` bekötés
- [ ] `PlanNestingSnapshot` entity + migration + repository
- [ ] `RegisterOffcutsOnPlanFrozenHandler` + tenant küszöb filter
- [ ] `IInventoryCuttingAdapter.RegisterOffcutsAsync()` + HTTP adapter
- [ ] ≥8 új teszt, mind zöld
- [ ] E2E: Plan Freeze után Offcut látható `/api/inventory/offcuts`-ban
- [ ] `cutting.offcut.min_width_mm` / `min_height_mm` tenant config olvasás (default 400mm)
- [ ] Cross-tenant teszt: Tenant A freeze nem hoz létre Tenant B offcutot

---

## 6. Kockázatok

| Kockázat | Mitigáció |
|---|---|
| Best-effort fail → Plan Frozen de nincs Offcut | Audit log + operátor alert v1; outbox pattern v2 ha valós probléma |
| Tervezett vs. tényleges waste eltérés | v1-ben nem tracked; ADR-039 jelölt (`OffcutActualized` event) |
| Unfreeze jövőbeli bevezetése | Ha Frozen → Published visszaút kell, ez az ADR revíziót igényel (Offcut release) |

---

## 7. Approval

```
[x] Gábor — Snapshot (B) opció elfogadva · APPROVED 2026-04-20
[x] Architect — Codebase-validált, 3 gap azonosítva és beépítve
```
