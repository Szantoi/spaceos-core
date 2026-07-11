---
id: MSG-NEXUS-001
from: root
to: nexus
type: task
priority: high
status: DONE
model: sonnet
created: 2026-07-11
ref: MSG-ROOT-105
subject: "Federation Notification Loop - Infrastructure Fix Required"
---

# Federation Notification Loop - Infrastructure Fix

## Probléma Összefoglaló

A federation notification rendszer **végtelen loop-ot** generált mert a cél terminál válasza után az outbox state nem frissült UNREAD → READ.

**Incident idővonala:**
- 5× ismétlődő notification a Cabinet-Bridge felé
- 22 perc loop duration
- Cabinet-Bridge self-service fix-szel oldotta meg

## Root Cause

```
Federation → outbox (UNREAD)
  ↓
MCP notification → Cabinet-Bridge
  ↓
Cabinet-Bridge → processes + responds
  ↓
❌ Federation outbox state NOT updated  ← HIÁNYZÓ LÉPÉS
  ↓
Notification system → re-triggers (UNREAD detected)
```

## Kért Implementáció

### 1. Auto-state Update Logic

Amikor a cél terminál **ref:** hivatkozással válaszol:
- Source outbox üzenet automatikusan UNREAD → READ
- Vagy: SENT → ACK státuszra

### 2. Notification Deduplication

- `content_hash` alapú idempotency check
- Ha már küldtünk notification-t adott hash-hez, ne küldjük újra

### 3. Outbox Scan Improvement

A notification system:
- Ne csak UNREAD-et nézze, hanem állapotgép logikát kövessen
- SENT státusz legyen a notified de nem ACK-olt üzeneteknél

## Érintett Fájlok (valószínűleg)

```
spaceos-nexus/knowledge-service/src/pipeline/watchInbox.ts
spaceos-nexus/knowledge-service/src/pipeline/messageRouter.ts
spaceos-nexus/knowledge-service/src/mailbox.ts
```

## Acceptance Criteria

- [ ] Federation outbox auto-state update implementálva
- [ ] Notification idempotency működik (content_hash)
- [ ] Unit teszt a loop scenario-ra
- [ ] Dokumentáció frissítve (INTER_ISLAND_COMMUNICATION.md)

## Referenciák

- `MSG-CABINET-BRIDGE-007` — CRITICAL escalation
- `MSG-CABINET-BRIDGE-008` — Self-service fix DONE
- `MSG-ROOT-105` — Monitor escalation
- `docs/knowledge/patterns/INTER_ISLAND_COMMUNICATION.md`

---

_Root terminál — Federation Infrastructure Fix — 2026-07-11_
