---
id: MSG-ARCH-004-RESPONSE
from: architect
to: root
type: response
priority: medium
status: READ
ref: SpaceOS_Q3_Planning_Brief_v1.md Téma 2
created: 2026-04-20
---

# ADR-038 — Offcut loop closure: 3 task, ~2 nap

**Spec:** `docs/architecture/ADR-038-offcut-creation-at-plan-freeze.md` ✅ APPROVED

## Kontextus

A Desktop session ADR-038 vázlatát codebase-szel validáltam. 3 implementációs gap volt amit a Desktop nem látott — beépítve. A spec kész, kiadható.

**Sorrend: NESTING-001 || INVENTORY-015 → CUTTING-042**
(Az első kettő párhuzamosan futhat, CUTTING-042 mindkettőre vár.)

---

## NESTING-001 — SpaceOS.Nesting.Algorithms 1.1.0

**Assignee:** CUTTING terminál · **Becsült idő:** ~0.5 nap · **Blokkoló:** —

**Feladat:** A `PanelAssignment` modell bővítése dimenzionált waste téglalapokkal.

```
SpaceOS.Nesting.Algorithms/
  Models/
    WastePiece.cs          ← ÚJ (X, Y, WidthMm, HeightMm)
    PanelAssignment.cs     ← MÓDOSÍTÁS: WastePieces[] property + constructor
  Strategies/
    GuillotineNestingStrategy.cs  ← MÓDOSÍTÁS: waste téglalapok visszaadása
    FfdhNestingStrategy.cs        ← MÓDOSÍTÁS: sor végén maradék téglalap
```

**Version bump:** `1.0.0 → 1.1.0`

**Definition of Done:**
- [ ] `WastePiece` model: X, Y, WidthMm, HeightMm
- [ ] `PanelAssignment.WastePieces` nem üres Guillotine kimenetben
- [ ] `WasteAreaMm2 == WastePieces.Sum(w => w.WidthMm * w.HeightMm)` teljesül
- [ ] Meglévő nesting tesztek (29) mind zölden maradnak
- [ ] ≥3 új teszt

---

## INVENTORY-015 — Batch offcut endpoint

**Assignee:** CUTTING terminál (Inventory repo) · **Becsült idő:** ~0.5 nap · **Blokkoló:** —

**Feladat:** `POST /api/inventory/offcuts/batch` endpoint + idempotency constraint.

```csharp
// OffcutEndpoints.cs-be:
app.MapPost("/api/inventory/offcuts/batch", RegisterOffcutBatch)
    .RequireAuthorization("ManufacturerOnly");

// Idempotency: upsert by (TenantId, SourceType=CuttingPlan, SourceId=PlanId)
// Migration: 20260422_0001_AddOffcutIdempotencyConstraint
```

**Definition of Done:**
- [ ] `POST /api/inventory/offcuts/batch` → 201 created
- [ ] Idempotent: ugyanaz a batch kétszer → 200 + eredeti ID-k
- [ ] Migration: unique constraint `(TenantId, SourceType, SourceId)`
- [ ] 401 auth teszt
- [ ] ≥4 új teszt

---

## CUTTING-042 — Handler + snapshot + event

**Assignee:** CUTTING terminál · **Becsült idő:** ~1 nap · **Blokkoló:** NESTING-001 + INVENTORY-015 DONE

**Feladat:** A teljes offcut registration pipeline bekötése.

1. `CuttingPlanFrozen` domain event létrehozása
2. `CuttingPlan.Freeze()` → `RaiseDomainEvent(new CuttingPlanFrozen(planId, tenantId, frozenAt))`
3. `PlanNestingSnapshot` entity + repo + migration
4. `GetNestingResultQueryHandler` bővítése: snapshot mentése nesting futásakor
5. `IInventoryCuttingAdapter.RegisterOffcutsAsync()` metódus + HTTP impl (Polly 3x retry)
6. `RegisterOffcutsOnPlanFrozenHandler` — tenant küszöb filter (default 400mm), best-effort
7. DI regisztráció

**Definition of Done:**
- [ ] `CuttingPlanFrozen` event + `Freeze()` bekötés
- [ ] `PlanNestingSnapshot` tábla + migration
- [ ] Handler: küszöb alatt lévő waste nem regisztrálódik
- [ ] Handler: snapshot hiányzik → warning log, nem dob kivételt
- [ ] Idempotency: kétszer Frozen ugyanaz a plan → 0 duplikált Offcut
- [ ] Cross-tenant teszt: Tenant A freeze nem hoz létre Tenant B offcutot
- [ ] ≥8 új teszt
- [ ] E2E: Plan Freeze → Offcut látható `/api/inventory/offcuts`-ban
- [ ] Meglévő tesztek (195+) mind zölden

**Teljes spec:** `docs/architecture/ADR-038-offcut-creation-at-plan-freeze.md`
