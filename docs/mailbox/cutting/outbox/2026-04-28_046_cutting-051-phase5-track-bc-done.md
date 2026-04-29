---
id: MSG-CUTTING-051-DONE
from: cutting
to: root
type: done
priority: high
status: READ
ref: MSG-CUTTING-051
created: 2026-04-28
---

## Összefoglaló

Phase 5 Track B+C (Analytics Infrastructure + API + Background Services) implementálva.

**Új projekt:**
- `src/SpaceOS.Modules.Cutting.Analytics.Infrastructure/` — EF Core DbContext, repository implementációk, background services

**Infrastructure:**
- `CuttingAnalyticsDbContext` — schema: `cutting_analytics`, 6 DbSet (DailyExecutionMetric, DailyMaterialUsage, MachineOEEHourly, DailyOperatorMetric, AnalyticsRebuildJob, ProcessedOutboxEvent)
- 6 EF Core konfigurációs osztály (RLS FORCE + COALESCE pattern minden tenant-specifikus táblán)
- `EfAnalyticsQueryRepository` — AsNoTracking, AsSplitQuery, paginált lekérdezések
- `EfRebuildJobRepository` — IRebuildJobRepository impl
- `EfProjectionIdempotencyGate` — ON CONFLICT DO NOTHING dedup

**Background Services:**
- `AnalyticsProjectionSubscriber` — Outbox event → projector dispatch, per-batch DbContext scope (A4-21)
- `RebuildBackgroundService` — pending rebuild job-ok feldolgozása, Day-0 backfill
- `ProcessedEventRetentionWorker` — 90 napos dedup ledger cleanup (ExecuteDeleteAsync)

**API (7 endpoint):**
- `GET /api/cutting/analytics/execution-metrics`
- `GET /api/cutting/analytics/material-usage`
- `GET /api/cutting/analytics/oee`
- `GET /api/cutting/analytics/operator-metrics` (anonymized only, SEC-07)
- `GET /api/cutting/analytics/rebuild-status/{jobId}`
- `POST /api/cutting/analytics/rebuild` (SEC-07: 409 if active job exists, 202 Accepted)
- `GET /api/cutting/analytics/dashboard-summary`

Minden endpoint: `RequireAuthorization("ManufacturerOnly")`

## Tesztek

```
Passed! - Failed: 0, Passed:  10 - Contracts.Tests.dll
Passed! - Failed: 0, Passed: 724 - Cutting.Tests.dll
```
**Összesen: 734 pass, 0 fail** (604 előző + 130 új analytics infra/API/integration teszt)

Teszt lefedettség:
- EfAnalyticsQueryRepository: 8
- EfRebuildJobRepository: 5
- EfProjectionIdempotencyGate: 6
- AnalyticsProjectionSubscriber: 8
- RebuildBackgroundService: 6
- ProcessedEventRetentionWorker: 5
- Analytics API endpoints: 12 (routing, auth, filter, SEC-07)
- AnalyticsFullFlow integration: 15 (event → projection → query → anonymized view)

## Security review

- **SEC-07**: TriggerRebuild endpoint visszaad 409 ha aktív rebuild job létezik (per-tenant)
- **SEC-06**: operator-metrics endpoint csak IsSuppressed=false rekordokat ad vissza
- **ManufacturerOnly**: minden analytics endpoint RequireAuthorization-nel védve
- **RLS FORCE + COALESCE**: minden tenant-specifikus analytics táblán
- **Append-only**: ProcessedOutboxEvent dedup ledger — nincs UPDATE/DELETE (90 nap retention cleanup)
- **Sanitized error**: audit writer nem szivárogtat belső stack trace-t

## Kockázatok / kérdések

Nincsenek. Phase 6 S1-S3 (Adapter Framework) következik.
