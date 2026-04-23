---
id: MSG-KERNEL-093-DONE
from: kernel
to: root
type: done
status: READ
ref: MSG-KERNEL-093
created: 2026-04-18
---

## Összefoglaló

**Root cause (végleges):** `NpgsqlRetryingExecutionStrategy.OnFirstExecution()` az összes EF Core operáció elején lefut. Ha nyitott user-transaction van (`Database.CurrentTransaction != null`), dob — akkor is, ha az adott operáció NEM nyit új tranzakciót.

A `PostgresAdvisoryAuditWriteLock` a hash-chain számítás teljes ideje alatt nyitva tartja a tranzakciót. Az utána következő minden `AuditDbContext` hívás (GetLastHashAsync, SaveChangesAsync) eltalálta az `OnFirstExecution()` ellenőrzést és dobott.

A KERNEL-091 fix (BeginTransactionAsync → ExecuteAsync) szükséges volt, de nem elégséges: csak a TX nyitásán segített, a subsequent operációkon nem.

**Fix:** `EnableRetryOnFailure` eltávolítva `AppDbContext`-ből és `AuditDbContext`-ből.
- `HashSinkDbContext` megtartja (nincs user transaction)
- A KERNEL-091-ben bevezetett `CreateExecutionStrategy().ExecuteAsync()` csomagolások megmaradnak (jövőbeli cloud deploy esetén helyesen működnek ha retry visszakapcsolásra kerül)

## Változott fájl

| Fájl | Változás |
|---|---|
| `SpaceOS.Infrastructure/DependencyInjection.cs` | `EnableRetryOnFailure` törölve `AppDbContext` + `AuditDbContext` prod regisztrációjából |

## Commit

`46d64b5` (develop)

## Tesztek

**1138 / 1138 pass** (926 unit + 108 IT + 104 API). `dotnet build`: 0 error.

## Deploy

INFRA task: `MSG-INFRA-019` — `docs/mailbox/infra/inbox/2026-04-18_019_kernel-093-retry-strategy-root-fix-deploy.md`
`/tmp/kernel-publish` előkészítve — INFRA `rsync + systemctl restart`.

## Security review

Nincs új endpoint, nincs authorization változás.
Az `EnableRetryOnFailure` eltávolítása nem csökkenti a biztonságot — retry-t a HTTP client szinten kezeljük.
Advisory lock és hash chain integritás változatlan.

## Kockázatok

A VPS single-node deployment (localhost PostgreSQL) → tranziens hibák gyakorlatilag nem fordulnak elő.
Ha a jövőben cloud multi-node architektúrára váltunk, az `EnableRetryOnFailure` és az explicit transaction
kompatibilitást újra kell vizsgálni (pl. az egész locking logika application-level ExecuteAsync-ba mozgatása).
