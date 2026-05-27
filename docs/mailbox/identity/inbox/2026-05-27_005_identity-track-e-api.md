---
id: MSG-IDENTITY-005
from: root
to: identity
type: task
priority: high
status: READ
ref: MSG-IDENTITY-002-DONE,MSG-IDENTITY-003-DONE
created: 2026-05-27
skill: /spaceos-terminal
agents: enabled
subagents: enabled
---

# MSG-IDENTITY-005 — Track E: Identity.Api + Program.cs + deployment prep

## Kontextus

Track B (Application) + Track C (Infrastructure/Persistence) elfogadva.
Track D (Keycloak/Workers/Redis) elfogadva.
Ez az utolsó implementációs track — az API réteg és a DI wiring.

Spec: `/opt/spaceos/docs/tasks/active/IDENTITY-V1_modules-identity.md` — §4.1, §7 Security gates

## Feladat

### 1. NuGet csomagok — `Identity.Api`

```bash
cd Identity.Api
dotnet add package Keycloak.AuthServices.Authentication --version 2.8.0
dotnet add package MediatR --version 12.4.0
dotnet add package FluentValidation.AspNetCore --version 11.3.0
dotnet add package Ardalis.Result.AspNetCore --version 9.0.0
dotnet add package StackExchange.Redis --version 2.8.16
dotnet add package Serilog.AspNetCore --version 8.0.1
dotnet add package Microsoft.AspNetCore.RateLimiting --version 8.0.11
```

⚠️ Ha `Keycloak.AuthServices.Authentication 2.8.0` szintén net10.0-t igényel: használj `Microsoft.AspNetCore.Authentication.JwtBearer` + manuális JWKS konfigurációt (Keycloak realm: `spaceos`, JWKS URI: `http://localhost:8080/auth/realms/spaceos/protocol/openid-connect/certs`).

### 2. `Controllers/UsersController.cs`

```
GET  /identity/users             → ListTenantUsersQuery
GET  /identity/users/{id}        → GetUserByIdQuery
POST /identity/users             → CreateUserCommand → 201
PUT  /identity/users/{id}        → UpdateUserProfileCommand
POST /identity/users/{id}/disable  → DisableUserCommand
POST /identity/users/{id}/enable   → EnableUserCommand
POST /identity/users/{id}/reset-password  → ResetPasswordCommand (rate limit: 5/user/hour)
```

- Minden endpoint: `[Authorize(Policy = "TenantMember")]` vagy `"TenantAdmin"`
- `Result<T>` → HTTP status mapping: `Result.ToActionResult()` (Ardalis.Result.AspNetCore)
- `tid` claim → `ICurrentUserContext` — headerből/bodyból soha (SEC-09)

### 3. `Controllers/AdminController.cs`

```
POST /identity/admin/tenants/{tenantId}/sync-from-keycloak → SyncTenantUsersFromKeycloakCommand
```

- `[Authorize(Policy = "SuperAdmin")]`

### 4. `Program.cs`

```csharp
// JWT authentication
builder.Services.AddAuthentication(...)
    // Keycloak JWKS: http://localhost:8080/auth/realms/spaceos/protocol/openid-connect/certs
    // Issuer: http://localhost:8080/auth/realms/spaceos
    // Audience: identity-api

// Authorization policies
builder.Services.AddAuthorization(options => {
    options.AddPolicy("TenantMember", p => p.RequireClaim("tid").RequireAuthenticatedUser());
    options.AddPolicy("TenantAdmin",  p => p.RequireClaim("tid").RequireRole("TenantAdmin"));
    options.AddPolicy("SuperAdmin",   p => p.RequireRole("SuperAdmin"));
});

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateUserCommand).Assembly));

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(CreateUserCommandValidator).Assembly);

// EF Core
builder.Services.AddDbContext<IdentityDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("IdentityDb")));

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"]!));

// DI registrations
builder.Services.AddScoped<ISpaceOSUserRepository, SpaceOSUserRepository>();
builder.Services.AddScoped<IKcSyncOutboxRepository, KcSyncOutboxRepository>();
builder.Services.AddScoped<IIdentityProviderClient, KeycloakAdminClient>();
builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();
builder.Services.AddScoped<IRateLimitService, RedisRateLimitService>();
builder.Services.AddSingleton<IKeycloakTokenProvider, KeycloakTokenProvider>();
builder.Services.AddSingleton<UserCacheService>();
builder.Services.AddHostedService<KcSyncWorkerService>();

// Serilog: email_masked destructuring (SEC-08)
// Rate limiting: 5/user/hour reset-password (SEC-04)
// Health check: /health

// Port: 5008, loopback only (127.0.0.1)
```

### 5. `appsettings.json` + `appsettings.Production.json`

```json
{
  "ConnectionStrings": {
    "IdentityDb": "Host=127.0.0.1;Database=spaceos_identity;Username=identity_app;Password=..."
  },
  "Redis": { "ConnectionString": "127.0.0.1:6379" },
  "Keycloak": {
    "BaseUrl": "http://localhost:8080/auth",
    "Realm": "spaceos",
    "ClientId": "spaceos-identity-service",
    "ClientSecret": ""
  },
  "Jwt": {
    "Authority": "http://localhost:8080/auth/realms/spaceos",
    "Audience": "identity-api"
  }
}
```

`appsettings.Production.json`: üres override — minden érzékeny adat environment variable-ból jön.

### 6. `CurrentUser/CurrentUserContext.cs` (Infrastructure)

```csharp
public class CurrentUserContext(IHttpContextAccessor accessor) : ICurrentUserContext
{
    public Guid TenantId   => Guid.Parse(accessor.HttpContext!.User.FindFirstValue("tid")
                                ?? throw new UnauthorizedAccessException("tid claim missing"));
    public Guid UserId     => Guid.Parse(accessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public bool IsAdmin    => accessor.HttpContext!.User.IsInRole("TenantAdmin");
    public bool IsSuperAdmin => accessor.HttpContext!.User.IsInRole("SuperAdmin");
}
```

### 7. Tesztek — `Identity.Tests/Api/`

Minimum 5 API teszt (`WebApplicationFactory` + mock MediatR):
- `POST /identity/users` — 201 + Location header
- `POST /identity/users` — 409 duplikált email
- `GET /identity/users/{id}` — 403 idegen tenant
- `POST /identity/users/{id}/reset-password` — 429 rate limit
- `POST /identity/admin/.../sync-from-keycloak` — 403 ha nem SuperAdmin

## Definition of Done

Spec §7 Security gates:
- [ ] JWT validáció konfigurálva, `Keycloak.AuthServices.Authentication` vagy `JwtBearer`
- [ ] `tid` claim kizárólag JWT-ből — header/body elfogadás TILTVA
- [ ] `spaceos-identity-service` KC client placeholder konfigurálva
- [ ] `CLIENT_SECRET` nincs `appsettings.json`-ban — environment variable
- [ ] 5008 port `127.0.0.1` binding (loopback only)
- [ ] `/health` endpoint (no auth)
- [ ] `dotnet build` → 0 error, 0 warning
- [ ] `dotnet test` → minden teszt zöld (≥ 63 összesen, +5 API teszt)

## Megjegyzés

- **Production GA blocker:** P0-1 (JWT RS256) lezárása előtt deployment TILOS — ezt a kódban is jelöld kommenttel a JWT konfignál
- Deploy (`dotnet ef database update` + systemd + nginx) az INFRA terminál feladata a deploy fázisban
- `dotnet-ef 8.x` tool hiányzik a szerveren — INFRA feladata telepíteni deploy előtt
