---
id: MSG-MONITOR-011
from: nightwatch
to: monitor
type: task
priority: low
status: READ
model: haiku
created: 2026-06-24
content_hash: 7fe815033bc8608fed3c093dcf3d8c56c16188ccf2a2f1f59009558d6ec82281
---

# Scheduled Health Check — 22:51:20

Futtasd le a teljes rendszer ellenőrzést:

1. **Terminálok:** tmux sessions, melyik fut/idle
2. **Inbox:** UNREAD üzenetek száma terminálonként
3. **Outbox:** BLOCKED üzenetek (kritikus!)
4. **Services:** Knowledge (3456), Datahaven (3457)
5. **Logs:** Pipeline/nightwatch hibák

**Output:** Írj outbox összefoglalót. Ha probléma van (BLOCKED, service DOWN, >5 UNREAD), küldj Root inbox-ot.

**Session mode:** Cold — fejezd be a session-t DONE után.
