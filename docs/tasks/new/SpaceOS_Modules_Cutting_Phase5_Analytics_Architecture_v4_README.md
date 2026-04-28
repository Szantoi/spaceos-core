# Cutting Phase 5: Analytics — Claude Code Agent Context

> **Companion to:** `SpaceOS_Modules_Cutting_Phase5_Analytics_Architecture_v4.md`
> **Audience:** Claude Code agent session(s) implementing this phase
> **Pre-requisite read:** the v4 document is the source of truth. This README operationalizes it.
> **Generated:** 2026-04-28

---

## 0. Quick orientation

You are implementing **Cutting Phase 5: Analytics** in the `spaceos-modules-cutting` repository.

| Fact | Value |
|------|-------|
| Repository | `spaceos-modules-cutting` (Phase 1–4 already deployed) |
| New project tree | `src/Analytics/` (new) + `src/Execution.Infrastructure/` (modifications) |
| Service port | `:5005` (shared with Phase 4) |
| Database schema | `cutting_analytics` (new, dedicated) |
| Migration prefix | `C-0007` .. `C-0019` (continues Phase 4 numbering at C-0006) |
| Contracts package version | bump to `v1.4.0` (additive) |
| Sprint length | ~21.1 dev-days, 3 parallel tracks |
| Test target | ≥115 new tests, plus all ~1873 existing must stay green |
| Status gate | DoD §9 of v4 — **every checkbox must close before merge** |

**You will not:** create UI, modify Doorstar Portal repo, change Kernel domain model, add new aggregates outside the `cutting_analytics` schema, or introduce packages outside the approved list.

---

## 1. Track assignment matrix

Three parallel tracks are designed for 4 agent sessions (3 implementation + 1 test). If you are running solo, execute serially in track order A → B → C, completing each day across all tracks before advancing.

### Track A — Domain + Application (~7 days work)

```
Owner files:
  src/Analytics/Domain/**
  src/Analytics/Application/**

Daily focus: 1, 2, 3, 4, 5, 6, 9, 10, 11, 14, 15
```

### Track B — Infrastructure + Persistence (~7 days work)

```
Owner files:
  src/Analytics/Infrastructure/**
  src/Execution.Infrastructure/Redis/** (modifications)
  src/Execution.Infrastructure/Tpm/** (modifications)
  src/Analytics/Infrastructure/Migrations/**

Daily focus: 1, 2, 3, 4, 5, 6, 7, 8, 11, 12, 13, 15, 16, 19, 20
```

### Track C — API + BackgroundService + Tests (~7 days work)

```
Owner files:
  src/Analytics/Api/**
  src/Analytics/Application/BackgroundServices/**
  tests/Analytics.Tests/**

Daily focus: 1 (test setup), 2-9 (test coverage of A/B output), 10, 11, 13, 14, 17, 18, 19, 20, 21
```

> **Day-by-day breakdown:** see v4 §13.1.

---

## 2. Per-track agent prompt skeleton

Use the v4 §13.2 master prompt as the **session opener**, then narrow to the track. Below are the three opening prompts you can copy verbatim into a new Claude Code session.

### 2.1 Track A opener

```
You are implementing Track A (Domain + Application) of SpaceOS Cutting Phase 5: Analytics.

Authoritative spec: SpaceOS_Modules_Cutting_Phase5_Analytics_Architecture_v4.md
Companion guide:    SpaceOS_Modules_Cutting_Phase5_Analytics_Architecture_v4_README.md

Scope for this session — Day {N} only (do not advance):
{paste the cell from §13.1 Track A column for day N}

Hard constraints (non-negotiable):
- Golden Rules 1–12 (see CLAUDE.md at repo root)
- New axioms A5-15..A5-17 (interceptor compliance, async cross-context, backward-compat)
- No public setters on read-model entities
- ConfigureAwait(false) on every production async call
- Result<T> on every handler
- All entities created via factory methods (no public constructors)
- FluentValidation validators are shape-only; business rules live in Domain

Definition of Done for this day:
- All files compile
- All new types have at least one unit test
- dotnet build && dotnet test pass
- Stop after the day's scope; do not advance to day {N+1}

After completing, summarize in a table: File · Change · Reason. Nothing else.
```

### 2.2 Track B opener

