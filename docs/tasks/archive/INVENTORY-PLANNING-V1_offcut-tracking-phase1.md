---
id: INVENTORY-PLANNING-V1
title: Inventory Planning v1 — Offcut Tracking + Reuse Workflow (Phase 1)
status: new
priority: high
assignee: INVENTORY
epic: inventory-planning-v1
blocked_by: JOINERY-PLANNING-V2
created: 2026-04-19
updated: 2026-04-19
docs:
  - docs/tasks/active/CUTTING-PLANNING-V1_initialization.md
  - SpaceOS_Growth_Strategy_v1.md
---

# INVENTORY-PLANNING-V1 — Phase 1: Offcut Tracking + Reuse Workflow

## Overview

**Scope:** Material offcut tracking + scrap management + reuse workflow

**Why:** Doorstar sustainability — minimize waste, track offcut inventory, enable reuse in future jobs

**Phase 1 Timeline:** 4-5 days (1 FTE)

---

## Domain Model

### 1. Offcut Entity

```csharp
public class Offcut
{
    public Guid Id { get; private set; }
    public Guid CuttingJobId { get; private set; }     // FK to CuttingJob
    public Guid MaterialId { get; private set; }        // Material reference
    public string MaterialCode { get; private set; }    // e.g., "18mm_MDF"
    public decimal WidthMm { get; private set; }
    public decimal HeightMm { get; private set; }
    public decimal ThicknessMm { get; private set; }
    public decimal VolumeM3 { get; private set; }       // Calculated: W × H × T / 1e9
    public decimal WeightKg { get; private set; }       // Calculated: Volume × density
    
    public OffcutStatus Status { get; private set; }    // Available | Reserved | Used | Scrapped
    public DateTime CreatedAt { get; private set; }
    public DateTime? UsedAt { get; private set; }
    public string? UsedInJobId { get; private set; }    // FK to job that used this offcut
    
    public Guid TenantId { get; private set; }          // RLS
}

public enum OffcutStatus { Available, Reserved, Used, Scrapped }
```

### 2. OffcutReservation (temporary hold)

```csharp
public class OffcutReservation
{
    public Guid Id { get; private set; }
    public Guid OffcutId { get; private set; }          // FK to Offcut
    public Guid JobId { get; private set; }             // Job planning to use it
    public ReservationStatus Status { get; private set; } // Pending | Approved | Cancelled
    public DateTime CreatedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }    // Auto-release after 7 days
}

public enum ReservationStatus { Pending, Approved, Cancelled }
```

---

## Phase 1 Tasks

### Task 1: Domain Model + EF Migration (1 day)

**Entities:**
- `Offcut` — material piece tracking
- `OffcutReservation` — temporary hold mechanism

**EF Configuration:**
- Tables: `Offcuts`, `OffcutReservations`
- RLS: `ENABLE ROW LEVEL SECURITY` on Offcuts (tenant isolation via `tid` claim)
- Indexes: (cuttingJobId, status), (status, createdAt desc), (volumeM3 desc)

**Tests:** 5+ EF/RLS tests

---

### Task 2: Event Capture (Cutting → Offcut)

**File:** `Application/Events/CuttingJobCompletedEventHandler.cs`

**Flow:**
1. When CuttingJob status → "Cut" (cutting complete):
   2. Emit `CuttingJobCompletedEvent` with { jobId, materialId, dimensions[], waste% }
   3. Handler creates `Offcut` record for each scrap piece
   4. Status: `Available` (ready for reuse)

**Command Handler:** `CreateOffcutFromCuttingWaste`

```csharp
public async Task Handle(CuttingJobCompletedEvent evt, CancellationToken ct)
{
    // 1. Fetch CuttingJob + material metadata
    var job = await _cuttingRepository.GetByIdAsync(evt.JobId);
    var material = await _materialRepository.GetByIdAsync(job.MaterialId);
    
    // 2. Calculate offcut pieces (stub: simple waste %)
    var wastePercent = 0.15; // 15% waste (configured by strategy)
    var wasteVolume = CalculateVolume(job.WidthMm, job.HeightMm, job.ThicknessMm) 
                    * wastePercent;
    
    // 3. Create Offcut records
    var offcuts = new List<Offcut>();
    // Stub: single offcut for simplicity (v1.5: multi-piece tracking)
    offcuts.Add(new Offcut(
        id: Guid.NewGuid(),
        cuttingJobId: job.Id,
        materialId: material.Id,
        materialCode: material.Code,
        widthMm: job.WidthMm * 0.5m,  // Example: half of original
        heightMm: job.HeightMm * 0.5m,
        thicknessMm: job.ThicknessMm,
        volumeM3: wasteVolume,
        weightKg: wasteVolume * material.DensityKgPerM3,
        status: OffcutStatus.Available,
        tenantId: job.TenantId));
    
    await _repository.AddRangeAsync(offcuts);
}
```

