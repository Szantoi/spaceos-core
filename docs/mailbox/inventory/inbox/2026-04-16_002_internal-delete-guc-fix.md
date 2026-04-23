---
id: MSG-INVENTORY-002
from: root
to: inventory
type: task
priority: high
status: READ
ref: MSG-INFRA-130-BLOCKED
created: 2026-04-16
---

# INVENTORY-002 — GUC bypass fix: DELETE /internal/panel-stocks/by-tenant

## Probléma

Az `InternalEndpoints.cs` `DELETE /internal/panel-stocks/by-tenant/{tenantId}` endpoint 500-as hibát dob:

```
PostgreSQL 22P02: invalid input syntax for type uuid: ""
```

**Root cause:** A `TenantSessionInterceptor` (`DbConnectionInterceptor`) a JWT `tid` claim-ből állítja be az `app.current_tenant_id` GUC-ot. Belső hívásokban nincs JWT → `tid = null` → `SET app.current_tenant_id = ''` → RLS UUID cast fail.

## Javítás

Az `InternalEndpoints.cs` endpoint handler-ben, **a repository hívás előtt**, állítsd be a GUC-ot manuálisan az URL paraméterből:

```csharp
// InternalEndpoints.cs — MapDelete handler-ben, a security gate-ek után:
// Inject: InventoryDbContext dbContext

// Set tenant context manually (no Bearer token in internal calls)
await dbContext.Database.ExecuteSqlRawAsync(
    $"SELECT set_config('app.current_tenant_id', '{tenantGuid}', false)");

var counts = await repo.DeleteByTenantAsync(tenantGuid, ct);
```

Ha a DbContext nem volt injektálva az endpoint handler-be, add hozzá a paraméterlistához.

## DoD

- [ ] `dotnet build` → 0 error
- [ ] `dotnet test` → 53 zöld (változatlan)
- [ ] git commit + push (main branch)

## Outbox

DONE: `mailbox/inventory/outbox/2026-04-16_002_internal-delete-guc-fix-done.md`

## Skillek & Agentек

- `/senior-backend` — DbContext injection, set_config
- Sub-agenteket nyugodtan indíts
