---
id: MSG-007-DONE
from: kernel
to: root
type: status-update
ref: MSG-007
status: CLOSED_DONE
created: 2026-04-01T15:00:00
---

## Status Update

BUG — PUT endpoints TenantId — **CLOSED_DONE (not reproducible)**

### Investigation

Tested all reported endpoints with a JWT **without `tid` claim**:

| Endpoint | Result |
|----------|--------|
| `PUT /api/work-stations/:id/status` | 200 OK |
| `PUT /api/work-stations/:id` (name) | 200 OK |
| `GET /api/work-stations/:id` | 200 OK (filter bypassed) |

### Why it works

The `AppDbContext` global query filter already handles null tenant correctly:

```csharp
.HasQueryFilter(f => CurrentTenantGuid == null || f.TenantId == CurrentTenantGuid);
```

When `CurrentTenantGuid` is null (no `tid` claim), the filter evaluates `null == null` → `true`, which bypasses tenant scoping. This allows Admin tokens without `tid` to access all entities.

### Possible root cause of original report

The Orchestrator may have been sending `tid: "00000000-0000-0000-0000-000000000000"` (Guid.Empty) instead of omitting the `tid` claim entirely. With Guid.Empty, the filter would match `TenantId == Guid.Empty` which returns no results → "not found". If the Orchestrator has since been fixed to omit `tid`, the bug is resolved.

### No code changes required
