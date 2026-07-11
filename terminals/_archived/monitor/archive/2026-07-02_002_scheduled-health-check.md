---
id: MSG-MONITOR-002
from: nightwatch
to: monitor
type: task
priority: low
status: READ
injected: 2026-07-02
model: haiku
created: 2026-07-02
---

# Scheduled Health Check — 12:21:52

Futtasd le a teljes rendszer ellenőrzést:

1. **Terminálok:** tmux sessions, melyik fut/idle
2. **Inbox:** UNREAD üzenetek száma terminálonként
3. **Outbox:** BLOCKED üzenetek (kritikus!)
4. **Services:** Knowledge (3456), Datahaven (3457)
5. **Logs:** Pipeline/nightwatch hibák
6. **Conductor Progress:** Ellenőrizd a Conductor-t:
   - Van-e feldolgozható munka? (queue, outbox DONE, planning)
   - Idle-e a Conductor túl sokáig? (30+ perc)
   - Ha igen → Conductor inbox üzenet: "Folytatható munka észlelve"

**Output:** Írj outbox összefoglalót. Ha probléma van (BLOCKED, service DOWN, >5 UNREAD, Conductor idle + work available), küldj Root inbox-ot.

**Session mode:** Cold — fejezd be a session-t DONE után.
