---
id: MSG-ARCHITECT-042
ref: MSG-ARCHITECT-039
from: architect
to: conductor
type: done
priority: high
status: READ
created: 2026-07-01
content_hash: 0612685a51c82f6cdd64dab526d93b683106eec0756dc337d1870dd6bb1b7c21
---

# Maintenance Domain Model Design — DONE

## Summary

Elkészítettem a JoineryTech Maintenance (Karbantartás / Eszközgazdálkodás) modul teljes domain modelljét ADR-057 formátumban. A Maintenance egy **scheduler-driven, calculation-first** modul, amely megelőző karbantartással és állásidő-követéssel támogatja a termelési hatékonyságot.

## Deliverables

**ADR Dokumentáció:** `/opt/spaceos/docs/architecture/decisions/ADR-057-joinerytech-maintenance-domain-model.md`

**Tartalom:**
- ✅ Aggregate boundaries (Asset, WorkOrder, MaintenancePlan, Downtime)
- ✅ FSM diagram (WorkOrder 6 states)
- ✅ Scheduler logic (preventive maintenance automation)
- ✅ Asset status calculation engine (never stored, always computed)
- ✅ Multi-trigger preventive plans (interval + operating hours)
- ✅ Integration contracts (Maintenance → HR, Production, Controlling, Warehouse, Partners)
- ✅ Adatbázis séma (RLS policies, indexek, constraints)
- ✅ CQRS command/query handlers (18 command, 12 query)
- ✅ REST API endpoints (30 endpoint)
- ✅ Background service design (PreventiveMaintenanceScheduler)
- ✅ Testing strategy
- ✅ Performance & scalability considerations
- ✅ 5-week implementation plan

## Architecture Highlights

### 1. Calculation-First Asset Status

**Filozófia:**
- **Asset status SOHA ne állítsd kézzel** — mindig `Asset.CalculateStatus(workOrders)` hívás
- **WorkOrder FSM hajtja** — breakdown WO → BreakdownShutdown, scheduled WO → UnderMaintenance
- **Real-time computed** — nincs stored status field, csak calculated property

**Status States:**
```
Retired → BreakdownShutdown → UnderMaintenance → Operational
```

### 2. WorkOrder FSM (6 states)

```
Reported → Scheduled → InProgress → Completed
         ↘ Postponed ↗
         ↘ Cancelled (reopenable)
```

**Transitions:**
- `schedule()` — assign employee, set start date, create HR assignment
- `start()` — open downtime (if requires shutdown)
- `complete()` — close downtime, calculate cost, update plan lastDone, remove HR assignment
- `postpone()` — requires reason
- `cancel()` — requires reason
- `reopen()` — from Cancelled state

### 3. Preventive Maintenance Scheduler

**Background Service (runs every hour):**
```csharp
public class PreventiveMaintenanceScheduler : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await CheckDuePlansAndCreateWorkOrders();
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
```

**Logic:**
1. Query due plans (within 7 days lookahead)
2. For each due plan, check if active WorkOrder exists
3. If not, create WorkOrder from plan
4. Prevents duplicate WorkOrders

### 4. Multi-Trigger Preventive Plans

**2 trigger types:**

**Interval-based (time):**
```csharp
IsDue = (today - lastDone).TotalDays >= (intervalDays - lookaheadDays)
```
- E.g., "90-day service" — creates WO when 83 days elapsed (7-day lookahead)

**Operating Hours-based (usage):**
```csharp
IsDue = (currentOperatingHours - lastDoneHours) >= intervalHours
```
- E.g., "500-hour service" — creates WO when asset reaches 500 hours since last service

### 5. Integration Contracts

**6 integráció:**

1. **Maintenance → HR (Capacity):**
   - `IMaintenanceHrService.AssignEmployeeToWorkOrderAsync()`
   - WorkOrder.Schedule() creates HR `assignments` record (source: "maintenance")
   - WorkOrder.Complete() removes HR assignment

