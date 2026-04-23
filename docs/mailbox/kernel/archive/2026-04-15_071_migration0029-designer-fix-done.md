---
id: MSG-KERNEL-071-DONE
from: kernel
to: root
type: done
priority: high
status: READ
ref: MSG-KERNEL-071
created: 2026-04-15
---

# MSG-KERNEL-071-DONE — Migration 0029 Designer.cs regenerálás

## Összefoglaló

A `Migration_0029_EcosystemActorTypes` kézzel írt migrációt (`.Designer.cs` nélkül) felváltotta
az EF Core által generált teljes migráció-csomag:

| Fájl | Változás |
|---|---|
| `20260415054837_Migration_0029_EcosystemActorTypes.cs` | Új — EF-generált scaffold + custom SQL Up/Down visszamásolva |
| `20260415054837_Migration_0029_EcosystemActorTypes.Designer.cs` | Új — EF-generált, hiányzott |
| `AppDbContextModelSnapshot.cs` | Frissítve — EF-generált, TenantType oszlop benne |
| `20260415090000_Migration_0029_EcosystemActorTypes.cs` | Törölve — orphan, Designer.cs nélküli változat |

### Folyamat

1. `cp` backup a custom SQL-ből
2. `DOTNET_ROOT=/opt/dotnet dotnet-ef migrations remove --context AppDbContext` — snapshot visszaállítva 0028 állapotra
3. `DOTNET_ROOT=/opt/dotnet dotnet-ef migrations add Migration_0029_EcosystemActorTypes --context AppDbContext` — generálta a Designer.cs-t és frissítette a snapshot-ot
4. Az EF-generált `Up()` (full table-create) felváltva az eredeti custom SQL deltával (ADD COLUMN + constraints + triggers + seed)
5. Orphan `20260415090000_*` fájl törölve

## Tesztek

1104 pass (910 unit + 101 integration + 93 API), 0 failed, 4 skipped.

## DoD ellenőrzés

- [x] `20260415054837_Migration_0029_EcosystemActorTypes.Designer.cs` létezik
- [x] `AppDbContextModelSnapshot.cs` tartalmazza TenantType mapping-et
- [x] `dotnet build` → 0 error
- [x] 1104 teszt pass (≥1104)
- [x] Commit `0fafdb9` + push develop

## Kockázatok / kérdések

Nincsenek. A migration timestamp megváltozott (`090000` → `054837`) — ez lokálisan
nem számít (nincs alkalmazott DB), VPS-en az INFRA terminál alkalmazza a migrációkat.
