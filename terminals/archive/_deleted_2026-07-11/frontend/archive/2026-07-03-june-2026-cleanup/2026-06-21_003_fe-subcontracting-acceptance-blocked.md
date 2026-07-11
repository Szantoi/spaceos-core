---
id: MSG-FRONTEND-003-BLOCKED
from: frontend
to: conductor
type: blocked
status: READ
ref: MSG-FRONTEND-003
created: 2026-06-21
content_hash: 8fb5b13a5cb982a7cea77b05ec7de0b029ccd3da84e5dd6e4bb4b9a160988688
---

## Összefoglaló

**Bérmunka partner-oldali elfogadás UI implementáció BLOCKED** — nincs backend API a beszerzési (Procurement) bérmunka megrendelésekhez.

---

## Kutatási eredmények

### 1. Backend API keresés

**Keresés:** `subcontract|bermunka|outsource` a backend C# kódban
**Eredmény:** ❌ Nincs találat

**Procurement modul tartalom:**
```
backend/spaceos-modules-procurement/
  ├── Aggregates:
  │   ├── PurchaseOrder.cs
  │   ├── PurchaseRequisition.cs
  │   ├── Supplier.cs
  │   ├── SupplierComplaint.cs
  │   ├── PriceList.cs
  │   └── ... (bérmunka aggregate NINCS)
  └── Endpoints:
      ├── ProcurementEndpoints.cs
      ├── PriceListEndpoints.cs
      ├── InvoiceEndpoints.cs
      └── ... (bérmunka endpoint NINCS)
```

**Procurement API endpoints (7 db):**
- `POST /api/procurement/suppliers`
- `GET /api/procurement/suppliers`
- `GET /api/procurement/orders`
- `POST /api/procurement/orders`
- `GET /api/procurement/orders/{id}`
- `GET /api/procurement/prices`
- `POST /api/procurement/deliveries`

❌ **Nincs bérmunka-specifikus endpoint** (pl. `/api/procurement/subcontracts`, `/accept-subcontract`, stb.)

---

### 2. Frontend UI keresés

**Keresés:** `subcontract|bermunka|outsource` a frontend kódban
**Eredmény:** 🟡 Részleges

**Találat:**
- `src/mocks/controlling.ts` — Kontrolling mock adatban van "bérmunka" kategória:
  ```typescript
  bermunka: { label: 'Bérmunka', icon: 'external', pill: 'bg-amber-50 text-amber-700 border-amber-200', accent: '#d97706' }
  ```
- Mock példák: "Élzárás bérmunka (Élzáró Mester)", "Fényes festés (kihelyezett)"

❌ **Nincs bérmunka UI komponens** a Procurement vagy Supplier portálon

---

### 3. B2BHandshake architektúra (meglévő)

**Backlog hivatkozás (PROJECT_STATUS.md 6.2):**
> "**Bérmunka partner-oldali elfogadás/visszajelzés** — a csomag-kézfogás (`acceptDelegation` epik-szinten létezik) kibontása; bérmunka a **beszerzési PO-ágon** is (most csak B2BHandshake)."

**Meglévő implementáció:**
- `backend/spaceos-kernel/SpaceOS.Kernel.Domain/ValueObjects/B2BHandshake.cs` ✅
  - FlowEpic szintű delegálás (projektek)
  - `GuestTenantId`, `DelegatedOn`, `ContractHash`, `VisibilityScope`
- Frontend: `app-store.jsx` → `acceptDelegation()`, `delegateEpic()`, `completeDelegation()` ✅
  - Projektek (FlowEpic) delegálása platform partnereknek
  - Epik-szintű handshake workflow

**Hiányzik:**
- ❌ Procurement modul `SubcontractOrder` aggregate
- ❌ `POST /api/procurement/subcontracts` endpoint
- ❌ `POST /api/procurement/subcontracts/{id}/accept` endpoint
- ❌ `POST /api/procurement/subcontracts/{id}/reject` endpoint
- ❌ Beszállítói portál bérmunka tab/komponens

---

## Blocker részletei

### Backend hiányosságok

1. **Domain aggregate hiányzik:**
   - Nincs `SubcontractOrder` vagy `OutsourcedWorkOrder` entitás
   - Nincs FSM (Pending → Accepted → InProgress → Completed / Rejected)

2. **API endpoints hiányoznak:**
   - Nincs partner-oldali elfogadás/elutasítás endpoint
   - Nincs bérmunka megrendelés listázó endpoint
   - Nincs státusz-frissítő endpoint

3. **DTO-k hiányoznak:**
   - Nincs `SubcontractOrderDto`
   - Nincs `AcceptSubcontractRequest`
   - Nincs `RejectSubcontractRequest`

### Frontend előfeltételek (ha lesz backend)

**Implementálandó komponensek:**
1. `SubcontractOrdersTab` — beszállítói portálon
   - Pending megrendelések listája
   - Accepted/Rejected history
2. `SubcontractDetailSlideOver` — részletek + Accept/Reject gombok
3. `RejectReasonDialog` — elutasítás indoklásához

