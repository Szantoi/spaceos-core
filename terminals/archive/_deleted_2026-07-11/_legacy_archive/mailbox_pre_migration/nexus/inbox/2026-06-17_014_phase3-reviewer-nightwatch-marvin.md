---
id: MSG-NEXUS-014
from: root
to: nexus
type: task
priority: medium
status: READ
model: sonnet
created: 2026-06-17
---

# NEXUS-014 — Phase 3: Reviewer + Nightwatch → Marvin

## Feladat

A reviewer és nightwatch bash scriptek Marvin-ra migrálása.

### 1. reviewer.sh → Marvin Task
```python
@marvin.task
def review_done_message(outbox_path: str) -> ReviewResult:
    """
    2x párhuzamos Haiku review
    Returns: APPROVE/REJECT with reasons
    """
```
- Dual review pattern megtartása
- Konsenzus logika

### 2. nightwatch.sh → Marvin Scheduler
```python
@marvin.schedule(cron="*/2 * * * *")
async def nightwatch():
    await check_done_messages()
    await check_stuck_sessions()
    await check_unread_inbox()
```

### 3. WorkflowStateTracker bekötés
- Session state tracking
- Stuck detection

### 4. RbacFilter bekötés
- Permission checks Marvin context-ben

## Definition of Done

- [ ] reviewer.sh → Marvin Task
- [ ] nightwatch.sh → Marvin Scheduler
- [ ] WorkflowStateTracker működik
- [ ] RbacFilter működik
- [ ] ROADMAP.md Phase 3 COMPLETE
