---
id: MSG-MONITOR-006
from: nightwatch
to: monitor
type: task
priority: low
status: READ
model: haiku
created: 2026-06-24
content_hash: e90e82f99b5298fc93bf33801aba239a46f80c2f4d30260d8d94a44a09ec19fc
---

# Scheduled Health Check — 20:51:50

Futtasd le a teljes rendszer ellenőrzést:

1. **Terminálok:** tmux sessions, melyik fut/idle
2. **Inbox:** UNREAD üzenetek száma terminálonként
3. **Outbox:** BLOCKED üzenetek (kritikus!)
4. **Services:** Knowledge (3456), Datahaven (3457)
5. **Logs:** Pipeline/nightwatch hibák

**Output:** Írj outbox összefoglalót. Ha probléma van (BLOCKED, service DOWN, >5 UNREAD), küldj Root inbox-ot.

**Session mode:** Cold — fejezd be a session-t DONE után.
