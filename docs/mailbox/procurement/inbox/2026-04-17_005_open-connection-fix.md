---
id: MSG-PROCUREMENT-005
from: root
to: procurement
type: task
priority: high
status: READ
ref: MSG-INFRA-135-BLOCKED
created: 2026-04-17
---

# PROCUREMENT-005 — InternalEndpoints OpenConnectionAsync connection affinity fix

## Root cause (megerősítve)

Az `ExecuteSqlRawAsync` (set_config) és `repo.DeleteByTenantAsync` különböző fizikai connection-t kapnak a poolból → pool GUC szennyezés → 22P02.

## Fix — `InternalEndpoints.cs`

```csharp
if (db.Database.IsRelational())
{
    await db.Database.OpenConnectionAsync(ct);
    await db.Database.ExecuteSqlRawAsync(
        $"SELECT set_config('app.current_tenant_id', {{0}}, false)",
        tenantGuid.ToString());
}

(int purchaseOrders, int deliveries) counts;
try
{
    counts = await repo.DeleteByTenantAsync(tenantGuid, ct);
}
finally
{
    if (db.Database.IsRelational())
        await db.Database.CloseConnectionAsync();
}

return Results.Ok(new { tenantId, deletedCounts = counts });
```

## DoD

- [ ] `dotnet build` → 0 error
- [ ] `dotnet test` → 48 zöld
- [ ] git commit + push (main) — ha nincs remote: lokális commit

## Outbox

DONE: `mailbox/procurement/outbox/2026-04-17_005_open-connection-fix-done.md`

## Skillek & Agentек

- `/senior-backend` — DbContext OpenConnectionAsync, try/finally
- Sub-agenteket nyugodtan indíts
