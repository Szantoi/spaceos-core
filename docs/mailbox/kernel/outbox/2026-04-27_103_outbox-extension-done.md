---
id: MSG-KERNEL-103-DONE
from: kernel
to: root
type: done
status: READ
ref: MSG-KERNEL-103
created: 2026-04-27
---

# KERNEL-103 — Outbox Extension DONE

## Összefoglaló

A Cutting Phase 4 előfeltételét képező OutboxMessages séma bővítés teljes egészében implementálva.

### Implementált változások

| Fájl | Változás |
|---|---|
| `SpaceOS.Kernel.Domain/Outbox/OutboxStatus.cs` | ÚJ — `Processed=0, Pending=1, Failed=2` enum |
| `SpaceOS.Kernel.Domain/Outbox/OutboxMessage.cs` | +8 mező: BatchId, BatchSequenceNumber, AggregateId, AggregateType, EventType, Status, Attempts, LastError; +`MarkFailed`, `ResetToPending` metódusok |
| `SpaceOS.Kernel.Domain/Outbox/IOutboxRepository.cs` | +`GetPendingAsync` (Status=Pending alapú) |
| `SpaceOS.Kernel.Domain/Outbox/ISignalROutboxFanOut.cs` | ÚJ — Phase 4 Track B stub interface |
| `SpaceOS.Kernel.Domain/Outbox/IHashChainOutboxSink.cs` | ÚJ — Phase 4 Track B stub interface |
| `SpaceOS.Infrastructure/Outbox/NullSignalROutboxFanOut.cs` | ÚJ — no-op implementáció |
| `SpaceOS.Infrastructure/Outbox/NullHashChainOutboxSink.cs` | ÚJ — no-op implementáció |
| `SpaceOS.Infrastructure/Data/Repositories/OutboxRepository.cs` | +`GetPendingAsync` (Status=Pending WHERE) |
| `SpaceOS.Infrastructure/Outbox/OutboxBackgroundWorker.cs` | Status-alapú polling, fan-out hívás, `MarkFailed` hiba esetén |
| `SpaceOS.Infrastructure/Data/Configurations/OutboxMessageConfiguration.cs` | +8 mező mapping + UNIQUE index |
| `SpaceOS.Infrastructure/DependencyInjection.cs` | `NullSignalROutboxFanOut` + `NullHashChainOutboxSink` regisztrálva |
| `SpaceOS.Infrastructure/Migrations/20260427040052_OutboxExtension.cs` | ÚJ — PostgreSQL típusokkal (uuid, character varying, integer) |

### Séma változás (VPS-en alkalmazva)

```sql
ALTER TABLE "OutboxMessages"
  ADD COLUMN "AggregateId"          uuid,
  ADD COLUMN "AggregateType"         character varying(200),
  ADD COLUMN "Attempts"              integer NOT NULL DEFAULT 0,
  ADD COLUMN "BatchId"               uuid,
  ADD COLUMN "BatchSequenceNumber"   integer,
  ADD COLUMN "EventType"             character varying(200),
  ADD COLUMN "LastError"             character varying(2000),
  ADD COLUMN "Status"                integer NOT NULL DEFAULT 1;

CREATE UNIQUE INDEX "IX_OutboxMessages_BatchId_SeqNum" ON "OutboxMessages" ("BatchId", "BatchSequenceNumber") WHERE "BatchId" IS NOT NULL;
CREATE INDEX "IX_OutboxMessages_Status" ON "OutboxMessages" ("Status");
```

Meglévő Phase 3 rekordok: `BatchId=NULL, Status=1 (Pending)` — backward compatible.

## Tesztek

- **Összesen: 1158 pass** (946 unit + 108 integration + 104 API)
- **DoD: ≥ 1156** ✅ (+12 új teszt)

### Új tesztek
- `SpaceOS.Kernel.Tests/Entities/OutboxMessagePhase4Tests.cs` — 8 teszt (Phase 4 mezők, MarkFailed, ResetToPending)
- `SpaceOS.Kernel.Tests/Infrastructure/OutboxBackgroundWorkerTests.cs` — 4 teszt (fan-out dispatch, Status=Processed, MarkFailed on error, empty batch)

## Security review

- Input validation: `OutboxMessage.Create` ArgumentException guarded (type, payload nem üres)
- Authorization: OutboxBackgroundWorker belső service, nem API endpoint — nem szükséges `[Authorize]`
- RLS: OutboxMessages tábla megőrizte a meglévő `tenant_isolation` RLS policy-t (VPS-en ellenőrizve)
- SQL injection: csak EF Core paraméteres query, nincs string concat
- Sensitive data: `LastError` max 2000 karakter, nem tartalmaz credentials-t

## Kockázatok / kérdések

Nincs blokkoló. A valódi fan-out implementáció (`ISignalROutboxFanOut`, `IHashChainOutboxSink`) a Cutting Phase 4 Track B-ben jön — jelenleg no-op stub fut.