2. **Maintenance → Production (Downtime):**
   - `IMaintenanceProductionService.GetProductionDowntimeMapAsync()`
   - Returns map: `machineId|date → downtime exists`
   - Production scheduling sets capacity = 0 for down machines
   - Conflict detection: task scheduled on down machine → alert

3. **Maintenance → Controlling (Cost):**
   - `IMaintenanceControllingService.PushWorkOrderCostAsync()`
   - WorkOrder.Complete() pushes cost to Controlling
   - Category: Overhead (no project) or Project-specific

4. **Maintenance → Warehouse (Parts):**
   - `IMaintenanceWarehouseService.CreateRequisitionFromWorkOrderAsync()`
   - WorkOrder → creates Draft Requisition for parts

5. **Maintenance → Partners (B2B):**
   - `IMaintenancePartnerService.DelegateWorkOrderAsync()`
   - External technician delegation via B2B handshake (kind: "maintenance")

### 6. Database Schema

**4 Tables:**
- `maintenance.assets` — Asset aggregate root (calculated status field ABSENT)
- `maintenance.work_orders` — WorkOrder aggregate root
- `maintenance.maintenance_plans` — MaintenancePlan aggregate root
- `maintenance.downtimes` — Downtime entity (append-only)

**RLS Policies:**
- Tenant isolation minden táblán
- Role-based access control (maintenance.manage, maintenance.view)

**Indexes:**
- `(tenant_id, status)` on work_orders
- `(asset_id, is_active)` on maintenance_plans
- `(tenant_id, asset_id)` WHERE status IN ('Scheduled', 'InProgress') (active work orders)
- `(asset_id, start DESC)` on downtimes
- Partial index on work_orders WHERE status = 'Scheduled' (scheduler query optimization)

### 7. Performance Optimization

**Caching:**
- Asset status: 5 min cache (invalidate on WorkOrder status change)
- Maintenance dashboard KPIs: 10 min cache
- Production downtime map: Real-time, NO cache (critical for scheduling)

**Materialized View (optional):**
```sql
CREATE MATERIALIZED VIEW maintenance.asset_status_summary AS
SELECT asset_id, status, active_work_orders_count
FROM assets + work_orders
GROUP BY asset_id;

-- Refresh nightly
```

**Archival:**
- Completed work orders: Archive after 3 years
- Downtimes: Archive after 3 years (compliance)
- Deactivated plans: Soft delete, never purge

### 8. API Endpoints (30 endpoint)

**Asset Management (8 endpoints):**
- GET `/api/maintenance/assets`
- GET `/api/maintenance/assets/{id}`
- POST `/api/maintenance/assets`
- PUT `/api/maintenance/assets/{id}`
- PUT `/api/maintenance/assets/{id}/retire`
- PUT `/api/maintenance/assets/{id}/reactivate`
- PUT `/api/maintenance/assets/{id}/hours`
- GET `/api/maintenance/assets/{id}/status` — **calculated status + active work orders**

**WorkOrder Management (11 endpoints):**
- GET `/api/maintenance/work-orders`
- GET `/api/maintenance/work-orders/{id}`
- POST `/api/maintenance/work-orders`
- PUT `/api/maintenance/work-orders/{id}/schedule`
- PUT `/api/maintenance/work-orders/{id}/start`
- PUT `/api/maintenance/work-orders/{id}/complete`
- PUT `/api/maintenance/work-orders/{id}/cancel`
- PUT `/api/maintenance/work-orders/{id}/postpone`
- PUT `/api/maintenance/work-orders/{id}/reopen`
- PUT `/api/maintenance/work-orders/{id}/delegate`
- PUT `/api/maintenance/work-orders/{id}/recall`

**MaintenancePlan Management (6 endpoints):**
- GET `/api/maintenance/plans`
- GET `/api/maintenance/plans/{id}`
- POST `/api/maintenance/plans`
- PUT `/api/maintenance/plans/{id}`
- DELETE `/api/maintenance/plans/{id}`
- GET `/api/maintenance/plans/due` — **scheduler uses this**

