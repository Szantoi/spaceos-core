# SpaceOS — Supplier Complaint Domain Architecture

## Beszállítói Reklamáció Specifikáció (QA Selejt → Complaint → Response → Resolution)

> **Verzió:** v4.0 — 2026-06-21 (Architect single-pass: v1 Domain → v2 FSM → v3 API → v4 Implementation)
> **Státusz:** 🟢 **IMPLEMENTÁCIÓRA KÉSZ**
> **Repo:** `spaceos-modules-procurement` (bővítés, nem új modul)
> **DB schema:** `spaceos_procurement` (additív bővítés)
> **Port:** 5006 (meglévő)
> **TargetFramework:** `net8.0`
> **Becsült effort:** ~4-5 nap
> **Referencia:** `SpaceOS_Modules_Procurement_v2_Architecture_v4.md` · ADR-039 · `Supplier` aggregate (meglévő)

---

## 0. Összefoglaló döntések (Pre-decision)

| # | Kérdés | Döntés | Indoklás |
|---|--------|--------|----------|
| 1 | Modul elhelyezés | **Procurement modul** (`spaceos-modules-procurement`) | A Complaint a beszállítói kapcsolat része — nem új modul, hanem a Procurement domain természetes bővítése. A QA selejt trigger is itt integrálódik (Delivery → QA check → Complaint). |
| 2 | QA selejt trigger | **Delivery aggregate kiterjesztése + QA result** | A Delivery.Record már létezik. Hozzáadunk egy `QualityInspectionResult` value object-et, ami a Complaint triggerét jelenti. Nincs külön QualityInspection aggregate — a QA egy property a Delivery-n. |
| 3 | Beszállítói auth | **Tenant-en belüli `supplier` role** (Keycloak) | Nem külön realm, hanem a tenant-ben `supplierId` claim + `supplier` role. RLS policy: `supplierId = current_supplier_id()`. Single Keycloak client. |
| 4 | Resolution jogosultság | **QA Manager VAGY Procurement Manager** | Mindkét szerepkör lezárhatja — `procurement.complaint_resolver` permission (nem role, hanem permission a tenant RBAC-ban). |

---

## 1. Domain Modell (v1)

### 1.1 Aggregate elhelyezés

```
spaceos_procurement (schema bővítés)
├── Supplier                    (meglévő — változatlan)
├── PurchaseOrder               (meglévő — változatlan)
├── Delivery                    (meglévő — BŐVÍTÉS: QualityInspectionResult)
├── SupplierComplaint           (ÚJ aggregate root)
├── ComplaintResponse           (owned entity)
└── ComplaintResolution         (owned entity / value object)
```

### 1.2 Delivery Bővítés (QA Selejt Trigger)

```csharp
public class Delivery
{
    // Meglévő mezők...
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid PurchaseOrderId { get; private set; }
    public decimal ReceivedQuantity { get; private set; }
    public DateTime ReceivedAt { get; private set; }
    public string? Notes { get; private set; }
    public string RecordedBy { get; private set; }

    // ÚJ mezők a QA-hoz
    public QualityInspectionResult? QualityInspection { get; private set; }
    public DateTime? InspectedAt { get; private set; }
    public string? InspectedBy { get; private set; }
}
```

### 1.3 QualityInspectionResult (Value Object)

```csharp
public record QualityInspectionResult
{
    public QualityStatus Status { get; init; }           // Passed | PartialReject | FullReject
    public decimal AcceptedQuantity { get; init; }       // Elfogadott mennyiség
    public decimal RejectedQuantity { get; init; }       // Selejtes mennyiség
    public string? DefectDescription { get; init; }      // Hiba leírás (max 2000 char)
    public List<string> DefectPhotoPaths { get; init; }  // MinIO path-ok (max 5 fotó)
}

public enum QualityStatus
{
    Passed = 0,        // Minden OK
    PartialReject = 1, // Részleges selejt
    FullReject = 2     // Teljes selejt
}
```

### 1.4 SupplierComplaint (ÚJ Aggregate Root)

