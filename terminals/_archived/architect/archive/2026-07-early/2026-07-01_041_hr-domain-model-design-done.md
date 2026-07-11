---
id: MSG-ARCHITECT-041
ref: MSG-ARCHITECT-038
from: architect
to: conductor
type: done
priority: high
status: READ
created: 2026-07-01
content_hash: ded1fcda6d2c376b392ed4958d27012de67f7013630cd9dde5d617868adec133
---

# HR Domain Model Design — DONE

## Summary

Elkészítettem a JoineryTech HR modul teljes domain modelljét ADR-056 formátumban. Az HR egy **capacity-first, calculation-heavy** modul, amely valós idejű kapacitás-számításokkal támogatja a termelési tervezést.

## Deliverables

**ADR Dokumentáció:** `/opt/spaceos/docs/architecture/decisions/ADR-056-joinerytech-hr-domain-model.md`

**Tartalom:**
- ✅ Aggregate boundaries (Employee, Absence)
- ✅ FSM diagramok (Absence approval workflow)
- ✅ Domain events katalógus (15 esemény)
- ✅ Capacity calculation engine (real-time, never stored)
- ✅ Vacation/sick leave balance formulas (Hungarian Labor Code §118)
- ✅ Integration contracts (HR → Controlling, Production, Logistics, EHS)
- ✅ Adatbázis séma (RLS policies, indexek, constraints)
- ✅ CQRS command/query handlers (11 command, 9 query)
- ✅ REST API endpoints (26 endpoint)
- ✅ Testing strategy
- ✅ Performance & scalability considerations
- ✅ 5-week implementation plan

## Architecture Highlights

### 1. Capacity-First Design

**Filozófia:**
- **Nincs stored capacity** — minden on-demand számított
- **Real-time load calculation** — naponta frissülő kapacitás/terhelés
- **Overload detection** — automatikus figyelmeztetés túlterhelt dolgozókra

**Capacity Engine:**
```csharp
DayCapacity = WeeklyHours / 5
DayLoad considers:
  - Assignments (project hours)
  - Absences (blocking states)
  - Logistics crew hours

Overload when: Assigned > Capacity
```

### 2. Absence FSM (5 states)

```
Requested → Approved → InProgress → Completed
         ↘ Rejected
```

**Blocking states:** Approved, InProgress, Completed
**Non-blocking:** Requested, Rejected

### 3. Vacation Balance Calculation

**Hungarian Labor Code §118:**
- 1 child: +2 days
- 2 children: +4 days
- 3+ children: +7 days

**Formula:**
```csharp
totalEntitlement = vacationBase + childExtra
remaining = totalEntitlement - usedDays
```

### 4. Integration Contracts

**4 integráció:**

1. **HR → Controlling:**
   - `ITimeLogService.GetTimeLogsForProjectAsync()`
   - Actual labor cost calculation

2. **HR → Production:**
   - `ICapacityQueryService.GetAvailableCapacityAsync()`
   - Production planning constraint

3. **HR → Logistics:**
   - `ICrewAssignmentService.GetCrewMembersForShipmentAsync()`
   - Logistics crew hours tracking

4. **HR → EHS:**
   - `ITrainingService.GetTrainingRecordsAsync()`
   - Training compliance validation

### 5. Database Schema

**5 Tables:**
- `hr.employees` — Employee aggregate root (with PersonalData JSONB)
- `hr.employee_skills` — Skills junction table
- `hr.absences` — Absence aggregate root
- `hr.assignments` — Project assignments (hours/day)
- `hr.time_logs` — Daily work logs

**RLS Policies:**
- Tenant isolation minden táblán
- Role-based access control (hr.manage, hr.view)
- Personal data protection (GDPR compliance)

**Indexes:**
- `(tenant_id, is_active)` on employees
- `(employee_id, status)` on absences
- `(employee_id, date)` on time_logs (performance critical)
- `(project_id, date)` on assignments
- Partial index on absences WHERE status IN ('Requested', 'Approved')

### 6. Performance Optimization

**Caching:**
- Employee info: 1 hour cache
- Vacation balance: 30 min cache per employee/year
- Daily capacity: 15 min cache per employee/date
- Absence approvals: Real-time, no cache

**Materialized View (optional):**
```sql
CREATE MATERIALIZED VIEW hr.employee_capacity_summary AS
SELECT employee_id, date, capacity, assigned, available, overloaded
FROM hr.employees
-- ... joins with assignments, absences, logistics
GROUP BY employee_id, date;
```

