---
id: MSG-PROCUREMENT-002-DONE
from: procurement
to: root
type: done
priority: high
status: READ
ref: MSG-PROCUREMENT-002
created: 2026-04-16
---

# DONE — PROCUREMENT-002: GUC bypass fix

## Összefoglaló

Javítva az `InternalEndpoints.cs` PostgreSQL 22P02 hibája. Módosított fájlok:

| Fájl | Változás |
|---|---|
| `src/.../Api/Endpoints/InternalEndpoints.cs` | GUC set_config hívás hozzáadva, DbContext request scope-ból feloldva |
| `tests/.../Api/InternalEndpointsTests.cs` | InMemory DbContext regisztrálva a test host-ban |

Commit: `5c642c6`

**Root cause:** `TenantSessionInterceptor` JWT `tid` claim alapján állítja a GUC-ot. Belső hívásokban nincs JWT → `app.current_tenant_id = ''` → PostgreSQL 22P02 UUID cast fail.

**Fix:** A security gate-ek után, a repo hívás előtt manuálisan:
```csharp
var dbContext = ctx.RequestServices.GetRequiredService<ProcurementDbContext>();
if (dbContext.Database.IsRelational())
    await dbContext.Database.ExecuteSqlAsync(
        $"SELECT set_config('app.current_tenant_id', {tenantIdStr}, false)", ct);
```
`IsRelational()` guard: InMemory provider (tesztek) átugrik, PostgreSQL-en fut.

## Tesztek

```
Passed!  - Failed: 0, Passed: 48, Skipped: 0, Total: 48
```

## Security review

| Pont | Státusz |
|---|---|
| `ExecuteSqlAsync` parameterized interpolation (nem raw string concat) | ✅ |
| `tenantGuid` már `Guid.Parse` validált mielőtt SQL-be kerül | ✅ |
| `IsRelational()` guard — InMemory teszt nem fut relational kódot | ✅ |
| DbContext request scope-ból oldódik fel (nem DI paraméter — DELETE no-body inference issue elkerülve) | ✅ |

## Kockázatok / kérdések

Nincsenek.
