# SpaceOS — Keycloak IdP Integration Architecture
## Dev Auth kiváltása OIDC-alapú Identity Provider-rel

> **Verzió:** v4.0 — 2026-04-09
> **Státusz:** IMPLEMENTÁCIÓRA KÉSZ
> **Blokkoló feltétel:** Phase 3C+ DoD (B2BHandshake + Module System)
> **Kumulált review:** `/database-designer` + `/database-schema-designer` → v2 · `/senior-security` → v3 · `/senior-backend` → v4
> **Érintett repók:** `spaceos-kernel` · `spaceos-orchestrator` · `spaceos-design-portal` · VPS infra
> **Becsült effort:** ~12 fejlesztői nap (3 repo + infra)
> **ADR hivatkozások:** ADR-011 (One Realm, Group-per-Tenant)

---

## 0. Architekturális döntések (lezárva)

| # | Kérdés | Döntés | Indoklás |
|---|--------|--------|----------|
| D-01 | Tenant↔Realm mapping | **1 realm (`spaceos`), tenant = Keycloak Group** | B2B Relativity: 1 user → N tenant group. RLS már biztosítja a tenant izolációt. ADR-011. |
| D-02 | Portal auth flow | **Authorization Code + PKCE** (Keycloak hosted login page) | Standard OIDC; social login ingyen; jelszó soha nem megy Portal/Orchestrator-on át |
| D-03 | Orchestrator szerepe | **Passthrough** — Portal JWT megy a Kernel-nek, Orchestrator csak forward | Kevesebb attack surface; Kernel közvetlenül validálja a Keycloak JWT-t |
| D-04 | Dev auth fallback | **Keycloak-only** — dev-ben is Keycloak Docker fut | Egy auth path; nincs code path divergencia |
| D-05 | User provisioning | **Admin-only** (invite) — soft launch-ra | Self-registration later; admin a Keycloak admin UI-ban adja hozzá |

---

## 1. Kumulált Finding Összesítő (v1 → v4)

| Review | Finding-ek | Legfontosabb javítás | Effort delta |
|--------|-----------|----------------------|--------------|
| v1 → `/database-designer` + `/database-schema-designer` → v2 | 0 CRITICAL · 2 HIGH · 3 MEDIUM | Script Mapper feature flag · Active-tenant header validáció | +0.5 nap |
| v2 → `/senior-security` → v3 | 2 CRITICAL · 2 HIGH · 2 MEDIUM | PKCE `state`+`nonce` parameter · CSP connect-src update | +1 nap |
| v3 → `/senior-backend` → v4 | 0 CRITICAL · 3 HIGH · 2 MEDIUM | Double-deser TenantClaimDto · JWKS retry-on-miss · E2E auth migration plan | +0.5 nap |
| **Összesen** | **2 CRITICAL · 7 HIGH · 7 MEDIUM** | | **~12 fejlesztői nap** |

### Finding részletek

| ID | Súly | Terület | Probléma | v_ javítás |
|----|------|---------|----------|------------|
| DB-01 | 🟠 HIGH | Keycloak Script Mapper | Keycloak 24 alapértelmezetten **letiltja** a JavaScript Protocol Mapper-t (`scripts` provider). Startup error. | v2: `KC_FEATURES=scripts` env var Docker-ben; dokumentálva hogy Keycloak 25+ esetén SPI plugin-ra kell váltani |
| DB-02 | 🟠 HIGH | Active tenant header | `X-SpaceOS-Active-Tenant` header-t a Portal küldi — nincs server-side validáció hogy a JWT `spaceos_tenants` listájának eleme. Attacker bármilyen tenant_id-t küldhet. | v2: `TenantSessionInterceptor` validáció: `activeTenantHeader ∈ tenants[].tenant_id`, ha nem → `Result.Forbidden()` + audit log |
| DB-03 | 🟡 MEDIUM | realm-export.json | Export tartalmazhat client secret-eket | v2: Csak public client → nincs secret; export script sanitize lépéssel; `.gitignore` entry a raw export-ra |
| DB-04 | 🟡 MEDIUM | Dev Keycloak H2 | `start-dev` H2-t használ → restart = adatvesztés | v2: Minden test user/group a `realm-export.json`-ban; nincs kézi admin munka dev-ben |
| DB-05 | 🟡 MEDIUM | Keycloak DB izolálás | `spaceos_keycloak_user` GRANT ALL de nincs explicit REVOKE a `spaceos` (fő app) DB-re | v2: `REVOKE ALL ON DATABASE spaceos FROM spaceos_keycloak_user;` a setup script-ben |
| SEC-01 | 🔴 CRITICAL | PKCE `state` param | v1 `redirectToLogin()` nem tartalmaz `state` parametert → CSRF a callback endpoint-on: attacker crafts `/callback?code=attacker_code` → victim session fixation | v3: `state` parameter generálás (crypto random) → sessionStorage; callback-ban validáció, mismatch → reject |
| SEC-02 | 🔴 CRITICAL | OIDC `nonce` | OpenID Connect megköveteli a `nonce` paramétert az auth request-ben; enélkül `id_token` replay attack lehetséges | v3: `nonce` generálás → sessionStorage; `id_token` validálásnál `nonce` claim ellenőrzés |
| SEC-03 | 🟠 HIGH | CSP `connect-src` | Portal CSP jelenleg `'self'` + BFF origin. A PKCE token exchange `fetch()` közvetlenül a Keycloak-hoz megy → CSP blokkolja. | v3: Nginx CSP header: `connect-src 'self' https://joinerytech.hu/auth/` hozzáadása; dev-ben `http://localhost:8080` |
| SEC-04 | 🟠 HIGH | Keycloak version pin | `quay.io/keycloak/keycloak:24.0` — minor/patch frissítés auto-pull → breaking change kockázat redeploy-kor | v3: Pin to `24.0.5` (latest patch); `Dockerfile` pinned hash-sel ideális, de image tag elegendő soft launch-ra |
| SEC-05 | 🟡 MEDIUM | Backchannel logout | Csak front-channel logout van. Keycloak admin session revoke → Portal nem tudja 15 percig (access token expiry). | v3: Dokumentált limitation; Phase 2 IdP: backchannel logout endpoint (`POST /bff/api/auth/backchannel-logout`) |
| SEC-06 | 🟡 MEDIUM | Keycloak Docker network | `host.docker.internal` Docker-ben → ha más container is fut, eléri a Keycloak :8080-at | v3: Docker network isolation: `internal: true` network + csak Nginx proxy fér hozzá; soft launch-ra `127.0.0.1` binding elegendő |
| BE-01 | 🟠 HIGH | TenantClaimDto deserialization | Keycloak Script Mapper `exports = JSON.stringify(...)` → JWT claim string típusú → `"[{...}]"` (dupla-szerializált). `JsonSerializer.Deserialize<List<TenantClaimDto>>()` elsőre string-et kap, nem tömböt. | v4: Kétlépcsős deserializáció: `var json = JsonSerializer.Deserialize<string>(tenantsClaim); var tenants = JsonSerializer.Deserialize<List<TenantClaimDto>>(json);` |
| BE-02 | 🟠 HIGH | JWKS health check | Keycloak = kritikus dependency. Ha JWKS endpoint nem elérhető → új login lehetetlen; meglévő token-ek cache-ből még validálhatók. Nincs health check. | v4: Kernel `IHealthCheck` implementáció: `JwksHealthCheck` — HTTP GET a JWKS URI-ra, 2s timeout; degraded ha fail (nem unhealthy, mert cache él) |
| BE-03 | 🟠 HIGH | JWKS cache miss on key rotation | `jwks-rsa` 10 min cache → key rotation után max 10 percig új token-ek rejected. | v4: `jwksRequestsPerMinute: 10` + Orchestrator retry logic: verify fail → 1× uncached JWKS fetch → retry verify; ha az is fail → 401 |
| BE-04 | 🟡 MEDIUM | E2E auth migration | Régi E2E tesztek `POST /bff/api/auth/token`-t használnak → az endpoint törlésével elszállnak | v4: E2E tesztek átírása: Keycloak Admin API `direct access grant` (Resource Owner Password) test user-rel → token; vagy test-specifikus Keycloak client `test-runner` |
| BE-05 | 🟡 MEDIUM | Portal token exchange error | `exchangeCode()` fetch() hiba → generic error, nincs retry | v4: 1× retry 1s delay-el; specifikus error page ha Keycloak nem elérhető ("Kérjük próbáld újra") |

