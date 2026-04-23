# SpaceOS — Sprint D · Phase 2
## Tool Registry Live Integration + P1 Security Debt Closure — v4.0

> v3.0 felülvizsgálva `/senior-backend` skillel — 2026-04-06  
> **8 backend finding javítva:** 2 CRITICAL + 3 HIGH + 3 MEDIUM  
> Kumulált review: `/database-schema-designer` → v2 · `/senior-security` → v3 · `/senior-backend` → v4  
> **Státusz: IMPLEMENTÁCIÓRA KÉSZ — végleges tervdokumentum**

| Attribútum | Érték |
|---|---|
| Verzió | v4.0 — 2026-04-06 |
| Előzmény | v3.0 felülvizsgálva `/senior-backend`: 2 CRITICAL + 3 HIGH + 3 MEDIUM finding |
| Blokkoló feltétel | Phase 1.5 v4 DoD teljes (T-01..T-05 zöld) |
| Becsült időtartam | **16 fejlesztői nap** (v3: 14 nap → +2 nap) |
| Track struktúra | A ‖ B párhuzamos, majd C sorrendben |
| Migration sorrend | `0014` (ExternalAuthTokenRef) · `0015` (Query indexes) |
| Következő fázis | Phase 3 — Modules.Joinery MVP |

---

## 1. Kumulált Finding Összesítő (v1 → v4)

| Review | Finding-ek | Legfontosabb javítás | Effort delta |
|---|---|---|---|
| v1 → `/database-schema-designer` → v2 | 1C + 2H + 3M | 3 fázisú KV migráció; composite indexek; single SQL summary; RLS query tables | +1 nap |
| v2 → `/senior-security` → v3 | 2C + 4H + 4M | Redis AUTH+TLS; prompt injection guard; SSE RL + sanitization; audit log; payload limit | +2 nap |
| v3 → `/senior-backend` → v4 | 2C + 3H + 3M | `BuildServiceProvider` anti-pattern; FULL OUTER JOIN cartesian; unobserved Task; KernelClient error map; SSE abort; `dotnet script`; RL backing store doc; ForwardedHeaders | +2 nap |
| **Összesen** | **4C + 9H + 11M** | **16 fejlesztői nap végleges becslés** | |

### `/senior-backend` Findings — v3 → v4

| ID | Súly | Terület | Probléma | v4 javítás |
|---|---|---|---|---|
| BE-P2-01 | 🔴 CRITICAL | T-07 DI | `services.BuildServiceProvider()` Redis factory-ban — 2. DI container, memory leak (azonos: Phase 1.5 BE-P15-01) | `IConnectionMultiplexer` singleton regisztrálása külön, factory lambda nélkül |
| BE-P2-02 | 🔴 CRITICAL | T-01 SQL | `FULL OUTER JOIN ... ON true` cartesian product — 100 epic × 50 WS = 5000 sor FILTER előtt, COUNT hibás | 3 soros CTE vagy `(SELECT COUNT(*) FROM ...)` scalar subquery |
| BE-P2-03 | 🟠 HIGH | T-01 Audit | `_ = task` unobserved Task — exception silently swallowed, audit silent failure | `_ = LoggedFireAndForget(task, _logger)` helper: `task.ContinueWith(LogException, ...)` |
| BE-P2-04 | 🟠 HIGH | T-02 KernelClient | Csak 401 kezelve — 429/503/timeout unhandled → raw 500 + internal exception a chat response-ban | `KernelClientError` enum: `AuthExpired / RateLimited / Unavailable / Timeout` — minden HTTP státuszra |
| BE-P2-05 | 🟠 HIGH | T-03 SSE | Client disconnect → `for await` fut tovább — LLM API billing + connection resource leak | `req.on('close', ...)` → `AbortController.abort()` → az async generator tisztán leáll |
| BE-P2-06 | 🟡 MEDIUM | T-05 Tooling | `dotnet script` nem standard CLI — VPS-en `dotnet-script` globális tool nélkül runtime fail | Standalone console project: `scripts/MigrateExternalAuthTokens/` |
| BE-P2-07 | 🟡 MEDIUM | T-07 RL | ASP.NET Core `AddRateLimiter` nem támogat Redis backing store-t — RL in-memory, dokumentálatlan korlát | Explicit dokumentálás: single-instance VPS-en elfogadható; multi-instance → `AspNetCoreRateLimit` + Redis |
| BE-P2-08 | 🟡 MEDIUM | T-07 RL Key | `ctx.Connection.RemoteIpAddress` = `127.0.0.1` Nginx proxy mögött — RL key = `sha256(userId)` lesz | `UseForwardedHeaders()` + `KnownProxies.Add(IPAddress.Loopback)` |

---

## 2. Kontextus

