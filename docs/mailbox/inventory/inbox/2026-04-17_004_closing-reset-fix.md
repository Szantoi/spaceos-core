---
id: MSG-INVENTORY-004
from: root
to: inventory
type: task
priority: high
status: READ
ref: MSG-INFRA-134-BLOCKED
created: 2026-04-17
---

# INVENTORY-004 — ConnectionClosingAsync reset fix

## Root cause (megerősítve)

Az `OpenConnectionAsync` null-check fix után a `ConnectionClosingAsync` még mindig mindig üres stringre reseteli a GUC-ot. A `ExecuteSqlRawAsync` (set_config) és a `DeleteByTenantAsync` különböző connection lifetime-on fut — a reset a két hívás között történik → 22P02.

## Fix — `TenantSessionInterceptor.cs` `ConnectionClosingAsync`:

```csharp
public override async ValueTask<InterceptionResult> ConnectionClosingAsync(
    DbConnection connection, ConnectionEventData eventData, InterceptionResult result,
    CancellationToken ct = default)
{
    var tenantId = ResolveTenantId();
    if (string.IsNullOrWhiteSpace(tenantId))
    {
        // Belső hívás (nincs JWT) — ne reseteld a GUC-ot
        return await base.ConnectionClosingAsync(connection, eventData, result, ct).ConfigureAwait(false);
    }
    await SetConfigAsync(connection, PgConfigKey, string.Empty, ct).ConfigureAwait(false);
    return await base.ConnectionClosingAsync(connection, eventData, result, ct).ConfigureAwait(false);
}
```

## DoD

- [ ] `dotnet build` → 0 error
- [ ] `dotnet test` → 53 zöld
- [ ] git commit + push (main)

## Outbox

DONE: `mailbox/inventory/outbox/2026-04-17_004_closing-reset-fix-done.md`

## Skillek & Agentек

- `/senior-backend` — interceptor ConnectionClosingAsync null-check
- Sub-agenteket nyugodtan indíts
