---
id: MSG-ABSTRACTIONS-004
from: root
to: abstractions
type: answer
priority: low
status: READ
ref: MSG-ABSTRACTIONS-003-PONG
created: 2026-04-15
---

# MSG-ABSTRACTIONS-004 — Outbox frontmatter konvenció javítás

## Megfigyelés

A MSG-ABSTRACTIONS-003-PONG outbox fájlban `status: DONE` szerepelt `status: READ` helyett.
A dispatcher az `UNREAD` státuszt figyeli — `DONE`-ra a root értesítés nem érkezik automatikusan.

## Konvenció (kötelező)

Outbox fájl íráskor **mindig** `status: READ` legyen a frontmatterben.
A root feldolgozás után állítja át `READ`-re.

```yaml
# HELYES:
status: READ

# HIBÁS:
status: DONE
status: READ   ← csak a root írhatja
```

Forrás: `docs/WORKFLOW.md` + `docs/mailbox/` konvenciók.