---

## 2. Auth Flow — Teljes Sequence

### 2.1 Login (Authorization Code + PKCE + state + nonce)

```
┌──────────┐     ┌──────────┐     ┌──────────┐     ┌──────────┐
│  Portal   │     │  Keycloak │     │Orchestratr│     │  Kernel  │
│  (React)  │     │  :8080    │     │  :3000    │     │  :5001   │
└────┬──────┘     └────┬──────┘     └────┬──────┘     └────┬─────┘
     │                  │                 │                  │
     │ 1. Generate:     │                 │                  │
     │    code_verifier │                 │                  │
     │    code_challenge│                 │                  │
     │    state (random)│                 │                  │
     │    nonce (random)│                 │                  │
     │    → store in    │                 │                  │
     │      sessionStorage                │                  │
     │                  │                 │                  │
     │ 2. Redirect to   │                 │                  │
     │    /realms/spaceos/protocol/       │                  │
     │    openid-connect/auth             │                  │
     │    ?client_id=portal-app           │                  │
     │    &response_type=code             │                  │
     │    &scope=openid profile email     │                  │
     │    &code_challenge={S256}          │                  │
     │    &code_challenge_method=S256     │                  │
     │    &redirect_uri=/callback         │                  │
     │    &state={state}    (SEC-01)      │                  │
     │    &nonce={nonce}    (SEC-02)      │                  │
     │──────────────────>│                 │                  │
     │                  │                 │                  │
     │ 3. User authenticates              │                  │
     │    (form / Google / Microsoft)     │                  │
     │                  │                 │                  │
     │ 4. Redirect back │                 │                  │
     │    /callback?code=abc&state={state}│                  │
     │<──────────────────│                 │                  │
     │                  │                 │                  │
     │ 5. Validate state│                 │                  │
     │    (sessionStorage == URL param)   │                  │
     │    If mismatch → reject (SEC-01)   │                  │
     │                  │                 │                  │
     │ 6. POST /realms/spaceos/protocol/  │                  │
     │    openid-connect/token            │                  │
     │    {code, code_verifier, client_id}│                  │
     │──────────────────>│                 │                  │
     │                  │                 │                  │
     │ 7. {access_token, refresh_token,   │                  │
     │     id_token (contains nonce)}     │                  │
     │<──────────────────│                 │                  │
     │                  │                 │                  │
     │ 8. Validate id_token nonce (SEC-02)│                  │
     │    Decode id_token → check nonce   │                  │
     │    claim == sessionStorage nonce   │                  │
     │                  │                 │                  │
     │ 9. Store tokens  │                 │                  │
     │    (memory-only) │                 │                  │
     │    Call /bff/api/auth/me           │                  │
     │─────────────────────────────────────>│                 │
     │                  │                 │ Parse JWT claims │
     │<─────────────────────────────────────│                 │
     │                  │                 │                  │
     │ 10. API request  │                 │                  │
     │     Authorization: Bearer {AT}     │                  │
     │     X-SpaceOS-Active-Tenant: {tid} │                  │
     │─────────────────────────────────────>│                 │
     │                  │                 │ 11. Forward      │
     │                  │                 │     Bearer + hdr │
     │                  │                 │────────────────>│
     │                  │                 │                  │
     │                  │                 │  12. Kernel:     │
     │                  │                 │  - JWKS validate │
     │                  │                 │  - Parse tenants │
     │                  │                 │  - Validate      │
     │                  │                 │    active_tenant │
     │                  │                 │    ∈ tenants[]   │
     │                  │                 │    (DB-02)       │
     │                  │                 │  - RLS ctx set   │
     │                  │                 │<────────────────│
     │<─────────────────────────────────────│                 │
```

### 2.2 Token Refresh

```
Portal                    Keycloak
  │                          │
  │ access_token expired     │
  │ (401 from Orchestrator)  │
  │                          │
  │ POST /realms/spaceos/protocol/openid-connect/token
  │ {grant_type: refresh_token, refresh_token, client_id: portal-app}
  │─────────────────────────>│
  │                          │
  │ {new access_token,       │
  │  new refresh_token}      │ (revokeRefreshToken: true → single-use)
  │<─────────────────────────│
  │                          │
  │ If refresh fails (403)   │
  │ → redirectToLogin()      │
```

A refresh teljes egészében Portal↔Keycloak között történik. Az Orchestrator és Kernel nem érintett.

### 2.3 Logout

```
Portal                    Keycloak
  │                          │
  │ Clear memory tokens      │
  │                          │
  │ Redirect to              │
  │ /realms/spaceos/protocol/│
  │ openid-connect/logout    │
  │ ?post_logout_redirect_uri│
  │  =https://joinerytech.hu │
  │ &id_token_hint=...       │
  │─────────────────────────>│
  │                          │
  │ Session invalidated      │
  │ Redirect to /            │
  │<─────────────────────────│
```

**Limitation (SEC-05):** Front-channel only. Keycloak admin revoke → Portal nem tudja az access token expiry-ig (15 perc). Backchannel logout = Phase 2 IdP.

---

## 3. JWT Claim Mapping

### 3.1 Keycloak JWT payload (target state)

```json
{
  "iss": "https://joinerytech.hu/auth/realms/spaceos",
  "sub": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "aud": ["kernel-api", "portal-app"],
  "exp": 1712700000,
  "iat": 1712699100,
  "azp": "portal-app",
  "nonce": "abc123random",
  "scope": "openid profile email",
  "email": "kovacs.janos@doorstar.hu",
  "preferred_username": "kovacs.janos",
  "given_name": "János",
  "family_name": "Kovács",
  "spaceos_tenants": "[{\"tenant_id\":\"aaa-bbb-ccc\",\"tenant_type\":\"Producer\",\"enabled_modules\":[\"door\"],\"brand_skin\":\"doorstar\"}]",
  "realm_access": {
    "roles": ["Admin", "default-roles-spaceos"]
  }
}
```

**Fontos (BE-01):** A `spaceos_tenants` claim **string típusú** (nem JSON tömb) — a Keycloak Script Mapper `JSON.stringify()`-t hív, a JWT ezt string claim-ként tárolja. A deserializáció kétlépcsős.

### 3.2 Keycloak Claim Mapper konfiguráció

| Mapper neve | Mapper típus | Source | Token target | Megjegyzés |
|-------------|-------------|--------|-------------|------------|
| `spaceos-tenants` | Script Mapper (JavaScript) | User groups + attributes | access_token + id_token | `KC_FEATURES=scripts` szükséges (DB-01) |
| `realm-roles` | Realm Role → Token Claim | Realm roles | access_token | Default mapper, de token claim name = `realm_access.roles` |
| `audience-kernel` | Audience Resolve | `kernel-api` client | access_token | Hogy a Kernel `ValidateAudience` ne dobja el |

