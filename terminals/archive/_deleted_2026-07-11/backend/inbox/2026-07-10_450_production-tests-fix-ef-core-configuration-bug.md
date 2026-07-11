---
id: MSG-BACKEND-450
from: root
to: backend
type: task
priority: critical
status: UNREAD
created: 2026-07-10
content_hash: 8524e1048c25a8aa881ba70380bf8d088e028b21ca5b533c89ffe0413a053cca
---

# Production Tests: Fix EF Core Configuration Bug

## Background

The VSTest TestPlatform dependency issue has been RESOLVED by Root:
- Installed .NET SDK 8.0.422
- Added `Microsoft.NET.Test.Sdk 17.10.0` to Production.Tests.csproj
- Changed `xunit.runner.visualstudio` to 2.8.2

**VSTest runner now works!** However, 10/11 tests FAIL due to EF Core configuration issue.

## Error Location

**File:** `Production.Infrastructure/Persistence/Configurations/ProductionJobConfiguration.cs:64`

**Error:**
```
System.InvalidOperationException: Nullable object must have a value.
   at Microsoft.EntityFrameworkCore.Metadata.Builders.OwnedNavigationBuilder.Property[TProperty](String propertyName)
   at ProductionJobConfiguration.Configure() line 64
```

## Root Cause

The `OwnsMany<WorkflowStep>` configuration at line 37-64 has a property type mismatch. The `Property<T>(string)` method at line 64 is failing because the property type or name doesn't match the entity model.

## Files to Investigate

1. `Production.Domain/Aggregates/ProductionJob.cs` — Check WorkflowStep owned collection definition
2. `Production.Infrastructure/Persistence/Configurations/ProductionJobConfiguration.cs` — Fix line 64
3. Compare with working configurations in other modules (e.g., CRM, Maintenance)

## Acceptance Criteria

- [ ] All 11 Production E2E tests PASS
- [ ] Build succeeds with 0 errors
- [ ] CP-DOORSTAR-QA checkpoint unblocked

## Acceptance Criteria

- [ ] All 11 Production E2E tests PASS
- [ ] Build succeeds with 0 errors
- [ ] CP-DOORSTAR-QA checkpoint unblocked
