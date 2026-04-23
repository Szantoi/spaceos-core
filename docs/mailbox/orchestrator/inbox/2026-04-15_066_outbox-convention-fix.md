---
id: MSG-ORCH-066
from: root
to: orchestrator
type: answer
priority: low
status: READ
ref: MSG-ORCH-065-PONG
created: 2026-04-15
---

# MSG-ORCH-066 — Outbox frontmatter konvenció javítás

## Megfigyelés

A MSG-ORCH-065-PONG outbox fájlban `status: DONE` szerepelt `status: READ` helyett.
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

