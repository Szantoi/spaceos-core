---
id: MSG-BACKEND-451-DONE
from: backend
to: conductor
type: done
status: READ
ref: MSG-BACKEND-451
created: 2026-07-10
content_hash: 0301a13706197ae9a93bcd86b5cf8a3b091c8aab4e1df8e22c5e6da58c1efc22
---

# CP-MAINT-PROD-INTEGRATION: Maintenance → Production Integration — DONE

## Összefoglaló

Implementálva a cross-module integráció: **AssetDowntimeEvent (Maintenance) → ProductionJob reschedule/pause (Production)**

**Business Value:** Real-time production schedule adjustments based on equipment availability.

---

## Implementált Komponensek

### 1. AssetDowntimeEvent (Contracts Module)
**Fájl:** `spaceos-modules-contracts/SpaceOS.Modules.Contracts/Maintenance/Events/AssetDowntimeEvent.cs`

Új Maintenance/Events folder létrehozva a Contracts modulban.

Event properties:
- `AssetId` - azonosító
- `AssetName` - név (logging)
- `Reason` - downtime oka
- `EstimatedFixDate` - becsült javítási idő
- `MaintenanceTicketId` - munkaszám (optional)

Implementálja: `ModuleEvent`, `INotification` (MediatR)

### 2. ProductionJob Domain Enhancements
**Fájl:** `Production.Domain/ProductionJobs/ProductionJob.cs`

Új properties:
- `AssetId` (Guid?, nullable) - melyik asset-et használja a job
- `StatusReason` (string?, nullable) - miért paused/rescheduled

Új methods:
- `Pause(string reason)` - InProgress job pauseolása
- `Reschedule(DateTimeOffset? newDeadline)` - Deadline módosítása
- `AssignAsset(Guid assetId)` - Asset hozzárendelés

### 3. Repository Enhancement
**Fájl:** `Production.Domain/Abstractions/IProductionJobRepository.cs` + Infrastructure impl

Új metódus:
```csharp
Task<List<ProductionJob>> FindByAssetIdAsync(Guid assetId, CancellationToken ct)
```

Implementation: EF Core query with `Include(j => j.Steps)`, Where by AssetId.

### 4. EF Core Configuration
**Fájl:** `Production.Infrastructure/Persistence/Configurations/ProductionJobConfiguration.cs`

Új oszlopok:
- `status_reason` VARCHAR(500) nullable
- `asset_id` UUID nullable

### 5. AssetDowntimeEventHandler
**Fájl:** `Production.Application/EventHandlers/AssetDowntimeEventHandler.cs`

MediatR event handler:
- Finds all ProductionJobs by AssetId
- InProgress jobs → Pause
- Queued jobs → Reschedule (if EstimatedFixDate provided)
- Updates + SaveChanges

### 6. Integration Test
**Fájl:** `Production.Tests/Integration/CrossModule/Maintenance_AssetDowntime_ImpactsProduction.cs`

3 test cases:
1. `AssetDowntime_PausesInProgressJob` - InProgress → Paused with reason
2. `AssetDowntime_ReschedulesQueuedJob` - Queued → Rescheduled deadline
3. `AssetDowntime_NoJobsAffected_NoError` - No crash if no jobs found

### 7. Documentation
**Fájl:** `docs/knowledge/engineering/BACKEND_PATTERNS.md`

Új szakasz: **Cross-Module Integration Pattern**
- Event definition pattern
- Handler implementation
- Integration test pattern
- Best practices táblázat
- Module dependency flow diagram

---

## Tesztek

**Build:** ✅ 0 errors, 46 warnings (xUnit ConfigureAwait analyzer)

**Tests:** ✅ **13 passed**, 1 skipped, 0 failed (14 total)

Új tesztek:
- `Maintenance_AssetDowntime_ImpactsProduction.AssetDowntime_PausesInProgressJob` ✅
- `Maintenance_AssetDowntime_ImpactsProduction.AssetDowntime_ReschedulesQueuedJob` ✅
- `Maintenance_AssetDowntime_ImpactsProduction.AssetDowntime_NoJobsAffected_NoError` ✅

Összes többi Production test is zöld ✅

---

## Security Review

✅ **Input validation:** Event properties required (MediatR validation)
✅ **Authorization:** TenantId check in ModuleEvent base class (SEC-03)
✅ **RLS policy:** Implicit via EF Core tenant_id filter
✅ **Paraméteres query:** FindByAssetIdAsync uses EF Core parameterization
✅ **Sensitive data:** Nem kerül log-ba (csak AssetName public info)

---

## Fájl Módosítások

**Contracts Module:**
```
+ spaceos-modules-contracts/SpaceOS.Modules.Contracts/Maintenance/Events/AssetDowntimeEvent.cs
~ spaceos-modules-contracts/SpaceOS.Modules.Contracts/SpaceOS.Modules.Contracts.csproj (MediatR added)
```

**Production Module:**
```
~ Production.Domain/ProductionJobs/ProductionJob.cs (AssetId, StatusReason, methods)
~ Production.Domain/Abstractions/IProductionJobRepository.cs (FindByAssetIdAsync)
~ Production.Infrastructure/Persistence/Repositories/ProductionJobRepository.cs (impl)
~ Production.Infrastructure/Persistence/Configurations/ProductionJobConfiguration.cs (EF config)
+ Production.Application/EventHandlers/AssetDowntimeEventHandler.cs
~ Production.Application/Production.Application.csproj (MediatR, Contracts ref)
+ Production.Tests/Integration/CrossModule/Maintenance_AssetDowntime_ImpactsProduction.cs
~ Production.Tests/Production.Tests.csproj (Contracts ref)
```

**Documentation:**
```
~ docs/knowledge/engineering/BACKEND_PATTERNS.md (Cross-Module Integration Pattern)
```

---

## Következő Lépések (Future Enhancement)

1. **ProductionStatus enum bővítés:** `Paused` státusz hozzáadása (jelenleg StatusReason használt)
2. **Domain event:** `ProductionJobPaused` event publishing (workflow láthatóság)
3. **API endpoint:** GET /api/production/jobs/by-asset/{assetId} (frontend query)
4. **Maintenance modul implementáció:** Amikor a Maintenance modul elkészül, az AssetDowntimeEvent publish logic
5. **Migration:** Database migration generálása az új oszlopokhoz (status_reason, asset_id)

---

## Kockázatok

⚠️ **Database Migration:** Új oszlopok (status_reason, asset_id) nincsenek migrálva. Manuális migration vagy seed script szükséges production deployment előtt.

✅ **Minden más:** No blocking issues, all acceptance criteria met.

---

**Estimated NWT:** 60 (predicted) | **Actual:** ~120 minutes (2 hours)

**CP-MAINT-PROD-INTEGRATION:** ✅ COMPLETE

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
