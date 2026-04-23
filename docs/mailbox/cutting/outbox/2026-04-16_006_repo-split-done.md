---
id: MSG-CUTTING-006-DONE
from: cutting
to: root
type: done
status: READ
ref: MSG-CUTTING-006
created: 2026-04-16
---

## Összefoglaló

Repo szétválasztás sikeresen elvégezve. Az Inventory és Procurement projektek kiemelve az önálló repókba.

### Változott fájlok / repók

**Új repo: `/opt/spaceos/spaceos-modules-inventory/`**
- `git init` + `main` branch + initial commit (`bd78215`)
- Solution: `SpaceOS.Modules.Inventory.sln` (6 projekt: Contracts, Domain, Application, Infrastructure, Api, Tests)
- `NuGet.Config` → lokális feed: `/opt/spaceos/spaceos-modules-cutting/nupkg`
- `CLAUDE.md` minimális fejléc

**Új repo: `/opt/spaceos/spaceos-modules-procurement/`**
- `git init` + `main` branch + initial commit (`9506f36`)
- Solution: `SpaceOS.Modules.Procurement.sln` (6 projekt: Contracts, Domain, Application, Infrastructure, Api, Tests)
- `NuGet.Config` → lokális feed: `/opt/spaceos/spaceos-modules-cutting/nupkg`
- `CLAUDE.md` minimális fejléc
- `Procurement.Contracts.csproj`: ProjectReference → `PackageReference SpaceOS.Modules.Inventory.Contracts 1.1.0`
- `Procurement.Application.csproj`: ProjectReference → `PackageReference SpaceOS.Modules.Inventory.Contracts 1.1.0`
- `Procurement.Infrastructure.csproj`: ProjectReference → `PackageReference SpaceOS.Modules.Inventory.Contracts 1.1.0`, eltávolított felesleges `Inventory.Infrastructure` ProjectReference

**Módosított repo: `/opt/spaceos/spaceos-modules-cutting/`** (commit `2038666`)
- Inventory.* és Procurement.* projektek eltávolítva solution-ből
- `Cutting.Contracts.csproj`: ProjectReference → `PackageReference SpaceOS.Modules.Inventory.Contracts 1.1.0`
- `Cutting.Application.csproj`: ProjectReference → `PackageReference SpaceOS.Modules.Inventory.Contracts 1.1.0`
- `Cutting.Infrastructure.csproj`: ProjectReference → `PackageReference SpaceOS.Modules.Inventory.Contracts 1.1.0`
- `Cutting.Contracts.Tests.csproj`: ProjectReference → PackageReference Inventory.Contracts 1.1.0 + Procurement.Contracts 1.0.0
- `NuGet.Config` létrehozva lokális feed-del

## Tesztek

| Repo | Teszt | Eredmény |
|---|---|---|
| spaceos-modules-inventory | 47 | ✅ mind zöld |
| spaceos-modules-procurement | 42 | ✅ mind zöld |
| spaceos-modules-cutting | 64 (54 + 10 contracts) | ✅ mind zöld |
| **Összesen** | **153** | **✅** |

## Security review

- Minden endpoint `[Authorize(Policy = "ManufacturerOnly")]` — változatlan, csak költözés
- RLS FORCE minden tenant-specifikus táblán — változatlan
- Cross-repo függőségek NuGet PackageReference-en keresztül (nem ProjectReference)
- Nincs új kód, csak strukturális szétválasztás

## Kockázatok / megjegyzések

- A lokális NuGet feed (`/opt/spaceos/spaceos-modules-cutting/nupkg/`) jelenleg a Cutting repóban van — ha az Inventory repo önállóan deployolásra kerül, saját feed vagy Nexus/GitHub Packages kell majd.
- A `Procurement.Infrastructure` korábban feleslegesen referenciázta `Inventory.Infrastructure`-t (nem használt typen) — eltávolítva, csak a Contracts PackageReference maradt.
