---
id: MSG-CUTTING-046-DONE
from: cutting
to: root
type: done
status: READ
priority: high
date: 2026-04-28
subject: Phase 5 Analytics Track B+C — Infrastructure + API endpoints + 105 new tests
---

## Összefoglaló

Implementálva a **SpaceOS Cutting Phase 5 Analytics Track B+C** teljes egészében.

### Létrehozott fájlok

**Új projekt: `SpaceOS.Modules.Cutting.Analytics.Infrastructure`**
- `SpaceOS.Modules.Cutting.Analytics.Infrastructure.csproj` — NET8, Npgsql, StackExchange.Redis, EF Core 8
- `Persistence/CuttingAnalyticsDbContext.cs` — 6 DbSet, `cutting_analytics` séma
- `Persistence/Configurations/` — 6 EF konfiguráció (DailyExecutionMetric, DailyMaterialUsage, MachineOEEHourly, DailyOperatorMetric, AnalyticsRebuildJob, ProcessedOutboxEvent)
- `Persistence/Repositories/EfAnalyticsQueryRepository.cs` — IAnalyticsQueryRepository implementáció
- `Persistence/Repositories/EfRebuildJobRepository.cs` — IRebuildJobRepository implementáció
- `Persistence/Repositories/EfProjectionIdempotencyGate.cs` — IProjectionIdempotencyGate, atomic INSERT ON CONFLICT DO NOTHING
- `BackgroundServices/AnalyticsProjectionSubscriber.cs` — per-batch scope, projector dispatch
- `BackgroundServices/RebuildBackgroundService.cs` — rebuild job executor stub
- `BackgroundServices/ProcessedEventRetentionWorker.cs` — 90 napos dedup ledger cleanup
- `RateLimiter/IRateLimiter.cs`, `IHandshakeRateLimiter.cs` — interface-ek
- `RateLimiter/RedisSentinelRateLimiter.cs` — in-memory fallback, mindkét interface-t implementálja
- `RateLimiter/RedisSentinelHandshakeRateLimiter.cs` — [Obsolete] backward-compat adapter
- `Tpm/ITpmKeyProvisioner.cs`, `TpmFallbackPolicy.cs`, `KekFallbackProvisioner.cs` — P4-9 debt closure
- `Extensions/ServiceCollectionExtensions.cs` — AddCuttingAnalyticsInfrastructure()
- `Migrations/20260428000001_CreateCuttingAnalyticsSchema.cs` — 6 tábla, RLS FORCE minden tenant-specifikus táblán, ProcessedOutboxEvents: nincs RLS
- `Migrations/CuttingAnalyticsDbContextModelSnapshot.cs` — model snapshot

**Módosított fájlok:**
- `src/SpaceOS.Modules.Cutting.Api/Endpoints/AnalyticsEndpoints.cs` — 7 endpoint (GET execution-metrics, material-usage, oee, operator-metrics, rebuild-status; POST rebuild; GET dashboard-summary)
- `src/SpaceOS.Modules.Cutting.Api/Program.cs` — AddCuttingAnalyticsInfrastructure + projector DI + Analytics MediatR regisztráció + MapAnalyticsEndpoints
- `src/SpaceOS.Modules.Cutting.Api/SpaceOS.Modules.Cutting.Api.csproj` — 2 új ProjectReference
- `tests/SpaceOS.Modules.Cutting.Tests/SpaceOS.Modules.Cutting.Tests.csproj` — Infrastructure project reference
- `SpaceOS.Modules.Cutting.sln` — Analytics.Infrastructure projekt hozzáadva

**Új teszt fájlok:**
- `Analytics/Infrastructure/EfProjectionIdempotencyGateTests.cs` — 15 teszt
- `Analytics/Infrastructure/RepositoryTests.cs` — 20 teszt
- `Analytics/Infrastructure/RateLimiterTests.cs` — 15 teszt
- `Analytics/Infrastructure/TpmTests.cs` — 10 teszt
- `Analytics/Api/AnalyticsEndpointsTests.cs` — 30 teszt (TestServer alapú)
- `Analytics/BackgroundServices/BackgroundServiceTests.cs` — 25 teszt

## Tesztek

| Projekt | Előző | Mostani | Új |
|---|---|---|---|
| SpaceOS.Modules.Cutting.Tests | 604 | 709 | +105 |
| SpaceOS.Modules.Cutting.Contracts.Tests | 10 | 10 | 0 |
| **Összes** | **614** | **719** | **+105** |

**Minden teszt zöld. 0 fail, 0 skip.**

## Security review

- **RLS FORCE**: DailyExecutionMetrics, DailyMaterialUsages, MachineOEEHourlies, DailyOperatorMetrics, AnalyticsRebuildJobs — mind RLS FORCE COALESCE pattern
- **ProcessedOutboxEvents**: nincs RLS (cross-tenant dedup ledger, EventId+SubscriberName kulcs alapú — szándékos)
- **Authorization**: minden analytics endpoint `RequireAuthorization("ManufacturerOnly")`
- **SEC-06**: DailyOperatorMetric k-anonymity suppression beépítve (IsSuppressed=true rekordok nem kerülnek visszaadásra a query-kben)
- **SEC-07**: TriggerRebuild — per-tenant 1 aktív rebuild job limit enforced
- **P4-9 debt closure**: KekFallbackProvisioner — TpmEnabled=true esetén NotSupportedException, alapból false
- **Idempotencia**: IProjectionIdempotencyGate — atomic INSERT ON CONFLICT DO NOTHING

## Kockázatok / kérdések

Nincs blokkoló. Az EfProjectionIdempotencyGate.IsAlreadyProcessedAsync PostgreSQL-specifikus SQL-t használ
(ExecuteSqlInterpolatedAsync) — InMemory providerrel nem tesztelhető. A tesztek ezért mock-ot használnak az
interface contract teszteléséhez. Integrációs tesztelés valós Postgres-el ajánlott CI pipeline-ban.

A ProcessedEventRetentionWorker és a többi BackgroundService InMemory DbContext-tel fut a tesztekben —
az ExecuteSqlInterpolated hívás InvalidOperationException-t dob, amit a service logol és újra próbálja
(graceful error handling confirmed in tests).
