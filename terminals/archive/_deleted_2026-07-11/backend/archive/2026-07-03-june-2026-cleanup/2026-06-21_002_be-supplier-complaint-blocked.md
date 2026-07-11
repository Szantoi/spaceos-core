---
id: MSG-BACKEND-002-BLOCKED
from: backend
to: conductor
type: blocked
status: READ
ref: MSG-BACKEND-002
created: 2026-06-21
content_hash: 0fb1be212d3c98b2686c1bc22e412a4b3488aa0300487bd12afdcacc0b01a66c
---

# BE-SERVICE-001: Beszállítói reklamáció-válasz API — BLOCKED

## Státusz: BLOCKED ⚠️

A beszállítói reklamáció-válasz API implementációja **blokkolva**, mivel **nincs meglévő domain infrastruktúra**.

---

## Kutatási eredmények

### 1. ❌ Nincs Complaint/Reklamáció entitás

Átvizsgált területek:
- `/opt/spaceos/backend/spaceos-modules-procurement/` — nincs complaint domain
- `/opt/spaceos/backend/spaceos-kernel/` — nincs service/complaint modul
- Teljes backend kódbázis (`grep -r complaint|quality|defect|selejt`) — 0 találat

**Meglévő Procurement domain:**
- `PurchaseOrder` (FSM: Draft → Submitted → Confirmed → Shipped → Delivered)
- `Delivery` (append-only receive record)
- `SupplierInvoice`
- **Nincs QA check, nincs complaint response mechanism**

### 2. ❌ Nincs QA selejt flow

A PROJECT_STATUS.md 6.2 szekció említi:
> "Beszállítói reklamáció-válasz — bejövő QA `selejt` → beszállítói válasz-hurok a portálon."

**Probléma:**
- A dokumentáció egy **frontend-heavy mock prototípus** kontextusában beszél (JoineryTech Portal, React + localStorage)
- **Nincs backend implementáció** a QA selejt flow-hoz
- Nem egyértelmű, hogy mi a "bejövő QA selejt" entitás/event

### 3. ❌ Nincs beszállítói portál backend infrastruktúra

**Hiányzó komponensek:**
- Supplier authentication/authorization (külön Keycloak realm? supplier role?)
- Supplier portal API endpoints (beszállító csak saját adatait látja)
- Beszállító-tenant mapping (RLS policy?)

**Meglévő infrastruktúra (Procurement modul):**
- `Supplier` aggregate (basic supplier master data)
- `IProcurementProvider` interface
- **Nincs supplier portal authentication flow**

---

## Blokkoló kérdések

### Architekturális döntések szükségesek:

1. **Melyik modulba kerüljön a Complaint domain?**
   - `spaceos-modules-procurement` — mivel beszállítói reklamáció?
   - `spaceos-kernel` — új Service/Quality modul?
   - Új standalone modul: `spaceos-modules-quality`?

2. **Mi a QA selejt entitás/event?**
   - Van-e meglévő Quality Check entitás (nem találtam)?
   - Egy új `QualityInspection` aggregate kell?
   - Vagy a `Delivery` aggregate-et kell kiterjeszteni quality check-kel?

3. **Beszállítói portál authentication?**
   - Külön Keycloak realm/client beszállítóknak?
   - Vagy tenant-en belüli supplier role?
   - API scoping: RLS policy `supplierId`-ra?

4. **Complaint FSM tervezés?**
   - Javasolt FSM: `pending → supplier_response → resolved/rejected`
   - Ki zárhatja le (approve/reject): tenant QA manager? procurement manager?

---

## Javasolt következő lépések

### Option A: Architect bevonás (AJÁNLOTT)

**Miért:**
- Új domain tervezés (Complaint aggregate)
- Cross-module döntések (Procurement ↔ Quality ↔ Kernel)
- Supplier portal authentication stratégia

**Teendő:**
1. Root/Conductor kér Architect specifikációt:
   - Complaint domain tervezés (aggregate boundaries, FSM)
   - Modul elhelyezés döntés
   - Supplier portal auth stratégia
2. Architect készít v1→v4 spec-et
3. Backend implementál a spec alapján

### Option B: Minimal viable implementation (gyors prototípus)

**Ha nincs idő Architectre:**
1. **Modul:** `spaceos-modules-procurement` (mivel beszállítói kapcsolat)
2. **Új entitások:**
   ```
   SupplierComplaint (Aggregate)
   ├── Id, TenantId, SupplierId, DeliveryId
   ├── IssueDescription, SupplierResponse
   ├── Status (Pending, SupplierResponded, Resolved, Rejected)
   └── CreatedAt, RespondedAt, ResolvedAt
   ```
3. **Endpoints:**
   ```
   POST /procurement/api/supplier-complaints        → create complaint (tenant oldalról)
   GET  /procurement/api/supplier-complaints/{id}   → get complaint details
   POST /procurement/api/supplier-complaints/{id}/respond  → supplier response
   ```
4. **Auth:** Supplier role + RLS `supplierId = current_supplier_id()`
5. **Tesztek:** xUnit, Moq

**Kockázat:**
- Nincs QA inspection infrastruktúra (mi a complaint trigger?)
- Supplier portal auth ad-hoc megoldás (később refaktorálandó)

---

## Ajánlásom

**ARCHITECT BEVONÁS** a legbiztonságosabb út:
- Új domain (Complaint) ⇒ architekturális döntés
- Supplier portal stratégia ⇒ cross-cutting concern
- QA flow hiánya ⇒ scope clarification szükséges

**Várható időkeret:**
- Architect spec: 1-2 nap
- Backend implementáció (spec alapján): 2-3 nap

**Kérdés Conductor/Root felé:**
- Architect bevonás OK?
- Vagy van már meglévő terv/spec (amit nem találtam)?

---

## Blocker resolution path

1. **Root/Conductor dönt:** Architect spec kell? vagy minimal MVP OK?
2. **Ha Architect:** inbox üzenet Architectnek (complaint domain + supplier portal strategy)
3. **Ha MVP:** Root/Conductor approve-ol egy minimal design (l. Option B)
4. **Backend unblocked:** spec/approval alapján implementál

---

## Referenciák

- Inbox task: `terminals/backend/inbox/2026-06-21_002_be-supplier-complaint.md`
- PROJECT_STATUS.md: Section 6.2 (Lánc-záró érték)
- Procurement domain: `/opt/spaceos/backend/spaceos-modules-procurement/src/SpaceOS.Modules.Procurement.Domain/`

---

**Blocker jelzés ideje:** 2026-06-21 13:30 UTC
**Következő lépés:** Conductor/Root döntés Architect bevonásról vagy minimal MVP approve-ról
