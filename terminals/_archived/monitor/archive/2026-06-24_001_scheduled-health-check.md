---
id: MSG-MONITOR-001
from: nightwatch
to: monitor
type: task
priority: low
status: READ
model: haiku
created: 2026-06-24
content_hash: 9ed9cf83a9d6f3ebdfbb7b7dd933bc33cc852357b93066a891e5ba963dc40f40
---

# Scheduled Health Check — 19:31:27

Futtasd le a teljes rendszer ellenőrzést:

1. **Terminálok:** tmux sessions, melyik fut/idle
2. **Inbox:** UNREAD üzenetek száma terminálonként
3. **Outbox:** BLOCKED üzenetek (kritikus!)
4. **Services:** Knowledge (3456), Datahaven (3457)
5. **Logs:** Pipeline/nightwatch hibák

**Output:** Írj outbox összefoglalót. Ha probléma van (BLOCKED, service DOWN, >5 UNREAD), küldj Root inbox-ot.

**Session mode:** Cold — fejezd be a session-t DONE után.