### 3.3 Script Mapper: `spaceos-tenants`

```javascript
// Keycloak Admin → Realm → Client Scopes → spaceos-claims → Mappers
// Név: spaceos-tenants-mapper
// Token Claim Name: spaceos_tenants
// Claim JSON Type: String
// Multivalued: OFF
// Add to ID Token: ON
// Add to Access Token: ON

// FONTOS: KC_FEATURES=scripts kell a docker-compose-ban (DB-01)

var groups = user.getGroupsStream().toArray();
var tenants = [];
for (var i = 0; i < groups.length; i++) {
    var g = groups[i];
    var attrs = g.getAttributes();
    var tid = attrs.getFirst("tenant_id");
    if (tid !== null && tid.length() === 36) {  // UUID format guard
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

### 3.4 Multi-tenant user (Phase 2 IdP — nem soft launch scope)

Soft launch: Doorstar user = 1 group → `spaceos_tenants[0]` az aktív.
Phase 2: Tenant switcher UI → `X-SpaceOS-Active-Tenant` header → TenantSessionInterceptor validálja (DB-02).

---

## 4. Kernel Changes

### 4.1 JWT validáció átállás

```csharp
// SpaceOS.Kernel.Api/Program.cs — diff

// REMOVE:
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new() {
            ValidIssuer = config["Jwt:Issuer"],
            ValidAudience = config["Jwt:Audience"],
            IssuerSigningKey = new ECDsaSecurityKey(ecdsa) // ES256
        };
    });

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
        // Keycloak realm_access.roles → flat ClaimsIdentity roles
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
                        {
                            identity!.AddClaim(new Claim(ClaimTypes.Role, role.GetString()!));
                        }
                    }
                }
                return Task.CompletedTask;
            }
        };
    });
```

```json
// appsettings.json
{
  "Jwt": {
    "Authority": "https://joinerytech.hu/auth/realms/spaceos",
    "Audience": "kernel-api"
  }
}

// appsettings.Development.json
{
  "Jwt": {
    "Authority": "http://localhost:8080/realms/spaceos",
    "Audience": "kernel-api"
  }
}
```

### 4.2 TenantSessionInterceptor — Claim extraction update (DB-02 + BE-01)

```csharp
// SpaceOS.Infrastructure/Interceptors/TenantSessionInterceptor.cs — diff

// ELŐTTE: custom claim name
var tenantId = httpContext.User.FindFirst("TenantId")?.Value;

// UTÁNA: Keycloak claim structure with double-deserialization (BE-01) + validation (DB-02)
string? tenantId = null;

var tenantsClaim = httpContext.User.FindFirst("spaceos_tenants")?.Value;
if (tenantsClaim is not null)
{
    // BE-01: Script Mapper double-serialization — claim value is a JSON string containing JSON
    List<TenantClaimDto>? tenants;
    try
    {
        // First: the claim value itself might be a JSON-encoded string
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
        // DB-02: Validate X-SpaceOS-Active-Tenant header against JWT claim list
        var activeTenantHeader = httpContext.Request.Headers["X-SpaceOS-Active-Tenant"]
            .FirstOrDefault();

        if (activeTenantHeader is not null)
        {
            var match = tenants.FirstOrDefault(t =>
                string.Equals(t.TenantId, activeTenantHeader, StringComparison.OrdinalIgnoreCase));
            if (match is null)
            {
                _logger.LogWarning(
                    "Active tenant header {Header} not in user's tenant list. Sub={Sub}",
                    activeTenantHeader, httpContext.User.FindFirst("sub")?.Value);
                // DB-02: Reject — do not silently fall back to first tenant
                throw new UnauthorizedAccessException(
                    "Active tenant not in user's authorized tenant list");
            }
            tenantId = match.TenantId;
        }
        else
        {
            // No header → default to first tenant (soft launch: single-tenant user)
            tenantId = tenants[0].TenantId;
        }
    }
}

// Fallback: direct claim (backward compat — REMOVE after full migration)
tenantId ??= httpContext.User.FindFirst("tenant_id")?.Value;

if (tenantId is null)
{
    _logger.LogWarning("No tenant_id resolved for user {Sub}",
        httpContext.User.FindFirst("sub")?.Value);
}
```

```csharp
// SpaceOS.Kernel.Application/DTOs/TenantClaimDto.cs — ÚJ
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

### 4.3 JWKS Health Check (BE-02)

```csharp
// SpaceOS.Infrastructure/Health/JwksHealthCheck.cs — ÚJ
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
            // Degraded, not Unhealthy — cached keys still work for existing tokens
            return HealthCheckResult.Degraded("JWKS endpoint unreachable", ex);
        }
    }
}

// Program.cs registration:
builder.Services.AddHealthChecks()
    .AddCheck<JwksHealthCheck>("jwks", tags: new[] { "ready" });
```

### 4.4 Eltávolítandó Kernel kód

| Fájl | Mit | Miért |
|------|-----|-------|
| `TokenService.cs` / `ITokenService` | Teljes törlés | Keycloak generálja a JWT-t |
| `LoginCommand` + `LoginCommandHandler` | Teljes törlés | PKCE flow — nincs Kernel login |
| `LoginCommandValidator` | Teljes törlés | |
| `RefreshTokenCommand` + `Handler` | Teljes törlés | Keycloak kezeli |
| `RefreshTokenCommandValidator` | Teljes törlés | |
| `RegisterCommand` + `Handler` (ha van) | Teljes törlés | Admin-only Keycloak provisioning |
| `/api/auth/token` endpoint mapping | Eltávolítás | Keycloak token endpoint |
| `/api/auth/refresh` endpoint mapping | Eltávolítás | Keycloak token endpoint |
| JWT key generation / `jwtKeys/` directory | Eltávolítás | JWKS auto-discovery |
| `JwtSettings.cs` (ha van, a régi `Key` property-vel) | Refaktor → `Authority` + `Audience` only | |

**Érintett tesztek:** Auth-specifikus unit és integration tesztek refaktorálandók — mock `ClaimsIdentity` a Keycloak claim formátummal.

### 4.5 Kernel Migration: nincs DB migráció

A `tenant_id` minden táblán UUID, RLS policy-k érvényesek. Csak kód szintű változás.

---

## 5. Orchestrator Changes

### 5.1 JWT verify átállás — JWKS + retry (BE-03)

```typescript
// spaceos-orchestrator/src/middleware/jwtVerify.ts — TELJES CSERE

import jwksRsa from 'jwks-rsa';
import jwt, { JwtPayload, JwtHeader, SigningKeyCallback } from 'jsonwebtoken';

const jwksClient = jwksRsa({
  jwksUri: process.env.JWKS_URI!,
  cache: true,
  cacheMaxEntries: 5,
  cacheMaxAge: 600_000,          // 10 min
  jwksRequestsPerMinute: 10,     // BE-03: allow burst on key rotation
  rateLimit: true
});

function getKey(header: JwtHeader, callback: SigningKeyCallback): void {
  jwksClient.getSigningKey(header.kid, (err, key) => {
    if (err) return callback(err);
    callback(null, key?.getPublicKey());
  });
}

export async function verifyToken(token: string): Promise<JwtPayload> {
  try {
    return await verifyOnce(token);
  } catch (err: any) {
    // BE-03: On signature validation failure, flush cache and retry once
    // This handles Keycloak key rotation within cache window
    if (err.name === 'JsonWebTokenError' && err.message.includes('signature')) {
      jwksClient.getSigningKeys(); // Force cache refresh
      return verifyOnce(token);
    }
    throw err;
  }
}

function verifyOnce(token: string): Promise<JwtPayload> {
  return new Promise((resolve, reject) => {
    jwt.verify(token, getKey, {
      issuer: process.env.JWT_ISSUER,
      audience: process.env.JWT_AUDIENCE,
      algorithms: ['RS256']
    }, (err, decoded) => {
      if (err) return reject(err);
      resolve(decoded as JwtPayload);
    });
  });
}
```

