---
id: MSG-FREETIER-006
from: root
to: freetier
type: task
priority: high
status: READ
ref: MSG-FREETIER-005-DONE
created: 2026-04-23
---

# FREETIER-006 — Infrastructure implementációk + API endpoints (Nap 8.0–12.0)

> **Skill:** `/spaceos-terminal` szerint dolgozz — inbox → build → test → outbox DONE
> **Spec:** `docs/architecture/SpaceOS_FreeTier_Architecture_v4.md` Section 5, 7.3, 7.4
> **Blokkoló:** FREETIER-005 ✅ DONE
> **Használhatsz sub-agent-eket** ha szükséges (csharp-expert, devils-advocate)

---

## Nap 8.0 — Repository implementációk (EF Core)

**Fájlok:** `Infrastructure/Repositories/`

Az Application rétegben definiált 5 repository interfész EF Core implementációja:

```csharp
// Infrastructure/Repositories/EfFreeTierUserRepository.cs
public sealed class EfFreeTierUserRepository : IFreeTierUserRepository
{
    private readonly FreeTierDbContext _db;
    // GetByEmailHashAsync, GetByIdAsync, AddAsync
}

// Hasonlóan: EfWorkspaceRepository, EfMagicLinkTokenRepository,
// EfUpgradeRequestRepository, EfShareTokenRepository
```

**Fontos:**
- `AsTracking()` explicit használata mutációs query-knél (a DbContext default NoTracking)
- `Include()` a navigációs property-knél ahol kell (Workspace → Revisions, ShareTokens)
- `GetSharedWorkspace` → `ShareDbContext` (nem FreeTierDbContext!)

**Tesztek (+5):** Repository CRUD tesztek Testcontainers-sel (FreeTierTestBase)

---

## Nap 9.0 — Redis infrastruktúra

### RedisConnectionFactory (D-25)

```csharp
// Infrastructure/Redis/RedisConnectionFactory.cs
public sealed class RedisConnectionFactory : IAsyncDisposable
{
    // AbortOnConnectFail=false, ConnectRetry=3, ConnectTimeout=5000, SyncTimeout=1000
    // Polly 3x exponential retry: 200ms/400ms/800ms
    // IConnectionMultiplexer singleton via lazy init
}
```

### RedisRateLimitService (D-15, D-18)

```csharp
// Infrastructure/Redis/RedisRateLimitService.cs : IRateLimitService
// Key pattern: rl:{scope}:{fingerprint}:{unix_ts/300}  (5 perc bucket)
// TTL: scope-dependent (3600s anon, 86400s auth)
// Lua INCR+EXPIRE atomic script
// **FAIL-CLOSED**: Redis exception → RateLimitUnavailableException dob (D-18)
```

### RedisSessionStore (FT-2)

```csharp
// Infrastructure/Redis/RedisSessionStore.cs
// Key: sess:{SHA256(nonce)}
// TTL: 600s (10 min)
// JSONB tartalom: { fingerprint, created_at, nesting_count }
```

**Tesztek (+8):** Redis mock (IConnectionMultiplexer) + fail-closed viselkedés teszt

---

## Nap 10.0 — External service kliensek

### BrevoEmailClient

```csharp
// Infrastructure/External/BrevoEmailClient.cs : IBrevoEmailService
// HttpClient + IOptions<BrevoSettings>
// POST https://api.brevo.com/v3/smtp/email
// Template ID-k: appsettings.json-ból
// Rate limit backoff: Polly retry
```

### TurnstileHttpClient

```csharp
// Infrastructure/External/TurnstileHttpClient.cs : ITurnstileValidator
// POST https://challenges.cloudflare.com/turnstile/v0/siteverify
// { secret, response, remoteip }
// Timeout: 5s
```

### SlackWebhookClient

```csharp
// Infrastructure/External/SlackWebhookClient.cs
// Upgrade request notification → #spaceos-sales webhook
// Fire-and-forget (nem blokkolja a fő flow-t)
```

### MagicLinkService

```csharp
// Infrastructure/Services/MagicLinkService.cs : IMagicLinkService
// 32-byte CSPRNG (RandomNumberGenerator)
// SHA-256 hash
// Base64Url encode
```

**Tesztek (+5):** HttpMessageHandler mock, Turnstile valid/invalid, MagicLink hash verify

---

## Nap 11.0 — API Endpoints (Minimal API)

**Fájlok:** `Api/Endpoints/`

### AnonymousNestingEndpoints

```csharp
// POST /api/freetier/nest — anonymous nesting
// - SemaphoreSlim(10) compute guard (D-18)
// - Rate limit: 3/nap/IP (IRateLimitService)
// - Turnstile token header: X-Turnstile-Token
// - Return: NestingResult JSON
```

### AuthEndpoints

