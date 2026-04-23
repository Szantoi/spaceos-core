---
id: MSG-KERNEL-086-DONE
from: kernel
to: root
type: done
priority: critical
status: READ
ref: MSG-KERNEL-086
created: 2026-04-17
---

## Összefoglaló

**KERNEL-070 — Audit chain hash mismatch fix** implementálva.

### Root cause

`PostgresAdvisoryAuditWriteLock` az `AppDbContext`-en szerezte meg a `pg_try_advisory_xact_lock`-ot, de az audit műveletek (read last hash, add events, save) az `AuditDbContext`-en futottak — **különböző DB connection-ök**. A xact lock az implicit tranzakció végén azonnal feloldódott (a SELECT visszatérése után), így a lock semmilyen tényleges szinkronizálást nem biztosított. Két concurrent writer ugyanazt a tail hash-t olvasta és forked chain-t hozott létre.

### Fix

| Fájl | Változás |
|---|---|
| `SpaceOS.Infrastructure/AuditLog/PostgresAdvisoryAuditWriteLock.cs` | `AppDbContext` → `AuditDbContext`, explicit tranzakció `BeginTransactionAsync`-kal, `TransactionCommitter` ami commit-ol dispose-kor |
| `SpaceOS.Kernel.IntegrationTests/AuditLog/AuditEventRaceConditionTests.cs` | Új teszt: `ConcurrentDispatch_VerifyChainQueryReturnsIsValid` — 50 concurrent write → VerifyChainQueryHandler `isValid=true` |

A lock most ugyanazon a connection-ön és tranzakción belül él, mint a read és write műveletek. A `TransactionCommitter.DisposeAsync()` commit-ol (vagy rollback-ol hiba esetén), így a lock a teljes read-compute-write ciklus alatt fennmarad.

### Commit

`82a849a` (develop)

## Tesztek

**1122 pass** (910 unit + 108 IT + 104 API), **0 fail**. Baseline: 1121 → +1 új teszt.

Utolsó sor:
```
Passed! - Failed: 0, Passed: 108, Skipped: 0, Total: 108
```

## Security review

- [x] **Advisory lock**: most AuditDbContext-en fut, ugyanaz a connection mint a read/write
- [x] **Explicit tranzakció**: ReadCommitted isolation, lock a teljes ciklus alatt
- [x] **Rollback handling**: ha SaveChangesAsync dob, a DisposeAsync catch-el és dispose-ol (implicit rollback)
- [x] **MD5 key derivation**: változatlan (SEC-06, nem kriptográfiai célú)
- [x] **Input validation**: nincs új external input
- [x] **SQL injection**: paraméteres query változatlan (`{0}` placeholder)
- [x] **Sensitive data**: nincs secret a logban

## Kockázatok / kérdések

Nincsenek. A fix backward compatible — `InProcessAuditWriteLock` (dev/test) változatlan, `PostgresAdvisoryAuditWriteLock` (production) javított.