```bash
# .env (orchestrator)
JWKS_URI=https://joinerytech.hu/auth/realms/spaceos/protocol/openid-connect/certs
JWT_ISSUER=https://joinerytech.hu/auth/realms/spaceos
JWT_AUDIENCE=kernel-api
```

**Megjegyzés:** Orchestrator audience = `kernel-api` (nem külön `orchestrator-bff`), mert D-03 passthrough: ugyanazt a token-t validálja, amit a Kernel is.

### 5.2 Eltávolítandó Orchestrator kód

| Fájl | Mit | Miért |
|------|-----|-------|
| `auth.routes.ts` (`POST /bff/api/auth/token`) | Teljes törlés | Dev auth → Keycloak |
| `auth.routes.ts` (`POST /bff/api/auth/refresh`) | Teljes törlés | Keycloak refresh |
| Static public key loading (`jwtKeys/`, `RS256` file read) | Teljes törlés | JWKS auto-discovery |
| `generateToken()` utility (ha van) | Törlés | Orchestrator nem generál token-t |

### 5.3 Megmaradó + Új Orchestrator felelősségek

| Felelősség | Státusz | Változás |
|---|---|---|
| JWT verify (JWKS) | ✅ Megmarad | `jwks-rsa` package-re vált |
| `X-SpaceOS-Brand` header injection | ✅ Megmarad | Nem érintett |
| Bearer token forward → Kernel | ✅ Megmarad (D-03) | Nem érintett |
| LLM tool registry (per-brand) | ✅ Megmarad | Nem érintett |
| `GET /bff/api/auth/me` | ✅ ÚJ | JWT claims → user info |

### 5.4 Új endpoint: `GET /bff/api/auth/me`

```typescript
// spaceos-orchestrator/src/routes/auth.routes.ts — ÚJ

import { Router } from 'express';
import { authMiddleware } from '../middleware/authMiddleware';

const router = Router();

// GET /bff/api/auth/me — JWT-ből kinyert user info + tenant lista
// Nincs Keycloak hívás — pure JWT decode
router.get('/bff/api/auth/me', authMiddleware, (req, res) => {
  const claims = req.user!;

  // BE-01: Double deserialization for spaceos_tenants
  let tenants: TenantInfo[] = [];
  try {
    const raw = claims.spaceos_tenants as string;
    if (raw) {
      const parsed = typeof raw === 'string' ? JSON.parse(raw) : raw;
      tenants = Array.isArray(parsed) ? parsed : JSON.parse(parsed);
    }
  } catch {
    tenants = [];
  }

  res.json({
    sub: claims.sub,
    email: claims.email,
    name: `${claims.given_name ?? ''} ${claims.family_name ?? ''}`.trim(),
    tenants: tenants.map((t: any) => ({
      tenantId: t.tenant_id,
      tenantType: t.tenant_type,
      enabledModules: t.enabled_modules ?? [],
      brandSkin: t.brand_skin ?? 'joinerytech'
    })),
    activeTenantId: tenants[0]?.tenant_id ?? null,
    roles: claims.realm_access?.roles ?? []
  });
});

interface TenantInfo {
  tenant_id: string;
  tenant_type: string;
  enabled_modules: string[];
  brand_skin: string;
}

export default router;
```

---

## 6. Portal Changes

### 6.1 PKCE Auth Module (SEC-01 + SEC-02)

```typescript
// packages/@spaceos/api-client/src/auth/keycloak.ts — ÚJ

const KC_REALM_URL = import.meta.env.VITE_KC_REALM_URL;
const KC_CLIENT_ID = import.meta.env.VITE_KC_CLIENT_ID;

// --- Crypto helpers ---

function base64UrlEncode(buffer: Uint8Array): string {
  return btoa(String.fromCharCode(...buffer))
    .replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

function generateRandom(length: number): string {
  const array = new Uint8Array(length);
  crypto.getRandomValues(array);
  return base64UrlEncode(array);
}

async function generateCodeChallenge(verifier: string): Promise<string> {
  const digest = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(verifier));
  return base64UrlEncode(new Uint8Array(digest));
}

// --- Public API ---

export async function redirectToLogin(): Promise<void> {
  const codeVerifier = generateRandom(32);
  const codeChallenge = await generateCodeChallenge(codeVerifier);
  const state = generateRandom(16);   // SEC-01: CSRF protection
  const nonce = generateRandom(16);   // SEC-02: Token replay protection

  // Store PKCE + security params for callback validation
  sessionStorage.setItem('pkce_verifier', codeVerifier);
  sessionStorage.setItem('oauth_state', state);
  sessionStorage.setItem('oauth_nonce', nonce);

  const params = new URLSearchParams({
    client_id: KC_CLIENT_ID,
    response_type: 'code',
    scope: 'openid profile email',
    redirect_uri: `${window.location.origin}/callback`,
    code_challenge: codeChallenge,
    code_challenge_method: 'S256',
    state,
    nonce
  });

  window.location.href = `${KC_REALM_URL}/protocol/openid-connect/auth?${params}`;
}

export async function handleCallback(
  searchParams: URLSearchParams
): Promise<TokenResponse> {
  const code = searchParams.get('code');
  const returnedState = searchParams.get('state');
  const error = searchParams.get('error');

  // Error from Keycloak
  if (error) {
    throw new AuthError(`Keycloak error: ${error}`, 'keycloak_error');
  }

  if (!code) {
    throw new AuthError('Missing authorization code', 'missing_code');
  }

  // SEC-01: Validate state parameter
  const storedState = sessionStorage.getItem('oauth_state');
  if (!storedState || storedState !== returnedState) {
    sessionStorage.removeItem('pkce_verifier');
    sessionStorage.removeItem('oauth_state');
    sessionStorage.removeItem('oauth_nonce');
    throw new AuthError('State mismatch — possible CSRF', 'state_mismatch');
  }

  const codeVerifier = sessionStorage.getItem('pkce_verifier');
  const storedNonce = sessionStorage.getItem('oauth_nonce');

  // Clean up immediately
  sessionStorage.removeItem('pkce_verifier');
  sessionStorage.removeItem('oauth_state');
  sessionStorage.removeItem('oauth_nonce');

  if (!codeVerifier) {
    throw new AuthError('Missing PKCE verifier', 'missing_verifier');
  }

  // Exchange code for tokens (BE-05: retry once)
  const tokens = await exchangeCodeWithRetry(code, codeVerifier);

  // SEC-02: Validate nonce in id_token
  if (storedNonce && tokens.id_token) {
    const idPayload = parseJwtPayload(tokens.id_token);
    if (idPayload.nonce !== storedNonce) {
      throw new AuthError('Nonce mismatch — possible token replay', 'nonce_mismatch');
    }
  }

  return tokens;
}

async function exchangeCodeWithRetry(
  code: string,
  codeVerifier: string,
  attempt = 0
): Promise<TokenResponse> {
  try {
    return await exchangeCode(code, codeVerifier);
  } catch (err) {
    // BE-05: Retry once after 1s
    if (attempt === 0) {
      await new Promise(r => setTimeout(r, 1000));
      return exchangeCodeWithRetry(code, codeVerifier, 1);
    }
    throw err;
  }
}

async function exchangeCode(code: string, codeVerifier: string): Promise<TokenResponse> {
  const res = await fetch(`${KC_REALM_URL}/protocol/openid-connect/token`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: new URLSearchParams({
      grant_type: 'authorization_code',
      client_id: KC_CLIENT_ID,
      code,
      redirect_uri: `${window.location.origin}/callback`,
      code_verifier: codeVerifier
    })
  });

  if (!res.ok) {
    const body = await res.text().catch(() => '');
    throw new AuthError(`Token exchange failed: ${res.status} ${body}`, 'exchange_failed');
  }
  return res.json();
}

export async function refreshAccessToken(refreshToken: string): Promise<TokenResponse> {
  const res = await fetch(`${KC_REALM_URL}/protocol/openid-connect/token`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: new URLSearchParams({
      grant_type: 'refresh_token',
      client_id: KC_CLIENT_ID,
      refresh_token: refreshToken
    })
  });

  if (!res.ok) throw new AuthError('Token refresh failed', 'refresh_failed');
  return res.json();
}

export function logoutUrl(idTokenHint: string): string {
  const params = new URLSearchParams({
    post_logout_redirect_uri: window.location.origin,
    id_token_hint: idTokenHint
  });
  return `${KC_REALM_URL}/protocol/openid-connect/logout?${params}`;
}

// --- Helpers ---

function parseJwtPayload(token: string): Record<string, any> {
  const base64 = token.split('.')[1]
    .replace(/-/g, '+').replace(/_/g, '/');
  return JSON.parse(atob(base64));
}

export class AuthError extends Error {
  constructor(message: string, public code: string) {
    super(message);
    this.name = 'AuthError';
  }
}

export interface TokenResponse {
  access_token: string;
  refresh_token: string;
  id_token: string;
  expires_in: number;
  token_type: string;
}
```