### Phase 1.5 lezárt (blokkoló feltétel)

P0-1 JWT ES256 · P0-2 Hash Sink · P0-3 Race condition · P0-4 RLS audit · P1-1 TenantId JWT

### Phase 2-ben zárul

Tool Registry live · SSE (secured) · P1-2 ExternalAuthToken KV · P1-5 IntentDataJson · P1-6 Redis RL · P1-7 Threat Model

### Phase 3-ra marad

P1-3 AggregateSnapshot · P1-4 Outbox · P1-8 ProofHash+WORM · P2-1..P2-6

---

## 3. Feladatok és sorrend — v4

| # | Track | Feladat | Effort | v3 delta |
|---|---|---|---|---|
| T-01 | A | Kernel query endpoints + RLS + indexek + audit + **CTE summary fix** | 3 nap | BE-P2-02 + BE-P2-03 beépítve |
| T-02 | A | Orchestrator Tool Registry + prompt inject guard + **teljes KernelClient error map** | 3 nap | BE-P2-04 beépítve |
| T-03 | A | SSE streaming + RL + sanitization + **AbortController disconnect** | 2 nap | BE-P2-05 beépítve |
| T-04 | A | Portal Chat UX — streaming + ToolResultCard | 1 nap | — |
| T-05 | B | ExternalAuthToken → KV ref (**standalone console project** + 2 credential) | 2 nap | BE-P2-06 beépítve |
| T-06 | B | IntentDataJson schema + maxDepth + RequestSizeLimit | 1 nap | — |
| T-07 | B | Redis RL + AUTH + TLS + **IConnectionMultiplexer singleton** + **UseForwardedHeaders** | 2 nap | BE-P2-01 + BE-P2-07 + BE-P2-08 beépítve |
| T-08 | C | Threat Model (STRIDE) | 1 nap | — |
| | | **Tesztek + E2E** | 1 nap | |

**Összesen: 16 fejlesztői nap**

---

## 4. Track A — Tool Registry Live Integration (v4)

### T-01 · Kernel: Query endpoint-ok (v4)

#### 4.1 BE-P2-02 FIX — Summary SQL: scalar subquery CTE, nem FULL OUTER JOIN

**v3 probléma:** `FULL OUTER JOIN "WorkStations" ws ON true` cartesian product-ot képez minden FlowEpic × WorkStation sor között. 100 FlowEpic és 50 WorkStation esetén 5000 sor keletkezik a `FILTER` futtatása előtt — a COUNT értékek hibásak és a query O(n²) komplexitású.

**v4 javítás — 3 független scalar subquery:**

```csharp
// Application/Query/Handlers/GetTenantSummaryQueryHandler.cs
public async Task<Result<TenantSummaryDto>> Handle(
    GetTenantSummaryQuery request, CancellationToken ct)
{
    // BE-P2-02: scalar subqueries — no cartesian product, no JOIN
    var summary = await _ctx.Database
        .SqlQueryRaw<TenantSummaryRaw>("""
            SELECT
              (SELECT COUNT(*)::int FROM "FlowEpics"
               WHERE "TenantId" = {0} AND "IsArchived" = false)
                  AS "FlowEpicCount",
              (SELECT COUNT(*)::int FROM "WorkStations"
               WHERE "TenantId" = {0} AND "Status" = 'Active' AND "IsArchived" = false)
                  AS "ActiveWorkstationCount",
              (SELECT COUNT(*)::int FROM "B2BHandshakes"
               WHERE "InitiatorTenantId" = {0} AND "Status" = 'Pending')
                  AS "PendingHandshakeCount"
            """, request.TenantId)
        .SingleAsync(ct)
        .ConfigureAwait(false);

    return Result.Success(new TenantSummaryDto(
        summary.FlowEpicCount,
        summary.ActiveWorkstationCount,
        summary.PendingHandshakeCount));
}
```

> **Miért scalar subquery és nem CTE?** A CTE-vel azonos query plan, de a scalar subquery olvashatóbb és minden sub-select pontosan 1 sort ad vissza — a `SingleAsync` nem vétkezhet. A meglévő `IX_FlowEpics_TenantId_Status` partial index (Migration 0015) mindhárom subquery-t lefedi.

#### 4.2 BE-P2-03 FIX — Unobserved Task: `LoggedFireAndForget` helper

**v3 probléma:** `_ = _auditDispatcher.DispatchAsync(...)` — az unobserved `Task` exception-t .NET 4.5+ óta elnyelődik (az `UnobservedTaskException` event alapértelmezetten nem terminál). Ha az audit DB nem érhető el, az audit event csendesen elveszik — nincs log, nincs metrika, nincs visszajelzés.

**v4 javítás:**

