---
id: MSG-JOINERY-013-DONE
from: joinery
to: root
type: response
priority: high
status: READ
ref: MSG-JOINERY-013
created: 2026-04-17
---

# MSG-JOINERY-013-DONE — ConnectionClosingAsync reset fix + OutboxWorker schema prefix

## Összefoglaló

### 1. TenantSessionInterceptor.ConnectionClosingAsync — null-check hozzáadva

**Fix:** `ResolveTenantId()` null/empty esetén skip a reset:
```csharp
var tenantId = ResolveTenantId();
if (string.IsNullOrWhiteSpace(tenantId))
    return await base.ConnectionClosingAsync(connection, eventData, result).ConfigureAwait(false);

await SetConfigAsync(connection, PgConfigKey, string.Empty, CancellationToken.None).ConfigureAwait(false);
return await base.ConnectionClosingAsync(connection, eventData, result).ConfigureAwait(false);
```

Az interceptor most szimmetrikus: belső hívásban sem Open, sem Close nem nyúl a GUC-hoz.

### 2. JoineryOutboxWorker — sémaelőtag javítva

**Fix:** `spaceos_joinery."JoineryOutboxEntries"` → teljes sémaqualifikált hivatkozás a `FromSqlRaw`-ban.

`FOR UPDATE SKIP LOCKED` query érintett — egyetlen hivatkozás volt, javítva.

## Build & Test

```
dotnet build  → 0 error, 0 warning
dotnet test   → 219/219 passed
```

Commit: `691be99 fix(joinery): ConnectionClosingAsync null-check + OutboxWorker schema prefix (JOINERY-013)`

## Security review

- GUC reset skip csak belső hívásokra — JWT-vel érkező rendes kérések továbbra is nullázzák a GUC-ot pool visszaadáskor (biztonságos)
- Schema prefix: security-neutral, csak elérhetőségi fix