```
You are implementing Track B (Infrastructure + Persistence) of SpaceOS Cutting Phase 5: Analytics.

Authoritative spec: SpaceOS_Modules_Cutting_Phase5_Analytics_Architecture_v4.md
Companion guide:    SpaceOS_Modules_Cutting_Phase5_Analytics_Architecture_v4_README.md

Scope for this session — Day {N} only (do not advance):
{paste the cell from §13.1 Track B column for day N}

Hard constraints:
- Migrations idempotent (IF NOT EXISTS + DO blocks per DB-09)
- RLS + FORCE ROW LEVEL SECURITY on every base table
- No raw ADO.NET via GetDbConnection().CreateCommand() (BE-01) — always ExecuteSqlInterpolatedAsync
- AsNoTracking() on every read-only repository method
- AsSplitQuery() on every multi-Include specification (BE-08)
- Phase 4 reuse: TenantSessionInterceptor, OutboxInterceptor must be registered on the new DbContext
- BYPASSRLS forbidden on cutting_app and cutting_analytics_reader (SEC-04)

Phase 4 components you must NOT duplicate — reference and reuse:
- ICuttingAuditLogger          (Phase 4 Audit)
- OutboxDispatcher             (Phase 4 BE-A01)
- TenantSessionInterceptor     (Phase 4 RLS)
- IWorkerSecurityPolicy        (Phase 4 worker consent)
- ITpmKeyProvisioner           (Phase 4 P4-9 base)
- KekFallbackProvisioner       (Phase 4 two-slot KEK)

Stop after the day's scope. Summary table only at end.
```

### 2.3 Track C opener

```
You are implementing Track C (API + BackgroundService + Tests) of SpaceOS Cutting Phase 5: Analytics.

Authoritative spec: SpaceOS_Modules_Cutting_Phase5_Analytics_Architecture_v4.md
Companion guide:    SpaceOS_Modules_Cutting_Phase5_Analytics_Architecture_v4_README.md

Scope for this session — Day {N} only:
{paste the cell from §13.1 Track C column for day N}

Hard constraints:
- xUnit v3 + Moq (no other test frameworks)
- Every new handler has a companion test file (no exceptions)
- Integration tests use Testcontainers PostgreSQL 16 (not in-memory provider)
- Pen-tests run as app_user_test role (non-superuser, see §5.2 below)
- Rate-limit load tests use real Redis Sentinel (Testcontainers)
- BackgroundService graceful-shutdown tests assert 30-sec drain window

API surface gates:
- Capability flag check on every endpoint (CuttingWaste / CuttingCapacity / CuttingOEE / CuttingOperatorMetrics)
- Rate-limit middleware on /v1/analytics/** routes (60 req/min standard, 30 req/min HeavyQuery)
- Idempotency-Key header support on POST /v1/analytics/rebuild

Stop after the day's scope. Summary table only at end.
```

---

## 3. Phase 4 dependency matrix

Phase 5 must NOT recreate any of these. If you find yourself writing one, stop and reference the existing Phase 4 implementation.

| Phase 4 component | What it provides | Phase 5 usage |
|-------------------|------------------|---------------|
| `OutboxDispatcher` (Kernel) | At-least-once event delivery, per-tenant FIFO | Subscribe `OutboxAnalyticsSubscriber` to it; project events into read-models |
| `TenantSessionInterceptor` | Sets `app.tenant_id` PostgreSQL session var | Register on `CuttingAnalyticsDbContext` (BE-01 compliance) |
| `OutboxInterceptor` | Tracks domain events for outbox | Register on `CuttingAnalyticsDbContext` (publishes Phase 5 events too) |
| `ICuttingAuditLogger` | Audit log for security events | Wrap with `IAnalyticsAuditLogger` for SEC-06 GDPR Art. 30 logging |
| `IWorkerSecurityPolicy` | Worker auth + consent | Extend with `ConsentChangeRateLimiter` (SEC-11) |
| `ITpmKeyProvisioner` | TPM key sealing/unsealing | Wrap with `TpmFallbackPolicy` v3 (SEC-02 per-tenant policy) |
| `KekFallbackProvisioner` | Two-slot KEK encryption | Used by `TpmFallbackPolicy` when fallback opt-in active |
| `IHandshakeRateLimiter` (Phase 4 interface) | Rate-limit handshake endpoints | **Keep alive** — `RedisSentinelRateLimiter` implements both this and `IRateLimiter` (BE-04) |
| Phase 4 RLS pattern | `app_user_test` non-superuser role for testing | Reuse for Phase 5 RLS pen-tests (DB-11) |
| Phase 4 `ResultExtensions.ToActionResult()` | HTTP status mapping | Audit per BE-10; add Conflict/Forbidden/NotFound mappings |

