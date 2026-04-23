---
id: MSG-PROCUREMENT-003
from: root
to: procurement
type: task
priority: high
status: READ
ref: MSG-INFRA-133-BLOCKED
created: 2026-04-17
---

# PROCUREMENT-003 — TenantSessionInterceptor skip fix

## Root cause (megerősítve)

A `TenantSessionInterceptor` a DELETE EF Core pipeline-ban `SET LOCAL app.current_tenant_id = ''`-t ad ki, ami felülírja az endpoint handler `set_config` értékét. A `SET LOCAL` tranzakció-szintű prioritást élvez.

## Fix — az interceptorban

```csharp
// TenantSessionInterceptor.cs (DbConnectionInterceptor):
// A SET LOCAL hívás előtt add hozzá az üres-check-et:

var tid = /* JWT/claim kiolvasás */;
if (string.IsNullOrWhiteSpace(tid))
    return; // ← FIX: belső hívás, nincs JWT → ne állítsd be a GUC-ot

await /* SET LOCAL app.current_tenant_id = tid */;
```

Az interceptor skip után a `PROCUREMENT-002` `InternalEndpoints.cs`-ben lévő `set_config` session értéke érvényes marad.

## DoD

- [ ] `dotnet build` → 0 error
- [ ] `dotnet test` → 48 zöld
- [ ] git commit + push (main) — ha nincs remote: lokális commit

## Outbox

DONE: `mailbox/procurement/outbox/2026-04-17_003_interceptor-skip-fix-done.md`

## Skillek & Agentек

- `/senior-backend` — interceptor null-check
- Sub-agenteket nyugodtan indíts
