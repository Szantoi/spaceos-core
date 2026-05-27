---
id: MSG-IDENTITY-003-DONE
from: identity
to: root
type: done
priority: high
status: READ
ref: MSG-IDENTITY-003
created: 2026-05-27
---

# MSG-IDENTITY-003-DONE — Track C: Identity.Infrastructure / Persistence kész

## Összefoglaló

Track C teljes egészében implementálva. Commit: `012fef4`

**Létrehozott fájlok:**

| Fájl | Leírás |
|---|---|
| `Persistence/IdentityDbContext.cs` | `identity` schema, `ApplyConfigurationsFromAssembly`, `SetTenantContextAsync` (DB-05) |
| `Persistence/IdentityDbContextFactory.cs` | EF tools design-time factory |
| `Persistence/Configurations/SpaceOSUserConfiguration.cs` | UNIQUE(email,tenant_id), partial UNIQUE keycloak_user_id, idx_spaceos_users_tenant_status, partial idx_spaceos_users_kc_sync_status |
| `Persistence/Configurations/KcSyncOutboxConfiguration.cs` | shadow processed_at/last_attempt_at props |
| `Persistence/Configurations/IdentityAuditLogConfiguration.cs` | idx_audit_log_tenant_occurred DESC composite index |
| `Persistence/Repositories/SpaceOSUserRepository.cs` | `ISpaceOSUserRepository` — SET LOCAL RLS, AsNoTracking reads, tranzakció minden write-on |
| `Persistence/Repositories/KcSyncOutboxRepository.cs` | `IKcSyncOutboxRepository` + `IKcSyncOutboxProcessor` impl |
| `Persistence/Migrations/20260527000000_InitialSchema.cs` | Kézi SQL migration: schema, 3 tábla, RLS policy, triggerek, identity_app role |
| `Persistence/Migrations/IdentityDbContextModelSnapshot.cs` | EF snapshot |
| `Tests/Infrastructure/CrossTenantAccessTests.cs` | 4 cross-tenant izoláció teszt |

## Tesztek

```
Passed!  - Failed: 0, Passed: 58, Skipped: 0, Total: 58, Duration: 614 ms
```

4 új CrossTenantAccess teszt:
- `ListTenantUsers_TenantAToken_ReturnsOnlyTenantAUsers`
- `GetUserById_TenantAToken_TenantBUserId_ReturnsForbidden` (BOLA)
- `ListTenantUsers_EmptyTenant_ReturnsEmptyList`
- `SpaceOSUserRepository_DeleteNotExposed_InvariantCheck`

## Definition of Done checklist

- [x] `0001_InitialSchema` up + down migration — kézileg írva, érvényes EF Core migration
- [x] `identity.spaceos_users`, `identity.kc_sync_outbox`, `identity.audit_log` DDL kész
- [x] RLS policy definálva: `tenant_id = current_setting('app.current_tenant_id')::UUID`
- [x] Partial UNIQUE `keycloak_user_id WHERE NOT NULL`
- [x] `idx_kc_sync_outbox_unprocessed` partial index `WHERE processed_at IS NULL`
- [x] `dotnet build` → 0 error, 0 warning
- [x] `dotnet test` → 58/58 zöld

## Security review

- DB-05: `SET LOCAL app.current_tenant_id` minden mutating repository metódusban (BeginTransaction → SetTenantContextAsync → SaveChanges → CommitTransaction)
- RLS policy aktív + FORCE ROW LEVEL SECURITY — még a táblatulajdonos sem kerüli el
- `identity_app` role: DELETE REVOKED on `spaceos_users` (DDL-ben, role creation IF NOT EXISTS)
- `ISpaceOSUserRepository` interfészen nincs Delete metódus — interface-szintű kényszer

## ⚠️ Megjegyzés — dotnet-ef tool inkompatibilitás

A szerverén telepített `dotnet-ef` tool v10.0.7 **.NET 8.0.25** runtimehoz van kötve, ami nincs telepítve. A migrációt kézzel írtam raw SQL-lel — ez funkcionálisan ekvivalens.

**Javasolt döntés root-nak:** INFRA terminál telepítse `dotnet-ef 8.x` globális tool-t a migration workflow-hoz.

## Kockázatok / kérdések

Nincsenek blokkolók. `dotnet ef database update` a VPS deploy előtt INFRA feladata.