**Verification grep before shipping:**

```bash
# In src/Analytics, you should NOT find any of these — they would mean a duplicate
grep -rn "class TenantSessionInterceptor"     src/Analytics/   # → 0 matches
grep -rn "class OutboxDispatcher"             src/Analytics/   # → 0 matches
grep -rn "class CuttingAuditLogger\b"         src/Analytics/   # → 0 matches
grep -rn "BuildServiceProvider"               src/Analytics/   # → 0 matches (Golden Rule 9)
grep -rn "GetDbConnection()\.CreateCommand"   src/Analytics/   # → 0 matches (BE-01)
```

---

## 4. Local dev environment setup

### 4.1 Prerequisites

| Tool | Version | Install |
|------|---------|---------|
| .NET SDK | 8.0.x LTS | `dotnet --list-sdks` |
| PostgreSQL | 16.x | Docker `postgres:16` recommended |
| Redis | 7.x with Sentinel | Docker `bitnami/redis-sentinel:7` |
| tpm2-pkcs11 | 1.9+ | Linux: `apt install tpm2-pkcs11`. Mac/Win dev: stub provider (see §4.4) |
| EF Core CLI | 8.0.x | `dotnet tool install --global dotnet-ef` |

### 4.2 Docker compose for dev

```yaml
# docker-compose.dev.yml (Phase 5 additions to Phase 4 compose)
services:
  postgres:
    image: postgres:16
    environment:
      POSTGRES_DB: spaceos
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: dev_only_password
    ports:
      - "5432:5432"
    volumes:
      - ./scripts/init-db.sql:/docker-entrypoint-initdb.d/00-init.sql

  redis:
    image: redis:7
    ports:
      - "6379:6379"

  redis-sentinel-1:
    image: bitnami/redis-sentinel:7
    environment:
      REDIS_MASTER_HOST: redis
      REDIS_SENTINEL_QUORUM: "2"
    ports:
      - "26379:26379"
    depends_on: [redis]

  redis-sentinel-2:
    image: bitnami/redis-sentinel:7
    environment:
      REDIS_MASTER_HOST: redis
      REDIS_SENTINEL_QUORUM: "2"
    ports:
      - "26380:26379"
    depends_on: [redis]

  redis-sentinel-3:
    image: bitnami/redis-sentinel:7
    environment:
      REDIS_MASTER_HOST: redis
      REDIS_SENTINEL_QUORUM: "2"
    ports:
      - "26381:26379"
    depends_on: [redis]
```

Bring up: `docker compose -f docker-compose.dev.yml up -d`

### 4.3 Initial DB roles (one-time, before first migration)

```sql
-- scripts/init-db.sql (run once on fresh DB)
-- Phase 4 cutting_app role assumed already present.
-- Phase 5 adds cutting_analytics_reader.

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'cutting_app') THEN
    CREATE ROLE cutting_app NOLOGIN NOBYPASSRLS;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'cutting_analytics_reader') THEN
    CREATE ROLE cutting_analytics_reader NOLOGIN NOBYPASSRLS;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'app_user_test') THEN
    CREATE ROLE app_user_test LOGIN PASSWORD 'test_only' NOBYPASSRLS;
    GRANT cutting_app TO app_user_test;        -- inherits write capability
  END IF;
END $$;

-- Pre-deploy gate sanity (SEC-04)
SELECT rolname, rolbypassrls FROM pg_roles
WHERE rolname IN ('cutting_app', 'cutting_analytics_reader', 'app_user_test');
-- Expected: rolbypassrls = false for all three
```

### 4.4 TPM stub for non-Linux dev

If you are developing on macOS or Windows, the real TPM hardware is unreachable. Use the stub provider:

```csharp
// In appsettings.Development.json:
{
  "Tpm": {
    "Provider": "Stub",
    "StubBehavior": "Available"        // or "Unavailable" to test fallback path
  }
}

// src/Execution.Infrastructure/Tpm/StubTpmKeyProvisioner.cs (already exists from Phase 4)
// Implements ITpmKeyProvisioner with deterministic test fixtures
```