**Tests:** 5+ integration tests (event handling, offcut creation)

---

### Task 3: Offcut Reuse Workflow (1 day)

**Commands:**

1. **ReserveOffcutCommand**
```csharp
public record ReserveOffcutCommand(
    Guid OffcutId,
    Guid JobId);  // Job planning to use it

// Handler: Create OffcutReservation (Pending)
// Response: { reservationId, expiresAt }
```

2. **ApproveOffcutReservationCommand**
```csharp
public record ApproveOffcutReservationCommand(
    Guid ReservationId);

// Handler: Offcut.Status → Reserved, Reservation.Status → Approved
// Response: { status: "Approved" }
```

3. **UseOffcutInJobCommand**
```csharp
public record UseOffcutInJobCommand(
    Guid OffcutId,
    Guid JobId);

// Handler: Offcut.Status → Used, Offcut.UsedInJobId = JobId, Offcut.UsedAt = now
// Response: { status: "Used", usedInJobId }
```

**Tests:** 8+ command handler tests (reservation lifecycle)

---

### Task 4: HTTP Endpoints (1 day)

**Routes:**

```
GET    /api/inventory/offcuts
       Filters: status (Available | Reserved | Used), material, minVolume, createdAfter
       Response: 200 + { offcuts[], total, page }

GET    /api/inventory/offcuts/{offcutId}
       Response: 200 + { offcut details, reservation history }

POST   /api/inventory/offcuts/{offcutId}/reserve
       Body: { jobId }
       Response: 201 + { reservationId, expiresAt }

POST   /api/inventory/offcuts/{offcutId}/approve-reservation
       Body: { reservationId }
       Response: 200 + { status: "Approved" }

POST   /api/inventory/offcuts/{offcutId}/use
       Body: { jobId }
       Response: 200 + { status: "Used", usedInJobId }

GET    /api/inventory/offcuts/stats/summary
       Response: 200 + { totalAvailableVolume, availableByMaterial, reservedCount, usedCount }
```

**Authorization:** `[Authorize(Policy = "ManufacturerOnly")]`

**RLS:** Automatic via DbContext interceptor (tenant_id)

**Tests:** 10+ API tests (CRUD, filtering, authorization)

---

### Task 5: Tests (1 day)

**Test File Breakdown:**

- `Domain/OffcutTests.cs` (6 tests)
  - Offcut creation + validation
  - Volume/weight calculation
  - Status transitions

- `Domain/OffcutReservationTests.cs` (4 tests)
  - Reservation creation
  - Expiry logic (7 days)
  - Status transitions

- `Application/Events/CuttingJobCompletedHandlerTests.cs` (5 tests)
  - Event capture
  - Offcut creation from waste
  - Material metadata lookup

- `Application/Commands/OffcutReservationTests.cs` (8 tests)
  - Reserve, Approve, Use commands
  - Validation
  - Error cases

- `Api/OffcutEndpointsTests.cs` (10 tests)
  - GET list + filters
  - GET detail
  - Reserve/Approve/Use POST
  - Stats endpoint

**Total:** 33+ tests

---

## Acceptance Criteria

- ✅ Offcut tracking on job completion
- ✅ Reservation + approval workflow
- ✅ Reuse tracking (UsedInJobId)
- ✅ API endpoints (list, detail, reserve, approve, use, stats)
- ✅ RLS enforced (tenant isolation)
- ✅ 33+ tests passing

---

## Timeline

- **Day 1:** Domain model + EF migration
- **Day 1–2:** Event capture (CuttingJobCompleted → Offcut)
- **Day 2–3:** Reuse workflow (commands)
- **Day 3–4:** API endpoints + tests
- **Day 4–5:** E2E validation

Total: 4-5 days, ~25 developer-hours

---

## Phase 1→2 Roadmap

**Phase 2 (Post-v1 backlog):**
- Advanced geometry tracking (actual piece dimensions, not stub)
- Batch offcut consolidation (combine small pieces)
- Scrap material sale integration (supplier orders for scrap)
- Offcut forecasting (predict future availability)

---

**Status: QUEUED — ready for INVENTORY terminal to start Phase 1 (after JOINERY Phase 1 initiated)**
