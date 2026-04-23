---
id: MSG-JOINERY-011
from: root
to: joinery
type: task
priority: high
status: READ
ref: MSG-INFRA-130-BLOCKED
created: 2026-04-16
---

# JOINERY-011 — GUC bypass fix: DELETE /internal/orders/by-tenant

## Probléma

Az `InternalEndpoints.cs` `DELETE /internal/orders/by-tenant/{tenantId}` endpoint 500-as hibát dob:

```
PostgreSQL 22P02: invalid input syntax for type uuid: ""
```

**Root cause:** A `TenantSessionInterceptor` (`DbConnectionInterceptor`) a JWT `tid` claim-ből állítja be az `app.current_tenant_id` GUC-ot. Belső hívásokban nincs JWT → `tid = null` → `SET app.current_tenant_id = ''` → RLS UUID cast fail.

## Javítás

Az `InternalEndpoints.cs` endpoint handler-ben, **a repository hívás előtt**, állítsd be a GUC-ot manuálisan az URL paraméterből:

```csharp
// InternalEndpoints.cs — MapDelete handler-ben, a security gate-ek után:
// Inject: ApplicationDbContext dbContext (vagy JoineryDbContext)

// Set tenant context manually (no Bearer token in internal calls)
await dbContext.Database.ExecuteSqlRawAsync(
    $"SELECT set_config('app.current_tenant_id', '{tenantGuid}', false)");

var counts = await repo.DeleteByTenantAsync(tenantGuid, ct);
```

> `false` = session-level (nem transaction-local) — a connection lifetime-ra érvényes, ami a kérés végéig él.

Ha a DbContext nem volt injektálva az endpoint handler-be, add hozzá a paraméterlistához:
```csharp
async (string tenantId, bool? confirm, IJoineryRepository repo,
       JoineryDbContext dbContext, IConfiguration config, ILogger<InternalEndpoints> logger, CancellationToken ct) =>
```

## Tesztek

Az `InternalDeleteByTenantTests.cs` meglévő `Delete_ValidRequest_Returns200WithCounts` teszt már teszteli ezt a path-t. Ha az in-memory SQLite WebApplicationFactory-t használja (nem PostgreSQL), a 22P02 nem jelenik meg tesztekben — a deployed verifikáció az INFRA deploy után fog zöld lenni.

**Nincs szükség új tesztre** — a meglévő 6 teszt lefedi a logikát.

## DoD

- [ ] `dotnet build` → 0 error
- [ ] `dotnet test` → mind zöld (jelenlegi tesztszám változatlan vagy +)
- [ ] git commit + push (main branch)

## Outbox

DONE: `mailbox/joinery/outbox/2026-04-16_011_internal-delete-guc-fix-done.md`

## Skillek & Agentек

- `/senior-backend` — DbContext injection, set_config paraméteres hívás
- Sub-agenteket nyugodtan indíts
