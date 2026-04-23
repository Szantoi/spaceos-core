---
id: MSG-K022
from: architect
to: kernel
type: task
status: DONE
priority: P0
sprint: "Sprint D · Phase 2"
ref: "/opt/spaceos/docs/SpaceOS_Sprint_D_Phase2_v4.md"
---

# Sprint D · Phase 2 — Kernel: Query Endpoints + Redis RL + ExternalAuthToken + IntentDataJson

## Összefoglaló

**4 feladat, ~8 fejlesztői nap.** A Phase 1.5 DoD teljesítése után (T-01..T-05 zöld) azonnal startolhat.

**Implementációs sorrend (v4):**
```
Nap 1:   T-07 — Redis RL hardening (singleton multiplexer + ForwardedHeaders)
Nap 2:   T-05 — ExternalAuthToken → KV ref (standalone console project + Migration 0014)
Nap 3–4: T-01 — Query endpoints + scalar subquery + LoggedFireAndForget + Migration 0015
Nap 5:   T-06 — IntentDataJson validáció + Kestrel limit
Nap 6:   T-08 — Threat Model (STRIDE) + ADR-006 + ADR-007
```

> ⚠️ MINDIG hivatkozd az eredeti dokumentumot: `/opt/spaceos/docs/SpaceOS_Sprint_D_Phase2_v4.md` — a teljes kódspecifikáció ott van.

---

## T-07 · Redis RL hardening (Nap 1) — 3 finding

### BE-P2-01 FIX — `IConnectionMultiplexer` singleton (CRITICAL)

**Probléma:** `services.BuildServiceProvider()` a Redis factory lambda-ban — 2. DI container, memory leak prod-ban.

**Javítás:** `Infrastructure/Extensions/RedisExtensions.cs` — `IConnectionMultiplexer` direkt singleton regisztrálása:
- `ConnectionMultiplexer.Connect(redisOptions)` egyszer, factory nélkül
- `AddStackExchangeRedisCache` → `ConnectionMultiplexerFactory = () => Task.FromResult<IConnectionMultiplexer>(multiplexer)`
- Redis hiány esetén → `AddDistributedMemoryCache()` + `Log.Warning`
- `redisOptions`: `Password`, `Ssl`, `AbortConnect = false`, `ConnectTimeout = 3000`, `ExponentialRetry`

**DoD:**
- [ ] `grep -r "BuildServiceProvider" --include="*.cs"` → 0 találat
- [ ] `redis-cli -a $REDIS_PASSWORD ping` → PONG (CI deploy gate)
- [ ] `ValidateOnStart()` prod fail-fast

### BE-P2-08 FIX — `UseForwardedHeaders()` (MEDIUM)

**Probléma:** `ctx.Connection.RemoteIpAddress` Nginx proxy mögött mindig `127.0.0.1` → RL key hibás.

**Javítás:** `SpaceOS.Kernel.Api/Program.cs`:
```
ForwardedHeaders = XForwardedFor | XForwardedProto
KnownProxies.Add(IPAddress.Loopback)
KnownNetworks.Clear()
RequireHeaderSymmetry = false
```
**KRITIKUS middleware sorrend:** `UseForwardedHeaders()` → `UseAuthentication()` → `UseAuthorization()`

RL key: `sha256(userId + valódi clientIp)` — nem `127.0.0.1`

### BE-P2-07 FIX — ADR-007 (MEDIUM)

**Probléma:** Az ASP.NET Core `AddRateLimiter` in-memory — Redis NEM backing store. Dokumentálatlan korlát.

**Javítás:** `docs/adr/ADR-007-rl-backing-store.md` létrehozása:
- Phase 2: elfogadott (single VPS instance)
- Upgrade gate: `AspNetCoreRateLimit` + Redis → multi-instance esetén

**DoD:**
- [ ] `IConnectionMultiplexer` singleton — nincs `BuildServiceProvider()`
- [ ] `UseForwardedHeaders` MEGELŐZI `UseAuthentication`-t
- [ ] RL key: sha256(userId + valódi IP)
- [ ] `requirepass` + `bind 127.0.0.1` Redis config (`config/redis-spaceos.conf`)
- [ ] `ADR-007` dokumentálva

---

## T-05 · ExternalAuthToken → KV ref (Nap 2) — BE-P2-06 FIX

**Probléma:** `dotnet script` parancs nem létezik alap .NET SDK-ban — VPS-en runtime fail.

**Javítás:** Önálló `MigrateExternalAuthTokens` console projekt:
```
scripts/
  MigrateExternalAuthTokens/
    MigrateExternalAuthTokens.csproj   (net8.0, Npgsql 8.*, Dapper 2.*)
    Phase1a/Program.cs                  ← DB readonly + KV write
    Phase1b/Program.cs                  ← DB write only
    TokenEntry.cs
```

**Futtatás:** `dotnet run --project scripts/MigrateExternalAuthTokens/Phase1a -- --db-readonly ... --kv-write ...`

**Migration 0014:** `ExternalAuthToken_To_KeyVaultRef` — rename + partial index, `suppressTransaction: true`