```csharp
// Application/Common/TaskExtensions.cs
public static class TaskExtensions
{
    /// <summary>
    /// Fire-and-forget with guaranteed exception logging.
    /// Prevents silent audit failures (BE-P2-03).
    /// </summary>
    public static void FireAndForget(this Task task, ILogger logger, string context)
        => task.ContinueWith(
            t => logger.LogError(t.Exception,
                "Fire-and-forget task failed. Context: {Context}", context),
            TaskContinuationOptions.OnlyOnFaulted |
            TaskContinuationOptions.ExecuteSynchronously);
}

// Használat a handler-ben:
_auditDispatcher
    .DispatchAsync(auditEvent, CancellationToken.None)
    .FireAndForget(_logger, "ToolQueryExecuted:list_flow_epics");
```

> `ExecuteSynchronously` — a continuation a ThreadPool-on fut, nem dedikált szálon. Nincs teljesítmény-overhead; a log garantáltan megjelenik a `CancellationToken.None` miatt.

#### 4.3 Endpoint specifikáció + DB fixes (v3-ból változatlan)

Endpoint-ok, RLS policy (DB-04), Migration 0015 composite indexek (DB-02), TenantId JWT claim — lásd v3 §4.1 + §4.3.

### T-02 · Orchestrator: Tool Registry + KernelClient (v4)

#### 4.4 BE-P2-04 FIX — Teljes KernelClient error map

**v3 probléma:** Csak a 401 volt kezelve. 429 (Kernel saját rate limit), 503 (Kernel unavailable), network timeout — ezek `throw` esetén az Express error handler raw 500-at küld vissza, ami az agentic loop-ban stack trace-t injektál a chat response-ba.

**v4 javítás:**

```typescript
// src/kernel/kernelClient.ts

export const enum KernelErrorCode {
  AuthExpired    = 'ERR_TOOL_AUTH_EXPIRED',
  RateLimited    = 'ERR_TOOL_RATE_LIMITED',
  Unavailable    = 'ERR_KERNEL_UNAVAILABLE',
  Timeout        = 'ERR_KERNEL_TIMEOUT',
  BadRequest     = 'ERR_TOOL_BAD_REQUEST',
  Unknown        = 'ERR_KERNEL_UNKNOWN',
}

export class KernelClientError extends Error {
  constructor(
    public readonly code: KernelErrorCode,
    public readonly httpStatus: number,
    message: string
  ) { super(message); this.name = 'KernelClientError'; }
}

async get<T>(path: string, params?: Record<string, unknown>): Promise<T> {
  let response: Response;

  try {
    const url = new URL(path, this.baseUrl);
    if (params) Object.entries(params).forEach(([k, v]) =>
      v != null && url.searchParams.set(k, String(v)));

    response = await fetch(url, {
      headers: { Authorization: `Bearer ${this.jwtProvider()}` },
      signal: AbortSignal.timeout(10_000),   // 10s kernel timeout
    });
  } catch (err: unknown) {
    if (err instanceof DOMException && err.name === 'TimeoutError') {
      throw new KernelClientError(KernelErrorCode.Timeout, 0,
        'Kernel request timed out after 10s');
    }
    throw new KernelClientError(KernelErrorCode.Unavailable, 0,
      'Kernel unreachable');
  }

  if (response.ok) return response.json() as Promise<T>;

  // Exhaustive HTTP status mapping — no raw status codes leak to chat
  switch (response.status) {
    case 400: throw new KernelClientError(KernelErrorCode.BadRequest,    400, 'Invalid query parameters');
    case 401: throw new KernelClientError(KernelErrorCode.AuthExpired,   401, 'Session expired. Please log in again.');
    case 429: throw new KernelClientError(KernelErrorCode.RateLimited,   429, 'Too many requests. Retry shortly.');
    case 503: throw new KernelClientError(KernelErrorCode.Unavailable,   503, 'Kernel temporarily unavailable.');
    default:  throw new KernelClientError(KernelErrorCode.Unknown, response.status,
                `Unexpected Kernel response: ${response.status}`);
  }
}
```

**Interpreter service — hibakezelés az agentic loop-ban:**

```typescript
// src/interpreter/interpreterService.ts
try {
  result = await executeTool(toolName, toolInput, kernelClient);
} catch (err) {
  if (err instanceof KernelClientError) {
    // Structured error → LLM tool_result, nem throw
    toolResultContent = buildToolErrorResult(toolUseBlock.id, err.code, err.message);
    continue;  // agentic loop folytatódik — az LLM dönt a következő lépésről
  }
  throw err;   // ismeretlen hiba → loop megszakad
}
```

> Az LLM tool_result-ként kapja a hibát (nem HTTP 500-at), és képes rá reagálni: "A session lejárt, kérlek lépj be újra."

#### 4.5 Prompt injection guard + JWT 401 wrapper (v3-ból változatlan)

