---
id: MSG-KERNEL-067
from: root
to: kernel
type: task
priority: high
status: DONE
ref: MSG-E2E-013-DONE, MSG-E2E-014
created: 2026-04-14
---

# MSG-KERNEL-067 — FlowEpic `PUT /close` FSM fix (BATCH-0-CLEANUP-01)

## Kontextus

Az `05-flowepic-lifecycle` E2E teszt a `PUT /bff/api/flow-epics/:id/close` lépésen
folyamatosan fail-el a Batch 2 baseline óta. Ezenkívül az E2E-014 (36-proof.chain)
a proof chain záróköveként ugyanezt a close endpoint-ot hívja — tehát ez **blokkolja
a Doorstar Q2 happy path verifikációját**.

## Szimptóma

```
05-flowepic-lifecycle | PUT /close — transition Delivery → ClosedDone | várt: 200 | kapott: ?
```

A teszt:
```typescript
const res = await PUT(`/bff/api/flow-epics/${epicId}/close`, {
  proofUrl: 'https://example.com/proof.pdf',
  proofHash: 'e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855',
});
// 200 várva — nem [404, 422, 429] kap (akkor sem failelne) — tehát 500-at kap
```

## Diagnosztikai útvonal

### 1. CloseFlowEpicCommandHandler — try/catch scope

A handler try/catch-e csak az `epic.Close()` hívást védi:

```csharp
try
{
    epic.Close(request.ProofUrl, request.ProofHash);
}
catch (DomainException ex)
{
    return Result.Error(ex.Message);
}

// Ezek NINCSENEK try/catch-ben — ha dobnak, 500 lesz:
await _snapshotRepository.AddAsync(snapshot, ct)...
await _outboxRepository.AddAsync(outboxMessage, ct)...
await _unitOfWork.SaveChangesAsync(ct)...
```

### 2. Lehetséges 500 okok

a) **Snapshot/Outbox DB hiba** — `AggregateSnapshots` vagy `OutboxMessages` tábla
   nem létezik, vagy az EF Core konfiguráció nem tartalmazza (migration probléma)

b) **FlowEpicId → Guid mapping hiba** — ha az epic lekérdezés elveszíti a tenant kontextust
   (RLS + `app.current_tenant_id`), a `GetByIdAsync` null-t ad vissza → 404 lenne, nem 500

c) **`ToSnapshotDto()` vagy JSON serialization** — belső kivétel a snapshot JSON készítésekor

### 3. Ellenőrzési lépések

```bash
# Kernel log (VPS):
journalctl -u spaceos-kernel -n 100 --no-pager | grep -i "close\|snapshot\|outbox\|error"

# DB táblák:
# psql -U spaceos -d spaceos_kernel -c '\dt "AggregateSnapshots"'
# psql -U spaceos -d spaceos_kernel -c '\dt "OutboxMessages"'
```

## Feladat

1. **Azonosítsd a tényleges hibaokot** (Kernel logból vagy unit test futtatásával)
2. **Javítsd** — a legvalószínűbb esetek:
   - Ha snapshot/outbox DB hiba: ellenőrizd a migration-t és az EF konfigurációt
   - Ha try/catch scope probléma: bővítsd a védett blokkot
   - Ha FSM guard bug: a `FlowEpic.Close()` Domain logikában javítsd
3. **Unit teszt**: a `CloseFlowEpicCommandHandlerTests.cs` (létező) fedi-e a hibaesetet?
   Ha nem → adj hozzá tesztet

## Definition of Done

- [ ] `PUT /api/flow-epics/:id/close` 200-at ad Delivery → ClosedDone átmeneten
- [ ] Azonosított és dokumentált gyökérok az outboxban
- [ ] 1075 (vagy több) teszt zöld
- [ ] DONE outbox: `MSG-KERNEL-067-DONE`

## Visszajelzés

Outboxba: `MSG-KERNEL-067-DONE`

## Megjegyzés

Deploy (INFRA-072) a DONE outbox elfogadása után következik.
Ha a fix migration-t igényel → jelezd BLOCKED-ban, root koordinálja az INFRA terminált.