```csharp
public class SupplierComplaint : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string ComplaintNumber { get; private set; }  // SC-{YYYY}-{NNNNN} per-tenant monoton

    // Kapcsolatok
    public Guid SupplierId { get; private set; }         // FK (logikai) → Supplier
    public Guid DeliveryId { get; private set; }         // FK → Delivery (QA selejt forrása)
    public Guid? PurchaseOrderId { get; private set; }   // FK → PO (denormalizált gyors lookup)

    // Reklamáció tartalma
    public ComplaintType Type { get; private set; }      // QualityDefect | QuantityShortage | Documentation | Other
    public string Subject { get; private set; }          // Rövid cím (max 200)
    public string Description { get; private set; }      // Részletes leírás (max 5000)
    public decimal AffectedQuantity { get; private set; }
    public decimal? ClaimedAmount { get; private set; }  // Igényelt kártérítés (opcionális)
    public char Currency { get; private set; }           // ISO 4217, 3 char

    // QA adatok (denormalizálva a Delivery-ből)
    public QualityInspectionResult? QaResult { get; private set; }
    public List<string> EvidencePaths { get; private set; }  // MinIO path-ok

    // FSM
    public ComplaintStatus Status { get; private set; }

    // Audit trail
    public string CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Owned entities
    public ComplaintResponse? SupplierResponse { get; private set; }
    public ComplaintResolution? Resolution { get; private set; }
}
```

### 1.5 ComplaintType (Enum)

```csharp
public enum ComplaintType
{
    QualityDefect = 0,      // Minőségi hiba (selejt)
    QuantityShortage = 1,   // Mennyiségi hiány
    Documentation = 2,      // Dokumentációs hiba (hibás anyagbizonylat, stb.)
    DeliveryDamage = 3,     // Szállítás közbeni sérülés
    Other = 4               // Egyéb
}
```

### 1.6 ComplaintResponse (Owned Entity)

A beszállító válasza a reklamációra.

```csharp
public class ComplaintResponse
{
    public ResponseType Type { get; private set; }       // Accept | Reject | Partial | ProposalCounter
    public string ResponseText { get; private set; }     // Max 3000 char
    public decimal? OfferedAmount { get; private set; }  // Felajánlott kompenzáció
    public string? CounterProposal { get; private set; } // Ellenajánlat részletei
    public List<string> AttachmentPaths { get; private set; }  // MinIO path-ok

    // Audit
    public string RespondedBy { get; private set; }      // supplier sub (JWT)
    public DateTime RespondedAt { get; private set; }
}

public enum ResponseType
{
    Accept = 0,             // Teljes elfogadás
    Reject = 1,             // Visszautasítás
    Partial = 2,            // Részleges elfogadás
    ProposalCounter = 3     // Ellenajánlat
}
```

### 1.7 ComplaintResolution (Owned Entity)

A tenant oldali lezárás.

```csharp
public class ComplaintResolution
{
    public ResolutionType Type { get; private set; }
    public string Summary { get; private set; }          // Max 2000 char
    public decimal? FinalAmount { get; private set; }    // Végső kompenzáció
    public ResolutionAction Action { get; private set; } // CreditNote | Replacement | Refund | NoAction

    // Audit
    public string ResolvedBy { get; private set; }
    public DateTime ResolvedAt { get; private set; }
}

public enum ResolutionType
{
    Accepted = 0,           // Beszállító válasza elfogadva
    Rejected = 1,           // Beszállító válasza elutasítva (escalation-re megy)
    Compromised = 2,        // Kompromisszum
    Withdrawn = 3           // Tenant visszavonta a reklamációt
}

public enum ResolutionAction
{
    CreditNote = 0,         // Jóváírás a következő számlán
    Replacement = 1,        // Csereanyag küldése
    Refund = 2,             // Pénzvisszatérítés
    NoAction = 3            // Nincs akció (visszavonás esetén)
}
```

---

## 2. FSM Állapotok és Átmenetek (v2)