`wrapToolResult()` + `sanitizeToolResultForLlm()` — lásd v3 §4.4.

### T-03 · SSE streaming + BE-P2-05 AbortController (v4)

#### 4.6 BE-P2-05 FIX — SSE connection abort on client disconnect

**v3 probléma:** Ha a felhasználó bezárja a böngésző tabját, az Express request `close` event-et küld, de a `for await (const chunk of interpreterService.streamChat(...))` loop fut tovább — az LLM API-t tovább terheli, a Kernel tool hívások futnak, és a kapcsolat erőforrás csak akkor szabadul fel, ha az LLM befejezi a generálást.

**v4 javítás:**

```typescript
// src/routes/chat.route.ts
router.post('/chat', authMiddleware, sseChatRateLimit, async (req, res) => {
  // SSE headers
  res.setHeader('Content-Type', 'text/event-stream');
  res.setHeader('Cache-Control', 'no-cache');
  res.setHeader('Connection', 'keep-alive');
  res.flushHeaders();

  // BE-P2-05: AbortController — client disconnect cleanly stops the generator
  const abortController = new AbortController();

  req.on('close', () => {
    abortController.abort();
  });

  try {
    for await (const chunk of interpreterService.streamChat(
      req.body,
      abortController.signal   // signal passed to Anthropic SDK + KernelClient
    )) {
      if (abortController.signal.aborted) break;
      SseSerializer.write(res, chunk);
    }
  } catch (err) {
    if ((err as Error).name !== 'AbortError') {
      logger.error({ err }, 'SSE stream error');
      SseSerializer.write(res, { error: 'stream_error' });
    }
    // AbortError: client disconnected — silent, expected
  } finally {
    res.write('data: [DONE]\n\n');
    res.end();
  }
});
```

**Interpreter service — AbortSignal propagation:**

```typescript
// src/interpreter/interpreterService.ts
async *streamChat(
  body: ChatRequest,
  signal: AbortSignal      // propagated from route
): AsyncGenerator<ChatChunk> {

  // Anthropic SDK natívan elfogadja az AbortSignal-t:
  const stream = await this.llmProvider.stream(messages, { signal });

  for await (const event of stream) {
    signal.throwIfAborted();   // explicit check az agentic loop minden iterációján
    yield mapEvent(event);
  }
}
```

> Az Anthropic Node.js SDK v0.20+ natívan kezeli az `AbortSignal`-t — az in-flight HTTP kérés megszakad. A `KernelClient` `fetch()` hívásai szintén az `AbortSignal`-t kapják, így a tool hívások is leállnak.

#### 4.7 SSE RL + data sanitization (v3-ból változatlan)

`sseChatRateLimit`, `SseSerializer.sanitize()`, Nginx `proxy_buffering off` — lásd v3 §4.6 + §4.7.

---

## 5. Track B — P1 Security Debt Closure (v4)

### T-05 · ExternalAuthToken → KV ref + BE-P2-06 standalone project

**v3 probléma (BE-P2-06):** `dotnet script scripts/migrate-...csx` — a `dotnet script` parancs nem létezik alap .NET SDK-ban. A `dotnet-script` egy harmadik fél által fejlesztett globális tool (`dotnet tool install -g dotnet-script`), amely a VPS-en nincs telepítve.

**v4 javítás:** Önálló `MigrateExternalAuthTokens` console projekt:

```
scripts/
  MigrateExternalAuthTokens/
    MigrateExternalAuthTokens.csproj
    Phase1a/
      Program.cs   ← DB readonly + KV write
    Phase1b/
      Program.cs   ← DB write only
    TokenEntry.cs
```

```xml
<!-- scripts/MigrateExternalAuthTokens/MigrateExternalAuthTokens.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Npgsql" Version="8.*" />
    <PackageReference Include="Dapper" Version="2.*" />
    <PackageReference Include="Microsoft.Extensions.Configuration.EnvironmentVariables" Version="8.*" />
  </ItemGroup>
</Project>
```

**Futtatás VPS-en:**

```bash
# Fázis 1a
dotnet run --project scripts/MigrateExternalAuthTokens/Phase1a \
  -- --db-readonly "$DB_READONLY_CONNECTION" --kv-write "$KV_WRITE_CREDENTIAL"

# Fázis 1b
dotnet run --project scripts/MigrateExternalAuthTokens/Phase1b \
  -- --db-write "$DB_WRITE_CONNECTION"
```