### 6.2 Callback page

```typescript
// apps/joinerytech/src/pages/CallbackPage.tsx — ÚJ

import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { handleCallback, AuthError } from '@spaceos/api-client';
import { useAuthStore } from '@spaceos/api-client';

export default function CallbackPage() {
  const navigate = useNavigate();
  const setTokens = useAuthStore((s) => s.setTokens);
  const fetchMe = useAuthStore((s) => s.fetchMe);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const params = new URLSearchParams(window.location.search);

    handleCallback(params)
      .then(async (tokens) => {
        setTokens(tokens.access_token, tokens.refresh_token, tokens.id_token);
        await fetchMe();
        navigate('/', { replace: true });
      })
      .catch((err) => {
        if (err instanceof AuthError) {
          console.error(`Auth error [${err.code}]:`, err.message);
          if (err.code === 'state_mismatch' || err.code === 'nonce_mismatch') {
            setError('Biztonsági hiba. Kérjük próbáld újra a bejelentkezést.');
          } else {
            setError('Bejelentkezés sikertelen. Kérjük próbáld újra.');
          }
        } else {
          console.error('Unexpected auth error:', err);
          setError('Váratlan hiba történt.');
        }
      });
  }, []);

  if (error) {
    return (
      <div className="flex flex-col items-center justify-center h-screen gap-4">
        <p className="text-red-600">{error}</p>
        <button
          className="px-4 py-2 bg-primary text-white rounded"
          onClick={() => navigate('/', { replace: true })}
        >
          Vissza a főoldalra
        </button>
      </div>
    );
  }

  return (
    <div className="flex items-center justify-center h-screen">
      <p>Bejelentkezés...</p>
    </div>
  );
}
```

### 6.3 AuthStore átírás

```typescript
// packages/@spaceos/api-client/src/stores/authStore.ts — TELJES CSERE

import { create } from 'zustand';
import { refreshAccessToken } from '../auth/keycloak';
import { createSpaceOsClient } from '../client';

export interface TenantInfo {
  tenantId: string;
  tenantType: string;
  enabledModules: string[];
  brandSkin: string;
}

export interface UserInfo {
  sub: string;
  email: string;
  name: string;
  tenants: TenantInfo[];
  activeTenantId: string | null;
  roles: string[];
}

interface AuthState {
  // Tokens — MEMORY ONLY, no persist (Phase 3C+ SEC finding)
  accessToken: string | null;
  refreshToken: string | null;
  idToken: string | null;

  // User info (from /bff/api/auth/me)
  user: UserInfo | null;

  // Derived
  isAuthenticated: boolean;

  // Actions
  setTokens: (access: string, refresh: string, id: string) => void;
  clearTokens: () => void;
  fetchMe: () => Promise<void>;
  tryRefresh: () => Promise<boolean>;
}

export const useAuthStore = create<AuthState>((set, get) => ({
  accessToken: null,
  refreshToken: null,
  idToken: null,
  user: null,
  isAuthenticated: false,

  setTokens: (access, refresh, id) =>
    set({ accessToken: access, refreshToken: refresh, idToken: id, isAuthenticated: true }),

  clearTokens: () =>
    set({
      accessToken: null, refreshToken: null, idToken: null,
      user: null, isAuthenticated: false
    }),

  fetchMe: async () => {
    const { accessToken } = get();
    if (!accessToken) return;

    const client = createSpaceOsClient(() => accessToken);
    const res = await fetch(`${client.baseUrl}/bff/api/auth/me`, {
      headers: { Authorization: `Bearer ${accessToken}` }
    });

    if (!res.ok) return;
    const data: UserInfo = await res.json();
    set({ user: data });
  },

  tryRefresh: async () => {
    const { refreshToken } = get();
    if (!refreshToken) return false;

    try {
      const tokens = await refreshAccessToken(refreshToken);
      set({
        accessToken: tokens.access_token,
        refreshToken: tokens.refresh_token,
        idToken: tokens.id_token,
        isAuthenticated: true
      });
      return true;
    } catch {
      get().clearTokens();
      return false;
    }
  }
}));
```

### 6.4 ProtectedRoute update

```typescript
// apps/joinerytech/src/components/ProtectedRoute.tsx — DIFF

import { redirectToLogin } from '@spaceos/api-client';
import { useAuthStore } from '@spaceos/api-client';

export function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);

  if (!isAuthenticated) {
    // Nem /login page-re navigálunk — Keycloak hosted login
    redirectToLogin();
    return <div className="flex items-center justify-center h-screen">Átirányítás...</div>;
  }

  return <>{children}</>;
}
```

### 6.5 Route changes

```typescript
// apps/joinerytech/src/router.tsx — DIFF

// ADD:
import CallbackPage from './pages/CallbackPage';
import LoginErrorPage from './pages/LoginErrorPage';

// Routes:
{ path: '/callback', element: <CallbackPage /> },
{ path: '/login-error', element: <LoginErrorPage /> },

// REMOVE:
// { path: '/login', element: <LoginPage /> }  — eltávolítandó
```

### 6.6 Eltávolítandó Portal kód

| Fájl / Komponens | Miért |
|---|---|
| `LoginPage.tsx` (username/password form) | PKCE — nincs Portal-ban jelszó |
| Dev auth API hívások (`/bff/api/auth/token`, `/bff/api/auth/refresh`) | Keycloak token endpoint |
| `authStore` régi login/logout action-ök | Teljes csere (fent) |
| Manual JWT decode (role, brandSkin extraction) | `/bff/api/auth/me` adja vissza |

### 6.7 Environment variables

```env
# apps/joinerytech/.env.production
VITE_KC_REALM_URL=https://joinerytech.hu/auth/realms/spaceos
VITE_KC_CLIENT_ID=portal-app

# apps/joinerytech/.env.development
VITE_KC_REALM_URL=http://localhost:8080/realms/spaceos
VITE_KC_CLIENT_ID=portal-app
```