### 2.1 ComplaintStatus Enum

```csharp
public enum ComplaintStatus
{
    Draft = 0,              // Vázlat (szerkeszthető)
    Submitted = 1,          // Beküldve beszállítónak
    SupplierReviewing = 2,  // Beszállító átnézi
    SupplierResponded = 3,  // Beszállító válaszolt
    UnderReview = 4,        // Tenant átnézi a választ
    Resolved = 5,           // Lezárva (terminál)
    Escalated = 6,          // Eszkalálva (terminál)
    Withdrawn = 7           // Visszavonva (terminál)
}
```

### 2.2 FSM Diagram

```
                                    ┌─────────────────────────────────────────────────┐
                                    │                                                 │
Draft ──Submit()──▶ Submitted ──────┼──────▶ SupplierReviewing ──Respond()──▶ SupplierResponded
  │                     │           │                                              │
  │                     │           │                                              │
  └──Delete()           └──Withdraw()──▶ Withdrawn (terminál)                     │
                                    │                                              │
                                    │                              ┌───────────────┘
                                    │                              │
                                    │                              ▼
                                    │          UnderReview ◀────Accept()
                                    │              │
                                    │              ├──Resolve(Accepted)──▶ Resolved (terminál)
                                    │              ├──Resolve(Rejected)──▶ Escalated (terminál)
                                    │              ├──Resolve(Compromised)──▶ Resolved (terminál)
                                    │              └──Withdraw()──▶ Withdrawn (terminál)
                                    │
                                    └──────────────────────────────────────────────────┘

Terminál állapotok: Resolved, Escalated, Withdrawn
```

### 2.3 Aggregate Metódusok

```csharp
// Factory
public static Result<SupplierComplaint> Create(
    Guid tenantId,
    Guid supplierId,
    Guid deliveryId,
    Guid? purchaseOrderId,
    ComplaintType type,
    string subject,
    string description,
    decimal affectedQuantity,
    decimal? claimedAmount,
    string currency,
    QualityInspectionResult? qaResult,
    List<string> evidencePaths,
    string createdBy)
    → Draft + SupplierComplaintCreated event

// Tenant akciók
public Result Submit(string submittedBy)
    → Draft → Submitted + SupplierComplaintSubmitted event
    Guard: Status == Draft

public Result Withdraw(string withdrawnBy, string reason)
    → Submitted | SupplierReviewing | UnderReview → Withdrawn + SupplierComplaintWithdrawn event
    Guard: Status in [Submitted, SupplierReviewing, UnderReview]

// Beszállító akciók
public Result MarkAsReviewing(string reviewedBy)
    → Submitted → SupplierReviewing + SupplierComplaintReviewing event
    Guard: Status == Submitted

public Result Respond(ComplaintResponse response)
    → SupplierReviewing → SupplierResponded + SupplierComplaintResponded event
    Guard: Status == SupplierReviewing

// Tenant lezárás
public Result AcceptResponse(string acceptedBy)
    → SupplierResponded → UnderReview + SupplierResponseAccepted event
    Guard: Status == SupplierResponded

public Result Resolve(ComplaintResolution resolution)
    → UnderReview → Resolved | Escalated + SupplierComplaintResolved event
    Guard: Status == UnderReview
    If resolution.Type == Rejected → Escalated
    Else → Resolved
```

### 2.4 Domain Events

```csharp
// Complaint lifecycle
SupplierComplaintCreated(Id, TenantId, SupplierId, DeliveryId, Type, Subject)
SupplierComplaintSubmitted(Id, TenantId, SupplierId, SubmittedBy)
SupplierComplaintWithdrawn(Id, TenantId, Reason, WithdrawnBy)

// Supplier actions
SupplierComplaintReviewing(Id, TenantId, SupplierId, ReviewedBy)
SupplierComplaintResponded(Id, TenantId, SupplierId, ResponseType, RespondedBy)

// Resolution
SupplierResponseAccepted(Id, TenantId, AcceptedBy)
SupplierComplaintResolved(Id, TenantId, ResolutionType, ResolvedBy)
```

