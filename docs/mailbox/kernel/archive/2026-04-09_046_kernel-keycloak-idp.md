---
id: MSG-KC01
from: architect
to: kernel
type: task
priority: P0
date: 2026-04-09
sprint: "Keycloak IdP Integration — Kernel"
effort: "~4 nap (Nap 3-4 a végrehajtási sorrendben)"
---

# Keycloak IdP Integration — Kernel

## Kontextus

Ref: `/opt/spaceos/docs/SpaceOS_Keycloak_IdP_Architecture_v4.md`

A dev auth (saját JWT generálás, `/api/auth/token` + `/api/auth/refresh`) teljesen ki lesz váltva Keycloak OIDC Authorization Code + PKCE flow-val. A Kernel feladata: JWKS-alapú JWT validáció + TenantSessionInterceptor Keycloak claim struktúrára frissítése.

**Függőség:** Infra (Keycloak Docker + realm setup) — lásd alább. Infra párhuzamosan fut (Track A).

---

## INFRA PREREQ — Keycloak setup (Track A, Nap 1-2)

Ezeket az infra csapatnak kell elvégeznie, mielőtt a Kernel tesztelni tud:

### Docker Compose: `/opt/spaceos/keycloak/docker-compose.yml`

```yaml
version: '3.9'
services:
  keycloak:
    image: quay.io/keycloak/keycloak:24.0.5
    command: start --optimized
    environment:
      KC_DB: postgres
      KC_DB_URL: jdbc:postgresql://host.docker.internal:5433/spaceos_keycloak
      KC_DB_USERNAME: ${KC_DB_USERNAME}
      KC_DB_PASSWORD: ${KC_DB_PASSWORD}
      KC_HOSTNAME: ${KC_HOSTNAME}
      KC_HOSTNAME_STRICT: "false"
      KC_HTTP_ENABLED: "true"
      KC_HTTP_PORT: 8080
      KC_PROXY: edge
      KC_HEALTH_ENABLED: "true"
      KC_FEATURES: scripts                    # DB-01: JS protocol mapper
      KEYCLOAK_ADMIN: ${KC_ADMIN_USER}
      KEYCLOAK_ADMIN_PASSWORD: ${KC_ADMIN_PASSWORD}
    ports:
      - "127.0.0.1:8080:8080"
    restart: always
    extra_hosts:
      - "host.docker.internal:host-gateway"
```

### PostgreSQL DB setup (VPS, superuser-rel):

```sql
CREATE DATABASE spaceos_keycloak;
CREATE USER spaceos_keycloak_user WITH PASSWORD '${KC_DB_PASSWORD}';
GRANT ALL PRIVILEGES ON DATABASE spaceos_keycloak TO spaceos_keycloak_user;
-- DB-05: Izolálás
REVOKE ALL ON DATABASE spaceos FROM spaceos_keycloak_user;
REVOKE ALL ON SCHEMA public FROM spaceos_keycloak_user;
```

### Nginx kiegészítés:

```nginx
location /auth/admin/ {
    allow 127.0.0.1;
    deny all;
    proxy_pass http://127.0.0.1:8080/admin/;
    proxy_set_header Host $host;
    proxy_set_header X-Forwarded-Proto $scheme;
}

location /auth/ {
    proxy_pass http://127.0.0.1:8080/;
    proxy_set_header Host $host;
    proxy_buffer_size 128k;
    proxy_buffers 4 256k;
}
# CSP connect-src update (SEC-03): add https://joinerytech.hu/auth/
```

### Dev Docker Compose: `spaceos-dev/docker-compose.yml`

```yaml
services:
  keycloak-dev:
    image: quay.io/keycloak/keycloak:24.0.5
    command: start-dev --import-realm
    environment:
      KEYCLOAK_ADMIN: admin
      KEYCLOAK_ADMIN_PASSWORD: admin
      KC_HTTP_PORT: 8080
      KC_FEATURES: scripts
    ports:
      - "8080:8080"
    volumes:
      - ./keycloak/realm-export.json:/opt/keycloak/data/import/realm-export.json
```

### Realm konfiguráció (Keycloak Admin UI / realm-export.json):

- Realm: `spaceos` (`registrationAllowed: false`, `revokeRefreshToken: true`, AT lifetime: 900s)
- Client `portal-app`: public, PKCE S256 required, redirects: `*/callback`
- Client `kernel-api`: bearer-only
- Client `test-runner`: confidential, Direct Access Grant (BE-04, csak E2E tesztekhez)
- Client Scope `spaceos-claims` → Script Mapper `spaceos-tenants-mapper`:

