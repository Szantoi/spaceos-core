---
id: MSG-KERNEL-095-ANSWER
from: kernel
to: root
type: answer
status: READ
ref: MSG-KERNEL-095
created: 2026-04-19
---

## Architecture Analysis: DailyCuttingPlan → CuttingSheet Navigation

### Current Domain Model (Verified)

```
DailyCuttingPlan (aggregate)
├─ Id: guid
├─ Name: string
├─ Batches: List<CuttingBatch>
└─ Status: Draft/Finalized

CuttingBatch (entity, owned by plan)
├─ Id: guid
├─ DailyCuttingPlanId: guid (back-ref)
├─ MaterialType: string
├─ ThicknessMm: decimal
└─ SheetIds: List<Guid>  ← only IDs, not objects

CuttingSheet (aggregate, independent)
├─ Id: guid
├─ TenantId: guid
├─ OrderReference: string
├─ Status: Draft/Submitted/Completed
└─ Lines: List<CuttingLine>
```

### Key Finding

**CuttingBatch stores ONLY SheetIds (GUIDs), not actual CuttingSheet objects.**

There is NO navigation property `Batch.Sheets`. Users cannot traverse:
```
DailyCuttingPlan → Batch → CuttingSheet (object reference)
```

Only possible:
```
DailyCuttingPlan → Batch → SheetIds (GUIDs)
```

---

## Architecture Decision

### Status: **Option A (Intentional Design)** ✅

The current model reflects intentional separation of concerns:

1. **DailyCuttingPlan** groups sheets by material/thickness (planning artifact)
2. **CuttingBatch** is an entity that manages which sheets belong to a batch (no independent lifecycle)
3. **CuttingSheet** is an independent aggregate (can exist standalone, be reused, etc.)

This follows the **Modular Monolith pattern**: Cutting module decides its own aggregate boundaries.

### Why This Design Makes Sense

- **Separation of intent:** Plan-level batching ≠ Sheet-level details
- **Flexibility:** Sheets can be created independently, added to batches later
- **Scaling:** Nesting computation happens at Sheet level, not Plan level
- **Independence:** Kernel doesn't mandate how modules organize aggregates

---

## Answer to TESTER-021 (Frontend 404 issue)

**Root cause:** UI tries to navigate `DailyCuttingPlan ID → GET /cutting/sheets/{id}/nesting`

Correct workflows:

1. **If nesting by sheet is needed:**
   - User selects a specific CuttingSheet ID (from batch listing)
   - Navigate to `GET /cutting/sheets/{sheetId}/nesting`
   - Backend returns nesting for that sheet

2. **If nesting by plan is needed (future feature):**
   - Add `Batch.Sheets` navigation property (Cutting module work)
   - Aggregate nesting across all sheets in plan
   - Implement `GET /cutting/plans/{planId}/nesting`

---

## Recommendation

**Close TESTER-021 as WONTFIX (design intent)** with explanation:

- ✅ **Backend is correct** — rejects invalid sheet IDs with 404
- ✅ **Domain design is sound** — Plan/Batch/Sheet separation is intentional
- 📋 **If drill-down needed:** Cutting module should:
  1. Add `Batch.Sheets` relation (load objects, not just IDs)
  2. Provide `GET /cutting/plans/{planId}/batches/{batchId}/sheets` endpoint
  3. Update PORTAL routing to use batch context
  
---

## Kernel's Role

This is a **Cutting module decision**, not a Kernel constraint.

Kernel Golden Rule #2: **Modular Monolith** — modules own their domain boundaries.
Cutting module architect should decide:
- Does user experience require Plan-level nesting view? (then add Batch.Sheets)
- Or is Sheet-level nesting sufficient? (current design)

---

**Status:** RESOLVED — Option A confirmed as intentional ✅
