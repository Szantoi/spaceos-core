---
id: MSG-INVENTORY-059
from: inventory
to: root
type: done
priority: high
status: READ
ref: MSG-INVENTORY-058
created: 2026-04-20
---

# INVENTORY-015 — Batch Offcut Endpoint DONE

## Summary

Commit: `459aaef`

### Delivered

`POST /api/inventory/offcuts/batch` endpoint with idempotency constraint.

**Idempotency key:** `(TenantId, SourceType, SourceId)` — unique index on `OffcutBatches` table.

| Scenario | Response |
|---|---|
| New batch | `201 Created` + `{BatchId, OffcutIds[], IsNew: true}` |
| Same batch repeated | `200 OK` + `{BatchId, OffcutIds[], IsNew: false}` |
| No auth (missing tid) | `401 Unauthorized` |
| Empty Items list | `400 Bad Request` |

### Migration

`20260420000005_AddOffcutBatch` — creates `OffcutBatches` table with unique index `IX_OffcutBatches_TenantId_SourceType_SourceId`.

### New files

- `Domain/Aggregates/OffcutBatch.cs` — idempotency entity
- `Application/Commands/RegisterOffcutBatch/RegisterOffcutBatchCommand.cs`
- `Application/Commands/RegisterOffcutBatch/RegisterOffcutBatchCommandHandler.cs`
- `Infrastructure/Persistence/Configurations/OffcutBatchConfiguration.cs`
- `Infrastructure/Migrations/20260420000005_AddOffcutBatch.cs`
- `tests/Api/OffcutBatchEndpointTests.cs` — 4 tests

### Test result
```
Passed! Failed: 0, Passed: 164, Skipped: 0, Total: 164
```

4 new tests: 201 new batch, 200 idempotent, 401 no-auth, 400 empty items.

## Blockers
None.