---

## 3. API Contract (v3)

### 3.1 Tenant-oldali Endpointok

```
POST   /procurement/api/supplier-complaints
       → Új reklamáció létrehozása (Draft státusz)
       Body: CreateSupplierComplaintRequest
       Response: 201 + SupplierComplaintDto
       Auth: tenant user + procurement.create_complaint permission

GET    /procurement/api/supplier-complaints
       → Lista (szűrhető: status, supplierId, dateRange)
       Query: ?status=Submitted&supplierId=xxx&from=2026-01-01&to=2026-12-31
       Response: 200 + PagedResult<SupplierComplaintSummaryDto>
       Auth: tenant user

GET    /procurement/api/supplier-complaints/{id}
       → Részletek (+ response, resolution)
       Response: 200 + SupplierComplaintDto
       Auth: tenant user

POST   /procurement/api/supplier-complaints/{id}/submit
       → Beküldés a beszállítónak
       Response: 200 + SupplierComplaintDto
       Auth: tenant user + procurement.submit_complaint permission

POST   /procurement/api/supplier-complaints/{id}/accept-response
       → Beszállító válaszának elfogadása (review fázisba lép)
       Response: 200 + SupplierComplaintDto
       Auth: tenant user + procurement.review_complaint permission

POST   /procurement/api/supplier-complaints/{id}/resolve
       → Lezárás
       Body: ResolveComplaintRequest
       Response: 200 + SupplierComplaintDto
       Auth: tenant user + procurement.resolve_complaint permission

DELETE /procurement/api/supplier-complaints/{id}
       → Visszavonás
       Body: WithdrawComplaintRequest (reason)
       Response: 200 + SupplierComplaintDto
       Auth: tenant user
```

### 3.2 Beszállítói Portál Endpointok

```
GET    /procurement/api/supplier-portal/complaints
       → Beszállító saját reklamációi (RLS: supplierId scope)
       Query: ?status=Submitted
       Response: 200 + PagedResult<SupplierComplaintSummaryDto>
       Auth: supplier role + supplierId claim

GET    /procurement/api/supplier-portal/complaints/{id}
       → Reklamáció részletei
       Response: 200 + SupplierComplaintDto
       Auth: supplier role, RLS: complaint.SupplierId == token.supplierId

POST   /procurement/api/supplier-portal/complaints/{id}/reviewing
       → Jelzi hogy átnézi
       Response: 200 + SupplierComplaintDto
       Auth: supplier role, RLS check

POST   /procurement/api/supplier-portal/complaints/{id}/respond
       → Válasz küldése
       Body: ComplaintResponseRequest
       Response: 200 + SupplierComplaintDto
       Auth: supplier role, RLS check
```

### 3.3 DTOs

```csharp
// Request DTOs
public record CreateSupplierComplaintRequest(
    Guid SupplierId,
    Guid DeliveryId,
    ComplaintType Type,
    string Subject,
    string Description,
    decimal AffectedQuantity,
    decimal? ClaimedAmount,
    string? Currency,
    List<string>? EvidencePaths
);

public record ComplaintResponseRequest(
    ResponseType Type,
    string ResponseText,
    decimal? OfferedAmount,
    string? CounterProposal,
    List<string>? AttachmentPaths
);

public record ResolveComplaintRequest(
    ResolutionType Type,
    string Summary,
    decimal? FinalAmount,
    ResolutionAction Action
);

public record WithdrawComplaintRequest(string Reason);

// Response DTOs
public record SupplierComplaintDto(
    Guid Id,
    string ComplaintNumber,
    Guid SupplierId,
    string SupplierName,
    Guid DeliveryId,
    Guid? PurchaseOrderId,
    ComplaintType Type,
    string Subject,
    string Description,
    decimal AffectedQuantity,
    decimal? ClaimedAmount,
    string? Currency,
    ComplaintStatus Status,
    QualityInspectionResultDto? QaResult,
    List<string> EvidencePaths,
    string CreatedBy,
    DateTime CreatedAt,
    ComplaintResponseDto? SupplierResponse,
    ComplaintResolutionDto? Resolution
);

public record SupplierComplaintSummaryDto(
    Guid Id,
    string ComplaintNumber,
    string SupplierName,
    ComplaintType Type,
    string Subject,
    ComplaintStatus Status,
    DateTime CreatedAt
);

public record ComplaintResponseDto(
    ResponseType Type,
    string ResponseText,
    decimal? OfferedAmount,
    string? CounterProposal,
    string RespondedBy,
    DateTime RespondedAt
);

public record ComplaintResolutionDto(
    ResolutionType Type,
    string Summary,
    decimal? FinalAmount,
    ResolutionAction Action,
    string ResolvedBy,
    DateTime ResolvedAt
);

public record QualityInspectionResultDto(
    QualityStatus Status,
    decimal AcceptedQuantity,
    decimal RejectedQuantity,
    string? DefectDescription
);
```

