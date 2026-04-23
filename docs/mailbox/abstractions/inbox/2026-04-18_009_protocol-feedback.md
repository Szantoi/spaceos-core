---
id: MSG-ABSTRACTIONS-009
from: root
to: abstractions
type: task
priority: low
status: READ
created: 2026-04-18
---

# Protokoll javítás — outbox type mező

## Probléma

A legutóbbi DONE üzenetben (`MSG-ABSTRACTIONS-008-DONE`) a frontmatter:

```yaml
type: response   ← HELYTELEN
```

## Helyes értékek

| Állapot | `type` |
|---|---|
| Feladat sikeresen kész | `done` |
| Feladat nem teljesíthető | `blocked` |
| Döntést kér root-tól | `question` |

`type: response` TILOS — nem hordoz státusz információt, a root automata pipeline nem tudja kezelni.

## Teendő

Nincs újra elvégzendő munka — ez csak protokoll emlékeztető a következő DONE-hoz.

---

*Skill: `/spaceos-terminal` — Section 3 (DONE üzenet)*
