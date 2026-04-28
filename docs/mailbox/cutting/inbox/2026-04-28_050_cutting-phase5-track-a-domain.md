---
id: MSG-CUTTING-050
from: root
to: cutting
type: task
priority: high
status: READ
ref: SpaceOS_Modules_Cutting_Phase5_Analytics_Architecture_v4.md
created: 2026-04-28
---

# CUTTING-050 — Phase 5 Track A: Domain + Application (Analytics)

> **Tervdok:** `docs/tasks/active/SpaceOS_Modules_Cutting_Phase5_Analytics_Architecture_v4.md` — KÖTELEZŐ olvasmány!
> **README:** `docs/tasks/active/SpaceOS_Modules_Cutting_Phase5_Analytics_Architecture_v4_README.md`
> **Skill:** `/spaceos-terminal` szerint dolgozz
> **Előfeltétel:** Cutting Phase 4 DEPLOYED (496 teszt)
> **Használhatsz sub-agent-eket** ha szükséges

---

## Scope

### Új projektek (a Cutting solution-ben)

- `SpaceOS.Modules.Cutting.Analytics.Domain/` — read-model entitások, VO-k, projector interfészek
- `SpaceOS.Modules.Cutting.Analytics.Application/` — query handlerek, projektorok, specification-ök

### Domain — Read-model entitások

A tervdok §3-4 alapján:

**Projection entitások (nem aggregate — read-model):**
- `DailyExecutionMetric` — gép × nap: completedCount, avgDuration, yieldPercent
- `DailyMaterialUsage` — anyag × nap: totalAreaUsed, wasteAreaMm2, offcutCount
- `MachineOEEHourly` — gép × óra: availability, performance, quality → OEE
- `DailyOperatorMetric` — operátor × nap: k-anonimitás + l-diversity (SEC-06)

**Value Objects:**
- `OEEScore` — Availability × Performance × Quality (0.0–1.0)
- `AnonymizationPolicy` — k-threshold, l-diversity min, suppress below threshold
- `MetricTimeRange` — from/to DateTimeOffset, resolution (Hourly/Daily)
- `RebuildJobStatus` — Pending/Running/Completed/Failed FSM

**Interfészek:**
- `IExecutionMetricProjector` — Phase 4 event → DailyExecutionMetric
- `IMaterialUsageProjector` — OffcutReported event → DailyMaterialUsage
- `IOEEProjector` — ExecutionStarted/Completed → MachineOEEHourly
- `IOperatorMetricProjector` — WorkerAssignment event → DailyOperatorMetric (anonymized)
- `IProjectionIdempotencyGate` — outbox event dedup (processedEventId ledger)
- `IRebuildJobRepository`
- `IAnalyticsQueryRepository`

### Application — Query handlers + Projectors

**Projector implementációk:**
- `ExecutionMetricProjector` — ExecutionCompleted → upsert DailyExecutionMetric
- `MaterialUsageProjector` — OffcutReported → upsert DailyMaterialUsage
- `OEEProjector` — hourly aggregálás
- `OperatorMetricProjector` — k-anon + l-diversity assertion (SEC-06)

**Query handlers:**
- `GetDailyExecutionMetricsQuery` — filter: machine, dateRange
- `GetMaterialUsageQuery` — filter: material, dateRange
- `GetMachineOEEQuery` — filter: machine, dateRange, resolution
- `GetOperatorMetricsQuery` — anonymized view only
- `GetRebuildJobStatusQuery`

**Specifications (Ardalis.Specification):**
- `MetricsByMachineAndDateRangeSpec`
- `MaterialUsageByDateRangeSpec`
- `OEEByMachineAndHourSpec`
- `PendingRebuildJobsSpec`

---

## Tesztek (95+)

**Domain (30+):** OEEScore calculation, AnonymizationPolicy thresholds, MetricTimeRange validation
**Projectors (30+):** event → read-model mapping, idempotency, k-anon assertion
**Queries (20+):** filter, date range, anonymized view
**Specifications (15+):** where expression, ordering

## Definition of Done

- [ ] 4 read-model entity + 4 VO
- [ ] 4 projector + IProjectionIdempotencyGate
- [ ] 5 query handler
- [ ] 4+ Ardalis.Specification
- [ ] SEC-06: k-anonimitás + l-diversity az OperatorMetricProjector-ban
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` ≥ 591 pass (496 előző + 95 új)
- [ ] Outbox DONE