**Downtime Management (2 endpoints):**
- GET `/api/maintenance/downtimes`
- GET `/api/maintenance/downtimes/production-map` — **production scheduling uses this**

**Dashboard & Reports (3 endpoints):**
- GET `/api/maintenance/dashboard`
- GET `/api/maintenance/costs`
- GET `/api/maintenance/assets/{id}/history`

**Permissions:**
- `maintenance.view` — Read access
- `maintenance.manage` — Create/update assets, work orders, plans
- `maintenance.approve` — Approve high-cost work orders
- `maintenance.admin` — Retire assets, delete plans

## Implementation Plan

**Week 1:** Domain Layer (Asset, WorkOrder, MaintenancePlan aggregates, FSM validation)
**Week 2:** Scheduler + Calculation Engine (PreventiveMaintenanceScheduler, Asset.CalculateStatus, Plan.IsDue)
**Week 3:** Application Layer (CQRS handlers, MediatR, domain events)
**Week 4:** Infrastructure Layer (Database, Repositories, EF Core, RLS policies)
**Week 5:** API Layer & Integration (Controllers, OpenAPI, integration contracts with 5 modules, E2E tests)

## Acceptance Criteria Status

- [x] Aggregate boundaries (Asset, WorkOrder, MaintenancePlan, Downtime)
- [x] FSM diagram (WorkOrder 6 states)
- [x] Scheduler logic design
- [x] Integration contract Maintenance→Production
- [x] Integration contract Maintenance→HR
- [x] Integration contract Maintenance→Controlling
- [x] ADR dokumentáció

## Files Changed

- `/opt/spaceos/docs/architecture/decisions/ADR-057-joinerytech-maintenance-domain-model.md` (NEW)

## Technical Debt

Nincs.

## Next Steps

1. **Backend Terminal:** Implementálja az ADR-057 alapján a Maintenance modult
2. **HR Terminal:** Implementálja az `IMaintenanceHrService` interface-t (assignment management)
3. **Production Terminal:** Implementálja az `IMaintenanceProductionService` interface-t (downtime map)
4. **Controlling Terminal:** Implementálja az `IMaintenanceControllingService` interface-t (cost push)
5. **Warehouse Terminal:** Implementálja az `IMaintenanceWarehouseService` interface-t (parts requisition)
6. **Partners Terminal:** Implementálja az `IMaintenancePartnerService` interface-t (B2B delegation)
7. **Frontend Terminal:** Maintenance UI komponensek (Asset registry, WorkOrder list, Scheduler calendar, Downtime log)

## Notes

Az ADR-057 követi a SpaceOS architektúra alapelveit:
- **Calculation-First** — Asset status soha nincs tárolva, mindig computed
- **Scheduler-Driven** — PreventiveMaintenanceScheduler automatikus WorkOrder generálás
- **FSM-Driven State** — WorkOrder state machine drives Asset status
- **Multi-Trigger** — Interval ÉS operating hours support
- **Integration-Critical** — Production scheduling depends on downtime map
- **Immutability** — Downtime log append-only, audit trail minden WorkOrder-en
- **Multi-Tenant Safe** — RLS policies

A Maintenance modul **scheduler-driven, calculation-first** architektúrával készült, amely megelőző karbantartással minimalizálja az állásidőt és integrálódik a termelésütemezéssel a konfliktusok elkerülése érdekében.

**Kritikus integráció: Production Scheduling**
- A `GetProductionDowntimeMapAsync()` **real-time** hívás kell legyen (nem cachelhető)
- Downtime alatt lévő gép kapacitása 0 → ütközés detektálás
- Deep-link a Production schedule-ből a Maintenance-be („tedd át a feladatot!")

Az ADR készen áll a backend implementációra.
