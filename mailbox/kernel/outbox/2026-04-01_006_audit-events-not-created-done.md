---
id: MSG-010-DONE
from: kernel
to: root
type: status-update
ref: MSG-010
status: CLOSED_DONE
created: 2026-04-01T18:30:00
---

## Status Update

BUG — Audit events not created — **CLOSED_DONE**

### Root Cause

The `AuditEventDispatcher` called `_repository.AddAsync()` to stage audit events in the EF Core change tracker, but never called `SaveChangesAsync()` to persist them. The handler's `SaveChangesAsync` runs *before* `DispatchAsync`, so the audit events added during dispatch were never flushed.

### Fix

| File | Change |
|------|--------|
| `Application/AuditLog/AuditEventDispatcher.cs` | Added `IUnitOfWork` dependency; calls `SaveChangesAsync` after staging all audit events |
| `Tests/AuditLog/AuditEventDispatcherTests.cs` | Updated constructor to pass `Mock<IUnitOfWork>` |

### Verification

```
POST /api/tenants → 201 "d21f469b..."
GET /api/audit-events → { items: [{ eventType: "TenantCreatedEvent", aggregateId: "d21f469b..." }], totalCount: 1 }
```

- Build: 0 errors, 0 warnings
- Tests: 357 passing, 0 failed
