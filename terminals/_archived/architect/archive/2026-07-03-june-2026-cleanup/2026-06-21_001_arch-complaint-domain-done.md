---
id: MSG-ARCHITECT-001-DONE
from: architect
to: conductor
type: done
ref: MSG-ARCHITECT-001
status: READ
created: 2026-06-21
content_hash: 9693d04f97382390bd0f92ddbc9c2221dce1f50e8329f3bbd7011aae116ff31a
---

# DONE: Beszállítói Reklamáció Domain Specifikáció

## Összefoglaló

A Backend terminál BLOCKED állapotát feloldó **v4 specifikáció elkészült**.

## Deliverable

**Fájl:** `docs/tasks/new/SpaceOS_Supplier_Complaint_Architecture_v4.md`

**Státusz:** 🟢 **IMPLEMENTÁCIÓRA KÉSZ**

## Fő döntések

| # | Kérdés | Döntés |
|---|--------|--------|
| 1 | **Modul elhelyezés** | `spaceos-modules-procurement` bővítés (nem új modul) |
| 2 | **QA selejt trigger** | `Delivery` aggregate bővítése `QualityInspectionResult` value object-tel |
| 3 | **Beszállítói auth** | Tenant-en belüli `supplier` role + `supplierId` claim (nem külön realm) |
| 4 | **Resolution jogosultság** | QA Manager VAGY Procurement Manager (`procurement.resolve_complaint` permission) |

## Specifikáció tartalma

1. **v1 Domain modell:**
   - `SupplierComplaint` aggregate root
   - `ComplaintResponse` owned entity (beszállító válasza)
   - `ComplaintResolution` owned entity (tenant lezárás)
   - `QualityInspectionResult` value object
   - 6 enum: `ComplaintStatus`, `ComplaintType`, `ResponseType`, `ResolutionType`, `ResolutionAction`, `QualityStatus`

2. **v2 FSM:**
   - 8 állapot: `Draft → Submitted → SupplierReviewing → SupplierResponded → UnderReview → Resolved/Escalated/Withdrawn`
   - Teljes FSM diagram és átmenet definíciók
   - 6 domain event

3. **v3 API Contract:**
   - 7 tenant-oldali endpoint
   - 4 supplier-portal endpoint
   - Teljes DTO struktúra
   - RLS policy és beszállítói auth stratégia

4. **v4 Implementation Guide:**
   - 5 track breakdown (A-E)
   - Teljes fájl struktúra
   - Discovery commands
   - DoD checklist
   - Claude Code agent block

## Becsült effort

**~4-5 nap** (5 track párhuzamosítható)

## Következő lépés

A Backend terminál folytathatja az implementációt a specifikáció alapján.

**Javasolt inbox:** `MSG-BACKEND-003` a `SpaceOS_Supplier_Complaint_Architecture_v4.md` referenciával.

---

**Architect session lezárva.**
