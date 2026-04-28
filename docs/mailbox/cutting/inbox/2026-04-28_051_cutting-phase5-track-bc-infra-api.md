---
id: MSG-CUTTING-051
from: root
to: cutting
type: task
priority: high
status: READ
ref: MSG-CUTTING-045-DONE
created: 2026-04-28
---

# CUTTING-051 — Phase 5 Track B+C: Infrastructure + API + BG Services

> **Tervdok:** `docs/tasks/active/SpaceOS_Modules_Cutting_Phase5_Analytics_Architecture_v4.md`
> **Skill:** `/spaceos-terminal` szerint dolgozz
> **Előfeltétel:** CUTTING-050 ✅ (604 teszt, Analytics Domain + Application)
> **Ez az UTOLSÓ Phase 5 track!**
> **Használhatsz sub-agent-eket** ha szükséges

---

## Track B: Infrastructure + Persistence

### CuttingAnalyticsDbContext

- Új EF Core DbContext — `cutting_analytics` séma
- Configurations: DailyExecutionMetric, DailyMaterialUsage, MachineOEEHourly, DailyOperatorMetric, AnalyticsRebuildJob, ProcessedOutboxEvent
- RLS FORCE minden táblán (COALESCE pattern!)

### Migrations C-0007..C-0019

A tervdok §6 alapján — idempotens DDL:
- `cutting_analytics` séma + role-ok
- 6 tábla + 1 security_barrier VIEW (daily_operator_metric_anonymized)
- Append-only processed_outbox_event (dedup ledger, 90 nap retention)
- `assert_anonymity_constraints` SECURITY DEFINER function (C-0019)

### Repository implementációk

- `EfAnalyticsQueryRepository` — IAnalyticsQueryRepository (AsSplitQuery)
- `EfRebuildJobRepository` — IRebuildJobRepository
- `EfProjectionIdempotencyGate` — IProjectionIdempotencyGate (processedEventId dedup)

### RateLimiter (P4-4 debt closure)

- `RedisSentinelRateLimiter` — IRateLimiter impl, sliding window, per-tenant
- Backward-compat: implementálja IHandshakeRateLimiter is (BE-04)
- 60 req/min standard + 5 concurrent HeavyQuery (SEC-05)

### TPM (P4-9 debt closure)

- `TpmKeyProvisioner` — ITpmKeyProvisioner impl (vagy stub ha nincs TPM hardware)
- `TpmFallbackPolicy` — per-tenant opt-in, default DISABLED
- `KekFallbackProvisioner` — two-slot KEK ha TPM unavailable

---

## Track C: API + Background Services

### API endpoints (7)

```
GET  /api/cutting/analytics/execution-metrics    — filter: machine, dateRange
GET  /api/cutting/analytics/material-usage       — filter: material, dateRange
GET  /api/cutting/analytics/oee                  — filter: machine, dateRange, resolution
GET  /api/cutting/analytics/operator-metrics     — anonymized view only
GET  /api/cutting/analytics/rebuild-status       — rebuild job status
POST /api/cutting/analytics/rebuild              — trigger rebuild (per-tenant 1 active, SEC-07)
GET  /api/cutting/analytics/dashboard-summary    — combined summary
```

Minden endpoint: `RequireAuthorization("ManufacturerOnly")`

### Background Services

- `AnalyticsProjectionSubscriber` — Outbox event → projector dispatch (per-batch DbContext scope, A4-21)
- `RebuildBackgroundService` — pending rebuild job-ok feldolgozása (Day-0 backfill)
- `ProcessedEventRetentionWorker` — 90 napos dedup ledger cleanup

---

## Tesztek (115+)

**Infrastructure (40+):** EF configs, RLS isolation, migration existence, dedup gate, repository CRUD
**API (30+):** 7 endpoint routing, auth, filter, pagination, rebuild trigger SEC-07
**BG Services (25+):** projection subscriber, rebuild, retention, per-batch scope
**RateLimiter (10+):** sliding window, per-tenant, backward-compat IHandshakeRateLimiter
**Integration (10+):** full flow: event → projection → query → anonymized view

## Definition of Done

- [ ] CuttingAnalyticsDbContext + migrations (RLS FORCE, COALESCE!)
- [ ] 3 repository implementáció
- [ ] RedisSentinelRateLimiter (P4-4) + backward-compat
- [ ] TPM stub (P4-9)
- [ ] 7 API endpoint
- [ ] 3 BackgroundService
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` ≥ 719 pass (604 + 115 új)
- [ ] Outbox DONE
