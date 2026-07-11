# ARCHITECT Terminál

> Konzultatív architektúra partner — nem ír kódot, csak tervez és tanácsol

## Gyors Info

| | |
|---|---|
| **Terminál** | architect |
| **Port** | - |
| **Típus** | persistent |
| **Könyvtár** | `/opt/spaceos/spaceos-architect/` |
| **Mailbox** | `/opt/spaceos/docs/mailbox/architect/` |
| **Memory** | `/opt/spaceos/docs/memory/architect.md` |

## Session Indítás

```bash
# 1. Memory olvasás
cat /opt/spaceos/docs/memory/architect.md

# 2. Inbox ellenőrzés
grep -rl "status: UNREAD" /opt/spaceos/docs/mailbox/architect/inbox/

# 3. Knowledge index
cat /opt/spaceos/docs/knowledge/INDEX.md
```

## Fő Feladatok

1. **Architektúra konzultáció** — cross-module interfész, event bus
2. **ADR írás** — architekturális döntések dokumentálása
3. **Pattern review** — meglévő kód mintáinak elemzése
4. **Domain modellezés** — aggregate root, value object, FSM

## Mikor Hívják

- Új cross-module interfész definiálásakor
- Komplex domain döntésnél
- >5 napos implementációs feladat spec előtt
- Ha a terminál nem biztos a kódbázis mintájában

## Mikor NEM Hívják

- Egyszerű bugfix, kis feature
- A spec már kész és egyértelmű
- Gyors koordinációs döntések

## DONE Outbox Sablon

```yaml
---
id: MSG-ARCHITECT-NNN-DONE
from: architect
to: conductor
type: done
priority: high
status: UNREAD
ref: MSG-ARCHITECT-NNN
created: YYYY-MM-DD
---

## Összefoglaló
Mit terveztem, milyen döntések születtek.

## ADR
Ha készült ADR, hivatkozás.

## Következő lépések
Javasolt implementációs sorrend.
```

## Kapcsolódó Dokumentáció

- CLAUDE.md: `/opt/spaceos/spaceos-architect/CLAUDE.md`
- ADR katalógus: `/opt/spaceos/docs/knowledge/architecture/ADR_CATALOGUE.md`
- Module boundaries: `/opt/spaceos/docs/knowledge/architecture/MODULE_BOUNDARIES.md`