---

## 4. Beszállítói Auth Stratégia (v3 Security)

### 4.1 Keycloak Konfiguráció

**Nem külön realm** — a beszállítók ugyanabban a Keycloak realm-ben (`spaceos`) vannak, de:

1. **Szerepkör:** `supplier` (realm role)
2. **Attribútum:** `supplierId` (UUID, a `Supplier` aggregate ID-ja)
3. **Claim mapping:** `supplierId` → JWT `supplier_id` claim

```json
// Keycloak user attribútumok (példa)
{
  "username": "supplier-acme@example.com",
  "enabled": true,
  "attributes": {
    "tenantId": ["63ef28b6-a43b-4d3f-a076-759a47911559"],
    "supplierId": ["a1b2c3d4-e5f6-7890-abcd-ef1234567890"]
  },
  "realmRoles": ["supplier"]
}
```

### 4.2 RLS Policy

```sql
-- supplier_complaint tábla RLS
CREATE POLICY "supplier_own_complaints" ON supplier_complaint
  FOR SELECT
  USING (
    current_setting('app.user_role', true) = 'tenant_admin'
    OR current_setting('app.user_role', true) = 'procurement_manager'
    OR current_setting('app.user_role', true) = 'qa_manager'
    OR (
      current_setting('app.user_role', true) = 'supplier'
      AND "SupplierId" = current_setting('app.supplier_id', true)::uuid
    )
  );

-- UPDATE/INSERT/DELETE policies hasonlóan
```

### 4.3 JWT Claims Resolver

```csharp
// A meglévő ClaimsResolver bővítése
public static class ClaimsExtensions
{
    public static Guid? GetSupplierId(this ClaimsPrincipal principal)
        => principal.FindFirst("supplier_id")?.Value is { } val
           && Guid.TryParse(val, out var id) ? id : null;

    public static bool IsSupplier(this ClaimsPrincipal principal)
        => principal.IsInRole("supplier");
}
```

### 4.4 API Guard

```csharp
// Supplier-portal controller-en
[Authorize(Roles = "supplier")]
[Route("procurement/api/supplier-portal/complaints")]
public class SupplierPortalComplaintsController : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var supplierId = User.GetSupplierId();
        if (supplierId is null)
            return Forbid();

        var complaint = await _repository.GetByIdAsync(id);
        if (complaint is null)
            return NotFound();

        // RLS check (double-defense)
        if (complaint.SupplierId != supplierId)
            return Forbid();

        return Ok(_mapper.Map(complaint));
    }
}
```

---

## 5. Database Schema (v3)

### 5.1 DDL

