---
id: MSG-INVENTORY-051
from: root
to: inventory
type: task
priority: high
status: READ
ref: MSG-JOINERY-050-QUEUED
created: 2026-04-19
---

# INVENTORY-051 — INVENTORY Planning v1 Phase 1: Offcut Tracking + Reuse Workflow

## Context

**Cutting Planning v1 Phase 1+2 COMPLETE** ✅ (181 tests, strategies + yield live)

**Joinery Planning v2 Phase 1 ACTIVE** 🟢 (PDF generation, QuestPDF)

**Scope:** Offcut (hulladék) nyomon követés + újrahasznosítás workflow

**Timeline:** 4-5 days, 1 FTE

**Doorstar context:** Fenntarthatóság — hulladék minimalizálás, offcut leltár, újrahasznosítás enable

---

## Phase 1: Offcut Tracking + Reuse Management

### Task 1: Domain Model + EF Migration

**Entities:**

1. **Offcut**
   - `id` (UUID)
   - `cuttingJobId` (FK to CuttingJob)
   - `materialId` (Material reference)
   - `materialCode` (e.g., "18mm_MDF")
   - `widthMm`, `heightMm`, `thicknessMm` (decimal)
   - `volumeM3` (computed: W × H × T / 1e9)
   - `weightKg` (computed: Volume × density)
   - `status` (Available | Reserved | Used | Scrapped)
   - `createdAt`, `usedAt`
   - `usedInJobId` (nullable FK — job that reused this offcut)
   - `tenantId` (RLS)

2. **OffcutReservation**
   - `id` (UUID)
   - `offcutId` (FK)
   - `jobId` (Job planning to use it)
   - `status` (Pending | Approved | Cancelled)
   - `createdAt`, `expiresAt` (auto-release after 7 days)

**EF Migration:**
- Tables: `Offcuts`, `OffcutReservations`
- RLS: `ENABLE ROW LEVEL SECURITY` on Offcuts
- Indexes: (cuttingJobId, status), (status, createdAt desc), (volumeM3 desc)

**Tests:** 5+ EF/RLS integration tests

**Location:** `SpaceOS.Modules.Inventory/Core/Offcut.cs` + migration

---

### Task 2: Event Capture — CuttingJobCompleted → Offcut

**File:** `Application/Events/CuttingJobCompletedEventHandler.cs`

**Flow:**

When CuttingJob.Status → "Cut" (cutting complete):
1. Emit `CuttingJobCompletedEvent` { jobId, materialId, wastePercent }
2. Handler creates Offcut records for scrap pieces
3. Status: Available (ready for reuse)

**Stub Implementation (v1):**
- Simple waste % calculation (15% default)
- Single offcut piece per job (v1.5: multi-piece tracking)
- Material metadata lookup (density for weight calculation)

**Command Handler Pseudocode:**

```csharp
public async Task Handle(CuttingJobCompletedEvent evt, CancellationToken ct)
{
    // 1. Fetch job + material
    var job = await _cuttingRepository.GetByIdAsync(evt.JobId);
    var material = await _materialRepository.GetByIdAsync(job.MaterialId);
    
    // 2. Calculate waste volume
    var wasteVolume = CalculateVolume(job.W, job.H, job.T) * 0.15m; // 15% waste
    
    // 3. Create Offcut (Available)
    var offcut = new Offcut(
        id: Guid.NewGuid(),
        cuttingJobId: job.Id,
        materialId: material.Id,
        widthMm: job.WidthMm * 0.5m,  // stub: half size
        heightMm: job.HeightMm * 0.5m,
        thicknessMm: job.ThicknessMm,
        volumeM3: wasteVolume,
        weightKg: wasteVolume * material.DensityKgPerM3,
        status: OffcutStatus.Available,
        tenantId: job.TenantId);
    
    await _repository.AddAsync(offcut);
}
```

**Tests:** 5+ integration tests (event handling, Offcut creation, material lookup)

---

### Task 3: Reuse Workflow — Commands

**3 Commands:**

1. **ReserveOffcutCommand**
   ```csharp
   public record ReserveOffcutCommand(Guid OffcutId, Guid JobId);
   // Handler: Create OffcutReservation (Pending), expiresAt = now + 7 days
   // Response: { reservationId, expiresAt }
   ```

2. **ApproveOffcutReservationCommand**
   ```csharp
   public record ApproveOffcutReservationCommand(Guid ReservationId);
   // Handler: Offcut.Status → Reserved, Reservation.Status → Approved
   // Response: { status: "Approved" }
   ```

