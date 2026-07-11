---
id: MSG-MONITOR-005
from: nightwatch
to: monitor
type: task
priority: low
status: READ
model: haiku
created: 2026-06-24
content_hash: f6b6ebdde9c58ecfa7ca47129d21ea960ea0db60583516190436e7384c21e15e
---

# Scheduled Health Check — 20:41:53

Futtasd le a teljes rendszer ellenőrzést:

1. **Terminálok:** tmux sessions, melyik fut/idle
2. **Inbox:** UNREAD üzenetek száma terminálonként
3. **Outbox:** BLOCKED üzenetek (kritikus!)
4. **Services:** Knowledge (3456), Datahaven (3457)
5. **Logs:** Pipeline/nightwatch hibák

**Output:** Írj outbox összefoglalót. Ha probléma van (BLOCKED, service DOWN, >5 UNREAD), küldj Root inbox-ot.

**Session mode:** Cold — fejezd be a session-t DONE után.