```sql
-- Delivery bővítés
ALTER TABLE delivery ADD COLUMN "QualityStatus" smallint NULL;
ALTER TABLE delivery ADD COLUMN "AcceptedQuantity" decimal NULL;
ALTER TABLE delivery ADD COLUMN "RejectedQuantity" decimal NULL;
ALTER TABLE delivery ADD COLUMN "DefectDescription" varchar(2000) NULL;
ALTER TABLE delivery ADD COLUMN "DefectPhotoPaths" jsonb NULL;
ALTER TABLE delivery ADD COLUMN "InspectedAt" timestamptz NULL;
ALTER TABLE delivery ADD COLUMN "InspectedBy" varchar(255) NULL;

-- SupplierComplaint
CREATE TABLE supplier_complaint (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId" uuid NOT NULL REFERENCES "Tenants"("Id"),
    "ComplaintNumber" varchar(20) NOT NULL,
    "SupplierId" uuid NOT NULL,
    "DeliveryId" uuid NOT NULL,
    "PurchaseOrderId" uuid NULL,
    "Type" smallint NOT NULL,
    "Subject" varchar(200) NOT NULL,
    "Description" varchar(5000) NOT NULL,
    "AffectedQuantity" decimal NOT NULL CHECK ("AffectedQuantity" > 0),
    "ClaimedAmount" decimal NULL,
    "Currency" char(3) NULL,
    "QualityStatus" smallint NULL,
    "QaAcceptedQty" decimal NULL,
    "QaRejectedQty" decimal NULL,
    "QaDefectDescription" varchar(2000) NULL,
    "EvidencePaths" jsonb NULL,
    "Status" smallint NOT NULL DEFAULT 0,
    "CreatedBy" varchar(255) NOT NULL,
    "CreatedAt" timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    -- Response (owned)
    "ResponseType" smallint NULL,
    "ResponseText" varchar(3000) NULL,
    "OfferedAmount" decimal NULL,
    "CounterProposal" varchar(2000) NULL,
    "ResponseAttachmentPaths" jsonb NULL,
    "RespondedBy" varchar(255) NULL,
    "RespondedAt" timestamptz NULL,
    -- Resolution (owned)
    "ResolutionType" smallint NULL,
    "ResolutionSummary" varchar(2000) NULL,
    "FinalAmount" decimal NULL,
    "ResolutionAction" smallint NULL,
    "ResolvedBy" varchar(255) NULL,
    "ResolvedAt" timestamptz NULL,
    -- xmin for optimistic concurrency
    CONSTRAINT fk_supplier FOREIGN KEY ("SupplierId") REFERENCES supplier("Id"),
    CONSTRAINT fk_delivery FOREIGN KEY ("DeliveryId") REFERENCES delivery("Id"),
    CONSTRAINT fk_po FOREIGN KEY ("PurchaseOrderId") REFERENCES purchase_order("Id")
);

-- Indexes
CREATE UNIQUE INDEX "IX_Complaint_Tenant_Number" ON supplier_complaint("TenantId", "ComplaintNumber");
CREATE INDEX "IX_Complaint_Tenant_Supplier" ON supplier_complaint("TenantId", "SupplierId");
CREATE INDEX "IX_Complaint_Tenant_Status" ON supplier_complaint("TenantId", "Status") WHERE "Status" NOT IN (5, 6, 7);
CREATE INDEX "IX_Complaint_Delivery" ON supplier_complaint("DeliveryId");

-- RLS
ALTER TABLE supplier_complaint ENABLE ROW LEVEL SECURITY;
ALTER TABLE supplier_complaint FORCE ROW LEVEL SECURITY;

CREATE POLICY "tenant_isolation" ON supplier_complaint
  USING ("TenantId" = current_setting('app.tenant_id')::uuid);

CREATE POLICY "supplier_own_complaints" ON supplier_complaint
  FOR SELECT
  USING (
    current_setting('app.user_role', true) IN ('tenant_admin', 'procurement_manager', 'qa_manager')
    OR (
      current_setting('app.user_role', true) = 'supplier'
      AND "SupplierId" = current_setting('app.supplier_id', true)::uuid
    )
  );

-- Monoton számgenerátor
CREATE OR REPLACE FUNCTION fn_next_complaint_number(p_tenant_id uuid, p_year int)
RETURNS text AS $$
DECLARE
    v_seq int;
BEGIN
    PERFORM pg_advisory_xact_lock(hashtext(p_tenant_id::text || 'complaint' || p_year::text));

    INSERT INTO procurement_sequences("TenantId", "SequenceType", "Year", "LastValue")
    VALUES (p_tenant_id, 'complaint', p_year, 1)
    ON CONFLICT ("TenantId", "SequenceType", "Year")
    DO UPDATE SET "LastValue" = procurement_sequences."LastValue" + 1
    RETURNING "LastValue" INTO v_seq;

    RETURN 'SC-' || p_year::text || '-' || lpad(v_seq::text, 5, '0');
END;
$$ LANGUAGE plpgsql;

-- Audit log entry type bővítés
-- (A meglévő procurement_audit_log-ot használjuk)
```

