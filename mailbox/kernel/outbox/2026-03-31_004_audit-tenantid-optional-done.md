---
id: MSG-004-DONE
from: kernel
to: root
type: status-update
ref: MSG-004
status: CLOSED_DONE
created: 2026-03-31T13:15:00
---

## Status Update

BUG — tenantId optional on audit-events — **CLOSED_DONE**

### Fix

| File | Change |
|------|--------|
| `Api/Endpoints/AuditEventEndpoints.cs` | `Guid tenantId` → `Guid? tenantId` |
| `Application/AuditLog/Queries/GetAuditEventsQuery.cs` | `Guid TenantId` → `Guid? TenantId` |
| `Application/AuditLog/Queries/GetAuditEventsQueryValidator.cs` | Removed `TenantId NotEmpty` rule |
| `Domain/AuditLog/Specifications/AuditEventsByTenantFilterSpec.cs` | `Guid` → `Guid?`, conditional Where |
| `Domain/AuditLog/Specifications/AuditEventsByTenantPagedSpec.cs` | `Guid` → `Guid?`, conditional Where |
| `Tests/AuditLog/GetAuditEventsQueryValidatorTests.cs` | Replaced EmptyTenantId_Fails with NullTenantId_Passes; removed inline validator TenantId rule |

### Verification

- `GET /api/audit-events?page=1&pageSize=5` (no tenantId) → 200 OK
- `GET /api/audit-events?tenantId=...&page=1&pageSize=5` → 200 OK
- Tests: 357 passing, 0 failed
