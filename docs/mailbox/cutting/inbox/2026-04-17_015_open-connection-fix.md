---
id: MSG-CUTTING-015
from: root
to: cutting
type: task
priority: high
status: READ
ref: MSG-INFRA-135-BLOCKED
created: 2026-04-17
---

# CUTTING-015 — InternalEndpoints OpenConnectionAsync connection affinity fix

## Root cause (megerősítve)

Az `ExecuteSqlRawAsync` (set_config) és `repo.DeleteByTenantAsync` különböző fizikai connection-t kapnak a poolból → OutboxWorker GUC = '' pool szennyezés → 22P02.

## Fix — `InternalEndpoints.cs`

```csharp
if (db.Database.IsRelational())
{
    await db.Database.OpenConnectionAsync(ct);
    await db.Database.ExecuteSqlAsync(
        $"SELECT set_config('{TenantGucKey}', {tenantIdStr}, false)", ct);
}

(int sheets, int plans) counts;
try
{
    counts = await repo.DeleteByTenantAsync(tenantGuid, ct);
}
finally
{
    if (db.Database.IsRelational())
        await db.Database.CloseConnectionAsync();
}

return Results.Ok(new { tenantId, deletedCounts = new { cuttingSheets = counts.sheets, dailyCuttingPlans = counts.plans } });
```

## DoD

- [ ] `dotnet build` → 0 error
- [ ] `dotnet test` → 77 zöld
- [ ] git commit + push (main)

## Outbox

DONE: `mailbox/cutting/outbox/2026-04-17_015_open-connection-fix-done.md`

## Skillek & Agentек

- `/senior-backend` — DbContext OpenConnectionAsync, try/finally
- Sub-agenteket nyugodtan indíts
