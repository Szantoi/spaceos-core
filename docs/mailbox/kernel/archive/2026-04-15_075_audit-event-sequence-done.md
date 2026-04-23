---
id: MSG-KERNEL-075-DONE
from: kernel
to: root
type: done
priority: high
status: READ
ref: MSG-KERNEL-075
created: 2026-04-15
---

# MSG-KERNEL-075 — AuditEvents sequence column DONE

## Összefoglaló

`AuditEvent.Sequence` BIGINT IDENTITY tiebreaker implementálva. 4 fájl érintett:

| Fájl | Változás |
|---|---|
| `SpaceOS.Kernel.Domain/AuditLog/AuditEvent.cs` | `public long Sequence { get; private set; }` — DB-generated, no setter |
| `SpaceOS.Infrastructure/Data/Configurations/AuditEventConfiguration.cs` | `ValueGeneratedOnAdd() + HasDefaultValue(0L)` |
| `SpaceOS.Infrastructure/Data/Repositories/AuditEventRepository.cs` | `GetChainAsync`: `ThenBy(e => e.Sequence)` + `GetLastHashAsync`: `ThenByDescending(r => r.Sequence)` |
| `SpaceOS.Infrastructure/Migrations/20260415060000_Migration_0030_AddAuditEventSequence.cs` | Raw SQL: `ALTER TABLE "AuditEvents" ADD COLUMN "Sequence" BIGINT GENERATED ALWAYS AS IDENTITY` |

## Technikai döntések

**`HasDefaultValue(0L)` az EF konfigban:**
- SQLite (`EnsureCreatedAsync`): `INTEGER NOT NULL DEFAULT 0` → INSERT nélkül is sikeres (0 értékkel)
- PostgreSQL: a Migration 0030 felülírja `GENERATED ALWAYS AS IDENTITY`-vel
- Nélküle: 10 SQLite-alapú integrációs teszt `NOT NULL constraint failed` hibával bukott

**Manuális migration (Designer.cs nélkül):**
- AppDbContext ignorálja az `AuditEvent`-et (`modelBuilder.Ignore<AuditEvent>()`)
- EF tool nem generálna semmi tartalmat auto-diff-ből
- Megközelítés: `AddSourceBrandToAuditEvents` és `Migration_0027_AuditHashesWorm` mintájára kézzel írva
- Raw SQL `migrationBuilder.Sql(...)` — hordozható, nincs Npgsql annotation kell

**`GetLastHashAsync` is frissítve:**
- Selectbe bekerült `ae.Sequence`
- `ThenByDescending(r => r.Sequence)` tiebreaker — azonos OccurredAt esetén a legnagyobb Sequence = legutolsó insert

## Tesztek

```
Passed! - Failed: 0, Passed: 910, Total: 910 (unit)
Passed! - Failed: 0, Passed: 107, Total: 107 (integration)
Passed! - Failed: 0, Passed:  93, Total:  97 (API — 4 skip pre-existing)
```

**Összesített: 1110 teszt zöld** — változatlan az előző MSG-074-hez képest.

## Security review

- `Sequence` property `private set` — alkalmazás kód nem tud értéket injektálni
- DB-generated identity → nem manipulálható külső input által
- Migration 0030 production-safe: existing rows automatikusan kapnak értéket INSERT sorrendben
- Nincs RLS érintettség, nincs auth változás

## Kockázatok / kérdések

Nincs nyitott kockázat. A Migration 0030 VPS-en `dotnet ef database update` paranccsal alkalmazandó:

```bash
DOTNET_ROOT=/opt/dotnet /home/gabor/.dotnet/tools/dotnet-ef database update \
  --project SpaceOS.Infrastructure \
  --startup-project SpaceOS.Kernel.Api \
  --context AppDbContext
```

Az `AppDbContext` migrations-hez tartozik (a meglévő AuditEvents schema migration patternnek megfelelően).
