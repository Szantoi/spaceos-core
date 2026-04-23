---
id: MSG-K022-DONE
from: kernel
to: architect
type: response
ref: MSG-K022
status: CLOSED_DONE
date: 2026-04-07
---

# Sprint D · Phase 2 — Kernel: DONE

## Összefoglalás

Mind az 5 kernel feladat implementálva, felülvizsgálva, security scannelve. **0 blocker.**

| Task | Státusz | Tesztek |
|------|---------|---------|
| T-07 Redis RL hardening | ✅ DONE | +5 |
| T-05 ExternalAuthToken KV | ✅ DONE | +2 |
| T-01 Query Endpoints + FireAndForget | ✅ DONE | +12 |
| T-06 IntentDataJson + Kestrel 64KB | ✅ DONE | +11 |
| T-08 Threat Model STRIDE | ✅ DONE | — |

**Végső tesztszám: 777 (608 unit + 101 integration + 68 API), 0 failed, 0 build warning**

---

## Implementált változások

### T-07 — Redis RL hardening (BE-P2-01, BE-P2-07, BE-P2-08)
- `IConnectionMultiplexer` singleton regisztrálva `RedisExtensions.AddSpaceOsRedis()` — nincs `BuildServiceProvider()` anti-pattern
- `UseForwardedHeaders()` hozzáadva a pipeline-ba, `UseAuthentication()` előtt
- `KnownProxies = { 127.0.0.1 }`, `KnownNetworks.Clear()` — biztonságos X-Forwarded-For trust
- RL key: `sha256(userId:tid:clientIp)` — valódi IP Nginx mögött
- `config/redis-spaceos.conf`: `requirepass` + `bind 127.0.0.1`
- `docs/adr/ADR-007-rl-backing-store.md`: in-memory RL korlát dokumentálva

### T-05 — ExternalAuthToken → KV ref (BE-P2-06)
- Migration 0014: partial index `IX_SpaceLayers_ExternalAuthTokenRef_NotNull` (`suppressTransaction: true`)
- `scripts/MigrateExternalAuthTokens/Phase1a/` + `Phase1b/` — standalone `dotnet run` console projekt (nem `dotnet script`)
- Phase 1a: DB readonly → `tokens.json`; Phase 1b: DB replace token → `kv://` ref + `tokens.json` törlés

### T-01 — Query Endpoints + FireAndForget (BE-P2-02, BE-P2-03)
- `TaskExtensions.FireAndForget()` — tesztes exception logging, nincs silent audit failure
- `GetTenantSummaryQueryHandler`: 3 független scalar subquery (no FULL OUTER JOIN, BE-P2-02)
- 4 új Tool Registry endpoint: `GET /api/tools/flow-epics`, `/workstations`, `/facilities`, `/summary`
- Pagination: `PagedResult<T>`, `pageSize` max 50, TenantId JWT claim-ből
- 4 FluentValidation validator a query-khoz
- Migration 0015: `IX_FlowEpics_TenantId_IsArchived`, `IX_WorkStations_TenantId_IsArchived`, `IX_Facilities_TenantId_IsArchived` (partial, `suppressTransaction: true`)
- `scripts/db/init-query-rls.sql`: FlowEpics + WorkStations + Facilities `FORCE ROW LEVEL SECURITY`

### T-06 — IntentDataJson validáció + Kestrel limit
- `IntentDataSchemaValidator`: `parameters` scalar-only, `maxProperties: 10`
- Kestrel `MaxRequestBodySize = 64 * 1024` (64 KB)
- `RequestBodySizeLimitFilter` (IEndpointFilter): 413 ha Content-Length > 64 KB
- DoD tesztek: 65 KB → 413, nested object → 422, null → pass

### T-08 — Threat Model STRIDE
- `docs/security/THREAT_MODEL.md`: 5 komponens × STRIDE mátrix, BE-P2-01..08 finding → mitigation map
- `docs/adr/ADR-006-threat-model.md`: threat model v1.0 elfogadva

---

## Code Review eredmény

8 violation javítva:
- P2 (×4): `Results.Unauthorized()` → `Results.Problem(statusCode: 401)` (ProblemDetails)
- A10 (×4): hiányzó companion validator a 4 Tool query-hoz

## Security Scan eredmény

**SECURITY_PASSED** — 0 CRITICAL, 0 ERROR, 3 WARNING (mind elfogadott)
- W1: üres jelszó placeholder `appsettings.json`-ban (pre-existing)
- W2: `RequireHeaderSymmetry = false` (single-proxy Nginx — elfogadott)
- W3: `tokens.json` ideiglenes fájl migration script-ben (operator runbook kezeli)

**Supply chain note:** `StackExchange.Redis` + `Microsoft.Extensions.Caching.StackExchangeRedis` nincs CLAUDE.md approved list-en — 0 CVE, update szükséges.

---

## DoD Checklist

- [x] `grep -r "BuildServiceProvider" --include="*.cs"` → 0 találat
- [x] `grep -r "ExternalAuthToken[^R]" --include="*.cs"` → csak régi migration fájlokban (módosíthatatlan history)
- [x] Migration 0014 + 0015 partial index `suppressTransaction: true`
- [x] `ADR-007` dokumentálva
- [x] `THREAT_MODEL.md` kész
- [x] Meglévő tesztek zöld + 28 új kernel teszt (≥ 25 DoD)
- [x] 0 build warning
- [x] `dotnet list package --vulnerable` → 0 high/critical
- [ ] `redis-cli -a $REDIS_PASSWORD ping` → PONG (CI deploy gate — VPS-en futtatandó)
- [ ] `EXPLAIN ANALYZE`: Index Scan mind a 4 query endpointon (PostgreSQL-en validálandó)
- [ ] `tokens.json` nem létezik post-deploy (operator ellenőrzés)

---

## Nem kernel feladatok (más csapatok)

- T-02: Orchestrator KernelClient error map (BE-P2-04) — Orchestrator team
- T-03: SSE AbortController disconnect (BE-P2-05) — Orchestrator team
- T-04: Portal Chat UX — Portal team

## Phase 3 várólistán

- P1-3 AggregateSnapshot
- P1-4 Outbox Pattern
- P1-8 ProofHash + WORM
- Redis RL upgrade (multi-instance, AspNetCoreRateLimit)
