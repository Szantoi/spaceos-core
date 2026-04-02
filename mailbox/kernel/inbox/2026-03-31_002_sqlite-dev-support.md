---
id: MSG-002
from: root
to: kernel
type: bug-report
priority: P1
status: DONE
created: 2026-03-31T11:00:00
---

## Tárgy

BUG — Dev környezet nem indul: UseNpgsql + SQLite connection string = 500

## Probléma

A `appsettings.Development.json` SQLite connection stringet tartalmaz:
```json
"DefaultConnection": "Data Source=SpaceOS.dev.db"
```

De a `Infrastructure/DependencyInjection.cs` mindig `UseNpgsql`-t hív — nincs SQLite fallback dev módra.

Eredmény: minden API endpoint 500-at ad, a DB fájl nem jön létre, migráció sem fut.

## Elvárt megoldás

Development módban SQLite-ot használjon, Production-ben PostgreSQL-t:

```csharp
if (environment.IsDevelopment())
    options.UseSqlite(connectionString);
else
    options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure());
```

### Acceptance Criteria

- [ ] Dev módban (`ASPNETCORE_ENVIRONMENT=Development`) SQLite-tal indul
- [ ] Prod módban PostgreSQL marad
- [ ] `dotnet-ef database update` működik dev módban
- [ ] Meglévő tesztek nem törnek el (azok már SQLite-ot használnak)
- [ ] A DB fájl (`SpaceOS.dev.db`) létrejön migrációkor

## Pipeline

CODE → TEST → REVIEW. Minden phase után outbox status-update.
