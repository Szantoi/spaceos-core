---
id: MSG-CUTTING-033-DONE
from: cutting
to: root
type: done
status: READ
ref: MSG-CUTTING-033
created: 2026-04-20
---

## Összefoglaló

CUTTING-033 migration javítás végrehajtva — a manuálisan írt migration törölve és az EF Core `dotnet ef` tool-lal újragenerálva.

### Változások

**Eszköz telepítése:**
- `.config/dotnet-tools.json` — lokális tool manifest létrehozva, `dotnet-ef 8.0.11` telepítve
- `src/SpaceOS.Modules.Cutting.Api/SpaceOS.Modules.Cutting.Api.csproj` — `Microsoft.EntityFrameworkCore.Design 8.0.11` hozzáadva (PrivateAssets=all), szükséges a `dotnet ef` startup project-hez

**Migration regenerálás:**
- `src/SpaceOS.Modules.Cutting.Infrastructure/Migrations/20260420000001_CuttingPlanStatusToEnum.cs` — törölve (manuálisan írt)
- `src/SpaceOS.Modules.Cutting.Infrastructure/Migrations/20260420073921_CuttingPlanStatusToEnum.cs` — EF Core által generált
- `src/SpaceOS.Modules.Cutting.Infrastructure/Migrations/20260420073921_CuttingPlanStatusToEnum.Designer.cs` — EF Core által generált
- `src/SpaceOS.Modules.Cutting.Infrastructure/Migrations/CuttingDbContextModelSnapshot.cs` — EF Core által frissített snapshot

**PostgreSQL USING clause:**
- Az EF által generált `AlterColumn<int>` cserélve raw SQL-re, amely a meglévő string értékeket (`Draft`, `Approved`, `InProgress`, `Closed`) konvertálja int értékekre CASE kifejezéssel

## Tesztek

- `dotnet build` → **0 error, 0 warning** ✅
- `dotnet test` → **195/195 pass** ✅

## Security review

- Nincs security-releváns változás (migration fix only)
- Migráció csak a `spaceos_cutting.CuttingPlans.Status` oszlopot érinti

## Kockázatok / kérdések

Nincs.