### 5.2 Sequence Tábla (ha nincs)

```sql
CREATE TABLE IF NOT EXISTS procurement_sequences (
    "TenantId" uuid NOT NULL,
    "SequenceType" varchar(20) NOT NULL,
    "Year" int NOT NULL,
    "LastValue" int NOT NULL DEFAULT 0,
    PRIMARY KEY ("TenantId", "SequenceType", "Year")
);
```

---

## 6. Implementation Guide (v4)

### 6.1 Track Breakdown

| Track | Feladat | Effort |
|-------|---------|--------|
| **A** | Domain: `SupplierComplaint` aggregate, VOs, Events, Repository interface | 1 nap |
| **B** | Infrastructure: EF config, Migration, RLS policies | 0.5 nap |
| **C** | Application: Commands, Handlers, DTOs | 1 nap |
| **D** | API: Controllers (tenant + supplier-portal), Validators | 1 nap |
| **E** | Tests: Unit (aggregate), Integration (API) | 1 nap |

### 6.2 File Structure

```
spaceos-modules-procurement/src/
├── SpaceOS.Modules.Procurement.Domain/
│   ├── Aggregates/
│   │   ├── SupplierComplaint.cs        (ÚJ)
│   │   ├── ComplaintResponse.cs        (ÚJ)
│   │   ├── ComplaintResolution.cs      (ÚJ)
│   │   └── Delivery.cs                 (BŐVÍTÉS)
│   ├── ValueObjects/
│   │   └── QualityInspectionResult.cs  (ÚJ)
│   ├── Enums/
│   │   ├── ComplaintStatus.cs          (ÚJ)
│   │   ├── ComplaintType.cs            (ÚJ)
│   │   ├── ResponseType.cs             (ÚJ)
│   │   ├── ResolutionType.cs           (ÚJ)
│   │   ├── ResolutionAction.cs         (ÚJ)
│   │   └── QualityStatus.cs            (ÚJ)
│   ├── Events/
│   │   ├── SupplierComplaintCreated.cs (ÚJ)
│   │   ├── SupplierComplaintSubmitted.cs (ÚJ)
│   │   ├── SupplierComplaintResponded.cs (ÚJ)
│   │   └── SupplierComplaintResolved.cs  (ÚJ)
│   └── Interfaces/
│       └── IComplaintRepository.cs     (ÚJ)
├── SpaceOS.Modules.Procurement.Application/
│   ├── Commands/
│   │   ├── CreateComplaintCommand.cs   (ÚJ)
│   │   ├── SubmitComplaintCommand.cs   (ÚJ)
│   │   ├── RespondToComplaintCommand.cs (ÚJ)
│   │   └── ResolveComplaintCommand.cs  (ÚJ)
│   ├── Handlers/
│   │   └── ComplaintCommandHandlers.cs (ÚJ)
│   ├── DTOs/
│   │   └── ComplaintDtos.cs            (ÚJ)
│   └── Mappers/
│       └── ComplaintMapper.cs          (ÚJ)
├── SpaceOS.Modules.Procurement.Infrastructure/
│   ├── Persistence/
│   │   └── Configuration/
│   │       └── SupplierComplaintConfiguration.cs (ÚJ)
│   └── Migrations/
│       └── YYYYMMDD_AddSupplierComplaint.cs (ÚJ)
└── SpaceOS.Modules.Procurement.Api/
    └── Controllers/
        ├── SupplierComplaintsController.cs (ÚJ)
        └── SupplierPortalComplaintsController.cs (ÚJ)
```

