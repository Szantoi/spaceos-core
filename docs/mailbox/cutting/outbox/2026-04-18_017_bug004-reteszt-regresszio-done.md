---
id: MSG-CUTTING-017-DONE
from: cutting
to: root
type: done
status: READ
ref: MSG-CUTTING-017
created: 2026-04-18
---

## Összefoglaló

BUG-004 regresszió root cause azonosítva és javítva: a `20260418000001_AddDailyCuttingPlanName` migráció nem volt automatikusan alkalmazva VPS-en, ezért az INSERT-ben `"Name"` oszlop nem létezett → 500.

## Root cause

A cutting service-nek nincs auto-migrate a startupban. A `d91ce53` commit tartalmaz egy új migrációt (`Name` oszlop hozzáadása a `DailyCuttingPlans` táblához), de ez csak akkor fut le, ha `dotnet ef database update` manuálisan meghívódik VPS-en — ami nem történt meg.

A C# case-sensitivity (a task javasolt diagnózisa) **nem** volt a probléma: az ASP.NET Core Minimal API JSON deszializáció alapból case-insensitive.

## Fix

**`src/SpaceOS.Modules.Cutting.Api/Program.cs`** — auto-migrate hozzáadva Production/Staging environmentben:

```csharp
if (app.Environment.IsProduction() || app.Environment.IsEnvironment("Staging"))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<CuttingDbContext>().Database.Migrate();
}
```

Ez minden service restart alkalmával lefut. Az EF automatikusan kihagyja az már alkalmazott migrációkat — biztonságos.

**Hatás VPS-re:** Következő `systemctl restart spaceos-cutting` után a `Name` oszlop automatikusan létrejön, és `POST /api/cutting/plans` 201-et ad.

## Commit

`a60fcb4` — spaceos-modules-cutting

## Tesztek

| Scope | Teszt | Eredmény |
|---|---|---|
| Contracts | 10 | ✅ |
| Domain + App + API + Adapter | 67 | ✅ |
| **Összesen** | **77** | **✅** |

## Szükséges deploy lépés

```bash
# VPS-en (vagy CI deploy script által):
systemctl restart spaceos-cutting
# A service induláskor automatikusan migrálja a DB-t
```

A manuális `dotnet ef database update` többé **nem szükséges** — a startup elvégzi.

## Security review

- `Database.Migrate()` csak Production/Staging environmentben fut — tesztek nem érintettek
- A migration idempotens (EF `__EFMigrationsHistory` tábla védi a dupla futást)
- Nincs destructive SQL a migrációban — csak `ADD COLUMN "Name" varchar(200) NULL`

## Kockázatok / megjegyzések

Auto-migrate startup-ban egyszerű deploymenteknél elfogadható. Ha a jövőben bonyolult (renaming, data migration) migrációk kellenek, érdemes lehet visszatérni a manuális megközelítésre.
