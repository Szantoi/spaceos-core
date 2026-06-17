---
id: MSG-CONDUCTOR-006
from: root
to: conductor
type: task
priority: medium
status: UNREAD
model: haiku
ref: MSG-LIBRARIAN-001, MSG-LIBRARIAN-002
created: 2026-06-17
---

# Librarian — Task Sequencing (Memory Sync + RAG Processing)

## Root Delegation

Az **MSG-LIBRARIAN-001** (memory-sync cron) és **MSG-LIBRARIAN-002** (RAG Knowledge Base) feladatok **soros feldolgozása szükséges**.

**Sorrend:**

1. **MSG-LIBRARIAN-001** — Memory szinkron (5 h cron, LOW prio)
   - Aktív terminál memória mappák: cleanup + stale törlés
   - Értékes tartalom szintetizálása → docs/knowledge/
   - DONE outbox küldés

2. **MSG-LIBRARIAN-002** — RAG Knowledge Base feldolgozása (HIGH prio)
   - RAG_Knowledge_Base_v1.md feldolgozása
   - docs/knowledge/ szintetizálása
   - Indexer mock-teszt

**Függőség:** MSG-LIBRARIAN-001 DONE → MSG-LIBRARIAN-002 START

---

## Conductor feladata

Jelezd a Librarian-nak, hogy az **MSG-LIBRARIAN-001** feldolgozása **szükséges az MSG-LIBRARIAN-002 előtt**.

**Mindkét üzenet már inbox-ban van**, soros végrehajtás folyamatos.

---

*Root note: Ez nem új feladat, csak sequencing confirmation. Librarian már tudja a sorrendet (cron + new task = soros feldolgozás).*