---

## 7. Keycloak Realm Configuration

### 7.1 Realm: `spaceos`

```json
{
  "realm": "spaceos",
  "enabled": true,
  "registrationAllowed": false,
  "loginWithEmailAllowed": true,
  "duplicateEmailsAllowed": false,
  "sslRequired": "external",
  "accessTokenLifespan": 900,
  "ssoSessionIdleTimeout": 1800,
  "ssoSessionMaxLifespan": 36000,
  "refreshTokenMaxReuse": 0,
  "revokeRefreshToken": true
}
```

### 7.2 Clients

| Client ID | Type | Access Type | PKCE | Valid Redirects | Web Origins |
|-----------|------|-------------|------|-----------------|-------------|
| `portal-app` | openid-connect | public | S256 required | `https://joinerytech.hu/callback`, `https://asztalostech.hu/callback`, `http://localhost:5173/callback` | `https://joinerytech.hu`, `https://asztalostech.hu`, `http://localhost:5173` |
| `kernel-api` | openid-connect | bearer-only | — | — | — |
| `test-runner` | openid-connect | confidential | — | — | — |

**`test-runner` client (BE-04):** Kizárólag E2E tesztekhez. `Direct Access Grant` engedélyezve → programmatic login username/password-del. **NEM** a prod Portal flow.

### 7.3 Client Scopes

| Scope | Mappers |
|-------|---------|
| `spaceos-claims` (assigned to `portal-app` + `test-runner`) | `spaceos-tenants-mapper` (Script) · `audience-kernel-mapper` (Audience Resolve) |

### 7.4 Groups (Doorstar soft launch)

| Group | Attributes |
|-------|-----------|
| `doorstar-kft` | `tenant_id`: `{Doorstar tenant UUID from Kernel seed}` · `tenant_type`: `Producer` · `enabled_modules`: `["door"]` · `brand_skin`: `doorstar` |

### 7.5 Users (Doorstar soft launch)

| Username | Email | Group | Realm Roles | Megjegyzés |
|----------|-------|-------|-------------|------------|
| `admin` | `admin@doorstar.hu` | `doorstar-kft` | `Admin` | Keycloak-ban beállított jelszó |
| `designer` | `designer@doorstar.hu` | `doorstar-kft` | `Designer` | |
| `test-admin` | `test@spaceos.local` | `doorstar-kft` | `Admin` | Csak `test-runner` client-tel használható (BE-04) |

### 7.6 Identity Providers (Day 2 — nem soft launch scope)

| Provider | Státusz |
|----------|---------|
| Google OIDC | Konfigurálható ha van Google Cloud project |
| Microsoft Entra ID | Konfigurálható ha van Azure AD tenant |

---

## 8. Infrastructure

### 8.1 Docker Compose (VPS production)

```yaml
# /opt/spaceos/keycloak/docker-compose.yml
version: '3.9'
services:
  keycloak:
    image: quay.io/keycloak/keycloak:24.0.5   # SEC-04: pinned patch version
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
      KC_FEATURES: scripts                     # DB-01: enable JS protocol mapper
      KEYCLOAK_ADMIN: ${KC_ADMIN_USER}
      KEYCLOAK_ADMIN_PASSWORD: ${KC_ADMIN_PASSWORD}
    ports:
      - "127.0.0.1:8080:8080"
    restart: always
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health/ready"]
      interval: 30s
      timeout: 10s
      retries: 3
    extra_hosts:
      - "host.docker.internal:host-gateway"
```

### 8.2 PostgreSQL (Keycloak DB) — DB-05 javítva

```sql
-- VPS-en futtatandó (superuser-rel)
CREATE DATABASE spaceos_keycloak;
CREATE USER spaceos_keycloak_user WITH PASSWORD '${KC_DB_PASSWORD}';
GRANT ALL PRIVILEGES ON DATABASE spaceos_keycloak TO spaceos_keycloak_user;

-- DB-05: Explicit izolálás a fő app DB-től
REVOKE ALL ON DATABASE spaceos FROM spaceos_keycloak_user;
REVOKE ALL ON SCHEMA public FROM spaceos_keycloak_user;  -- fő DB public schema
```

### 8.3 Nginx kiegészítés — SEC-03 CSP update

```nginx
# /etc/nginx/sites-available/spaceos — kiegészítés

# --- Keycloak proxy ---

# Admin konzol — CSAK localhost (ProdReady SEC-02)
# FONTOS: ez a block a /auth/ előtt kell legyen (longest prefix match OK, de explicit order jobb)
location /auth/admin/ {
    allow 127.0.0.1;
    deny all;
    proxy_pass http://127.0.0.1:8080/admin/;
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
}

# Keycloak public (login, JWKS, token endpoint)
location /auth/ {
    proxy_pass http://127.0.0.1:8080/;
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
    proxy_buffer_size 128k;
    proxy_buffers 4 256k;
}

# --- CSP update (SEC-03) ---
# Meglévő CSP header módosítása:
# connect-src 'self' https://joinerytech.hu/auth/ https://asztalostech.hu/auth/;
# (dev-ben: connect-src 'self' http://localhost:8080/)
```

### 8.4 Dev Docker Compose (lokális fejlesztés)

```yaml
# spaceos-dev/docker-compose.yml
version: '3.9'
services:
  keycloak-dev:
    image: quay.io/keycloak/keycloak:24.0.5
    command: start-dev --import-realm
    environment:
      KEYCLOAK_ADMIN: admin
      KEYCLOAK_ADMIN_PASSWORD: admin
      KC_HTTP_PORT: 8080
      KC_FEATURES: scripts       # DB-01
    ports:
      - "8080:8080"
    volumes:
      - ./keycloak/realm-export.json:/opt/keycloak/data/import/realm-export.json
```

**`realm-export.json` (DB-04):** Tartalmazza az összes client, group, mapper, test user konfigurációt. `docker compose up` → teljes auth infrastruktúra azonnal elérhető. H2 adatvesztés nem probléma mert minden import-ból jön.

---

## 9. Test Strategy

### 9.1 Kernel tesztek

| Teszt | Leírás | Darab |
|-------|--------|-------|
| `TenantSessionInterceptor_Keycloak_Claims` | Mock `ClaimsIdentity` `spaceos_tenants` JSON stringgel; double-deser (BE-01) | 4 |
| `TenantSessionInterceptor_ActiveTenant_Validation` | `X-SpaceOS-Active-Tenant` header ∈ / ∉ tenants[] (DB-02) | 3 |
| `TenantSessionInterceptor_MalformedClaims` | Érvénytelen JSON, üres tömb, hiányzó claim | 3 |
| `JwtBearer_Authority_Config` | `WebApplicationFactory` + Authority config → JWKS endpoint discovery | 2 |
| `Auth_Endpoints_Removed` | `POST /api/auth/token` → 404; `POST /api/auth/refresh` → 404 | 2 |
| `JwksHealthCheck` | Mock HTTP → healthy / degraded | 2 |
| `RealmRoles_Mapping` | `realm_access.roles` → `ClaimTypes.Role` mapping (OnTokenValidated) | 2 |
| **Összesen** | | **18** |

### 9.2 Orchestrator tesztek

| Teszt | Leírás | Darab |
|-------|--------|-------|
| `JwksVerify_ValidToken` | Mock JWKS endpoint (`nock`) + valid JWT → pass | 2 |
| `JwksVerify_ExpiredToken` | Expired JWT → 401 | 1 |
| `JwksVerify_KeyRotation_Retry` | First verify fail, cache flush, second verify pass (BE-03) | 1 |
| `AuthMe_Endpoint` | JWT payload → response mapping (`spaceos_tenants` parse) | 3 |
| `Auth_Routes_Removed` | `POST /bff/api/auth/token` → 404 | 2 |
| `BearerPassthrough` | Token forwarded to Kernel unchanged (D-03) | 2 |
| **Összesen** | | **11** |

