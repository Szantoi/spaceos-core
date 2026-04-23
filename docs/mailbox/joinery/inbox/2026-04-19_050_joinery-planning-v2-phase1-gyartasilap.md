---
id: MSG-JOINERY-050
from: root
to: joinery
type: task
priority: high
status: READ
ref: MSG-CUTTING-026-DONE
created: 2026-04-19
---

# JOINERY-050 — JOINERY Planning v2 Phase 1: Gyártásilap PDF Generation

## Context

**Cutting Planning v1 Phase 1 DONE** ✅ (136 tests, data model + 5 endpoints live)

**Scope:** Gyártásilap (manufacturing spec sheet) PDF generation with 4 label variants

**Timeline:** 5-7 days, 1 FTE

**Doorstar context:** Production flow — operators print gyártásilap + labels at cutting station before assembly

---

## Phase 1: PDF Generation + Label Variants

### Task 1: Domain Model + EF Migration

**Entities:**

1. **Gyartasilap**
   - `id` (UUID)
   - `joynerOrderId` (FK)
   - `planId` (Cutting plan reference)
   - `version` ("v1.0")
   - `pdfContent` (BYTEA)
   - `storageUrl` (MinIO WORM path)
   - `status` (Draft | Finalized | Archived)
   - `createdAt`, `updatedAt`

**EF Migration:**
- Table: `Gyartasilaps`
- RLS: Inherit from JoineryOrder (tenant isolation via FK)
- Indexes: (joinery_order_id, status), (plan_id, created_at desc)

**Tests:** 5+ EF/RLS tests

**Location:** `SpaceOS.Modules.Joinery/Core/Gyartasilap.cs` + migration

---

### Task 2: QuestPDF PDF Builder

**File:** `Application/Documents/GyartasilapPdfBuilder.cs`

**Content structure (all pages):**

```
[PAGE 1]
┌─────────────────────────────────┐
│ GYÁRTÁSILAP v1.0                │
│ Order ID: J-2026-0519-001       │
│ Date: 2026-04-19 · Customer: XYZ│
├─────────────────────────────────┤
│ Material List (table)            │
│ - 18mm MDF 2400×1200 (qty: 2)   │
│ - Birch veneer (roll: 100m)      │
│ - PVA glue (500ml)              │
├─────────────────────────────────┤
│ Cutting Jobs (table)             │
│ - Job CJ-001 | 18mm MDF 800×600 │
│ - Job CJ-002 | 18mm MDF 1200×X  │
├─────────────────────────────────┤
│ Assembly Instructions            │
│ 1. Glue joints (30 min cure)    │
│ 2. Drill dowel holes (5mm)      │
│ 3. Insert dowels + frame        │
│ 4. QC check dimensions          │
├─────────────────────────────────┤
│ LABELS (choose variant)          │
│ L1 (Basic)   L2 (Premium)        │
│ L3 (Barcode) L4 (Full)           │
└─────────────────────────────────┘

[PAGE 2+] — Label sheets (1 label per label page, repeat 4×)
```

**Label Variants:**

1. **L1 (Basic)** — 10pt font, minimal
   ```
   ORDER: J-2026-0519-001
   DATE: 2026-04-19
   MATERIAL: 18mm MDF
   QTY: 2 sheets
   ```

2. **L2 (Premium)** — 10pt + QR code
   ```
   ORDER: J-2026-0519-001
   DATE: 2026-04-19
   QR CODE: [QR linking to joinery order]
   MATERIAL: 18mm MDF
   ```

3. **L3 (Barcode)** — 11pt + barcode generator
   ```
   BARCODE: [Code128(J-2026-0519-001)]
   ORDER: J-2026-0519-001
   DATE: 2026-04-19
   MATERIAL: 18mm MDF
   ```

4. **L4 (Full)** — 12pt + barcode + QR + margin notes
   ```
   BARCODE: [Code128(...)]
   QR: [QR code]
   ORDER: J-2026-0519-001
   MATERIAL: 18mm MDF
   NOTE: Glue cure 30 min, then drill
   ```

**PDF Builder Signature:**

```csharp
public class GyartasilapPdfBuilder
{
    public byte[] GeneratePdf(
        JoineryOrder order,
        CuttingJob[] jobs,
        LabelVariant variant) // L1, L2, L3, L4
    {
        // QuestPDF Document.Create() → PDF bytes
        // Render: header + materials + jobs + assembly + labels
    }
}
```

