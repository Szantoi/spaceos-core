---
id: MSG-ARCHITECT-001
from: conductor
to: architect
type: task
priority: high
status: READ
model: opus
ref: MSG-BACKEND-002-BLOCKED
created: 2026-06-21
content_hash: d9a2e0fe642f84f11c641bfd52817addbf8a30026cc9a8171ec9fea5616482da
---

# ARCH-001: Beszállítói Reklamáció Domain Specifikáció

## Háttér

A Backend terminál BLOCKED státuszba került a **Beszállítói reklamáció-válasz API** implementációjánál, mert:

1. **Nincs Complaint/Reklamáció entitás** a kódbázisban
2. **Nincs QA selejt flow** (mi a trigger?)
3. **Nincs beszállítói portál auth stratégia**

Architekturális specifikáció szükséges mielőtt a Backend folytathatja.

---

## Felkérés

Készíts **v1→v4 specifikációt** az alábbi kérdésekre:

### 1. Modul elhelyezés

**Opciók:**
- A) `spaceos-modules-procurement` (beszállítói kapcsolat miatt)
- B) `spaceos-kernel` új Service/Quality modul
- C) Új standalone: `spaceos-modules-quality`

**Döntési kritériumok:** Domain boundaries, aggregate root elhelyezés, cross-module függőségek.

### 2. Domain modell

**Kérdések:**
- Mi a **QA selejt** entitás/event ami a reklamációt kiváltja?
  - Új `QualityInspection` aggregate?
  - Vagy `Delivery` aggregate kiterjesztése?
- **Complaint aggregate** tervezés:
  - Root entity: `SupplierComplaint`
  - Value objects: `ComplaintResponse`, `Resolution`?
  - FSM: `Pending → SupplierResponded → Resolved/Rejected`?

### 3. Beszállítói Portál Auth Stratégia

**Kérdések:**
- Külön Keycloak realm/client beszállítóknak?
- Vagy tenant-en belüli `supplier` role?
- RLS policy: `supplierId = current_supplier_id()`?
- API endpoint scoping: `/suppliers/{supplierId}/...` vs `/supplier-portal/...`?

### 4. API Contract

**Javasolt endpointok:**
```
POST /procurement/api/supplier-complaints        → tenant oldal létrehoz reklamációt
GET  /procurement/api/supplier-complaints/{id}   → részletek
POST /procurement/api/supplier-complaints/{id}/respond  → beszállító válaszol
POST /procurement/api/supplier-complaints/{id}/resolve  → tenant lezárja
```

**Kérdés:** Ki zárhatja le (resolve/reject)? QA manager? Procurement manager? Mindkettő?

---

## Meglévő Infrastruktúra (Backend kutatás alapján)

**Procurement domain (használható):**
- `Supplier` aggregate (master data)
- `PurchaseOrder` (FSM: Draft → Submitted → Confirmed → Shipped → Delivered)
- `Delivery` (append-only receive record)
- `SupplierInvoice`, `SupplierPriceList`
- `IProcurementProvider` interface

**Hiányzik:**
- QA inspection infrastruktúra
- Complaint domain
- Supplier portal authentication flow

---

## Definition of Done

- [ ] v1: Domain modell draft (aggregate boundaries, entities, value objects)
- [ ] v2: FSM diagram + state transitions
- [ ] v3: API contract (endpoints, DTOs, validation rules)
- [ ] v4: Implementation guide (modul, fájlok, tesztek)

---

## Referenciák

- Backend BLOCKED outbox: `terminals/backend/outbox/2026-06-21_002_be-supplier-complaint-blocked.md`
- PROJECT_STATUS.md: Section 6.2 (Lánc-záró érték)
- Procurement domain: `/opt/spaceos/backend/spaceos-modules-procurement/`

---

**Időkeret:** A Backend erre a specifikációra vár - prioritás HIGH.
