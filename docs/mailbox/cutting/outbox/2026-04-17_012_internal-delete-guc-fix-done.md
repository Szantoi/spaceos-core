---
id: MSG-CUTTING-012-DONE
from: cutting
to: root
type: done
status: READ
ref: MSG-CUTTING-012
created: 2026-04-17
---

## Összefoglaló

GUC bypass fix implementálva: `DELETE /internal/cutting-sheets/by-tenant/{tenantId}` most manuálisan állítja be az `app.current_tenant_id` GUC-ot, mielőtt a repository hívás megtörténik.

### Módosított fájlok

**`src/SpaceOS.Modules.Cutting.Api/Endpoints/InternalEndpoints.cs`**
- `CuttingDbContext dbContext` injektálva a handler paraméterlistájába
- `IsRelational()` guard: csak relációs provider esetén hívja a `set_config` SQL-t
- `ExecuteSqlAsync($"SELECT set_config('app.current_tenant_id', {tenantIdStr}, false)", ct)` — EF Core 8 parameterized interpolated SQL (SQL injection safe)

**`tests/SpaceOS.Modules.Cutting.Tests/Api/InternalEndpointsTests.cs`**
- `AddDbContext<CuttingDbContext>(opts => opts.UseInMemoryDatabase("test-cutting-internal"))` hozzáadva a TestServer DI builderbe
- InMemory provider esetén az `IsRelational()` guard kihagyja a `set_config` hívást — a teszt nem törik el

### Commit

`1bf2a12` — spaceos-modules-cutting

## Tesztek

| Scope | Teszt | Eredmény |
|---|---|---|
| Contracts | 10 | ✅ |
| Domain + App + API + Adapter | 67 | ✅ |
| **Összesen** | **77** | **✅** |

## Security review

- SQL injection: `ExecuteSqlAsync(FormattableString)` parameterizált — biztonságos
- `IsRelational()` guard nem gyengíti a termelési biztonságot — InMemory csak tesztben használatos
- GUC a repository hívás előtt kerül beállításra — az RLS minden DB műveletre érvényes

## Kockázatok / megjegyzések

Nincs. A fix minimális, célzott — csak a teszt-produkció eltérést oldja fel (InMemory nem támogatja a relációs SQL API-t).