Refresh: Nightly cron vagy on-demand

**Archival:**
- Time logs: Archive after 2 years (tax compliance)
- Completed absences: Archive after 3 years
- Inactive employees: Soft delete, never purge (legal requirement)

### 7. API Endpoints (26 endpoint)

**Employee Management (9 endpoints):**
- GET `/api/hr/employees`
- GET `/api/hr/employees/{id}`
- POST `/api/hr/employees`
- PUT `/api/hr/employees/{id}`
- DELETE `/api/hr/employees/{id}`
- PUT `/api/hr/employees/{id}/activate`
- PUT `/api/hr/employees/{id}/deactivate`
- GET `/api/hr/employees/{id}/skills`
- PUT `/api/hr/employees/{id}/skills`

**Absence Management (7 endpoints):**
- GET `/api/hr/absences`
- GET `/api/hr/absences/{id}`
- POST `/api/hr/absences`
- PUT `/api/hr/absences/{id}/approve`
- PUT `/api/hr/absences/{id}/reject`
- PUT `/api/hr/absences/{id}/start`
- PUT `/api/hr/absences/{id}/complete`

**Capacity & Load (5 endpoints):**
- GET `/api/hr/capacity/daily/{employeeId}` — Single day load
- GET `/api/hr/capacity/weekly/{employeeId}` — Week summary
- GET `/api/hr/capacity/team` — Team capacity overview
- GET `/api/hr/vacation-balance/{employeeId}` — Vacation balance
- GET `/api/hr/sick-balance/{employeeId}` — Sick leave balance

**Time Logs (3 endpoints):**
- GET `/api/hr/timelogs` — Query time logs
- POST `/api/hr/timelogs` — Create time log
- PUT `/api/hr/timelogs/{id}` — Update time log

**Assignments (2 endpoints):**
- POST `/api/hr/assignments` — Assign employee to project
- DELETE `/api/hr/assignments/{id}` — Remove assignment

**Permissions:**
- `hr.view` — Read access
- `hr.manage` — Create/update employees, assignments
- `hr.approve` — Approve/reject absences
- `hr.admin` — Deactivate employees, modify personal data

## Implementation Plan

**Week 1:** Domain Layer (Employee, Absence aggregates, PersonalData value object, FSM)
**Week 2:** Capacity Engine (HrEngine static methods, DayLoad, WeekSummary calculations)
**Week 3:** Application Layer (CQRS handlers, MediatR, domain events)
**Week 4:** Infrastructure Layer (Database, Repositories, EF Core, RLS policies)
**Week 5:** API Layer & Integration (Controllers, OpenAPI, integration contracts)

## Acceptance Criteria Status

- [x] Aggregate boundaries definiálva
- [x] FSM diagramok (Absence)
- [x] Capacity calculation engine
- [x] Vacation balance formula (Hungarian Labor Code §118)
- [x] Integration contract HR→Controlling
- [x] ADR dokumentáció

## Files Changed

- `/opt/spaceos/docs/architecture/decisions/ADR-056-joinerytech-hr-domain-model.md` (NEW)

## Technical Debt

Nincs.

## Next Steps

1. **Backend Terminal:** Implementálja az ADR-056 alapján a HR modult
2. **Controlling Terminal:** Implementálja az `ITimeLogService` interface-t
3. **Production Terminal:** Implementálja az `ICapacityQueryService` interface-t
4. **Logistics Terminal:** Implementálja az `ICrewAssignmentService` interface-t
5. **EHS Terminal:** Implementálja az `ITrainingService` interface-t
6. **Frontend Terminal:** HR UI komponensek (Employee grid, Absence calendar, Capacity dashboard)

## Notes

Az ADR-056 követi a SpaceOS architektúra alapelveit:
- **Calculation-First** — capacity és balances on-demand számítva
- **No Stored Calculations** — csak assignments és time logs írhatók
- **Immutability** — audit trail minden módosításon
- **Multi-Tenant Safe** — RLS policies + GDPR compliance
- **Performance-Aware** — caching + optional materialized view

A HR modul **capacity-first, calculation-heavy** architektúrával készült, amely valós idejű terhelés-számítással támogatja a termelési tervezést és a túlterheltség-detektálást.

Az ADR készen áll a backend implementációra.