> Standard `dotnet run` — nincs külső tool dependency. A VPS-en a `dotnet` már elérhető (`/opt/dotnet`, Phase 1 D-P1 javítás #17).

A 2 credential-készlet és a `tokens.json` lifecycle (v3 SEC-P2-08) — változatlan.

### T-06 · IntentDataJson validáció (v3-ból változatlan)

Schema (`parameters` scalar-only, `maxProperties`), Kestrel `MaxRequestBodySize = 64 KB` — lásd v3 §5 T-06.

### T-07 · Redis RL + BE-P2-01 + BE-P2-07 + BE-P2-08

#### BE-P2-01 FIX — `IConnectionMultiplexer` singleton, nincs `BuildServiceProvider()`

**v3 probléma:** `services.BuildServiceProvider()` a Redis factory lambda-ban — azonos a Phase 1.5 BE-P15-01 CRITICAL finding-gel. Második DI container keletkezik, Singleton/Scoped conflict lehetséges, memory leak prod-ban.

**v4 javítás:** `IConnectionMultiplexer` direkt singleton regisztrálása:

```csharp
// Infrastructure/Extensions/RedisExtensions.cs
public static IServiceCollection AddSpaceOsRedis(
    this IServiceCollection services,
    IConfiguration config)
{
    var redisUrl = config["Redis:ConnectionString"];

    if (string.IsNullOrEmpty(redisUrl))
    {
        // Dev fallback: InMemoryCache — ne inicializálja a Redis-t
        services.AddDistributedMemoryCache();
        return services;
    }

    // BE-P2-01 FIX: IConnectionMultiplexer singleton — no BuildServiceProvider()
    var redisOptions = ConfigurationOptions.Parse(redisUrl);
    redisOptions.Password       = config["Redis:Password"];
    redisOptions.Ssl            = config.GetValue<bool>("Redis:UseTls", false);
    redisOptions.AbortConnect   = false;
    redisOptions.ConnectTimeout = 3_000;
    redisOptions.SyncTimeout    = 2_000;
    redisOptions.KeepAlive      = 60;
    redisOptions.ReconnectRetryPolicy =
        new ExponentialRetry(deltaBackoffMs: 500, maxDeltaBackoffMs: 10_000);

    // Singleton — egyszer csatlakozik, multiplexed connection pool
    var multiplexer = ConnectionMultiplexer.Connect(redisOptions);
    services.AddSingleton<IConnectionMultiplexer>(multiplexer);

    // StackExchange.Redis cache a már regisztrált multiplexerre hivatkozva
    services.AddStackExchangeRedisCache(opts =>
        opts.ConnectionMultiplexerFactory = () =>
            Task.FromResult<IConnectionMultiplexer>(multiplexer));

    return services;
}
```

#### BE-P2-07 FIX — RL backing store limitáció dokumentálása + upgrade path

**v3 probléma:** Az ASP.NET Core beépített `AddRateLimiter` (dotnet 7+) in-memory partition store-t használ — a `SlidingWindowLimiter` state a folyamat memóriájában él. Ez dokumentálatlan, és félrevezető a Redis dependency mellett.

**v4 javítás — explicit ADR:**

```
ADR-007: Rate Limiting Backing Store — Single Instance Constraint

Kontextus: Az ASP.NET Core beépített AddRateLimiter sliding window limiter
in-memory state-t használ. Redis-t NEM használja backing store-ként.

Döntés: Phase 2-ben elfogadott — SpaceOS egyetlen VPS instance-on fut.
Az in-memory RL per-process, ami single instance esetén == globális.

Upgrade gate (Phase 3+ multi-instance esetén):
  AspNetCoreRateLimit (NuGet) + IDistributedCache (Redis) → globális RL.
  Trigger: load balancer bevezetése VAGY horizontális skálázás.

Jelenlegi korlát dokumentálva: ha a process újraindul, az RL számlálók nullázódnak.
```

#### BE-P2-08 FIX — `UseForwardedHeaders()` a valódi client IP-hez

**v3 probléma:** Az ASP.NET Core `ctx.Connection.RemoteIpAddress` Nginx reverse proxy mögött mindig `127.0.0.1` (loopback) — mert a fizikai TCP kapcsolat az Orchestrator felől jön. `X-Forwarded-For` header alapján lehet a valódi IP-t kiolvasni, de ez explicit middleware konfigurációt igényel.

**v4 javítás:**

```csharp
// SpaceOS.Kernel.Api/Program.cs
// BE-P2-08: UseForwardedHeaders — VPS Nginx proxy mögött kötelező
builder.Services.Configure<ForwardedHeadersOptions>(opts =>
{
    opts.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    opts.KnownProxies.Add(IPAddress.Loopback);      // 127.0.0.1 — Nginx loopback proxy
    opts.KnownNetworks.Clear();                      // csak explicit KnownProxies
    opts.RequireHeaderSymmetry = false;
});

// Pipeline-ban: UseForwardedHeaders ELŐBB mint UseAuthentication
app.UseForwardedHeaders();
app.UseAuthentication();
app.UseAuthorization();
```

> **Middleware sorrend kritikus:** `UseForwardedHeaders()` az authentication és rate limiting előtt kell, hogy a `RemoteIpAddress` már a valódi client IP-t tartalmazza mire az RL policy lefut.

**RL key frissítés (most helyes):**

```csharp
// Middleware: partition key — valódi client IP az X-Forwarded-For után
app.Use(async (ctx, next) =>
{
    var userId   = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
    // ctx.Connection.RemoteIpAddress most már a valódi IP (UseForwardedHeaders után)
    var clientIp = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    var rlKey    = Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes($"{userId}:{clientIp}")));

    ctx.Items["RateLimitKey"] = rlKey;
    await next(ctx).ConfigureAwait(false);
});
```

---

## 6. Track C — Threat Model (v4)

### T-08 · STRIDE — v4 backend findings integrálva

**Frissített STRIDE map:**

| Komponens | S | T | R | I | D | E |
|---|---|---|---|---|---|---|
| Nginx | ✅ HSTS | ✅ TLS 1.3 | ✅ logs | ⚠️ brand spoof | ✅ n/a | ✅ CSP |
| Orchestrator | ✅ JWT fwd | ✅ prompt inject guard | ✅ audit | ✅ tool result wrap | ✅ SSE RL + abort | ✅ 401 wrap + error map |
| Kernel API | ✅ JWT ES256 | ✅ HMAC chain | ✅ ToolQueryExecuted | ✅ ExternalToken KV | ✅ PG guard | ✅ 64KB limit + ForwardedHeaders |
| PostgreSQL | ✅ RLS (all tables) | ⚠️ backup enc | ✅ pg_log | ✅ role sep | ⚠️ no HA | ✅ |
| Redis | ✅ AUTH + bind 127 | ✅ loopback-only | ✅ volatile ephemeral | ✅ IP-kötött key | ✅ in-memory (single instance) | ✅ |

Output: `ADR-006 — Threat Model v1.0` + `ADR-007 — RL Backing Store` + `docs/security/THREAT_MODEL.md`

---

## 7. Migration sorrend — v4 (változatlan v3-ból)

| # | Migration | Context | suppressTransaction |
|---|---|---|---|
| 0014 | `ExternalAuthToken_To_KeyVaultRef` — rename + partial index | `AppDbContext` | ✅ |
| 0015 | `QueryEndpointIndexes` — 4 composite/partial index | `AppDbContext` | ✅ |

**Futtatási sorrend:**

```bash
# 1. Fázis 1a — DB readonly + KV write
dotnet run --project scripts/MigrateExternalAuthTokens/Phase1a \
  -- --db-readonly "$DB_READONLY_CONNECTION" --kv-write "$KV_WRITE_CREDENTIAL"

# 2. Fázis 1b — DB write
dotnet run --project scripts/MigrateExternalAuthTokens/Phase1b \
  -- --db-write "$DB_WRITE_CONNECTION"

# 3. EF migrations
dotnet ef database update 0014_ExternalAuthToken_To_KeyVaultRef --context AppDbContext
dotnet ef database update 0015_QueryEndpointIndexes --context AppDbContext

# 4. RLS init
psql -U postgres -d spaceos -f scripts/db/init-query-rls.sql

# 5. Redis hardening
sudo cp config/redis-spaceos.conf /etc/redis/redis.conf
sudo systemctl restart redis-server
redis-cli -a "$REDIS_PASSWORD" ping   # → PONG
```

---

## 8. Implementációs sorrend — v4

```
Nap 1:     T-07 Redis (singleton multiplexer + ForwardedHeaders + AUTH config)
Nap 2:     T-05 Fázis 1a+1b script (console project) + Migration 0014
Nap 3:     T-01 Kernel query endpoints + scalar subquery summary + LoggedFireAndForget
Nap 4:     T-01 RLS init + Migration 0015 + EXPLAIN ANALYZE gate
Nap 5:     T-06 IntentDataJson schema + Kestrel MaxRequestBodySize
Nap 6:     T-02 KernelClient full error map + prompt inject guard
Nap 7:     T-02 Tool Registry live wiring (4 tool)
Nap 8:     T-03 SSE streaming + AbortController + SseSerializer + Nginx config
Nap 9:     T-04 Portal Chat UX (ToolResultCard + streaming hook)
Nap 10:    T-08 Threat Model + ADR-006 + ADR-007
Nap 11–13: Tesztek (unit + integration + E2E)
Nap 14:    EXPLAIN ANALYZE mind a 4 query endpointon + security gate (redis-cli ping + grep ExternalAuthToken)
Nap 15:    DoD checklist final
Nap 16:    Buffer / hotfix
```

---

## 9. Definition of Done — v4

### T-01 — Kernel query endpoint-ok
- [ ] 4 endpoint: `AsNoTracking`, Ardalis.Spec, `Result<PagedList<T>>`, `ConfigureAwait(false)`
- [ ] Summary: **scalar subquery** (nem FULL OUTER JOIN, nem 3 COUNT)
- [ ] `ToolQueryExecuted` audit: **`LoggedFireAndForget` helper** — nem raw `_ = task`
- [ ] TenantId JWT claim-ből, nem header
- [ ] RLS: FlowEpics + WorkStations + Facilities `FORCE ROW LEVEL SECURITY`
- [ ] Migration 0015 `suppressTransaction: true` — `CONCURRENTLY` fut
- [ ] **`EXPLAIN ANALYZE`: Index Scan minden endpointon — Seq Scan nincs**
- [ ] Integration teszt: cross-tenant → 0 sor; audit event fire-and-forget fail → `Log.Error` (nem silent)

### T-02 — Orchestrator Tool Registry
- [ ] 4 új live tool: `KernelClient` wrapper, JWT forward
- [ ] `wrapToolResult()` + `sanitizeToolResultForLlm()` minden tool result-re
- [ ] **`KernelClientError` enum: AuthExpired / RateLimited / Unavailable / Timeout / BadRequest / Unknown**
- [ ] **Interpreter: `KernelClientError` → `buildToolErrorResult()`, nem throw**
- [ ] Unit teszt: 401 → `ERR_TOOL_AUTH_EXPIRED`; 429 → `ERR_TOOL_RATE_LIMITED`; 503 → `ERR_KERNEL_UNAVAILABLE`; timeout → `ERR_KERNEL_TIMEOUT`
- [ ] Unit teszt: prompt injection tartalmú tool result → `[REDACTED]`
- [ ] E2E: chat → tool call → live Kernel adat → LLM válasz

### T-03 — SSE streaming
- [ ] `Content-Type: text/event-stream`, `[DONE]` sentinel
- [ ] **`AbortController` + `req.on('close', abort)` — disconnect cleanup**
- [ ] **`AbortSignal` propagálva az Anthropic SDK-ba és `KernelClient.fetch()`-be**
- [ ] `SseSerializer.sanitize()` — `\r`, `\n` → escaped
- [ ] `sseChatRateLimit`: 10 req/perc/user → 429 + retryAfter
- [ ] **Nginx: `proxy_buffering off; proxy_read_timeout 300s;` SSE route-ra**
- [ ] Unit teszt: client disconnect szimulál → generator leáll, LLM hívás abortál

### T-04 — Portal Chat UX
- [ ] SSE streaming reader + `ToolResultCard.tsx` + `isToolResult()` type guard
- [ ] Portal unit teszt: streaming render + kártyamegjelenítés

### T-05 — ExternalAuthToken → KV ref
- [ ] **`scripts/MigrateExternalAuthTokens/` standalone console projekt — nem `dotnet script`**
- [ ] Phase 1a: `tokens.json` létrejön; Phase 1b: `tokens.json` törölve
- [ ] Migration 0014: `ExternalAuthToken` nem létezik; `ExternalAuthTokenRef` + partial index
- [ ] `IKeyVaultClient` + `LocalKeyVaultClient` (dev) + `AzureKeyVaultClient` (prod)
- [ ] Smoke test: meglévő federált SpaceLayer → 200 OK
- [ ] `grep -r "ExternalAuthToken[^R]" --include="*.cs"` → 0 találat
- [ ] `tokens.json` nem létezik post-deploy

### T-06 — IntentDataJson validáció
- [ ] Schema: `parameters` scalar-only, `maxProperties: 10`
- [ ] Kestrel `MaxRequestBodySize = 64 KB`
- [ ] Unit teszt: 65 KB → 413; nested object → 422; null → pass

### T-07 — Redis RL
- [ ] **`IConnectionMultiplexer` singleton — nincs `BuildServiceProvider()`**
- [ ] **`UseForwardedHeaders()` + `KnownProxies.Add(Loopback)` — pipeline elsőként**
- [ ] **`UseForwardedHeaders` MEGELŐZI `UseAuthentication`-t a pipeline-ban**
- [ ] RL key: `sha256(userId + clientIp)` — valódi IP (nem 127.0.0.1)
- [ ] `requirepass` + `bind 127.0.0.1` Redis config
- [ ] Redis hiány → `AddDistributedMemoryCache()` + `Log.Warning`
- [ ] `ValidateOnStart()` prod fail-fast
- [ ] **`ADR-007` dokumentálja az in-memory RL backing store korlátot**
- [ ] `redis-cli -a $REDIS_PASSWORD ping` → PONG (CI deploy gate)

### T-08 — Threat Model
- [ ] `THREAT_MODEL.md` + `ADR-006` + `ADR-007`
- [ ] STRIDE: 5 komponens (Nginx, Orchestrator, Kernel, PostgreSQL, Redis)
- [ ] Minden v4 finding (BE-P2-01..BE-P2-08) szerepel a mitigáció map-ben

### Összesített
- [ ] Meglévő **1049+ teszt** zöld
- [ ] Phase 2 új tesztek: **≥ 45 db**
- [ ] 0 build warning (xUnit1051 kivételével)
- [ ] `EXPLAIN ANALYZE`: Index Scan mind a 4 query endpointon
- [ ] `dotnet list package --vulnerable` → 0 high/critical
- [ ] `redis-cli -a $REDIS_PASSWORD ping` → PONG
- [ ] `grep -r "BuildServiceProvider" --include="*.cs"` → 0 találat
- [ ] E2E: chat → live Kernel adat → ToolResultCard a Portalon

---

## 10. Kockázatok és mitigációk — v4

| Kockázat | Valószínűség | Hatás | Mitigáció |
|---|---|---|---|
| `UseForwardedHeaders` middleware sorrend felcserélve | Közepes | Magas | DoD gate: unit teszt — `RemoteIpAddress != 127.0.0.1` authenticated request esetén |
| Scalar subquery SQL injection — `{0}` paraméter | Alacsony | Kritikus | EF Core `SqlQueryRaw` paraméteres — `{0}` → `$1` PostgreSQL prepared statement |
| `AbortController.abort()` race condition az SSE finally-ban | Alacsony | Alacsony | `finally { res.end() }` — idempotens, dupla hívás nincs hatással |
| `tokens.json` megmarad deploy hiba esetén | Közepes | Kritikus | Deployment runbook post-deploy gate: `test ! -f tokens.json` |
| `JsonSchema.Net` package approved list | Alacsony | Közepes | Egyeztetés implementáció előtt — egyetlen új dependency |
| In-memory RL nullázódik process restart-kor | Alacsony | Közepes | ADR-007 dokumentálja; single-instance VPS-en elfogadható |

---

## 11. Security adósság státusz — Phase 2 v4 után

| ID | Tétel | Phase 1.5 | Phase 2 v4 | Marad |
|---|---|---|---|---|
| P0-1..P0-4 | JWT ES256 + Sink + Race + RLS (Audit) | ✅ | — | — |
| P1-1 | TenantId JWT claim + Interceptor | ✅ | — | — |
| P1-2 | ExternalAuthToken → KV ref | ❌ | ✅ T-05 | — |
| P1-3 | AggregateSnapshot | ❌ | ❌ | Phase 3 |
| P1-4 | Outbox Pattern | ❌ | ❌ | Phase 3 |
| P1-5 | IntentDataJson schema + size limit | ❌ | ✅ T-06 | — |
| P1-6 | Identity RL (Redis + AUTH + ForwardedHeaders) | ❌ | ✅ T-07 | — |
| P1-7 | Threat Model STRIDE | ❌ | ✅ T-08 | — |
| P1-8 | ProofHash + WORM | ❌ | ❌ | Phase 3 |
| DB-02..DB-04 | Query index + RLS (query tables) | ❌ | ✅ T-01 | — |
| SEC-P2-01..10 | Redis AUTH + prompt inject + SSE guard + audit + limits | ❌ | ✅ | — |
| BE-P2-01..08 | Singleton RL; scalar SQL; unobserved Task; error map; SSE abort; dotnet script; RL backing; ForwardedHeaders | ❌ | ✅ | — |

> ✅ **Phase 2 v4 után: minden P0 + P1 finding lezárva (P1-3, P1-4, P1-8 kivételével).**  
> **Implementációra kész — nincs további tervezett review iteráció.**

---

## 12. Mi jön Phase 2 után

| Fázis | Tartalom | Blokkoló feltétel |
|---|---|---|
| **Phase 3A — Modules.Joinery MVP** | C# Driver: ajtóméretek, anyaglista, vágási terv. Doorstar Kft. pilot. | Phase 2 kész |
| **Phase 3B — P1 security zárás** | P1-3 AggregateSnapshot, P1-4 Outbox, P1-8 ProofHash + WORM | Phase 2 kész |
| **Phase 3C — Multi-brand Portal** | Turborepo, JoineryTech brand skin | Phase 2 kész |
| **Horizon 2 — Escrow GA** | Sink upgrade S3/Azure Immutable + RFC 3161 TSA | Phase 3B kész |

---

*SpaceOS · Sprint D · Phase 2 v4.0 · `/database-schema-designer` + `/senior-security` + `/senior-backend` reviewed · 2026-04-06*  
*Státusz: IMPLEMENTÁCIÓRA KÉSZ — végleges tervdokumentum*
