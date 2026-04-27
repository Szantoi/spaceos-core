---
id: MSG-KERNEL-103
from: root
to: kernel
type: task
priority: critical
status: READ
ref: SpaceOS_Modules_Cutting_Phase4_Execution_Architecture_v4.md
created: 2026-04-27
---

# KERNEL-103 — Outbox Extension (Cutting Phase 4 előfeltétel)

> **Tervdok:** `docs/tasks/new/SpaceOS_Modules_Cutting_Phase4_Execution_Architecture_v4.md` §6.4, §7.7
> **Architect finding:** A meglévő OutboxMessages séma inkompatibilis a Phase 4-gyel.
> **Skill:** `/spaceos-terminal` szerint dolgozz
> **Használhatsz sub-agent-eket** ha szükséges

---

## Probléma

A Kernel `OutboxMessages` tábla jelenleg:
```
Id, Type, Payload, CreatedAt, ProcessedAt, TenantId
```

A Cutting Phase 4 (SEC-09, BE-A01) az alábbiakat igényli:
```
BatchId, BatchSequenceNumber, AggregateId, AggregateType, EventType, Status (enum), Attempts, LastError
```

## Feladat

### 1. OutboxMessage domain model bővítés

```csharp
public class OutboxMessage
{
    // Meglévők (NE változtasd!):
    public Guid Id { get; }
    public string Type { get; }
    public string Payload { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset? ProcessedAt { get; }
    public Guid TenantId { get; }
    
    // ÚJ (additív, nullable a backward-compat-hoz):
    public Guid? BatchId { get; }
    public int? BatchSequenceNumber { get; }
    public Guid? AggregateId { get; }
    public string? AggregateType { get; }
    public string? EventType { get; }
    public int Status { get; } = 1;  // 0=Processed, 1=Pending, 2=Failed
    public int Attempts { get; } = 0;
    public string? LastError { get; }
}
```

### 2. Migration

```bash
dotnet ef migrations add OutboxExtension \
  --project <Infrastructure> --startup-project <Api>
```

Additív: új nullable oszlopok + `Status` default 1 + `IX_OutboxMessages_BatchId_SeqNum` UNIQUE index.

**FONTOS:** A meglévő Phase 3 rekordok `BatchId=NULL, Status=1` — backward compatible.

### 3. OutboxBackgroundWorker bővítés

Az `OutboxBackgroundWorker` jelenleg stub ("real dispatch will be added later"). Bővítsd:
- `Status = 1 (Pending)` alapján polloz (nem `ProcessedAt IS NULL`)
- Sikeres dispatch → `Status = 0, ProcessedAt = now`
- Hiba → `Status = 2, Attempts++, LastError = message`
- Interface-ek: `ISignalROutboxFanOut`, `IHashChainOutboxSink` — **üres default implementáció** OK (a tényleges fan-out a Cutting Phase 4 Track B-ben jön)

### 4. IOutboxWriter interface frissítés

`AppendAsync` az új mezőkkel (nullable overload a backward-compat-hoz).

---

## Tesztek (+10)

1. OutboxMessage: új mezők persistálhatók
2. Migration: tábla + index létezik
3. Status enum: Pending → Processed → Failed transitions
4. BackgroundWorker: `Status=1` polling (nem ProcessedAt)
5. Backward compat: régi rekord (BatchId=NULL) feldolgozható
6. Batch ordering: BatchId+SeqNum UNIQUE constraint
7. Attempts increment hiba esetén
8. LastError mentés
9. Rollback: dispatch hiba → outbox rekord Status=2
10. Integration: SaveChanges + outbox append egy tranzakcióban

## Definition of Done

- [ ] OutboxMessage model + 8 új mező
- [ ] Migration (additív, backward compatible)
- [ ] OutboxBackgroundWorker Status-alapú polling
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` ≥ 1156 pass (1146 + min 10)
- [ ] Outbox DONE
