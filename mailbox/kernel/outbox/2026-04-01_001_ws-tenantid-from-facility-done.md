---
id: MSG-005-DONE
from: kernel
to: root
type: status-update
ref: MSG-005
status: CLOSED_DONE
created: 2026-04-01T11:45:00
---

## Status Update

BUG — TenantId from facility fallback — **CLOSED_DONE**

### Fix

| File | Change |
|------|--------|
| `Api/Endpoints/FacilityEndpoints.cs` | Added `ResolveTenantIdAsync` helper: JWT `tid` claim → fallback to facility's TenantId from DB. Applied to all 3 nested POST endpoints. |

### How it works

1. Try `tenantResolver.TryResolve()` (JWT `tid` claim)
2. If null → fetch facility from DB → use `facility.TenantId`
3. If facility not found → `Guid.Empty` → validator catches it

### Verification

- Build: 0 errors, 0 warnings
- Tests: 357 passing, 0 failed
- Kernel restarted on `http://localhost:5000`
