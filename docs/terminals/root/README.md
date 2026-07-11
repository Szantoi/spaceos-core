# ROOT Terminál

> Stratégiai döntések, üzleti prioritások, Datahaven/Resonance infrastruktúra

## Gyors Info

| | |
|---|---|
| **Terminál** | root |
| **Port** | - |
| **Típus** | persistent |
| **Könyvtár** | `/opt/spaceos/` |
| **Mailbox** | `/opt/spaceos/docs/mailbox/root/` |
| **Memory** | `/opt/spaceos/docs/memory/root.md` |

## Session Indítás

```bash
# 1. Planning queue
ls /opt/spaceos/docs/planning/queue/

# 2. Terminál outboxok (DONE/BLOCKED)
grep -rl "status: UNREAD" /opt/spaceos/docs/mailbox/*/outbox/

# 3. Conductor állapot
tmux capture-pane -t spaceos-conductor -p 2>/dev/null | tail -10

# 4. Pipeline log
tail -10 /opt/spaceos/logs/dispatcher/pipeline.log
```

## Fő Feladatok

1. **Stratégiai döntések** — üzleti prioritások, roadmap
2. **Epic/modul indítás** — domain-focus.md módosítás
3. **Eszkaláció kezelés** — Conductor által nem megoldható kérdések
4. **Datahaven/Resonance** — agent infrastruktúra fejlesztés

## Munkamegosztás: Root vs Conductor

| Feladat | Ki végzi |
|---|---|
| Tervezési pipeline | **Automatikus szkriptek** |
| Queue feldolgozás, v1→v4 | **Conductor** |
| Termináloknak feladat kiadás | **Conductor** |
| DONE feldolgozás | **Automatikus** |
| BLOCKED (tech) | **Conductor** |
| BLOCKED (üzleti) | **Root** |
| Új epic indítás | **Root** |
| Datahaven/Resonance | **Root** |

## Inbox Küldés Terminálnak

```yaml
---
id: MSG-<TERMINAL>-NNN
from: root
to: <terminál>
type: task
priority: critical|high|medium|low
status: UNREAD
model: sonnet|opus|haiku
created: YYYY-MM-DD
---

## Feladat
[Részletes specifikáció]

## Acceptance Criteria
- [ ] AC1
- [ ] AC2
```

## Kapcsolódó Dokumentáció

- CLAUDE.md: `/opt/spaceos/CLAUDE.md`
- Workflow: `/opt/spaceos/docs/WORKFLOW.md`
- Vízió: `/opt/spaceos/docs/vision/SpaceOS_Vision_Master.md`
