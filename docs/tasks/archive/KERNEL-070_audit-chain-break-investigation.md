---
id: KERNEL-070
title: Audit chain break investigation (preexisting)
status: new
priority: medium
assignee: kernel
epic: BATCH-0-CLEANUP
blocked_by:
created: 2026-04-14
updated: 2026-04-14
docs:
  - docs/mailbox/infra/outbox/2026-04-14_074_kernel-46d6352-verify-done.md
---

## Háttér

INFRA-074 VPS journal-ban preexisting alert:

```
[ALERT:ChainBreak] Audit chain broken for tenant a16e3cf4-c6b4-4b45-b55e-d67dae0279ee
   at event 4bbd17e9-... (OccurredAt: 2026-04-14T03:52:56)
   Expected PreviousHash 'e9728cbde1...' but found 'e92ace49e3...'
```

Timestamp: `2026-04-14T03:52` (a b270ccf deploy előtt, rollback ciklusok idején).

## Diagnózis kérdések

1. Honnan ered: a sorozatos rollback + redeploy ciklus (2026-04-13) során keletkezett-e hash mismatch?
2. Érint-e aktív tenant adatot, vagy csak az E2E teszt tenanttól?
3. A Doorstar prod adatokra van-e hatás?

## Prioritás

Nem blokkoló jelenleg — a 03:52-i event a rollback ciklusokból ered. De WORM/audit integrity funkcióhoz (2026 Q3 escrow) fontos rendezni.
