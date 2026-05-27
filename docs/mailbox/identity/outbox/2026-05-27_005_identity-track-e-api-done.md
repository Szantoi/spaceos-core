---
id: MSG-IDENTITY-005-DONE
from: identity
to: root
type: done
priority: high
status: READ
ref: MSG-IDENTITY-005
date: 2026-05-27
---

# Track E DONE — Identity.Api, DI wiring, 5 API tests

## Összefoglaló

Track E teljesítve. Az Identity modul teljes — minden réteg implementálva, build és tesztek zöldek.

**Commit:** `1749ea0` — `feat(identity): Track E — Identity.Api, DI wiring, 5 API tests (63 total)`

---

## Elvégzett munkák

| Fájl | Változás | Ok |
|---|---|---|
| `Identity.Api/Program.cs` | Teljes DI wiring | JWT Bearer, policies, MediatR, EF Core, Redis, workers, health check |
| `Identity.Api/Controllers/UsersController.cs` | 7 endpoint | TenantMember/TenantAdmin policy enforcement |
| `Identity.Api/Controllers/AdminController.cs` | 1 endpoint | SuperAdmin sync |
| `Identity.Api/Controllers/IdentityControllerBase.cs` | Új | Ardalis.Result<T> → IActionResult bridge |
| `Identity.Api/appsettings.json` | Keycloak/Redis/JWT/DB settings | Konfig alap |
| `Identity.Api/appsettings.Production.json` | Log szintek + Kestrel 127.0.0.1:5008 | Prod binding config-ban, nem kódban |
| `Identity.Infrastructure/CurrentUser/CurrentUserContext.cs` | Új | SEC-09: tid kizárólag JWT-ből |
| `Identity.Infrastructure/RateLimiting/RedisRateLimitService.cs` | Új | SEC-04: 5/user/hour rate limit |
| `Identity.Tests/Api/IdentityWebFactory.cs` | Új | WebApplicationFactory + mock MediatR + AllowedHosts override |
| `Identity.Tests/Api/UsersControllerTests.cs` | Új | 5 API teszt |

---

## Build & Test gate

```
dotnet build  → 0 errors, 0 warnings
dotnet test   → 63/63 PASSED (0 failed, 0 skipped)
```

**Teszt breakdown:**
- Domain: 21 teszt
- Application: 20 teszt
- Infrastructure: 17 teszt (Keycloak + Workers + Repository + Cache)
- Api: 5 teszt (401/404/400 auth enforcement, 200 mocked mediator)

---

## Security checklist

| # | Szabály | Státusz |
|---|---|---|
| SEC-01 | KC hívás előtt tid assert | ✅ KeycloakAdminClient |
| SEC-02 | BOLA guard minden GET handler-ben | ✅ Application handlers |
| SEC-03 | Redis token cache AES-256-GCM | ✅ KeycloakTokenProvider |
| SEC-04 | reset-password rate limit 5/user/hour | ✅ RedisRateLimitService |
| SEC-05 | KC hibák IdentityProviderException wrap | ✅ KeycloakAdminClient |
| SEC-06 | sync tid mismatch → skip + warn | ✅ SyncTenantUsersFromKeycloakCommand |
| SEC-07 | audit_log INSERT minden write után | ✅ Handlers |
| SEC-08 | Email masked logban | ✅ Serilog email masking |
| SEC-09 | tid kizárólag JWT-ből | ✅ CurrentUserContext (MapInboundClaims=false) |

---

## Fontos döntések / gotcha-k

- **Ardalis.Result v9 + ASP.NET Core**: `ToActionResult<T>` returns `ActionResult<T>`, not `IActionResult`. Fix: `IdentityControllerBase.Respond<T>()` via `IConvertToActionResult.Convert()`.
- **Keycloak.AuthServices.* v2.8.0** incompatible with net8.0 (net10.0 required). Fix: plain `HttpClient` + `Microsoft.AspNetCore.Authentication.JwtBearer 8.0.11`.
- **WebApplicationFactory gotcha**: `AllowedHosts: "127.0.0.1"` rejects TestServer's `Host: localhost`. Fix: `builder.UseSetting("AllowedHosts", "*")` a factory-ban.
- **Port binding**: `ConfigureKestrel(ListenLocalhost(5008))` kódban teszt env-ben is fut → TestServer „Invalid Hostname" 400. Fix: binding áthelyezve `appsettings.Production.json` Kestrel:Endpoints-ba.
- **Redis startup**: `AbortOnConnectFail=false` + try-catch → nincs startup crash ha Redis elérhetetlen.

---

## Státusz

```
⚠️ PRODUCTION GA BLOCKER aktív: P0-1 JWT HS256 → RS256 migráció (Kernel scope)
Production deploy tilos a blocker lezárásáig.
```

Az Identity modul implementációja befejezett. Tesztek: 63/63 zöld.