The stub mirrors the real provider's interface but uses an in-memory key vault. **Never enable in production** — guarded by `ASPNETCORE_ENVIRONMENT == "Production"` startup check.

---

## 5. Migration & test execution

### 5.1 Migration runner

```bash
# From repo root
cd src/Analytics

# Generate a new migration scaffold (Track B days 1-6)
dotnet ef migrations add C-0007_AnalyticsSchema \
    --project Infrastructure \
    --startup-project ../Api \
    --context CuttingAnalyticsDbContext \
    --output-dir Persistence/Migrations

# Apply migrations to local DB
dotnet ef database update \
    --project Infrastructure \
    --startup-project ../Api \
    --context CuttingAnalyticsDbContext

# Verify schema
psql -h localhost -U postgres -d spaceos -c "\dt cutting_analytics.*"
```

### 5.2 Pen-test execution (DB-11 + SEC-04)

```bash
# Run RLS pen-tests as the non-superuser role
PGPASSWORD=test_only psql -h localhost -U app_user_test -d spaceos -c "
  SET app.tenant_id = '00000000-0000-0000-0000-000000000001';
  -- Should succeed: own tenant
  SELECT count(*) FROM cutting_analytics.daily_execution_metric;

  -- Should return 0 rows: other tenant via RLS
  SET app.tenant_id = '00000000-0000-0000-0000-000000000002';
  SELECT count(*) FROM cutting_analytics.daily_execution_metric;
"

# Pre-deploy BYPASSRLS audit (SEC-04)
psql -h localhost -U postgres -d spaceos -v ON_ERROR_STOP=1 -c "
  DO \$\$
  DECLARE bad_count int;
  BEGIN
    SELECT count(*) INTO bad_count FROM pg_roles
    WHERE rolbypassrls = TRUE AND rolname NOT IN ('postgres');

    IF bad_count > 0 THEN
      RAISE EXCEPTION 'PRE-DEPLOY GATE FAILED: % roles with BYPASSRLS', bad_count;
    END IF;
  END \$\$;
"
```

### 5.3 Test execution

```bash
# Unit tests only (fast feedback during dev)
dotnet test tests/Analytics.Tests/Analytics.Tests.csproj \
    --filter "Category!=Integration"

# Integration tests (Testcontainers, ~3 min)
dotnet test tests/Analytics.Tests/Analytics.Tests.csproj \
    --filter "Category=Integration"

# Pen-tests (uses app_user_test role)
dotnet test tests/Analytics.Tests/Analytics.Tests.csproj \
    --filter "Category=PenTest"

# Full suite (CI gate)
dotnet test --logger "trx;LogFileName=test-results.trx"

# Coverage report
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

---

## 6. Per-track CLAUDE.md location

Create or update these CLAUDE.md files. They are agent-discoverable hints when a session starts in a subdirectory.

| File path | Audience | Content |
|-----------|----------|---------|
| `src/Analytics/CLAUDE.md` | Any agent entering Analytics module | Pointer to v4 doc, Golden Rules summary, axioms A5-1..A5-17 list |
| `src/Analytics/Domain/CLAUDE.md` | Track A agent | "Domain layer — zero external deps. No EF Core, no DI containers. Read-models are NOT aggregates — they are denormalized projections, but follow the same encapsulation rules (private setters, factory methods)." |
| `src/Analytics/Application/Projections/CLAUDE.md` | Track A | "Every projector wraps gate + UPSERT in a single transaction (BE-07). Use `ExecuteSqlInterpolatedAsync` not raw ADO (BE-01). Subscribe to 7 event types (see OutboxAnalyticsSubscriber)." |
| `src/Analytics/Infrastructure/Migrations/CLAUDE.md` | Track B | "Idempotent DDL (IF NOT EXISTS + DO blocks). Every base table needs RLS + FORCE RLS + WITH CHECK. Use existing C-0001..C-0006 from Phase 4 as template." |
| `src/Execution.Infrastructure/Redis/CLAUDE.md` | Track B | "DO NOT rename `RedisSentinelHandshakeRateLimiter` (BE-04). New `RedisSentinelRateLimiter` implements BOTH `IRateLimiter` and `IHandshakeRateLimiter`. Phase 4 deployed config must work unchanged." |
| `tests/Analytics.Tests/CLAUDE.md` | Track C | "Pen-tests run as `app_user_test` non-superuser role. Integration tests use Testcontainers (not in-memory provider). Every new handler has a companion test file." |

---

## 7. Smoke test checklist (deploy-after)

After merging and deploying Phase 5, run these checks against the production environment **before** announcing readiness.

### 7.1 Schema & roles

- [ ] `\dt cutting_analytics.*` lists 6 tables + 1 view
- [ ] `\df cutting_analytics.*` lists 3 functions (assert_k_anonymity, assert_l_diversity, assert_anonymity_constraints)
- [ ] `SELECT rolname, rolbypassrls FROM pg_roles WHERE rolname LIKE 'cutting_%'` — both NOBYPASSRLS
- [ ] `SELECT viewdef FROM pg_views WHERE viewname='daily_operator_metric_anonymized'` — contains `WITH (security_barrier=true)` semantics

### 7.2 Day-0 backfill (DB-03)

- [ ] `SELECT tenant_id, scope, status FROM cutting_analytics.analytics_rebuild_job ORDER BY created_at DESC LIMIT 20`
  - Expected: 1 `Full` job per active tenant, status `Pending` or `Running` immediately after deploy
- [ ] Wait 1 hour — re-run query
  - Expected: all jobs `Completed` with `chunk_done = chunk_total`
- [ ] `SELECT tenant_id, count(*) FROM cutting_analytics.daily_execution_metric GROUP BY tenant_id` — non-zero for every tenant that has execution data since 2026-04-20

### 7.3 BI access boundary (DB-01)

```bash
# Should FAIL with permission denied:
PGPASSWORD=$READER_PWD psql -h prod-db -U cutting_analytics_reader -d spaceos -c "
  SET app.tenant_id = '<doorstar-uuid>';
  SELECT * FROM cutting_analytics.daily_operator_metric LIMIT 1;