```csharp
// POST /api/freetier/auth/magic-link — request magic link
// - Rate limit: fail-closed
// - Turnstile required
// - Return: 202 Accepted (no token in response!)

// POST /api/freetier/auth/verify — verify magic link
// - Return: 200 { userId, sessionToken } + Set-Cookie: ft_sess (D-14-REV)
```

### WorkspaceEndpoints

```csharp
// GET  /api/freetier/workspaces          — list user workspaces
// GET  /api/freetier/workspaces/{id}     — get workspace
// POST /api/freetier/workspaces          — create/save workspace
// GET  /api/freetier/workspaces/{id}/revisions — revision history
```

### ShareEndpoints (public, no auth!)

```csharp
// POST /api/freetier/workspaces/{id}/share     — generate share token [auth required]
// DELETE /api/freetier/workspaces/{id}/share/{shareId} — revoke [auth required]
// GET  /api/freetier/share/{tokenPrefix}/{rawToken}    — view shared workspace [NO AUTH]
```

### UpgradeEndpoints

```csharp
// POST /api/freetier/upgrade — submit upgrade request [auth required]
```

### Middleware pipeline (Program.cs sorrend)

```csharp
// 1. TurnstileMiddleware (csak /nest és /auth/magic-link-re)
// 2. RateLimitMiddleware
// 3. UserSessionMiddleware (ft_sess cookie → app.user_id GUC)
// 4. Endpoints
```

**Tesztek (+12):** WebApplicationFactory endpoint tesztek:
- Anonymous nesting: 200 + SemaphoreSlim guard
- Magic link: 202 request, 200 verify, 401 second use
- Workspace CRUD: 201 create, 200 get, 404 not found
- Share: 200 public view, 401 expired
- Upgrade: 201 submit
- Rate limit: 429 Too Many Requests

---

## Nap 12.0 — DI registration + appsettings + összekötés

**Program.cs kiegészítések:**

```csharp
// DI:
builder.Services.AddScoped<IFreeTierUserRepository, EfFreeTierUserRepository>();
builder.Services.AddScoped<IWorkspaceRepository, EfWorkspaceRepository>();
// ... többi repository
builder.Services.AddSingleton<RedisConnectionFactory>();
builder.Services.AddScoped<IRateLimitService, RedisRateLimitService>();
builder.Services.AddHttpClient<IBrevoEmailService, BrevoEmailClient>();
builder.Services.AddHttpClient<ITurnstileValidator, TurnstileHttpClient>();
builder.Services.AddScoped<IMagicLinkService, MagicLinkService>();

// Endpoint mapping:
app.MapAnonymousNestingEndpoints();
app.MapAuthEndpoints();
app.MapWorkspaceEndpoints();
app.MapShareEndpoints();
app.MapUpgradeEndpoints();
```

**appsettings.json kiegészítések:**

```json
{
  "Redis": {
    "ConnectionString": "127.0.0.1:6379,password=...,abortConnect=false"
  },
  "Brevo": {
    "ApiKey": "FROM_ENV",
    "MagicLinkTemplateId": 1,
    "SenderEmail": "no-reply@joinerytech.hu"
  },
  "Turnstile": {
    "SecretKey": "FROM_ENV",
    "SiteKey": "FROM_ENV"
  },
  "FreeTier": {
    "MagicLinkBaseUrl": "https://eszkozok.joinerytech.hu/auth/verify",
    "MaxWorkspacesPerUser": 20,
    "AnonymousNestingRateLimit": 3,
    "SessionTimeoutSeconds": 600,
    "ComputeConcurrencyLimit": 10
  }
}
```

**Érzékeny adatok:** API kulcsokat NE az appsettings-be, hanem environment variable-ből (`/etc/spaceos/freetier.env`) — `builder.Configuration.AddEnvironmentVariables()` már be van kötve.

---

## Definition of Done

- [ ] 5 EF Core repository implementáció (IFreeTierUserRepository, IWorkspaceRepository, IMagicLinkTokenRepository, IUpgradeRequestRepository, IShareTokenRepository)
- [ ] RedisConnectionFactory (D-25: AbortOnConnectFail=false, Polly retry)
- [ ] RedisRateLimitService (D-15: 5 perc bucket, Lua atomic, D-18: fail-closed)
- [ ] RedisSessionStore (FT-2: 10 min TTL)
- [ ] BrevoEmailClient + TurnstileHttpClient + SlackWebhookClient + MagicLinkService
- [ ] 5 Minimal API endpoint csoport (AnonymousNesting, Auth, Workspace, Share, Upgrade)
- [ ] SemaphoreSlim(10) compute guard (D-18)
- [ ] Middleware pipeline: Turnstile → RateLimit → UserSession → Endpoints
- [ ] DI registration komplett (Program.cs)
- [ ] appsettings.json + env variable pattern (érzékeny adatok NEM appsettings-ben)
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` ≥ 134 pass (104 előző + min 30 új)
- [ ] Outbox DONE üzenet küldve
