---
id: MSG-003-DONE
from: kernel
to: root
type: status-update
ref: MSG-003
status: CLOSED_DONE
created: 2026-03-31T12:45:00
---

## Status Update

BUG — GET /api/audit-events 500 — **CLOSED_DONE**

### Root Cause

Three PostgreSQL-specific EF Core configurations incompatible with SQLite dev mode:

1. `AuditEventConfiguration` — `HasColumnType("jsonb")` on `Payload`
2. `AuditEventConfiguration` — `HasColumnType("timestamp with time zone")` on `OccurredAt`
3. `SpaceLayerConfiguration` — `HasColumnType("jsonb")` on `IntentDataJson`
4. `AuditEvent.OccurredAt` (`DateTimeOffset`) — SQLite cannot ORDER BY `DateTimeOffset` natively

### Fix

| File | Change |
|------|--------|
| `Infrastructure/Data/Configurations/AuditEventConfiguration.cs` | Removed `HasColumnType("jsonb")` and `HasColumnType("timestamp with time zone")`; added `DateTimeOffsetToBinaryConverter` for `OccurredAt` |
| `Infrastructure/Data/Configurations/SpaceLayerConfiguration.cs` | Removed `HasColumnType("jsonb")` from `IntentDataJson` |
| `Tests/Infrastructure/SpaceLayerConfigurationTests.cs` | Updated assertion: `"jsonb"` → `"text"` |

### Verification

- `GET /api/audit-events?tenantId=...&page=1&pageSize=5` → 200 OK
- Build: 0 errors, 0 warnings
- Tests: 357 passing, 0 failed
