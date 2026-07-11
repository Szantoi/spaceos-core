---
processed: 2026-07-06
id: MSG-BACKEND-147
from: conductor
to: backend
type: task
priority: high
status: READ
model: haiku
ref: MSG-BACKEND-146-DONE
created: 2026-07-06
estimated_nwt: 15
content_hash: 964c405bb6d0949dabf53dca4bf59eb69dc0c065128b23ac77e230fdbb7473bb
---

# QA Week 1 — Compilation Fixes (30 min)

MSG-146 DONE review: **90% complete** — domain logic és 73 unit test excellent! ✅

**De:** 54 compilation error blokkolja a build-et. Javítsd ki ezeket:

## Compilation Error Categories

### 1. Missing using statements
```csharp
using SpaceOS.Kernel.Domain.Primitives;  // Add to aggregates for DomainException
```

### 2. Event signature mismatches (17 events)
**Példa:** InspectionPlannedEvent
- Aggregate expects: `(Id, TenantId, CheckpointId, InspectorId, PlannedAt)`
- Actual event has: `(Id, TenantId, CheckpointId, OrderId, ProjectId, ScheduledDate)`

→ Recreate events to match aggregate method signatures

### 3. InspectionResult.Pending enum value
- Enum only has: Pass, Fail, Conditional
- Either add `Pending` value OR change initial state to `Pass`

### 4. InspectionCriteria.CriteriaType property
- Verify value object signature

### 5. QACheckpointId.Value access
- Use `.Value` property consistently in RootCauseAnalysisService

## Acceptance Criteria
- ✅ 54 compilation errors → 0 errors
- ✅ Build: `dotnet build` SUCCESS
- ✅ Tests: `dotnet test` 73/73 PASS
- ✅ NO warnings

## Next After This
Ha build SUCCESS:
1. Mark CP-QA-BACKEND checkpoint DONE
2. Integration planning (Production blocking logic wire-up)

**Estimated:** 15 NWT (~30 min) — systematic corrections

---
📎 Ref: `/opt/spaceos/terminals/backend/outbox/2026-07-06_146_joinerytech-qa-week1-domain-layer-done.md`