```javascript
var groups = user.getGroupsStream().toArray();
var tenants = [];
for (var i = 0; i < groups.length; i++) {
    var g = groups[i];
    var attrs = g.getAttributes();
    var tid = attrs.getFirst("tenant_id");
    if (tid !== null && tid.length() === 36) {
        tenants.push({
            tenant_id: tid,
            tenant_type: attrs.getFirst("tenant_type") || "Unknown",
            enabled_modules: JSON.parse(attrs.getFirst("enabled_modules") || "[]"),
            brand_skin: attrs.getFirst("brand_skin") || "joinerytech"
        });
    }
}
exports = Java.to(JSON.stringify(tenants), "java.lang.String");
```

- Group `doorstar-kft`: attrs: `tenant_id=a1b2c3d4-e5f6-7890-abcd-ef1234567890`, `tenant_type=Producer`, `enabled_modules=["door"]`, `brand_skin=doorstar`
- Users: `admin@doorstar.hu` (Admin role), `designer@doorstar.hu` (Designer role), `test-admin@spaceos.local` (test-runner only)
- `realm-export.json` verziózva `spaceos-docs` repo-ban (no secrets)

---

## T1 — JWT Authority-alapú JWKS validáció (Nap 3)

### Fájl: `SpaceOS.Kernel.Api/Program.cs` — AddJwtBearer csere

```csharp
// REMOVE: static ECDsa key loading
// ADD:
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.Authority = config["Jwt:Authority"];
        options.Audience = config["Jwt:Audience"];
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "preferred_username",
            RoleClaimType = ClaimTypes.Role
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var realmAccess = context.Principal?.FindFirst("realm_access")?.Value;
                if (realmAccess is not null)
                {
                    var parsed = JsonDocument.Parse(realmAccess);
                    if (parsed.RootElement.TryGetProperty("roles", out var roles))
                    {
                        var identity = context.Principal!.Identity as ClaimsIdentity;
                        foreach (var role in roles.EnumerateArray())
                            identity!.AddClaim(new Claim(ClaimTypes.Role, role.GetString()!));
                    }
                }
                return Task.CompletedTask;
            }
        };
    });
```

### `appsettings.json`:
```json
{ "Jwt": { "Authority": "https://joinerytech.hu/auth/realms/spaceos", "Audience": "kernel-api" } }
```

### `appsettings.Development.json`:
```json
{ "Jwt": { "Authority": "http://localhost:8080/realms/spaceos", "Audience": "kernel-api" } }
```

---

## T2 — TenantSessionInterceptor Keycloak claim support (Nap 3)

### Fájl: `SpaceOS.Kernel.Application/DTOs/TenantClaimDto.cs` — ÚJ

```csharp
namespace SpaceOS.Kernel.Application.DTOs;

public sealed record TenantClaimDto
{
    [JsonPropertyName("tenant_id")]
    public required string TenantId { get; init; }

    [JsonPropertyName("tenant_type")]
    public required string TenantType { get; init; }

    [JsonPropertyName("enabled_modules")]
    public required string[] EnabledModules { get; init; }

    [JsonPropertyName("brand_skin")]
    public required string BrandSkin { get; init; }
}
```

### Fájl: `SpaceOS.Infrastructure/Interceptors/TenantSessionInterceptor.cs` — diff

Cseréld le a `tenantId` kinyerési logikát:

```csharp
string? tenantId = null;

var tenantsClaim = httpContext.User.FindFirst("spaceos_tenants")?.Value;
if (tenantsClaim is not null)
{
    List<TenantClaimDto>? tenants;
    try
    {
        // BE-01: double-deserialization — Keycloak Script Mapper JSON.stringify() wraps the array
        var json = tenantsClaim.StartsWith('[')
            ? tenantsClaim
            : JsonSerializer.Deserialize<string>(tenantsClaim) ?? tenantsClaim;
        tenants = JsonSerializer.Deserialize<List<TenantClaimDto>>(json, _jsonOptions);
    }
    catch (JsonException ex)
    {
        _logger.LogWarning(ex, "Failed to deserialize spaceos_tenants claim for user {Sub}",
            httpContext.User.FindFirst("sub")?.Value);
        tenants = null;
    }

    if (tenants is { Count: > 0 })
    {
        var activeTenantHeader = httpContext.Request.Headers["X-SpaceOS-Active-Tenant"]
            .FirstOrDefault();

        if (activeTenantHeader is not null)
        {
            // DB-02: header ∈ JWT tenants[] — nem esik vissza silently
            var match = tenants.FirstOrDefault(t =>
                string.Equals(t.TenantId, activeTenantHeader, StringComparison.OrdinalIgnoreCase));
            if (match is null)
            {
                _logger.LogWarning(
                    "Active tenant header {Header} not in user's tenant list. Sub={Sub}",
                    activeTenantHeader, httpContext.User.FindFirst("sub")?.Value);
                throw new UnauthorizedAccessException(
                    "Active tenant not in user's authorized tenant list");
            }
            tenantId = match.TenantId;
        }
        else
        {
            tenantId = tenants[0].TenantId;  // soft launch: single-tenant
        }
    }
}

// Backward compat — REMOVE after full migration
tenantId ??= httpContext.User.FindFirst("tenant_id")?.Value;
```