**Tests:** 12+ unit tests (content rendering, variants, text formatting)

---

### Task 3: Command Handler + MinIO Storage

**File:** `Application/Commands/GenerateAndStoreGyartasilap/`

**Command:**

```csharp
public record GenerateAndStoreGyartasilapCommand(
    Guid JoineryOrderId,
    Guid CuttingPlanId,
    string LabelVariant = "L1");
```

**Handler Logic:**

1. Fetch JoineryOrder + CuttingJobs
2. Validate: order not archived
3. Select label variant (L1–L4)
4. Build PDF (QuestPDF)
5. Store in MinIO `gyartasilap/{tenantId}/{planId}/gyartasilap_{variant}.pdf` (WORM)
6. Create Gyartasilap aggregate (status: Draft)
7. Save to DB
8. Return: { gyartasilapId, storageUrl, status }

**MinIO Bucket:** `gyartasilap` (WORM mode enabled)

**RLS:** Enforce via TenantCommandInterceptor (inherited from JoineryOrder FK)

**Tests:** 8+ integration tests (MinIO roundtrip, RLS, error handling)

---

### Task 4: HTTP Endpoints

**API Routes:**

```
POST   /api/joinery/gyartasilap/generate
       Body: { joynerOrderId, cuttingPlanId, labelVariant? }
       Response: 201 + { gyartasilapId, storageUrl, status }

GET    /api/joinery/gyartasilap/{gyartasilapId}
       Response: 200 + binary PDF (Content-Type: application/pdf)

PUT    /api/joinery/gyartasilap/{gyartasilapId}/finalize
       Response: 200 + { status: "Finalized" }

GET    /api/joinery/gyartasilap/{joynerOrderId}/list
       Params: status? (Draft | Finalized | Archived)
       Response: 200 + { variants[], createdAt, status }
```

**Implementation:**
- Use existing `JoineryEndpoints.cs` pattern
- Validate request models (FluentValidation)
- Return appropriate status codes (201, 200, 400, 404, 409)
- RLS enforcement automatic via DbContext interceptor

**Tests:** 10+ E2E tests (HTTP roundtrips, all 4 label variants)

---

## Technical Specs

**Database:** PostgreSQL (spaceos_joinery)

**NuGet:** QuestPDF 2024.10 (Community License)

**MinIO Bucket:** `gyartasilap` with WORM (immutable) enabled

**RLS:** Tenant isolation via `tid` claim (inherited from JoineryOrder)

**Validation:**
- labelVariant: must be "L1" | "L2" | "L3" | "L4"
- joynerOrderId: must exist + not archived
- cuttingPlanId: must reference cutting job allocations

**Error Handling:**
- 400: Invalid labelVariant
- 404: Order/Plan not found
- 409: Order already finalized (can't generate new variant)

---

## Deliverables (Phase 1 Complete)

- ✅ EF Core migration + Gyartasilap entity
- ✅ QuestPDF builder (4 label variants)
- ✅ GenerateAndStoreGyartasilap command handler
- ✅ MinIO WORM integration
- ✅ 4 HTTP endpoints working (CRUD)
- ✅ 30+ tests passing (unit + integration + E2E)
- ✅ Postman/curl examples documented
- ✅ Ready for Phase 2 (UI integration + batch generation)

---

## Timeline

- **Day 1–2:** Domain + EF migration
- **Day 2–3:** QuestPDF builder (4 variants)
- **Day 3–4:** Command handler + MinIO
- **Day 4–5:** API endpoints + tests

Total: 5-7 days, ~35 developer-hours

---

## Next: Phase 2

After Phase 1 DONE (MSG-JOINERY-050-DONE):
- Phase 2 inbox message (UI batch generation + supplier order list)
- Portal: Gyártásilap view + download
- Integration: Link to Inventory (material consumption tracking)

---

## Reference Docs

- Task plan: `docs/tasks/new/JOINERY-PLANNING-V2_gyartasilap-pdf-phase1.md`
- Growth strategy: `SpaceOS_Growth_Strategy_v1.md`
- Cutting Planning: Already DONE (Phase 1)

---

**Execute Phase 1 immediately. Report JOINERY-050-DONE when PDF generation + MinIO storage complete and tested.**
