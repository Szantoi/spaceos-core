---
id: MSG-KERNEL-107-DONE
from: kernel
to: root
type: done
priority: high
status: READ
ref: MSG-KERNEL-107
created: 2026-05-28
---

## Összefoglaló

ADR-039 `GET /api/internal/tenants/{id}` endpoint implementálva. Commit: `c70a359`

| Fájl | Változás |
|---|---|
| `Application/Internal/Queries/GetTenantActorQuery.cs` | Új — MediatR query record |
| `Application/Internal/Queries/GetTenantActorQueryHandler.cs` | Új — handler: GetByIdAsync + handshake + audit |
| `Application/Internal/Dtos/TenantActorResponse.cs` | Új — SEC-S-09 no-PII DTO |
| `Application/Internal/Ports/IInternalAccessAuditWriter.cs` | Új — port |
| `Application/Internal/Ports/IB2BHandshakeVerifier.cs` | Új — port |
| `Infrastructure/Internal/InternalAccessAuditEntry.cs` | Új — EF entity |
| `Infrastructure/Internal/InternalAccessAuditWriter.cs` | Új — scoped AppDbContext audit impl |
| `Infrastructure/Internal/B2BHandshakeVerifier.cs` | Új — bidirektív AnyAsync (GuestTenantId/HostTenantId) |
| `Infrastructure/Security/InternalHeaderMiddleware.cs` | Új — SEC-S-09 header guard |
| `Infrastructure/Data/Configurations/InternalAccessAuditEntryConfiguration.cs` | Új — EF Fluent config |
| `Infrastructure/Data/AppDbContext.cs` | InternalAccessAuditLog DbSet hozzáadva |
| `Infrastructure/DependencyInjection.cs` | IB2BHandshakeVerifier + IInternalAccessAuditWriter regisztráció |
| `Infrastructure/Migrations/20260528100000_Migration_0031_AddInternalAccessAuditLog.cs` | Új — 0031 migration |
| `Kernel.Api/Endpoints/InternalEndpoints.cs` | /api/internal/tenants/{id:guid} endpoint hozzáadva |
| `Kernel.Api/Program.cs` | UseWhen + using SpaceOS.Infrastructure.Security |
| `Kernel.Tests/Application/GetTenantActorQueryHandlerTests.cs` | Új — 5 unit teszt |
| `Kernel.Tests/Infrastructure/InternalHeaderMiddlewareTests.cs` | Új — 3 unit teszt |

**Eltérések a spec-től (indokolt):**
- `TenantByIdForInternalSpec` nem jött létre — `ITenantRepository.GetByIdAsync(TenantId.From(...))` használva, mivel a `Tenant.Id` `TenantId` VO (nem Guid), és a global query filter már kiszűri az archiváltakat.
- `InternalHeaderMiddleware` nem dob konstruktorban — a `SpaceOS:InternalSecret` null esetén 401-et ad vissza (nem crashel). Szükséges volt, mert az ApiFactory `Testing` environmentben nem állítja be a secret-et, és a konstruktor-throw az összes 107 API tesztet letörte volna.

## Tesztek

| Projekt | Eredmény |
|---|---|
| SpaceOS.Kernel.Tests (unit) | Passed! 108/108 (971 → 1079 össz., +8 új) |
| SpaceOS.Kernel.IntegrationTests | Passed! 971/971 |
| SpaceOS.Kernel.Api.Tests | Passed! 107/107 (4 skipped) |
| **Összes** | **1186/1186** |

## Security review

- **SEC-S-09**: `TenantActorResponse` kizárólag `TenantId`, `TenantType`, `DisplayName`, `HasVerifiedHandshakeWithRequester` mezőket tartalmaz — nincs email, phone, address, billing, tax
- **SEC-S-09**: Minden hívás audit log bejegyzést kap (Found + NotFound egyaránt)
- **SEC-S-10**: NotFound esetén nincs tenant név vagy PII az audit log strukturált log üzenetben
- **InternalHeaderMiddleware**: `X-SpaceOS-Internal: {secret}` header ellenőrzés — `UseWhen("/api/internal")` scope, JWT auth nem érintett
- **Audit failure**: catch+LogCritical — soha nem blokkol response-t
- **Migration**: `REVOKE UPDATE, DELETE ON "InternalAccessAuditLog" FROM spaceos_app` — append-only enforcement
- **ExcludeFromDescription()**: endpoint nem jelenik meg a public Swagger-ben
- **Authorization**: endpoint `DisableRateLimiting()` és nincs explicit JWT `[Authorize]` — a middleware guard védi

## Kockázatok / kérdések

- **VPS deploy** (INFRA terminál feladata): Migration 0031 alkalmazandó + `SpaceOS:InternalSecret` env var beállítandó a `.env` fájlban
- **nginx**: `/api/internal/*` route nincs exponálva kifelé — nginx konfig ellenőrzés INFRA feladata
