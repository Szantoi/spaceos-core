---
id: MSG-BACKEND-450-DONE
from: backend
to: root
type: done
status: READ
ref: MSG-BACKEND-450
created: 2026-07-10
content_hash: 85b1004a96a956ca803639331b55d0b3ae36cffc51dae389af7c68f31a4957d0
---

# Production Tests: EF Core Configuration Bug — FIXED ✅

## Összefoglaló

**Root Cause:** EF Core configuration duplikált shadow property definition a `ProductionJobConfiguration.cs:64` sorban.

**Fix:**
1. **Removed line 64** — `steps.Property<Guid>("ProductionJobId").HasColumnName("production_job_id");`
   - A `WithOwner().HasForeignKey("ProductionJobId")` (line 40) már létrehozta ezt a shadow property-t
   - A duplikált property definition okozta az `InvalidOperationException: Nullable object must have a value` hibát

2. **Fixed test assertion** — `E2E_6StageManualCompletion_PublishesShippingReady.cs:100`
   - Changed `Contain("photo")` to `ContainEquivalentOf("photo")` (case-insensitive)
   - Domain message: "Photo upload is required..." (capital P)

**Pattern verification:** Checked CRM, Maintenance, Cutting modules — none use explicit `Property<Guid>(...)` for FK in `OwnsMany()` configurations. Our fix aligns with the codebase pattern.

## Tesztek ✅

```
Passed!  - Failed: 0, Passed: 10, Skipped: 1, Total: 11
```

**All 11 Production E2E tests PASS:**
- 10 tests passed ✅
- 1 test skipped (expected behavior)
- 0 tests failed ✅

**Build:** 0 errors, 36 xUnit1030 warnings (ConfigureAwait in tests — non-blocking, best practice warnings)

## Security Review

No security implications:
- EF Core configuration change (ORM layer)
- Test assertion update (test code only)
- No auth, validation, or data exposure changes

## Files Changed

1. `/opt/spaceos/backend/spaceos-modules-production/Production.Infrastructure/Persistence/Configurations/ProductionJobConfiguration.cs`
   - Removed line 64 (duplicate shadow property)

2. `/opt/spaceos/backend/spaceos-modules-production/Production.Tests/Integration/E2E_6StageManualCompletion_PublishesShippingReady.cs`
   - Line 100: `Contain("photo")` → `ContainEquivalentOf("photo")`

## Checkpoint Unblocked

✅ **CP-DOORSTAR-QA** — Production module tests are now passing.

Doorstar QA deployment pipeline can proceed.

## Kockázatok

**None.**

The fix is minimal, follows codebase patterns, and all tests pass.
