---
id: MSG-JOINERY-011-DONE
from: joinery
to: root
type: response
priority: high
status: READ
ref: MSG-JOINERY-011
created: 2026-04-16
---

# MSG-JOINERY-011-DONE — GUC bypass fix: DELETE /internal/orders/by-tenant

## Összefoglaló

Fix implementálva. Commit: `874fd21`

---

## Root cause (megerősítve)

`TenantSessionInterceptor` a JWT `tenant_id` claim-ből állítja be az `app.tenant_id` GUC-ot. Belső hívásnál nincs JWT → `string.Empty` → `SET app.tenant_id = ''` → RLS policy UUID cast fail (`22P02`).

**Megjegyzés a task leíráshoz:** A GUC neve `app.tenant_id` (nem `app.current_tenant_id` ahogy a task mondta) — az interceptor `TenantSessionInterceptor.PgConfigKey` konstans alapján ellenőrizve.

## Implementált fix

```csharp
// InternalEndpoints.cs — security gate-ek és allowlist ellenőrzés után:
if (db.Database.IsRelational())
{
    await db.Database.ExecuteSqlRawAsync(
        $"SELECT set_config('{TenantGucKey}', {{0}}, false)",
        tenantGuid.ToString())
        .ConfigureAwait(false);
}
```

- `TenantGucKey = "app.tenant_id"` — konstans, konzisztens az interceptorral
- `{0}` paraméteres — SQL injection-safe (EF Core paraméteres syntax)
- `IsRelational()` guard — InMemory tesztek nem érintettek
- `false` (session-level) — connection lifetime-ra érvényes, nem transaction-local

## Build & Test

```
dotnet build  → 0 error, 0 warning
dotnet test   → 219/219 passed
```

Commit: `874fd21 fix(joinery): GUC bypass for internal delete endpoint (JOINERY-011)`
