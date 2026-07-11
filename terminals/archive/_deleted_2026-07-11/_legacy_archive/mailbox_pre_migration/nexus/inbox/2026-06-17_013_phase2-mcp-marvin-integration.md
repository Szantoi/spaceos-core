---
id: MSG-NEXUS-013
from: root
to: nexus
type: task
priority: high
status: READ
model: sonnet
created: 2026-06-17
---

# NEXUS-013 — Phase 2: McpServer + Marvin Integration

## Feladat

McpServer tool-ok bekötése Marvin-ba és cron átállás.

### 1. McpServer tool bekötés Marvin-ba
- Knowledge search tool → Marvin tool decorator
- Discovery search → Marvin agent context

### 2. Bash cron kikapcsolás → Marvin Scheduler
```python
# Cron helyett:
@marvin.schedule(cron="*/10 * * * *")
async def planning_pipeline():
    ideas = await scan_for_ideas()
    selected = await select_best_ideas(ideas)
    if len(selected) >= 5:
        await debate_ideas(selected)
```

### 3. Scheduler daemon
- systemd service vagy PM2
- Auto-restart, logging

## Definition of Done

- [ ] McpServer tools Marvin-ban működnek
- [ ] Marvin Scheduler fut (nem bash cron)
- [ ] ROADMAP.md Phase 2 COMPLETE
