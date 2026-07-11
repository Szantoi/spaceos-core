---
id: MSG-MONITOR-012
from: nightwatch
to: monitor
type: task
priority: low
status: READ
model: haiku
created: 2026-06-24
content_hash: 1eabd2af9e2655d9ec7230aa19d94f71b45f1b686183bc0fbf30d09216589e20
---

# Scheduled Health Check — 23:11:17

Futtasd le a teljes rendszer ellenőrzést:

1. **Terminálok:** tmux sessions, melyik fut/idle
2. **Inbox:** UNREAD üzenetek száma terminálonként
3. **Outbox:** BLOCKED üzenetek (kritikus!)
4. **Services:** Knowledge (3456), Datahaven (3457)
5. **Logs:** Pipeline/nightwatch hibák

**Output:** Írj outbox összefoglalót. Ha probléma van (BLOCKED, service DOWN, >5 UNREAD), küldj Root inbox-ot.

**Session mode:** Cold — fejezd be a session-t DONE után.
