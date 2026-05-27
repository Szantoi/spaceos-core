---
id: MSG-IDENTITY-003
from: root
to: identity
type: task
priority: high
status: UNREAD
ref: MSG-IDENTITY-001-DONE
created: 2026-05-27
skill: /spaceos-terminal
agents: enabled
subagents: enabled
---

# MSG-IDENTITY-003 — Track C: Identity.Infrastructure / Persistence

## Kontextus

Track A (Domain) elfogadva. Track C párhuzamosan futhat Track B-vel és D-vel.

Spec: `/opt/spaceos/docs/tasks/active/IDENTITY-V1_modules-identity.md` — §3, §5

## Feladat

Implementáld az **`Identity.Infrastructure/Persistence`** réteget: EF Core, Migration, RLS, Repository.

### NuGet csomagok

```bash
cd Identity.Infrastructure
dotnet add package Microsoft.EntityFrameworkCore --version 8.0.11
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.11
dotnet add package Ardalis.Specification.EntityFrameworkCore --version 9.1.0
dotnet add package Microsoft.Extensions.DependencyInjection.Abstractions --version 8.0.2
```

### 1. `IdentityDbContext.cs`

- `HasDefaultSchema("identity")`
- `ApplyConfigurationsFromAssembly(...)`
- Minden mutating metódus előtt: `SET LOCAL app.current_tenant_id = {0}` (DB-05 javítás)

### 2. Configurations/

**`SpaceOSUserConfiguration.cs`** — spec §3.1 és §5 alapján:
- `kc_sync_status` VARCHAR(20), HasConversion<string>()
- `keycloak_user_id` nullable, partial UNIQUE WHERE NOT NULL
- `UNIQUE(email, tenant_id)` composite constraint
- `idx_spaceos_users_tenant_status` composite index
- `idx_spaceos_users_kc_sync_status` partial index (csak Pending/Failed)
- Self-ref FK: `created_by_user_id → id ON DELETE SET NULL`

**`KcSyncOutboxConfiguration.cs`**
- `idx_kc_sync_outbox_unprocessed` partial index WHERE `processed_at IS NULL`

**`IdentityAuditLogConfiguration.cs`**
- `idx_audit_log_tenant_occurred` composite index (tenant_id, occurred_at DESC)

### 3. Migration: `0001_InitialSchema`

```bash
# EF Core tools telepítve van a szerveren
dotnet ef migrations add InitialSchema --project Identity.Infrastructure --startup-project Identity.Api
```

A migration **UP** tartalmazza (spec §3.1 teljes DDL):
- `CREATE SCHEMA IF NOT EXISTS identity`
- 3 tábla: `spaceos_users`, `kc_sync_outbox`, `audit_log`
- Összes constraint, index, trigger (`set_updated_at`), RLS policy
- `identity_app` role: DELETE **TILTVA** a `spaceos_users` táblán

A migration **DOWN** tartalmazza:
- `DROP TABLE` fordított sorrendben
- `DROP FUNCTION set_updated_at()`
- `DROP SCHEMA identity`

### 4. `SpaceOSUserRepository.cs`

Implementálja `ISpaceOSUserRepository`-t:
- `GetByIdAsync(id, tenantId, ct)` — AsNoTracking + TenantUserByIdSpec
- `ListByTenantAsync(tenantId, status?, ct)` — AsNoTracking + TenantUsersByStatusSpec
- `AddAsync(user, ct)` — SET LOCAL + SaveChangesAsync
- `UpdateAsync(user, ct)` — SET LOCAL + SaveChangesAsync

### 5. Integration teszt — `Identity.Tests/Infrastructure/`

**`CrossTenantAccessTests.cs`** (spec DoD: kötelező):
- Két tenant, mindkettőhöz user → tenant A tokennel csak tenant A userét látja
- `identity_app` role DELETE tiltva — teszt bizonyítja (nem kötelező Testcontainerrel, mock is elfogadható ha az invariáns tesztelve)

## Definition of Done

Spec §7 Migration gates:
- [ ] `0001_InitialSchema` up + down migration zöld (`dotnet ef database update`)
- [ ] `identity.spaceos_users`, `identity.kc_sync_outbox`, `identity.audit_log` létezik
- [ ] RLS policy aktív — cross-tenant query 0 sort ad vissza
- [ ] Partial UNIQUE `keycloak_user_id` — több NULL beilleszthető, két azonos KC ID nem
- [ ] `idx_kc_sync_outbox_unprocessed` partial index létezik
- [ ] `dotnet build` → 0 error, 0 warning
- [ ] `dotnet test` → minden teszt zöld

## Megjegyzés

- A VPS-en fut PostgreSQL — a migration teszteléséhez a `spaceos` DB-t használd, vagy hozz létre egy `spaceos_identity_test` DB-t
- `dotnet ef` tool telepítve van: `/root/.dotnet/tools/dotnet-ef`
- Ha Testcontainers kellene: ne add hozzá, mock repositoryval teszteld az invariánsokat
