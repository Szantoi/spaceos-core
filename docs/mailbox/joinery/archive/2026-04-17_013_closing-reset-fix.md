---
id: MSG-JOINERY-013
from: root
to: joinery
type: task
priority: high
status: READ
ref: MSG-INFRA-134-BLOCKED
created: 2026-04-17
---

# JOINERY-013 — ConnectionClosingAsync reset fix + OutboxWorker schema prefix

## Két probléma

### 1. ConnectionClosingAsync — GUC reset belső hívásban

**Root cause:** Az `OpenConnectionAsync`-ban javítottuk a null-check-et, de a `ConnectionClosingAsync` még mindig mindig üres stringre reseteli a GUC-ot. Belső hívásnál a `ExecuteSqlRawAsync` és a `DeleteAllByTenantAsync` különböző connection lifetime-on fut (pool) — a reset a két hívás között történik.

**Fix — `TenantSessionInterceptor.cs` `ConnectionClosingAsync`-ban, ugyanolyan null-check:**

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

### 2. JoineryOutboxWorker — schema prefix hiánya

```
42P01: relation "JoineryOutboxEntries" does not exist
```

A `JoineryOutboxWorker` raw SQL-je sémaelőtag nélkül hivatkozik a táblára — PostgreSQL `public."JoineryOutboxEntries"`-t keres, de a tábla a `spaceos_joinery` sémában van.

**Fix — a raw SQL hivatkozásban:**
```csharp
// ELŐTTE:
.FromSqlRaw(@"SELECT * FROM ""JoineryOutboxEntries"" WHERE ...")

// UTÁNA:
.FromSqlRaw(@"SELECT * FROM spaceos_joinery.""JoineryOutboxEntries"" WHERE ...")
```

Ha több helyen szerepel a `JoineryOutboxEntries` táblanév a `JoineryOutboxWorker.cs`-ben, mindenhol javítsd.

## DoD

- [ ] `dotnet build` → 0 error
- [ ] `dotnet test` → 219 zöld
- [ ] git commit + push (main)

## Outbox

DONE: `mailbox/joinery/outbox/2026-04-17_013_closing-reset-fix-done.md`

## Skillek & Agentек

- `/senior-backend` — interceptor ConnectionClosingAsync, raw SQL schema prefix
- Sub-agenteket nyugodtan indíts
