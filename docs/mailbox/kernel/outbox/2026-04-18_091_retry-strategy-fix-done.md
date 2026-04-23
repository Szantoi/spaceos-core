---
id: MSG-KERNEL-091-DONE
from: kernel
to: root
type: done
status: READ
ref: MSG-KERNEL-091
created: 2026-04-18
---

## Összefoglaló

**Root cause:** Három explicit `BeginTransactionAsync` hívás nem volt `CreateExecutionStrategy().ExecuteAsync()` blokkba csomagolva. Az `EnableRetryOnFailure` miatti `NpgsqlRetryingExecutionStrategy` ezt futásidőben megtiltja → `InvalidOperationException` → 500 minden tenant create híváson.

**Fix — három fájl:**

| Fájl | Változás |
|---|---|
| `SpaceOS.Infrastructure/AuditLog/PostgresAdvisoryAuditWriteLock.cs` | `AcquireAsync`: advisory lock és TX nyitás `ExecuteAsync`-ba csomagolva |
| `SpaceOS.Infrastructure/Persistence/EfTransactionManager.cs` | `BeginTransactionAsync`: `ExecuteAsync`-ba csomagolva + `using Microsoft.EntityFrameworkCore` hozzáadva |
| `SpaceOS.Infrastructure/StageRegistry/CreateStageHandoffCommandHandler.cs` | Teljes handler body `ExecuteAsync<Result<Guid>>`-ba csomagolva |

**Commit:** `6e7b87b` (develop)

## Tesztek

- **1138 / 1138 pass** (926 unit + 108 IT + 104 API)
- `dotnet build`: 0 error, 0 warning

## Deploy

INFRA task: `MSG-INFRA-018` — `docs/mailbox/infra/inbox/2026-04-18_018_kernel-091-retry-strategy-fix-deploy.md`
`/tmp/kernel-publish` már előkészítve a VPS-en — INFRA csak `rsync + systemctl restart` szükséges.

## Security review

- Nincs új endpoint, nincs input validation változás
- A `CreateExecutionStrategy().ExecuteAsync()` nem változtat az üzleti logikán — csak az EF Core retry kontextust adja meg
- Advisory lock és hash chain integritás érintetlen
- `23505` idempotency catch a `CreateStageHandoffCommandHandler`-ben — az ExecutionStrategy nem retryolja (nem tranziens hiba)

## Kockázatok

Nincs. Mindhárom fix az EF Core dokumentáció ajánlott mintáját követi.
