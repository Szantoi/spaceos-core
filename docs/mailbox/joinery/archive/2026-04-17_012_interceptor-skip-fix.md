---
id: MSG-JOINERY-012
from: root
to: joinery
type: task
priority: high
status: READ
ref: MSG-INFRA-133-BLOCKED
created: 2026-04-17
---

# JOINERY-012 — TenantSessionInterceptor skip fix + JoineryOutboxEntries migration

## Két probléma

### 1. GUC prioritás — TenantSessionInterceptor felülírja a handler fix-et

**Root cause (megerősítve INFRA-133 deploy után):**
A `TenantSessionInterceptor` a DELETE EF Core pipeline-ban `SET LOCAL app.tenant_id = ''`-t ad ki, ami felülírja a handler szinten beállított session értéket. A `SET LOCAL` tranzakció-szintű és prioritást élvez.

**Fix — az interceptorban, NOT az endpointban:**

```csharp
// TenantSessionInterceptor.cs — ConnectionOpened/ConnectionOpenedAsync-ban:
// Keresd meg ahol a SET LOCAL hívás van, és add hozzá az üres-check-et:

var tid = /* JWT claim kiolvasás */;
if (string.IsNullOrWhiteSpace(tid))
    return; // ← EZ A FIX: belső hívás (nincs JWT) → NE állítsd be a GUC-ot

await connection.ExecuteNonQueryAsync(
    $"SET LOCAL \"{PgConfigKey}\" = '{tid}'");
```

Ezután a **handler szintű `set_config`** érvényes marad, mert az interceptor nem írja felül.

A JOINERY-011 `InternalEndpoints.cs`-ben lévő `set_config` hívás maradhat — az lesz az aktív érték.

### 2. JoineryOutboxEntries tábla hiányzik

```
42P01: relation "JoineryOutboxEntries" does not exist
```

Az OutboxWorker 500-at dob és elrontja a DELETE endpoint HTTP response-át is.

**Ellenőrizd:** Van-e migration a `JoineryOutboxEntries` táblához a codebase-ben?

```bash
grep -r "JoineryOutboxEntries\|OutboxEntries" src/ --include="*.cs" | grep -i migration
```

- **Ha van migration:** Add hozzá az `app.MapInternalEndpoints()` elé a `db.Database.MigrateAsync()` hívást, VAGY hozd létre manuálisan a táblát a migration SQL alapján.
- **Ha nincs migration:** Hozd létre a migration-t (`dotnet ef migrations add AddOutboxEntries`), majd apply-old.
- **Ha az OutboxWorker nem kritikus a test reset-hez:** Tedd `try/catch`-be az OutboxWorker startup-ját, hogy ne crashelje az endpoint pipeline-t.

## DoD

- [ ] `dotnet build` → 0 error
- [ ] `dotnet test` → 219 zöld
- [ ] git commit + push (main)

## Outbox

DONE: `mailbox/joinery/outbox/2026-04-17_012_interceptor-skip-fix-done.md`

## Skillek & Agentек

- `/senior-backend` — interceptor null-check, EF Core migration
- Sub-agenteket nyugodtan indíts