### 6.3 Discovery Commands

```bash
# Domain structure
ls backend/spaceos-modules-procurement/src/SpaceOS.Modules.Procurement.Domain/Aggregates/

# Meglévő Delivery aggregate
cat backend/spaceos-modules-procurement/src/SpaceOS.Modules.Procurement.Domain/Aggregates/Delivery.cs

# Meglévő Supplier aggregate
cat backend/spaceos-modules-procurement/src/SpaceOS.Modules.Procurement.Domain/Aggregates/Supplier.cs

# EF Configuration minták
ls backend/spaceos-modules-procurement/src/SpaceOS.Modules.Procurement.Infrastructure/Persistence/Configuration/

# API Controllers
ls backend/spaceos-modules-procurement/src/SpaceOS.Modules.Procurement.Api/Controllers/
```

### 6.4 DoD Checklist

- [ ] Domain: `SupplierComplaint` aggregate + owned entities + VOs
- [ ] Domain: Összes FSM transition implementálva `Result<T>` return-nel
- [ ] Domain: 6 domain event + handler-ek (audit log)
- [ ] Infra: EF configuration (owned entity mapping, composite FK)
- [ ] Infra: Migration létrehozva és tesztelve
- [ ] Infra: RLS policies alkalmazva
- [ ] API: Tenant controller (7 endpoint)
- [ ] API: Supplier-portal controller (4 endpoint)
- [ ] API: FluentValidation rules
- [ ] Security: `supplierId` claim resolver
- [ ] Security: Double-defense RLS + controller guard
- [ ] Test: Unit tesztek (aggregate FSM transitions) — min. 15
- [ ] Test: Integration tesztek (API happy path) — min. 10
- [ ] Test: E2E teszt (tenant létrehoz → supplier válaszol → tenant lezár)

### 6.5 Claude Code Agent Block

```markdown
## Track A: Domain Layer

1. Hozd létre a `QualityStatus`, `ComplaintStatus`, `ComplaintType`, `ResponseType`, `ResolutionType`, `ResolutionAction` enum-okat
2. Hozd létre a `QualityInspectionResult` value object-et
3. Bővítsd a `Delivery` aggregate-ot a QA mezőkkel + `RecordQualityInspection()` metódussal
4. Hozd létre a `SupplierComplaint` aggregate-ot az összes FSM metódussal
5. Hozd létre a domain event-eket
6. Hozd létre az `IComplaintRepository` interface-t

## Track B: Infrastructure

1. Hozd létre a `SupplierComplaintConfiguration.cs`-t (owned entity mapping)
2. Bővítsd a `DeliveryConfiguration`-t a QA mezőkkel
3. Generálj migration-t: `dotnet ef migrations add AddSupplierComplaint`
4. Alkalmazd az RLS policy-kat a migration-ben

## Track C-D: Application + API

1. Commands + Handlers
2. DTOs + Mappers
3. Controllers + Validators
4. Regisztráld a DI-ban

## Track E: Tests

1. Unit: SupplierComplaintTests (FSM transitions)
2. Integration: ComplaintApiTests (CRUD + FSM)
3. E2E: Complaint flow test
```

---

## 7. Open Questions (Nincs — minden döntés meghozva)

Minden kérdés a Pre-decision szekcióban eldöntve.

---

## 8. Referenciák

- Procurement v2: `docs/tasks/archive/SpaceOS_Modules_Procurement_v2_Architecture_v4.md`
- ADR-039: Cross-module integration pattern
- Sales v4: Quote → Order conversion pattern (ADR-039 precedens)
- Supplier aggregate: `spaceos-modules-procurement/src/.../Aggregates/Supplier.cs`
- Delivery aggregate: `spaceos-modules-procurement/src/.../Aggregates/Delivery.cs`
