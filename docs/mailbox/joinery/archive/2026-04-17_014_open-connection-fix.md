---
id: MSG-JOINERY-014
from: root
to: joinery
type: task
priority: high
status: READ
ref: MSG-INFRA-135-BLOCKED
created: 2026-04-17
---

# JOINERY-014 — InternalEndpoints OpenConnectionAsync connection affinity fix

## Root cause (megerősítve)

Az `ExecuteSqlRawAsync` (set_config) és `repo.DeleteAllByTenantAsync` különböző fizikai connection-t kapnak a poolból. Az OutboxWorker GUC = '' értékű connection-öket ad vissza a poolba → ezek versenyeznek → a DELETE üres GUC-ot lát → 22P02.

## Fix — `InternalEndpoints.cs`

Az endpoint handler-ben explicit nyitva kell tartani a connection-t a teljes delete művelet alatt:

```csharp
// BIZTONSÁGI GATE-EK UTÁN, de a delete előtt:

if (db.Database.IsRelational())
{
    await db.Database.OpenConnectionAsync(ct);
    await db.Database.ExecuteSqlRawAsync(
        $"SELECT set_config('{TenantGucKey}', {{0}}, false)",
        tenantGuid.ToString());
}

TenantDeletedCounts counts;
try
{
    counts = await repo.DeleteAllByTenantAsync(tenantGuid, ct);
}
finally
{
    if (db.Database.IsRelational())
        await db.Database.CloseConnectionAsync();
}

return Results.Ok(new { tenantId, deletedCounts = counts });
```

**Miért működik:** Ha a `DbContext` connection már explicit nyitva van, EF Core NEM nyit új connection-t a `DeleteAllByTenantAsync`-ban — ugyanazon a physikai connection-ön fut, ahol a `set_config` beállítva van.

## DoD

- [ ] `dotnet build` → 0 error
- [ ] `dotnet test` → 219 zöld
- [ ] git commit + push (main)

## Outbox

DONE: `mailbox/joinery/outbox/2026-04-17_014_open-connection-fix-done.md`

## Skillek & Agentек

- `/senior-backend` — DbContext OpenConnectionAsync, try/finally pattern
- Sub-agenteket nyugodtan indíts
