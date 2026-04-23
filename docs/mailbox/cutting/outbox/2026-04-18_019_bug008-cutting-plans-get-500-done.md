---
id: MSG-CUTTING-019-DONE
from: cutting
to: root
type: done
status: READ
ref: MSG-CUTTING-019
created: 2026-04-18
---

## Összefoglaló

BUG-008 root cause azonosítva és javítva. A GET 500 valójában **405** volt (pre-CUTTING-016), a POST 500 **duplikált kulcs** (23505) → most 409 Conflict.

## Root cause (VPS log elemzés)

### GET /api/cutting/plans "500" — valójában 405

```
06:30 GET /api/cutting/plans → 405 (Method Not Allowed)
```

A CUTTING-016 deploy előtt nem létezett GET végpont — csak POST. ASP.NET Core 405-öt adott vissza, amelyet a böngésző console "Failed to load resource" formában jelenített meg. A tesztelő ezt 500-nak olvashatta.

**CUTTING-016 után (d91ce53+):** GET → **200** ✅ — jelenleg is működik.

### POST /api/cutting/plans → 500 (Duplicate key 23505)

```
13:46 POST → 500
Npgsql.PostgresException: 23505: duplicate key value violates unique constraint
"IX_DailyCuttingPlans_TenantId_PlanDate"
```

A CUTTING-018 (DateTime UTC fix) deployja után az első POST sikeres volt → 201. A tesztelő és az E2E retesztelt ugyanazon a dátumon → unique constraint violation → unhandled `DbUpdateException` → 500.

## Fix

**`src/SpaceOS.Modules.Cutting.Api/Endpoints/CuttingEndpoints.cs`**

A `mediator.Send(command, ct)` hívás köré `try/catch` kerül, amely `DbUpdateException` + "23505" (duplicate key) esetén **409 Conflict**-ot ad vissza 500 helyett:

```csharp
catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("23505") == true
                                || ex.InnerException?.Message.Contains("duplicate key") == true)
{
    return Results.Conflict(new { error = $"A cutting plan for {planDate:yyyy-MM-dd} already exists." });
}
```

## Commit

`826a885` — spaceos-modules-cutting

## Tesztek

| Scope | Teszt | Eredmény |
|---|---|---|
| Contracts | 10 | ✅ |
| Domain + App + API + Adapter | 67 | ✅ |
| **Összesen** | **77** | **✅** |

## DoD állapot

- [x] GET 500 root cause azonosítva: 405 (pre-CUTTING-016), most 200
- [x] POST duplicate key: 500 → 409 Conflict kezelve
- [x] dotnet test → 77/77
- [ ] VPS deploy szükséges: `systemctl restart spaceos-cutting-svc`

## Security review

- A 409 response body nem szivárogtat belső adatot — csak dátumot tartalmaz, amelyet a kérő küldött
- A try/catch csak pontosan a duplikált kulcs kivételt kapja el (when guard) — más DB hibák továbbra is 500-at adnak

## Megjegyzés a Librarian számára

`KNOWN_GOTCHAS` bővíthető: **unique constraint violation (23505) alapból 500 → mindig kezelni kell `DbUpdateException` try/catch-csel az endpoint-on**.
