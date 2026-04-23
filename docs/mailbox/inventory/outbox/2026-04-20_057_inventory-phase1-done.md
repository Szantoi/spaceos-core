---
id: MSG-INVENTORY-051-DONE
from: inventory
to: root
type: done
priority: high
status: READ
ref: MSG-INVENTORY-056
created: 2026-04-20
---

# INVENTORY-051 — Offcut Tracking + Reuse Workflow Phase 1 COMPLETE ✅

## Summary

**All 5 days executed. Phase 1 DONE.**

---

## Commit Log

| Commit | Day | Tartalom |
|---|---|---|
| `c022043` | D1 | Offcut v2 domain model + EF migration + OffcutReservation aggregate |
| `ae23cf8` | D2 | CuttingJobCompletedEvent handler stub + 6 tests |
| `f68441d` | D3 | Reuse commands: Reserve, Approve, UseOffcutInJob + 10 tests |
| `5e99f6d` | D4 | HTTP endpoints: 6 offcut routes + 3 query handlers + 12 tests |
| `0bc0e11` | D5 | E2E lifecycle validation: 3 integration tests |

---

## Acceptance Criteria — All PASS ✅

| Kritérium | Státusz |
|---|---|
| EF migration + RLS on Offcuts + OffcutReservations table | ✅ |
| CuttingJobCompleted event handler (stub, Option A) | ✅ |
| Reserve → Approve → Use command pipeline | ✅ |
| 6 HTTP endpoints (GET list/detail/stats, POST reserve/approve/use) | ✅ |
| 410 on expired reservation | ✅ |
| E2E lifecycle test: CuttingJobCompleted → Available → Reserve → Approve → Use → stats | ✅ |
| Build: 0 error, 0 warning | ✅ |
| Tests: 150/150 pass | ✅ |

---

## Final Test Count

```
Passed! Failed: 0, Passed: 150, Skipped: 0, Total: 150
```

**Breakdown by day:**
- Pre-existing: 99 tests
- D1 (domain): +20 → 119
- D2 (event handler): +6 → 125
- D3 (reuse commands): +10 → 135
- D4 (HTTP endpoints): +12 → 147
- D5 (E2E lifecycle): +3 → **150**

---

## E2E Lifecycle Result

```
FullLifecycle_CuttingJobCompleted_To_Used_AllStepsPass   ✅ PASS
EventHandler_ComputesCorrectVolumeAndStubDimensions      ✅ PASS
StatsQuery_MixedStatuses_CorrectAggregation              ✅ PASS
```

Full pipeline validated: real domain + real application handlers + in-memory EF (no mocks).

---

## Architecture Notes

- **CuttingJobCompletedEvent** is in Application layer (not Domain) — no MediatR dependency leak into Domain
- **ReserveOffcut** creates OffcutReservation (Pending) without changing offcut status; **ApproveOffcutReservation** calls `offcut.Reserve()` — clean two-step workflow
- **UseOffcutInJob** enforces Reserved status (409 if not Reserved)
- **CUTTING-028 hook point**: when Cutting module publishes real events, just redirect to the same `CuttingJobCompletedEvent` handler

---

## Security Review

- RLS: `ENABLE ROW LEVEL SECURITY` + `FORCE ROW LEVEL SECURITY` + policy on both `Offcuts` and `OffcutReservations` — migration `20260420000004`
- All 6 endpoints: `[Authorize(Policy = "ManufacturerOnly")]` via RequireAuthorization
- TenantId extracted from JWT `tid` claim (never from request body)
- No SQL injection vectors: all queries via EF Core parameterized

---

## Phase 2 Proposal

Ha root jóváhagyja, Phase 2 scope:

1. **CUTTING-028**: Valódi event publishing a Cutting modulban → `CuttingJobCompletedEvent` helyett valódi integration event bus
2. **Offcut density tracking**: `MaterialCatalog` + `DensityKgPerM3` mező → pontos WeightKg számítás
3. **Multi-piece offcut**: 1 vágásból N offcut (jelenleg: 1 job → 1 offcut stub)
4. **Offcut expiry cleanup worker**: expired `OffcutReservation` auto-cancel (hasonló `ReservationCleanupWorker`-hez)
5. **Offcut photo upload**: MinIO integration, SHA-256 audit event

**Ajánlás**: CUTTING-028 a legfontosabb — real event bus feloldja a stub-ot.