### 9.3 Portal tesztek

| Teszt | Leírás | Darab |
|-------|--------|-------|
| `redirectToLogin_URL` | URL params: client_id, PKCE, state, nonce (SEC-01, SEC-02) | 3 |
| `handleCallback_Success` | Mock fetch → token response → store update | 2 |
| `handleCallback_StateMismatch` | Stored state ≠ URL state → AuthError (SEC-01) | 1 |
| `handleCallback_NonceMismatch` | id_token nonce ≠ stored nonce → AuthError (SEC-02) | 1 |
| `handleCallback_MissingCode` | No code param → AuthError | 1 |
| `exchangeCode_Retry` | First fetch fail → retry after 1s → success (BE-05) | 1 |
| `authStore_SetClear` | setTokens / clearTokens / isAuthenticated | 2 |
| `authStore_TryRefresh` | Mock refresh → new tokens | 2 |
| `authStore_TryRefresh_Fail` | Refresh fail → clearTokens | 1 |
| `ProtectedRoute` | Not authenticated → redirectToLogin called | 1 |
| `CallbackPage_ErrorDisplay` | Keycloak error param → error message | 1 |
| **Összesen** | | **16** |

### 9.4 E2E tesztek (BE-04)

| Teszt | Leírás | Darab |
|-------|--------|-------|
| `Login_DirectAccessGrant` | `test-runner` client + test user → valid token → dashboard access | 2 |
| `Protected_Endpoint_NoToken` | No Authorization header → 401 | 1 |
| `Protected_Endpoint_BadToken` | Invalid JWT → 401 | 1 |
| `Protected_Endpoint_ValidToken` | Valid token → 200 + correct tenant data | 2 |
| `Logout_SessionInvalidation` | Logout → token rejected on next request | 1 |
| `Tenant_Header_Validation` | Invalid `X-SpaceOS-Active-Tenant` → 403 (DB-02) | 1 |
| **Összesen** | | **8** |

**E2E auth helper:**

```typescript
// e2e/helpers/keycloakAuth.ts — ÚJ
export async function getTestToken(username = 'test-admin', password = 'test'): Promise<string> {
  const res = await fetch(
    `${KC_URL}/realms/spaceos/protocol/openid-connect/token`,
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body: new URLSearchParams({
        grant_type: 'password',           // Direct Access Grant
        client_id: 'test-runner',
        client_secret: process.env.KC_TEST_CLIENT_SECRET!,
        username,
        password
      })
    }
  );
  const data = await res.json();
  return data.access_token;
}
```

**Összesített teszt target: ≥ 53 új teszt** (18 Kernel + 11 Orchestrator + 16 Portal + 8 E2E)

---

## 10. Definition of Done

### Infra gates

- [ ] Keycloak 24.0.5 Docker container fut VPS-en (:8080, 127.0.0.1-only)
- [ ] `KC_FEATURES=scripts` env var beállítva (DB-01)
- [ ] `spaceos_keycloak` DB létrehozva PostgreSQL 16-on
- [ ] `REVOKE ALL ON DATABASE spaceos FROM spaceos_keycloak_user` futtatva (DB-05)
- [ ] Nginx `/auth/` proxy működik (HTTPS)
- [ ] Nginx `/auth/admin/` → 403 külső IP-ről, 200 localhost-ról (ProdReady SEC-02)
- [ ] Nginx CSP: `connect-src` tartalmazza `/auth/` origin-t (SEC-03)
- [ ] Keycloak health: `curl http://localhost:8080/health/ready` → UP
- [ ] `realm-export.json` verziózva `spaceos-docs` repo-ban (DB-03: no secrets)
- [ ] Dev Docker Compose: `docker compose up` → Keycloak + realm import működik

### Keycloak realm gates

- [ ] `spaceos` realm létrehozva, `registrationAllowed: false`
- [ ] `portal-app` client: public, PKCE S256 required, valid redirects
- [ ] `kernel-api` client: bearer-only
- [ ] `test-runner` client: confidential, Direct Access Grant (BE-04)
- [ ] `spaceos-claims` client scope: `spaceos-tenants` Script Mapper + audience mapper
- [ ] Group `doorstar-kft` + attributes (`tenant_id`, `tenant_type`, `enabled_modules`, `brand_skin`)
- [ ] UUID format validation a Script Mapper-ben (`tid.length() === 36`)
- [ ] Test users: `admin`, `designer`, `test-admin` — group membership + realm roles
- [ ] Token endpoint: `curl -X POST .../token` → valid JWT with `spaceos_tenants` claim

### Kernel gates

- [ ] `AddJwtBearer` Authority-alapú JWKS auto-discovery (config-driven: BE-01 ProdReady)
- [ ] `OnTokenValidated` event: `realm_access.roles` → flat `ClaimTypes.Role`
- [ ] `TenantSessionInterceptor`: `spaceos_tenants` double-deser (BE-01)
- [ ] `TenantSessionInterceptor`: `X-SpaceOS-Active-Tenant` ∈ tenants[] validáció (DB-02)
- [ ] `TenantSessionInterceptor`: invalid header → `UnauthorizedAccessException` + log
- [ ] `JwksHealthCheck` regisztrálva (BE-02)
- [ ] Régi auth kód ELTÁVOLÍTVA: LoginCommand, RefreshTokenCommand, TokenService, `/api/auth/*`
- [ ] Meglévő 915 Kernel teszt ZÖLD (refactored claim mocks)
- [ ] ≥ 18 új Kernel teszt
- [ ] 0 build warning
- [ ] `ConfigureAwait(false)` minden production async call-ban
- [ ] `dotnet list package --vulnerable` → 0 high/critical

### Orchestrator gates

- [ ] `jwks-rsa` package: JWKS endpoint + cache + retry on miss (BE-03)
- [ ] Régi auth routes ELTÁVOLÍTVA: `POST /bff/api/auth/token`, `POST /bff/api/auth/refresh`
- [ ] Static key loading ELTÁVOLÍTVA: `jwtKeys/` directory
- [ ] `GET /bff/api/auth/me` endpoint: JWT claims → user info + tenants
- [ ] Bearer token passthrough → Kernel (D-03)
- [ ] Meglévő 153 Orchestrator teszt ZÖLD
- [ ] ≥ 11 új Orchestrator teszt
- [ ] 0 TypeScript error

### Portal gates

- [ ] PKCE flow: `state` (SEC-01) + `nonce` (SEC-02) parameters
- [ ] `CallbackPage`: state validation, nonce validation, code exchange
- [ ] `LoginErrorPage`: error handling UI
- [ ] Token refresh: `refreshAccessToken()` on 401 → `tryRefresh()`
- [ ] Logout: Keycloak RP-initiated logout + memory clear
- [ ] `authStore`: memory-only (no persist — no localStorage/sessionStorage for tokens)
- [ ] `ProtectedRoute`: redirect to Keycloak if not authenticated
- [ ] Régi `LoginPage` (username/password form) ELTÁVOLÍTVA
- [ ] `exchangeCode` retry logic (BE-05)
- [ ] Meglévő 321 Portal teszt ZÖLD
- [ ] ≥ 16 új Portal teszt
- [ ] 0 TypeScript error

### E2E gates