3. **UseOffcutInJobCommand**
   ```csharp
   public record UseOffcutInJobCommand(Guid OffcutId, Guid JobId);
   // Handler: Offcut.Status → Used, Offcut.UsedInJobId = JobId
   // Response: { status: "Used", usedInJobId }
   ```

**Validation:**
- Offcut must exist + Status == Available
- Reservation must exist + not expired
- Job must exist + have capacity for this material

**Error Handling:**
- 404: Offcut/Reservation not found
- 409: Offcut not Available (already Used/Scrapped)
- 410: Reservation expired (7 days passed)

**Tests:** 8+ command handler tests (full lifecycle, expiry, validation)

---

### Task 4: HTTP Endpoints

**API Routes:**

```
GET    /api/inventory/offcuts
       Filters: status? (Available | Reserved | Used)
               material? (materialCode)
               minVolumeM3? (float)
               createdAfter? (ISO 8601 date)
       Paging: page=1&pageSize=20
       Response: 200 + { offcuts[], total, page, pageSize }

GET    /api/inventory/offcuts/{offcutId}
       Response: 200 + { offcut, reservationHistory[] }

POST   /api/inventory/offcuts/{offcutId}/reserve
       Body: { jobId }
       Response: 201 + { reservationId, expiresAt }

POST   /api/inventory/offcuts/{offcutId}/approve-reservation
       Body: { reservationId }
       Response: 200 + { status: "Approved" }

POST   /api/inventory/offcuts/{offcutId}/use
       Body: { jobId }
       Response: 200 + { status: "Used", usedInJobId, usedAt }

GET    /api/inventory/offcuts/stats/summary
       Response: 200 + {
         totalAvailableVolumeM3: 12.5,
         totalAvailableWeightKg: 187.5,
         availableByMaterial: { "18mm_MDF": { volumeM3, weightKg }, ... },
         reservedCount: 3,
         usedCount: 127,
         scrappedCount: 45
       }
```

**Implementation:**
- Use existing `InventoryEndpoints.cs` pattern
- FluentValidation for requests
- RLS enforced via DbContext interceptor
- Authorization: `[Authorize(Policy = "ManufacturerOnly")]`

**Tests:** 10+ E2E tests (list + filters, detail, reserve/approve/use, stats)

---

## Technical Specs

**Database:** PostgreSQL (spaceos_inventory)

**RLS:** Tenant isolation via `tid` claim (inherited from module-level RLS)

**Validation:**
- offcutId: must exist
- jobId: must exist + have material space
- status: Must be one of enum values
- volumeM3: must be > 0

**Error Handling:**
- 400: Invalid input (negative volume, unknown material)
- 404: Offcut/Job/Reservation not found
- 409: Status conflict (Offcut not Available)
- 410: Reservation expired

---

## Deliverables (Phase 1 Complete)

- ✅ EF Core migration (Offcut, OffcutReservation)
- ✅ Event handler (CuttingJobCompleted → Offcut creation)
- ✅ Reuse workflow (3 commands: Reserve, Approve, Use)
- ✅ 6 HTTP endpoints (list, detail, reserve, approve, use, stats)
- ✅ 33+ tests passing (domain + event + command + API)
- ✅ RLS enforced (tenant isolation)
- ✅ Postman/curl examples documented
- ✅ Ready for Phase 2 (advanced geometry + scrap sale)

---

## Timeline

- **Day 1:** Domain model + EF migration
- **Day 1–2:** Event capture (CuttingJobCompleted → Offcut)
- **Day 2–3:** Reuse workflow (commands)
- **Day 3–4:** API endpoints + tests
- **Day 4–5:** E2E validation

Total: 4-5 days, ~25 developer-hours

---

## Next: Phase 2 (Backlog)

After Phase 1 DONE (MSG-INVENTORY-051-DONE):
- Advanced geometry tracking (actual piece dimensions)
- Batch consolidation (combine small pieces)
- Scrap sale integration (supplier orders for waste)
- Offcut forecasting (future availability prediction)

---

## Reference Docs

- Task plan: `docs/tasks/new/INVENTORY-PLANNING-V1_offcut-tracking-phase1.md`
- Cutting Planning Phase 1: ✅ DONE
- Joinery Planning Phase 1: 🟢 ACTIVE

---

**Execute Phase 1 immediately. Report INVENTORY-051-DONE when offcut tracking + reuse workflow complete and tested.**