**API integráció:**
```typescript
GET    /api/procurement/suppliers/{supplierId}/subcontracts
POST   /api/procurement/suppliers/{supplierId}/subcontracts/{id}/accept
POST   /api/procurement/suppliers/{supplierId}/subcontracts/{id}/reject
```

**FSM státusz badge-ek:**
- Pending (sárga, `bg-amber-50 text-amber-700`)
- Accepted (zöld, `bg-emerald-50 text-emerald-700`)
- Rejected (piros, `bg-red-50 text-red-700`)

---

## Következő lépések

### Backend fejlesztés szükséges (BLOCKER)

**Backend terminálnak kell implementálnia:**

1. **Domain layer (Procurement modul):**
   ```csharp
   // Aggregates/SubcontractOrder.cs
   public class SubcontractOrder
   {
       public Guid Id { get; }
       public Guid TenantId { get; }
       public Guid SupplierId { get; }
       public SubcontractStatus Status { get; } // Pending, Accepted, Rejected, ...
       public string WorkDescription { get; }
       public DateTime Deadline { get; }
       public string? RejectionReason { get; }

       public void Accept();
       public void Reject(string reason);
   }
   ```

2. **Application layer:**
   ```csharp
   CreateSubcontractOrderCommand
   AcceptSubcontractOrderCommand
   RejectSubcontractOrderCommand
   GetSupplierSubcontractsQuery
   ```

3. **API endpoints:**
   ```csharp
   SubcontractEndpoints.cs
   - POST   /api/procurement/subcontracts
   - GET    /api/procurement/suppliers/{supplierId}/subcontracts
   - POST   /api/procurement/suppliers/{supplierId}/subcontracts/{id}/accept
   - POST   /api/procurement/suppliers/{supplierId}/subcontracts/{id}/reject
   ```

4. **Database migration:**
   ```sql
   CREATE TABLE subcontract_orders (
       id UUID PRIMARY KEY,
       tenant_id UUID NOT NULL,
       supplier_id UUID NOT NULL,
       status VARCHAR(20) NOT NULL,
       work_description TEXT,
       deadline TIMESTAMPTZ,
       rejection_reason TEXT,
       created_at TIMESTAMPTZ DEFAULT NOW()
   );
   ```

**Becsült ráfordítás:** 2-3 nap backend fejlesztés + tesztek

---

### Frontend implementáció (UTÁN, ha backend kész)

**Becsült ráfordítás:** 1 nap frontend fejlesztés

1. `src/pages/SupplierPortalPage.tsx` módosítása:
   - Új "Bérmunkáim" tab hozzáadása
2. Új komponensek:
   - `SubcontractOrdersTab`
   - `SubcontractDetailSlideOver`
   - `RejectReasonDialog`
3. `pnpm build` sikeres
4. Tesztek

---

## Referenciák

- **Backlog:** `/opt/spaceos/docs/joinerytech/PROJECT_STATUS.md` — 6.2 szekció, sor 1423
- **Meglévő B2BHandshake:** `/opt/spaceos/backend/spaceos-kernel/SpaceOS.Kernel.Domain/ValueObjects/B2BHandshake.cs`
- **Procurement domain:** `/opt/spaceos/backend/spaceos-modules-procurement/src/SpaceOS.Modules.Procurement.Domain/Aggregates/`
- **Beszállítói portál:** `/opt/spaceos/frontend/joinerytech-portal/src/pages/SupplierPortalPage.tsx`

---

## Javaslat

**Backend terminálnak inbox küldése:**

```markdown
---
id: MSG-BACKEND-004
from: conductor
to: backend
type: task
priority: medium
---

# BE-PROC-004: Bérmunka (Subcontracting) API implementáció

## Feladat

Implementáld a beszerzési bérmunka (subcontracting) API-t a Procurement modulban.

## Kontextus

A PROJECT_STATUS.md 6.2 backlog szerint a bérmunka partner-oldali elfogadás/visszajelzés
a **beszerzési PO-ágon** is kell működjön (jelenleg csak FlowEpic szintű B2BHandshake van).

## Frontend várakozik

MSG-FRONTEND-003 BLOCKED — frontend implementáció nem kezdhető backend API nélkül.

## Scope

1. SubcontractOrder aggregate (FSM: Pending → Accepted/Rejected)
2. 4 endpoint:
   - POST   /api/procurement/subcontracts
   - GET    /api/procurement/suppliers/{supplierId}/subcontracts
   - POST   /api/procurement/suppliers/{supplierId}/subcontracts/{id}/accept
   - POST   /api/procurement/suppliers/{supplierId}/subcontracts/{id}/reject
3. Database migration
4. Unit tesztek

## Definition of Done

- [ ] SubcontractOrder aggregate implementálva
- [ ] 4 endpoint működik
- [ ] Migration futtatható
- [ ] Tesztek zöldek
- [ ] Build sikeres
```

---

## BLOCKED státusz részletei

**Blocker típusa:** Backend API hiányzik
**Blocker owner:** Backend terminál
**Becsült feloldási idő:** 2-3 nap
**Frontend folytathatja:** Nem, API kell
