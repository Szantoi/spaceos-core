---
id: MSG-BACKEND-003
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: MSG-ARCHITECT-001-DONE
created: 2026-06-21
content_hash: 6ea65e50994ed494ccc23f73a135ac437cb4502c6bf23aa309dab3fc954ac1db
---

# BE-PROC-003: Beszállítói Reklamáció API Implementáció

## Unblock: Architect specifikáció KÉSZ

A BLOCKED állapotot feloldó **v4 architektúra specifikáció elkészült**.

**Specifikáció:** `docs/tasks/new/SpaceOS_Supplier_Complaint_Architecture_v4.md`

---

## Fő döntések (Architect)

| # | Kérdés | Döntés |
|---|--------|--------|
| 1 | **Modul** | `spaceos-modules-procurement` bővítés |
| 2 | **QA trigger** | `Delivery` aggregate + `QualityInspectionResult` value object |
| 3 | **Auth** | Tenant-en belüli `supplier` role + `supplierId` claim |
| 4 | **Resolution** | QA Manager VAGY Procurement Manager |

---

## Feladat

Implementáld a **Beszállítói Reklamáció** domain-t a specifikáció alapján.

### Track A: Domain (1. nap)
- `SupplierComplaint` aggregate root
- `ComplaintResponse`, `ComplaintResolution` owned entities
- `QualityInspectionResult` value object
- Enumok: `ComplaintStatus`, `ComplaintType`, `ResponseType`, `ResolutionType`
- 6 domain event

### Track B: FSM (1. nap)
- 8 állapot FSM: `Draft → Submitted → SupplierReviewing → SupplierResponded → UnderReview → Resolved/Escalated/Withdrawn`
- Állapot átmenetek validációval

### Track C: Application (1-2 nap)
- CQRS Commands: `CreateComplaint`, `SubmitComplaint`, `RespondToComplaint`, `ResolveComplaint`
- Queries: `GetComplaintById`, `ListComplaints`, `GetComplaintsBySupplier`
- Domain event handlers

### Track D: Infrastructure (1 nap)
- EF Core config (owned entities, JSON columns)
- Migration: `AddSupplierComplaint`
- Repository implementáció

### Track E: API (1 nap)
- 7 tenant-oldali endpoint
- 4 supplier-portal endpoint
- RLS policy: `supplierId = current_supplier_id()`

---

## API Endpoints (spec-ből)

**Tenant oldal:**
```
POST   /api/procurement/complaints                      → Create draft
PUT    /api/procurement/complaints/{id}                 → Update draft
POST   /api/procurement/complaints/{id}/submit          → Submit to supplier
GET    /api/procurement/complaints                      → List all
GET    /api/procurement/complaints/{id}                 → Get details
POST   /api/procurement/complaints/{id}/resolve         → Resolve
POST   /api/procurement/complaints/{id}/escalate        → Escalate
```

**Supplier portal:**
```
GET    /api/supplier-portal/complaints                  → List my complaints
GET    /api/supplier-portal/complaints/{id}             → Get details
POST   /api/supplier-portal/complaints/{id}/acknowledge → Acknowledge
POST   /api/supplier-portal/complaints/{id}/respond     → Submit response
```

---

## Definition of Done

- [ ] Domain entities implementálva
- [ ] FSM működik (állapot átmenetek)
- [ ] API endpointok működnek
- [ ] Unit tesztek (min. 10)
- [ ] Integration tesztek (min. 5)
- [ ] `dotnet test` PASS
- [ ] `dotnet build` PASS
- [ ] Migration létrejön

---

## Referenciák

- **Spec:** `docs/tasks/new/SpaceOS_Supplier_Complaint_Architecture_v4.md`
- **Modul:** `/opt/spaceos/backend/spaceos-modules-procurement/`
- **Meglévő domain:** `Supplier`, `Delivery`, `PurchaseOrder`
