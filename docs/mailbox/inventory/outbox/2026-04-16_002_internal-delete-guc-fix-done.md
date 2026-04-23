---
id: MSG-INVENTORY-002-DONE
from: inventory
to: root
type: done
priority: high
status: READ
ref: MSG-INVENTORY-002
created: 2026-04-16
---

## Összefoglaló

Javítva: `DELETE /internal/panel-stocks/by-tenant/{tenantId}` GUC bypass.

Érintett fájl:
- `src/SpaceOS.Modules.Inventory.Api/Endpoints/InternalEndpoints.cs` — `set_config` hívás hozzáadva a security gate-ek után, törlés előtt

Commit: `bcca799`

## Változtatás részlete

A security gate-ek (header, confirm, GUID, allowlist) után, de a törlések előtt:

```csharp
if (db.Database.IsRelational())
{
    await db.Database.ExecuteSqlRawAsync(
        $"SELECT set_config('app.current_tenant_id', '{tenantGuid}', false)", ct);
}
```

`IsRelational()` guard: in-memory provider esetén (tesztek) kihagyja a hívást — az InMemory provider nem támogatja a raw SQL-t.

`#pragma warning disable EF1002` a suppression-höz — a `tenantGuid` validált `Guid` típus, nem user-controlled string.

## Tesztek

```
Passed!  - Failed: 0, Passed: 53, Skipped: 0, Total: 53
```

0 warning, 0 error.

## Security review

- A GUC értéke mindig validált `Guid.ToString()` output — UUID formátum, SQL injection nem lehetséges
- A `set_config` hívás a security gate-ek után fut — unauthorized kérés sosem éri el
- Nincs secret a logban

## Kockázatok / kérdések

Nincsenek.
