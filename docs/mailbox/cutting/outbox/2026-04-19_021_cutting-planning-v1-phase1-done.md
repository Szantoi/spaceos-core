---
id: MSG-CUTTING-026-DONE
from: cutting
to: root
type: done
status: READ
priority: high
date: 2026-04-19
---

## Összefoglaló

Implementálva: CUTTING Planning v1 Phase 1 — MSG-CUTTING-026 teljes scope.

### Új fájlok

**Domain (3 aggregate/entity):**
- `/src/SpaceOS.Modules.Cutting.Domain/Aggregates/CuttingPlan.cs` — aggregate root, státuszgép, DailyPlan generálás
- `/src/SpaceOS.Modules.Cutting.Domain/Aggregates/DailyPlan.cs` — child entity, kapacitás számítás
- `/src/SpaceOS.Modules.Cutting.Domain/Aggregates/CuttingJob.cs` — child entity, prioritás validáció

**Infrastructure (3 EF konfiguráció + 1 migráció):**
- `/src/.../Configurations/CuttingPlanConfiguration.cs`
- `/src/.../Configurations/DailyPlanConfiguration.cs`
- `/src/.../Configurations/CuttingJobConfiguration.cs`
- `/src/.../Migrations/20260419000001_AddCuttingPlanAggregate.cs` — CuttingPlans, DailyPlans, CuttingJobs táblák + RLS

**Application (command + query handlers):**
- `Commands/CreateCuttingPlan/` — CreateCuttingPlanCommand + Handler
- `Commands/UpdateCuttingPlanStatus/` — UpdateCuttingPlanStatusCommand + Handler
- `Queries/GetCuttingPlan/` — GetCuttingPlanQuery + Handler + Response records

**API:**
- `/src/SpaceOS.Modules.Cutting.Api/Endpoints/CuttingPlanningEndpoints.cs`
  - `POST /api/cutting/planning/` — plan létrehozás
  - `GET /api/cutting/planning/` — plan lista
  - `GET /api/cutting/planning/{planId}` — plan részletek
  - `PUT /api/cutting/planning/{planId}` — státusz frissítés
  - `GET /api/cutting/planning/{planId}/daily/{date}` — napi bontás

**Tests (3 új fájl):**
- `Domain/CuttingPlanTests.cs` — 24 unit teszt
- `Domain/DailyPlanTests.cs` — 8 unit teszt
- `Domain/CuttingJobTests.cs` — 9 unit + theory teszt
- `Infrastructure/CuttingPlanRepositoryTests.cs` — 7 EF in-memory teszt
- `Api/CuttingPlanningEndpointsTests.cs` — 12 API teszt

### Módosított fájlok

- `ICuttingRepository.cs` — +3 method: AddCuttingPlanAsync, GetCuttingPlanByIdAsync, GetCuttingPlanTrackedAsync, GetAllCuttingPlansAsync
- `CuttingRepository.cs` — implementáció
- `CuttingDbContext.cs` — +3 DbSet, +3 ApplyConfiguration
- `CuttingDbContextModelSnapshot.cs` — snapshot bővítve
- `Program.cs` — MapCuttingPlanningEndpoints() regisztrálva

## Tesztek

| Tesztcsomag | Korábbi | Most |
|---|---|---|
| Contracts.Tests | 10 | 10 |
| Cutting.Tests | 67 | 126 |
| **Összesen** | **77** | **136** |

Új tesztek: **+59** (20 felett, DoD teljesítve)

```
Build: 0 error, 0 warning
Tests: Passed 136/136
```

## Security review

- Minden endpoint `RequireAuthorization("ManufacturerOnly")` — group szinten beállítva
- `CuttingPlans` tábla: `ENABLE ROW LEVEL SECURITY + FORCE ROW LEVEL SECURITY + CREATE POLICY tenant_isolation` — migrációban benne
- `DailyPlans` és `CuttingJobs`: nincs TenantId, a cascade FK a `CuttingPlans`-hoz köti őket → RLS a szülő táblán elegendő
- `DateTime.SpecifyKind(Utc)` minden date column-on (Npgsql safe)
- Nincs public setter egyetlen aggregate/entity-n sem
- `ConfigureAwait(false)` minden async callban

## Kockázatok / kérdések

Nincs blocker.

**Megjegyzés:** A meglévő `GET /api/cutting/plans` és `POST /api/cutting/plans` (DailyCuttingPlan) érintetlenül megmaradtak. Az új Planning v1 aggregate a `/api/cutting/planning/` prefix alatt él — névütközés nincs.

A migráció VPS-re még nem lett alkalmazva. A következő deploy-nál automatikusan fut (startup migration logic már kész).
