# CONDUCTOR Terminál

> Tervezési pipeline, feladatkiosztás, terminál koordináció

## Gyors Info

| | |
|---|---|
| **Terminál** | conductor |
| **Port** | - |
| **Típus** | persistent |
| **Könyvtár** | `/opt/spaceos/spaceos-conductor/` |
| **Mailbox** | `/opt/spaceos/docs/mailbox/conductor/` |
| **Memory** | `/opt/spaceos/docs/memory/conductor.md` |

## Session Indítás

```bash
# 1. Memory olvasás
cat /opt/spaceos/docs/memory/conductor.md

# 2. Planning queue ellenőrzés
ls /opt/spaceos/docs/planning/queue/

# 3. Terminál outboxok (DONE/BLOCKED)
grep -rl "status: UNREAD" /opt/spaceos/docs/mailbox/*/outbox/

# 4. Inbox ellenőrzés
grep -rl "status: UNREAD" /opt/spaceos/docs/mailbox/conductor/inbox/
```

## Fő Feladatok

1. **Planning queue feldolgozás** - konszenzusos tervek v1→v4 pipeline
2. **Termináloknak inbox kiadás** - feladat delegálás
3. **DONE/BLOCKED feldolgozás** - automatikus pipeline értesítések
4. **Eszkaláció Root-nak** - stratégiai döntések kérése

## Pipeline Workflow

```
docs/planning/queue/          ← Konszenzusos tervek (2-3 pufferleve)
    ↓
Conductor feldolgozás         ← v1 → v4 tervdok pipeline
    ↓
docs/tasks/new/               ← Kész tervdok
    ↓
Terminálnak inbox kiadás      ← Feladat delegálás
    ↓
docs/tasks/active/            ← Aktív feladat
```

## Szkriptek

```bash
# Manuális librarian sync (>5 órás READ üzeneteket archivál)
bash /opt/spaceos/scripts/cron-librarian.sh

# Pipeline log
tail -20 /opt/spaceos/logs/dispatcher/pipeline.log

# Nightwatch log
tail -10 /opt/spaceos/logs/dispatcher/nightwatch.log
```

## Inbox Küldés Terminálnak

```yaml
---
id: MSG-<TERMINAL>-NNN
from: conductor
to: <terminál>
type: task
priority: high|medium|low
status: UNREAD
model: sonnet|opus|haiku
created: YYYY-MM-DD
---

## Feladat
[Részletes specifikáció]

## Acceptance Criteria
- [ ] AC1
- [ ] AC2

## Kapcsolódó fájlok
- `path/to/file.md`
```

## DONE Feldolgozás

Amikor terminál DONE outbox-ot ír:
1. Reviewer.sh automatikusan futtat 2× Haiku review-t
2. Dual APPROVE → pipeline.sh frissíti README + Status
3. Conductor értesítést kap a következő lépésről

## Eszkaláció Root-nak

Ha üzleti döntés kell (nem tech/infra):
```yaml
---
id: MSG-ROOT-NNN
from: conductor
to: root
type: escalation
priority: high
status: UNREAD
---

## Kérdés
[Mit kell eldönteni]

## Opciók
1. Opció A - előnyök/hátrányok
2. Opció B - előnyök/hátrányok
```

## Kapcsolódó Dokumentáció

- CLAUDE.md: `/opt/spaceos/spaceos-conductor/CLAUDE.md`
- Workflow: `/opt/spaceos/docs/WORKFLOW.md`
- Pipeline config: `/opt/spaceos/scripts/plan-config.yaml`
