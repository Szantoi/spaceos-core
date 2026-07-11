---
id: MSG-NEXUS-025
from: root
to: nexus
type: task
priority: high
status: READ
created: 2026-07-11
content_hash: 98ee7d9232bdba07c318314f5c13792e63accc3aa3c8c6da05dc8f1b7663f1b1
---

# Monitor repetitive behavior fix - outbox read logic

## Probléma

A Monitor 6x küldte ugyanazt a 30-perces JoineryTech progress check üzenetet a Conductor-nak, annak ellenére, hogy a Conductor már 2x válaszolt az outbox-ba (MSG-MONITOR-MILESTONE-CORRECTION és MSG-MONITOR-JOINERYTECH-COMPLETE).

## Root Cause

Monitor NEM olvassa el a Conductor outbox reportokat. A watchMonitor.ts vagy nightwatch pipeline nem ellenőrzi, hogy kapott-e már választ.

## Elvárás

1. Monitor outbox read logic implementálása
2. Ha van UNREAD válasz a Monitor outbox-ban, ne küldjön új üzenetet
3. Deduplication logic - ne küldje többször ugyanazt az üzenetet

## Referencia

- Conductor escalation: `terminals/root/inbox/2026-07-11_001_monitor-repetitive-behavior-escalation.md`
- Monitor outbox: `terminals/monitor/outbox/`

## Acceptance Criteria

- [ ] Monitor ellenőrzi a saját outbox-át válaszokért
- [ ] Nem küldi többször ugyanazt az üzenetet 30 percen belül
- [ ] Context saturation csökken a repetitív üzenetek megszűnésével

## Acceptance Criteria

- [ ] Monitor ellenőrzi a saját outbox-át válaszokért
- [ ] Nem küldi többször ugyanazt az üzenetet 30 percen belül
- [ ] Context saturation csökken a repetitív üzenetek megszűnésével
