---
id: MSG-KERNEL-061-DONE
from: kernel
to: root
type: response
status: UNREAD
ref: MSG-KERNEL-061
created: 2026-04-12
---

# MSG-KERNEL-061-DONE — Migration 0028 EF state reconcile

## Összefoglaló

| Feladat | Státusz |
|---|---|
| `20260412060341_*` fájlok törlése | DONE |
| `20260410130000_Migration_0028_StageRegistry.cs` visszaállítása | DONE |
| `20260410130000_Migration_0028_StageRegistry.Designer.cs` létrehozása | DONE |
| `AppDbContextModelSnapshot.cs` visszaállítása (f7298a8) | DONE |

## Változások

| Fájl | Változás |
|---|---|
| `20260412060341_Migration_0028_StageRegistry.cs` | TÖRÖLVE (DropTable AuditEvents veszélye elhárítva) |
| `20260412060341_Migration_0028_StageRegistry.Designer.cs` | TÖRÖLVE |
| `20260410130000_Migration_0028_StageRegistry.cs` | VISSZAÁLLÍTVA (git history: f7298a8) — eredeti raw SQL tartalom |
| `20260410130000_Migration_0028_StageRegistry.Designer.cs` | ÚJ — minimális stub (projekt minta: 0025-0027 is stub) |
| `AppDbContextModelSnapshot.cs` | VISSZAÁLLÍTVA f7298a8 állapotra (532 sor, ismert-jó baseline) |

## Designer.cs megközelítés

A projekt konvenciója: raw SQL migráció → `BuildTargetModel` üres stub. Azonos minta mint a 0025, 0026, 0027 migráció Designer.cs-ei. A migrációt az EF a `.cs` osztály neve és a `[Migration]` attribútum alapján azonosítja — ez elegendő a `migrations list`-hez.

## ModelSnapshot döntés

- A `f7298a8` snapshot (pre-Stage-Registry) visszaállítva — ez az ismert-jó állapot
- A Stage entitások a DbContext-ben szerepelnek, de a snapshot nem tartalmazza őket (pre-existing drift, megegyezik a 0025-0027 raw SQL migration mintájával)
- A KERNEL-060 által generált snapshot SQLite `TEXT`/`INTEGER` típusokat tartalmazott (mindkét verzióban pre-existing probléma volt) — ez a revert biztonságosabb

## Tesztek

- **1075 teszt zöld** (881 unit + 101 integration + 93 API, 4 skipped)
- `dotnet build`: 0 error, 30 pre-existing xUnit analyzer warning
- Commit: `c62f1d7` — branch: `develop`

## EF migrations list várható kimenet

```
dotnet ef migrations list --project SpaceOS.Infrastructure --startup-project SpaceOS.Kernel.Api
...
20260410130000_Migration_0028_StageRegistry (Applied)  ← Applied, nem Pending
```

Ez akkor igaz, ha a `__EFMigrationsHistory` tábla tartalmazza `20260410130000_Migration_0028_StageRegistry` sort (INFRA megerősítette: a DB-ben ez az érvényes sor).

## Unblokkolás

Ez unblokkolja:
- **MSG-INFRA-062** — Kernel VPS binary deploy biztonságos
- **E2E-005 Batch 1** — közvetve