- [ ] `test-runner` client + `getTestToken()` helper (BE-04)
- [ ] Login flow E2E: Direct Access Grant → dashboard access
- [ ] Protected endpoint: valid/invalid/missing token (401/403)
- [ ] Tenant header validation: invalid header → 403 (DB-02)
- [ ] Logout E2E
- [ ] Meglévő 63 E2E teszt ZÖLD (refactored auth helper)
- [ ] ≥ 8 új E2E teszt

### Összesített

- [ ] Meglévő 1452 teszt ZÖLD (minden repo)
- [ ] ≥ 53 új teszt (18 Kernel + 11 Orchestrator + 16 Portal + 8 E2E)
- [ ] 0 build warning (minden repo)
- [ ] Dev auth code path TELJESEN eltávolítva — `grep -r "auth/token\|generateToken\|loginCommand" → 0`
- [ ] `realm-export.json` reprodukálható dev setup — `docker compose up` = working auth
- [ ] Golden Rules 1–12 teljesül
- [ ] EXPLAIN ANALYZE: nem releváns (nincs új DB query)

---

## 11. Security Debt Status

| ID | Tétel | Korábbi státusz | Ez a phase | Marad |
|----|-------|----------------|------------|-------|
| — | Dev auth kiváltása IdP-vel | ❌ OPEN | ✅ **RESOLVED** | — |
| — | JWT key management (manual key files) | ❌ OPEN | ✅ **RESOLVED** (JWKS auto-discovery) | — |
| — | Refresh token rotation | ❌ OPEN | ✅ **RESOLVED** (Keycloak `revokeRefreshToken: true`) | — |
| SEC-05 | Backchannel logout | — | 🟡 NEW | Phase 2 IdP |
| — | Social login (Google/Microsoft) | — | 🟡 DESIGNED | Day 2 (realm ready) |
| — | Tenant switcher UI (multi-group user) | — | 🟡 DESIGNED | Phase 2 IdP |
| — | Self-registration flow | — | 🟡 DEFERRED | 2+ tenant |
| — | Keycloak HA (multi-instance) | — | 🟡 DEFERRED | Scale gate |
| — | Keycloak theme (branded login) | — | 🟡 DEFERRED | UX polish |
| — | KC Script Mapper → SPI plugin (Keycloak 25+) | — | 🟡 DOCUMENTED | Upgrade gate |

---

## 12. Security Considerations Summary

| Terület | Kockázat | Mitigáció | Státusz |
|---------|---------|-----------|---------|
| CSRF on callback | Attacker injects auth code → session fixation | `state` param + validation (SEC-01) | ✅ v3 |
| Token replay | Stolen id_token reuse | `nonce` param + validation (SEC-02) | ✅ v3 |
| CSP block | Portal→Keycloak fetch blocked | `connect-src` update (SEC-03) | ✅ v3 |
| PKCE verifier XSS | sessionStorage readable via XSS | CSP strict (already enforced); short-lived (callback clears) | ✅ Existing |
| Refresh token storage | sessionStorage → tab close = re-login | Acceptable trade-off; secure > convenient | ✅ Design decision |
| Keycloak admin access | User/group manipulation → cross-tenant | Nginx 127.0.0.1 only (ProdReady SEC-02) | ✅ Inherited |
| Group attribute spoofing | Admin modifies group attr | RLS = final defense (Kernel TenantSessionInterceptor) | ✅ Inherited |
| Token lifetime | 15 min access + 30 min idle | Keycloak realm config | ✅ |
| JWKS unavailable | New logins blocked | `JwksHealthCheck` degraded status; cached keys valid (BE-02) | ✅ v4 |
| Key rotation gap | 10 min cache → new tokens rejected | JWKS retry-on-miss (BE-03) | ✅ v4 |
| Active tenant spoofing | Forged header → wrong tenant context | Server-side validation ∈ JWT tenants[] (DB-02) | ✅ v2 |

---

## 13. Végrehajtási sorrend

| Nap | Repo | Feladat | Függőség | Track |
|-----|------|---------|----------|-------|
| 1 | Infra | Keycloak Docker + PostgreSQL DB + REVOKE (DB-05) + Nginx config | VPS hozzáférés | A |
| 1 | Infra | Realm setup: clients, groups, mappers, test users | Keycloak running | A |
| 2 | Infra | `realm-export.json` export + dev Docker Compose | Realm ready | A |
| 3 | Kernel | JWT Authority + JWKS + `OnTokenValidated` role mapping | Keycloak realm | B |
| 3 | Kernel | `TenantSessionInterceptor` refaktor (double-deser + header validation) | | B |
| 4 | Kernel | Régi auth kód eltávolítás + `JwksHealthCheck` | | B |
| 4 | Kernel | Teszt refaktor + 18 új teszt | | B |
| 5 | Orchestrator | `jwks-rsa` + JWKS verify middleware + retry (BE-03) | Keycloak JWKS | C |
| 5 | Orchestrator | Régi auth routes eltávolítás + `GET /bff/api/auth/me` | | C |
| 6 | Orchestrator | Tesztek (11 új) | | C |
| 7 | Portal | Keycloak PKCE module (state + nonce) + `CallbackPage` | | D |
| 7 | Portal | `authStore` átírás + `ProtectedRoute` update | | D |
| 8 | Portal | Régi login kód eltávolítás + tesztek (16 új) | | D |
| 9 | Portal | Nginx CSP update (SEC-03) + Integration testing | All repos | D |
| 10 | E2E | `test-runner` client + `getTestToken()` helper + 8 E2E teszt | All deployed | E |
| 11 | All | Full-stack integration testing + bugfix buffer | | — |
| 12 | All | Final DoD verification + realm-export.json freeze | | — |

### Kockázatok és mitigációk

| Kockázat | Valószínűség | Hatás | Mitigáció |
|----------|-------------|-------|-----------|
| Keycloak 24 Script Mapper deprecated | Közepes | Mapper nem működik | `KC_FEATURES=scripts` env var (DB-01); Keycloak 25 upgrade → SPI plugin |
| JWKS endpoint timeout dev-ben | Alacsony | Dev tesztek flaky | Docker health check wait; test retry |
| E2E tesztek nagy refaktor | Közepes | +1 nap csúszás | `getTestToken()` helper egyszer megírva → minden teszt használja |
| Meglévő tesztek auth mock breakage | Közepes | Sok teszt pirosra vált | Claim mock factory: `createKeycloakClaimsPrincipal()` helper |
| CSP strict-dynamic ütközés | Alacsony | Portal fetch blocked | Nginx CSP header explicit `connect-src` |

---

## 14. Mi jön utána

| Feladat | Prioritás | Mikor | Becsült effort |
|---------|----------|-------|----------------|
| Tenant switcher UI (Portal) | P2 | Ha multi-tenant user megjelenik | ~3 nap |
| Backchannel logout endpoint (SEC-05) | P2 | Phase 2 IdP | ~2 nap |
| Social login (Google/Microsoft) config | P3 | Doorstar feedback | ~1 nap (config only) |
| Keycloak branded login theme | P3 | UX polish | ~2 nap |
| Self-registration flow | P3 | 2+ tenant | ~3 nap |
| Keycloak HA (2 instance) | P3 | Scale gate | ~2 nap |
| Script Mapper → SPI plugin | P3 | Keycloak 25 upgrade | ~3 nap |

---

*SpaceOS · Keycloak IdP Architecture v4.0 · `/database-designer` + `/database-schema-designer` + `/senior-security` + `/senior-backend` reviewed · 2026-04-09*
*Státusz: IMPLEMENTÁCIÓRA KÉSZ — 16 finding beépítve (2 CRITICAL · 7 HIGH · 7 MEDIUM), minden döntés lezárva*