"
# Expected: ERROR:  permission denied for table daily_operator_metric

# Should SUCCEED with anonymized rows only:
PGPASSWORD=$READER_PWD psql -h prod-db -U cutting_analytics_reader -d spaceos -c "
  SET app.tenant_id = '<doorstar-uuid>';
  SELECT worker_id, is_anonymized FROM cutting_analytics.daily_operator_metric_anonymized LIMIT 5;
"
# Expected: worker_id always NULL, is_anonymized always TRUE
```

### 7.4 API endpoints

```bash
JWT=$DOORSTAR_ADMIN_JWT

# Smoke: waste report
curl -s -H "Authorization: Bearer $JWT" \
  "https://cutting.spaceos.local:5005/v1/analytics/waste?from=2026-04-20&to=2026-04-28" | jq .

# Smoke: capacity report
curl -s -H "Authorization: Bearer $JWT" \
  "https://cutting.spaceos.local:5005/v1/analytics/capacity?from=2026-04-20&to=2026-04-28" | jq .

# Smoke: operator metrics (anonymized)
curl -s -H "Authorization: Bearer $JWT" \
  "https://cutting.spaceos.local:5005/v1/analytics/operators?from=2026-04-21&to=2026-04-28&view=anonymized" | jq .
# Verify: response.rows[*].workerId field DOES NOT EXIST

# Negative: short range — should 422
curl -s -o /dev/null -w "%{http_code}\n" -H "Authorization: Bearer $JWT" \
  "https://cutting.spaceos.local:5005/v1/analytics/operators?from=2026-04-27&to=2026-04-28&view=anonymized"
# Expected: 422 (DateRange.MinDays=7 violation)

# Negative: rate-limit (61 req in 1 min)
for i in {1..61}; do
  curl -s -o /dev/null -w "%{http_code} " -H "Authorization: Bearer $JWT" \
    "https://cutting.spaceos.local:5005/v1/analytics/waste?from=2026-04-20&to=2026-04-28"