**DoD:**
- [ ] Phase 1a: `tokens.json` létrejön; Phase 1b: `tokens.json` törölve
- [ ] Migration 0014: `ExternalAuthTokenRef` + partial index, `ExternalAuthToken` nem létezik
- [ ] `IKeyVaultClient` + `LocalKeyVaultClient` (dev) + `AzureKeyVaultClient` (prod)
- [ ] Smoke test: meglévő federált SpaceLayer → 200 OK
- [ ] `grep -r "ExternalAuthToken[^R]" --include="*.cs"` → 0 találat
- [ ] `tokens.json` nem létezik post-deploy

---

## T-01 · Kernel Query Endpoints (Nap 3–4) — 2 finding

### BE-P2-02 FIX — Summary SQL: scalar subquery, nem FULL OUTER JOIN (CRITICAL)

**Probléma:** `FULL OUTER JOIN "WorkStations" ws ON true` → cartesian product, O(n²), hibás COUNT.

**Javítás:** `Application/Query/Handlers/GetTenantSummaryQueryHandler.cs` — 3 független scalar subquery:
```sql
SELECT
  (SELECT COUNT(*)::int FROM "FlowEpics" WHERE "TenantId" = {0} AND "IsArchived" = false),
  (SELECT COUNT(*)::int FROM "WorkStations" WHERE "TenantId" = {0} AND "Status" = 'Active' AND "IsArchived" = false),
  (SELECT COUNT(*)::int FROM "B2BHandshakes" WHERE "InitiatorTenantId" = {0} AND "Status" = 'Pending')
```
Nincs JOIN, nincs cartesian product. Az `IX_FlowEpics_TenantId_Status` partial index (Migration 0015) lefedi.

### BE-P2-03 FIX — Unobserved Task: `FireAndForget` helper (HIGH)

**Probléma:** `_ = _auditDispatcher.DispatchAsync(...)` — exception elnyelődik, audit event csendesen elveszik.

**Javítás:** `Application/Common/TaskExtensions.cs`:
```csharp
public static void FireAndForget(this Task task, ILogger logger, string context)
    => task.ContinueWith(
        t => logger.LogError(t.Exception, "Fire-and-forget task failed. Context: {Context}", context),
        TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
```
Használat: `_auditDispatcher.DispatchAsync(...).FireAndForget(_logger, "ToolQueryExecuted:list_flow_epics")`

### Endpoint specifikáció

4 endpoint: `AsNoTracking`, Ardalis.Spec, `Result<PagedList<T>>`, `ConfigureAwait(false)`
- TenantId JWT claim-ből (nem header)
- RLS: FlowEpics + WorkStations + Facilities `FORCE ROW LEVEL SECURITY`
- Migration 0015 `suppressTransaction: true` — `CONCURRENTLY` fut

**Migration 0015 futtatási sorrend:**
```bash
dotnet run --project scripts/MigrateExternalAuthTokens/Phase1a -- ...
dotnet run --project scripts/MigrateExternalAuthTokens/Phase1b -- ...
dotnet ef database update 0014_ExternalAuthToken_To_KeyVaultRef --context AppDbContext
dotnet ef database update 0015_QueryEndpointIndexes --context AppDbContext
psql -U postgres -d spaceos -f scripts/db/init-query-rls.sql
```

**DoD:**
- [ ] 4 endpoint: `AsNoTracking`, Ardalis.Spec, `Result<PagedList<T>>`, `ConfigureAwait(false)`
- [ ] Summary: scalar subquery (nem FULL OUTER JOIN)
- [ ] `ToolQueryExecuted` audit: `FireAndForget` helper — nem raw `_ = task`
- [ ] TenantId JWT claim-ből
- [ ] RLS: FlowEpics + WorkStations + Facilities `FORCE ROW LEVEL SECURITY`
- [ ] Migration 0015 `suppressTransaction: true`
- [ ] **`EXPLAIN ANALYZE`: Index Scan minden endpointon — Seq Scan nincs**
- [ ] Integration teszt: cross-tenant → 0 sor; audit fail → `Log.Error` (nem silent)

---

## T-06 · IntentDataJson validáció (Nap 5)

**Javítás:**
- Schema: `parameters` scalar-only, `maxProperties: 10`
- Kestrel: `MaxRequestBodySize = 64 KB`

**DoD:**
- [ ] Unit teszt: 65 KB → 413; nested object → 422; null → pass

---

## T-08 · Threat Model (Nap 6)

**Output:** `docs/security/THREAT_MODEL.md` + `docs/adr/ADR-006-threat-model.md` + `docs/adr/ADR-007-rl-backing-store.md`

STRIDE: 5 komponens (Nginx, Orchestrator, Kernel, PostgreSQL, Redis) — minden BE-P2-01..BE-P2-08 finding szerepeljen a mitigáció map-ben.

---

## Összesített DoD (Kernel)

- [ ] `grep -r "BuildServiceProvider" --include="*.cs"` → 0 találat
- [ ] `grep -r "ExternalAuthToken[^R]" --include="*.cs"` → 0 találat
- [ ] `EXPLAIN ANALYZE`: Index Scan mind a 4 query endpointon
- [ ] `redis-cli -a $REDIS_PASSWORD ping` → PONG
- [ ] `tokens.json` nem létezik post-deploy
- [ ] `ADR-007` dokumentálva, `THREAT_MODEL.md` kész
- [ ] Meglévő tesztek zöld + Phase 2 új kernel tesztek ≥ 25 db
- [ ] 0 build warning (xUnit1051 kivételével)
- [ ] `dotnet list package --vulnerable` → 0 high/critical
