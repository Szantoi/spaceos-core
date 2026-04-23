---
id: MSG-KERNEL-090-DONE
from: kernel
to: root
type: done
status: READ
ref: MSG-KERNEL-090
created: 2026-04-18
---

## Összefoglaló

**Root cause azonosítva:** `AuditEventConfiguration.HasDefaultValue(0L)` a `Sequence` property-n.

EF Core/Npgsql az annotáció alapján minden `AuditEvent` INSERT-be belefoglalta a `Sequence = 0`
explicit értéket. A PostgreSQL `GENERATED ALWAYS AS IDENTITY` oszlop tiltja az explicit értéket
→ `ERROR: cannot insert a non-DEFAULT value into column "Sequence"` → 500.

**Miért most romlott el?** Migration 0030 (`5bfe0a1`, 2026-04-15) addolta a `Sequence` oszlopot
`GENERATED ALWAYS AS IDENTITY`-vel. Előtte az oszlop nem létezett, az audit INSERT-ek rendben
futottak. Az E2E-049 (migration 0030 előtt futott) ezért volt zöld.

**Fix:** `AuditDbContext.OnModelCreating` PostgreSQL esetén `UseIdentityAlwaysColumn()`-t hív,
ami felülírja a `HasDefaultValue(0L)` annotációt és közli az EF Core-ral, hogy ne foglalja bele
a `Sequence`-t INSERT-ekbe. SQLite path változatlan.

## Változott fájlok

| Fájl | Változás |
|---|---|
| `SpaceOS.Infrastructure/Persistence/AuditDbContext.cs` | `OnModelCreating`: `Database.IsNpgsql()` ágban `UseIdentityAlwaysColumn()` |

## Tesztek

- **1138 / 1138 pass** (926 unit + 108 IT + 104 API)
- Commit: `e448f2d` (develop)
- `dotnet build`: 0 error, 0 warning (csak pre-existing xUnit lint warnings)

## Deploy

INFRA redeploy szükséges: `MSG-INFRA-017` kiadva (`docs/mailbox/infra/inbox/`).
Nincs új migration — migration 0030 már alkalmazva.

VPS curl teszt (201) a deploy után lehetséges.

## Security review

- Nincs új endpoint, nincs authorization-változás
- Nincs SQL string concat — Npgsql paraméteres query
- Az `AuditDbContext` csak INSERT/SELECT jogosultságú (`spaceos_audit_writer` role) — nem változott
- `Database.IsNpgsql()` futásidőben vizsgál, nem konfigurációból — nem hamisítható

## Kockázatok / kérdések

Nincs. A fix izolált, csak az EF Core modell annotációját érinti, üzleti logika nem változott.
