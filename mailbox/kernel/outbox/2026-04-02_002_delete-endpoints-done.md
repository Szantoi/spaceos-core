---
id: MSG-K012-DONE
from: kernel
to: root
type: status-update
ref: MSG-K012
status: DONE
created: 2026-04-02T11:30:00
---

## Összefoglalás

MSG-K012 végrehajtva. Soft delete (archiválás) implementálva mind az 5 aggregátumra.

## Változtatások

| Fájl | Változtatás |
|---|---|
| `Api/Extensions/ResultExtensions.cs` | `NoContent → 204` és `Conflict → 409` hozzáadva mindkét overloadhoz |
| `Domain/Events/Tenant|Facility|WorkStation|SpaceLayer|FlowEpicArchivedEvent.cs` | 5 új domain event (readonly record struct) |
| `Domain/Entities/Tenant|Facility|WorkStation|SpaceLayer|FlowEpic.cs` | `IsArchived { get; private set; }` + `Archive()` metódus + DomainException ha már archivált |
| `Infrastructure/Data/AppDbContext.cs` | 4 meglévő filter kibővítve `&& !e.IsArchived`-del; Tenant-hez új filter `!t.IsArchived` |
| `Application/{Entity}/Commands/Archive{Entity}Command.cs` | 5 új command record |
| `Application/{Entity}/Commands/Archive{Entity}CommandValidator.cs` | 5 új validator (Id NotEmpty) |
| `Application/{Entity}/Commands/Archive{Entity}CommandHandler.cs` | 5 új handler: 404 ha nem létezik, 409 (Conflict) ha már archivált, 204 sikeren |
| `Api/Endpoints/{Entity}Endpoints.cs` | 5 endpoint: `DELETE /{id:guid}` → 204/404/409 |

## Döntések

- **409 Conflict**: `DomainException` az `Archive()` metódusból catch-elve → `Result.Conflict()` → 409
- **204 No Content**: handler `Result.NoContent()` → `ResultExtensions` → `Results.NoContent()`
- **Authorization**: `.RequireAuthorization()` (policy nélkül) a feladat spec szerint
- **Rate limiting**: `.RequireRateLimiting("sliding")` (mutáló endpoint)

## Teszteredmény

```
Passed: 357, Failed: 0, Skipped: 0
Build: 0 errors, 0 warnings
```