done
# Expected: 60× "200" then 1× "429"
```

### 7.5 Cron jobs

- [ ] `SELECT * FROM cron.job WHERE jobname IN ('analytics-dedup-cleanup');`
  - Expected: `0 3 * * *` schedule, active=true
- [ ] After 24h: `SELECT * FROM cron.job_run_details WHERE jobname='analytics-dedup-cleanup' ORDER BY start_time DESC LIMIT 5;`
  - Expected: status=`succeeded`, runtime <30 sec

### 7.6 Metrics & alerting

- [ ] Prometheus: `cutting_tpm_availability_ratio` reporting from every node
- [ ] Prometheus: `cutting_tpm_fallback_used_total` baseline = 0 (no fallback usage)
- [ ] Alert rule: `cutting_tpm_availability_ratio < 0.99 for 5m` → SIEM ticket (manual trigger test)

### 7.7 Phase 4 backward-compat (BE-04)

- [ ] Phase 4 handshake endpoint still rate-limits (no behavior change observable)
- [ ] Application logs do NOT contain "service not registered" or DI exceptions on startup
- [ ] Build output: `[Obsolete]` warning visible for `RedisSentinelHandshakeRateLimiter` (in expected list, not error)

### 7.8 GDPR Art. 30 audit (SEC-06)

- [ ] After Doorstar admin queries `/v1/analytics/operators?view=personal` once:
  - `SELECT * FROM cutting.audit_log WHERE event_type='OperatorMetricsAccessed' ORDER BY occurred_at DESC LIMIT 1;`
  - Expected: row exists with admin user_id, tenant_id, queried date range

---

## 8. Decision log — when in doubt

| Question | v4 reference | Default action |
|----------|--------------|----------------|
| "Should I add a new aggregate?" | §3.1 | NO. Phase 5 has 1 aggregate (`AnalyticsRebuildJob`). Read-models are projections, not aggregates. |
| "Can I use raw ADO.NET for performance?" | BE-01 | NO. Use `ExecuteSqlInterpolatedAsync`. Interceptor pipeline is non-negotiable. |
| "Can I dispatch MediatR notification cross-context?" | BE-02 | NO. Cross-bounded-context = Outbox-pattern. |
| "Can I drop C-0015 / C-0016 functions now (BE-09 supersedes them)?" | §4.1 | NO. Drop in next phase release. Comment them DEPRECATED. |
| "Can I rename `RedisSentinelHandshakeRateLimiter`?" | BE-04 | NO. Keep as `[Obsolete]` wrapper. Phase 4 backward-compat. |
| "Can I increase k-anonymity threshold for a tenant?" | SEC-01 | YES upward only. Global minimum k=5 enforced. Configure via tenant config. |
| "Can I shorten DateRange.MinDaysForOperatorQuery?" | SEC-01 / A5-11 | NO. 7 days is a hard architectural axiom. |
| "Can I enable TPM fallback in production by default?" | SEC-02 / A5-12 | NO. Per-tenant explicit opt-in only. |
| "Can I batch projection events to reduce DB round-trips?" | BE-07 | YES, but each event still needs its own gate INSERT in the same batch transaction. |
| "Can I cache the dashboard query response?" | not addressed in v4 | YES with caveats. 5-min TTL max. Document it. Treat as enhancement, not gating. |
| "What if I find a CRITICAL bug during implementation?" | meta | STOP. File the finding. Do not auto-resolve scope changes — escalate to architect. |

---

## 9. End-of-sprint handoff checklist

Before declaring Phase 5 implementation complete:

- [ ] All 21 days' tasks ticked off (DoD §9 in v4)
- [ ] ≥115 new tests pass; full suite ≥1988 pass (existing 1873 + new 115)
- [ ] `dotnet build` clean: 0 errors, 0 warnings (except expected `[Obsolete]` from BE-04)
- [ ] `dotnet list package --vulnerable` → 0 high/critical
- [ ] Pre-deploy validator passes against staging DB (SEC-04)
- [ ] Smoke test §7 above passes against staging
- [ ] CLAUDE.md updated in 6 locations (§6 above)
- [ ] OpenAPI snapshot diff reviewed and committed
- [ ] Contracts NuGet `v1.4.0` published
- [ ] Migration runbook reviewed by DBA
- [ ] Day-0 backfill runbook reviewed by ops
- [ ] `Codebase_Status_YYYYMMDD.md` updated with Phase 5 results
- [ ] Architect signs off on the test count delta and effort actuals vs. ~21.1 day budget

---

*SpaceOS — Modules.Cutting Phase 5 Analytics v4.0 README · 2026-04-28*
*Companion to: `SpaceOS_Modules_Cutting_Phase5_Analytics_Architecture_v4.md`*
*Audience: Claude Code agent sessions implementing the phase*