---

## T3 — JwksHealthCheck (Nap 4)

### Fájl: `SpaceOS.Infrastructure/Health/JwksHealthCheck.cs` — ÚJ

```csharp
public sealed class JwksHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;

    public JwksHealthCheck(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default)
    {
        var authority = _config["Jwt:Authority"];
        var jwksUri = $"{authority}/protocol/openid-connect/certs";
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(2);
            var response = await client.GetAsync(jwksUri, ct).ConfigureAwait(false);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("JWKS endpoint reachable")
                : HealthCheckResult.Degraded($"JWKS returned {response.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("JWKS endpoint unreachable", ex);
        }
    }
}
```

### Program.cs regisztráció:
```csharp
builder.Services.AddHealthChecks()
    .AddCheck<JwksHealthCheck>("jwks", tags: new[] { "ready" });
```

---

## T4 — Régi auth kód eltávolítása (Nap 4)

Törlendő fájlok / kód:

| Fájl | Akció |
|------|-------|
| `TokenService.cs` / `ITokenService` | Törlés |
| `LoginCommand` + `LoginCommandHandler` + Validator | Törlés |
| `RefreshTokenCommand` + `Handler` + Validator | Törlés |
| `/api/auth/token` endpoint mapping | Eltávolítás |
| `/api/auth/refresh` endpoint mapping | Eltávolítás |
| JWT key generation / `jwtKeys/` directory | Eltávolítás |
| `JwtSettings.cs` (ha tartalmaz `Key` property-t) | Refaktor → csak `Authority` + `Audience` |

**Ellenőrzés:** `grep -r "auth/token\|generateToken\|loginCommand\|LoginCommand" --include="*.cs"` → 0 találat

---

## T5 — Tesztek (Nap 4)

Meglévő auth tesztek mock-jai frissítendők: `ClaimsIdentity` Keycloak claim struktúrával.

Helper factory:
```csharp
// Tests/Helpers/ClaimsPrincipalFactory.cs
public static ClaimsPrincipal CreateKeycloakPrincipal(
    string tenantId = "test-tenant-id",
    string tenantType = "Producer",
    string[] roles = null)
{
    var tenants = JsonSerializer.Serialize(new[] {
        new { tenant_id = tenantId, tenant_type = tenantType,
              enabled_modules = new[] { "door" }, brand_skin = "doorstar" }
    });
    var claims = new List<Claim>
    {
        new("sub", Guid.NewGuid().ToString()),
        new("spaceos_tenants", tenants),
        new("preferred_username", "test.user"),
    };
    if (roles != null)
        foreach (var r in roles) claims.Add(new(ClaimTypes.Role, r));
    return new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
}
```

**≥18 új teszt** (részletek az arch doc Section 9.1-ben):

- `TenantSessionInterceptor_Keycloak_Claims` × 4
- `TenantSessionInterceptor_ActiveTenant_Validation` × 3
- `TenantSessionInterceptor_MalformedClaims` × 3
- `JwtBearer_Authority_Config` × 2
- `Auth_Endpoints_Removed` × 2
- `JwksHealthCheck` × 2
- `RealmRoles_Mapping` × 2

---

## DoD gates

```bash
cd /opt/spaceos/spaceos-kernel
dotnet build 2>&1 | tail -3         # 0 error, 0 warning
dotnet test --no-build 2>&1 | tail -5  # ≥915 meglévő + ≥18 új = ≥933 pass, 0 fail
dotnet list package --vulnerable 2>&1 | grep -E "High|Critical"  # 0
grep -r "auth/token\|generateToken\|LoginCommand" --include="*.cs" src/  # 0 találat
```

---

## Output

Ha kész: `mailbox/outbox/2026-04-09_046_kernel-keycloak-idp-done.md`

Tartalom:
- Teszt összesítő (meglévő + új)
- `curl /health/ready` kimenet (JWKS healthy)
- Törölt fájlok listája
- Esetleges eltérések az arch doc-tól
