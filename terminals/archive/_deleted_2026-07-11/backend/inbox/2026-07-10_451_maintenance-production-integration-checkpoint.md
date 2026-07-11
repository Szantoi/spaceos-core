---
id: MSG-BACKEND-451
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: CP-MAINT-PROD-INTEGRATION
created: 2026-07-10
estimated_nwt: 60
epic_id: EPIC-JT-MAINT
checkpoint_id: CP-MAINT-PROD-INTEGRATION
content_hash: af6359197bce264d27b6cd01ee997d20104393641c39e28278804edfa34adce1
---

# CP-MAINT-PROD-INTEGRATION: Maintenance → Production Integration

**Epic:** EPIC-JT-MAINT (Maintenance Module)
**Checkpoint:** CP-MAINT-PROD-INTEGRATION
**Scope:** Asset downtime events → Production schedule impact
**Estimate:** 60 NWT (~2 hours)

---

## 🎯 GOAL

Implement cross-module integration: When a Maintenance asset goes down (AssetDowntimeEvent), automatically impact the Production schedule (ProductionJob reschedule/pause).

**Business Value:** Real-time production schedule adjustments based on equipment availability.

---

## ✅ ACCEPTANCE CRITERIA

1. **Event Handler Created**
   - `AssetDowntimeEventHandler` in `Production.Application/EventHandlers/`
   - Subscribes to `AssetDowntimeEvent` from Maintenance module
   - Locates affected ProductionJobs by AssetId
   - Reschedules or pauses jobs as needed

2. **Integration Test**
   - `Maintenance_AssetDowntime_ImpactsProduction.cs`
   - Scenario: Asset DOWN → ProductionJob paused/rescheduled
   - Verify event propagation + job status change

3. **Build Success**
   - 0 errors, 0 warnings
   - All existing tests still pass

4. **Documentation**
   - Update `docs/knowledge/architecture/MODULE_BOUNDARIES.md` with integration pattern

---

## 📋 IMPLEMENTATION GUIDE

### 1. Event Handler (Domain Event Pattern)

**Location:** `/opt/spaceos/backend/spaceos-modules-production/Production.Application/EventHandlers/AssetDowntimeEventHandler.cs`

```csharp
public class AssetDowntimeEventHandler : INotificationHandler<AssetDowntimeEvent>
{
    private readonly IProductionJobRepository _repository;

    public async Task Handle(AssetDowntimeEvent notification, CancellationToken ct)
    {
        // Find all ProductionJobs using this asset
        var affectedJobs = await _repository.FindByAssetIdAsync(notification.AssetId, ct);

        foreach (var job in affectedJobs)
        {
            if (job.Status == ProductionStatus.InProgress)
            {
                job.Pause(reason: $"Asset {notification.AssetId} unavailable");
            }
            else if (job.Status == ProductionStatus.Scheduled)
            {
                job.Reschedule(newStartDate: notification.EstimatedFixDate);
            }
        }

        await _repository.SaveAsync(affectedJobs, ct);
    }
}
```

### 2. Repository Enhancement

Add method to `IProductionJobRepository`:
```csharp
Task<List<ProductionJob>> FindByAssetIdAsync(Guid assetId, CancellationToken ct);
```

### 3. Integration Test

**Location:** `/opt/spaceos/backend/spaceos-modules-production/Production.Tests/Integration/CrossModule/Maintenance_AssetDowntime_ImpactsProduction.cs`

**Scenario:**
1. Create ProductionJob (status: InProgress, assetId: ASSET-123)
2. Publish AssetDowntimeEvent (assetId: ASSET-123)
3. Assert: ProductionJob.Status == Paused
4. Assert: ProductionJob.StatusReason contains "unavailable"

---

## 📚 CONTEXT

**Maintenance Module ADR:** ADR-057
**Production Module ADR:** ADR-062
**Event Bus:** MediatR domain events
**Pattern:** Cross-module integration via domain events (NOT direct DB calls)

**Related Messages:**
- MSG-BACKEND-196 DONE (Production module complete)
- MSG-BACKEND-450 DONE (Production tests GREEN)

**Unblocked By:** All dependencies complete (Maintenance + Production modules both done)

---

## 🔍 VERIFICATION CHECKLIST

- [ ] AssetDowntimeEventHandler implements INotificationHandler
- [ ] IProductionJobRepository.FindByAssetIdAsync added
- [ ] Integration test GREEN (asset down → job paused)
- [ ] Existing Production tests still pass (10/10)
- [ ] Build: 0 errors, 0 warnings
- [ ] MODULE_BOUNDARIES.md updated with integration pattern

---

## 🚀 NEXT CHECKPOINT

After this DONE → CP-EHS-HR-INTEGRATION (parallel dispatch with Backend-2)

---

**Conductor Note:** Parallel dispatch with MSG-BACKEND2-002 (EHS→HR integration). Estimated completion: 2 hours from now (2026-07-10 ~23:50 UTC).

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
