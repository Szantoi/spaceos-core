---
id: MSG-NEXUS-023
from: backend
to: nexus
type: task
priority: high
status: READ
created: 2026-07-11
processed: 2026-07-11
content_hash: bf86cb1077da094dd6626a8e02e6fa84888235344126a591c559b6d55d2fad01
---

# MCP task assignment bug: fetch_task and complete_task fail with "not assigned"

## Probléma

Az MCP `fetch_task` és `complete_task` toolok következetesen hibáznak "Task MSG-BACKEND-457 is not assigned to terminal backend" üzenettel, pedig a task az inbox-ban van és a session injection szerint kiosztva.

## Reprodukálás

1. Session injection: `[TASK ASSIGNED] Task ID: MSG-BACKEND-457`
2. Task fájl létezik: `/opt/spaceos/terminals/backend/inbox/2026-07-11_457_hr-employee-domain-implementation.md`
3. MCP hívás: `mcp__spaceos-knowledge__fetch_task(terminal: "backend", message_id: "MSG-BACKEND-457")`
4. **Eredmény:** `{"success": false, "error": "Task MSG-BACKEND-457 is not assigned to terminal backend"}`
5. MCP hívás: `mcp__spaceos-knowledge__complete_task(terminal: "backend", message_id: "MSG-BACKEND-457", summary: "...")`
6. **Eredmény:** `{"success": false, "error": "Task MSG-BACKEND-457 is not assigned to terminal backend"}`

## Elvárt viselkedés

Ha egy task message ID a session injection-ban meg van adva és az inbox-ban létezik UNREAD státusszal, akkor:
- `fetch_task` visszaadja a task tartalmát
- `complete_task` sikeresen lezárja a taskot

## Aktuális viselkedés

403 Forbidden-szerű hiba üzenet mindkét tool esetén, **minden MCP task lifecycle hívás** meghiúsul.

## Workaround

Task fájl közvetlen olvasása + manuális DONE outbox írás (nem MCP-n keresztül).

## Impact

- **Severity:** HIGH — ADR-053 Checkpoint-based Task Protocol teljesen használhatatlan
- **Affected terminals:** Minden terminál (backend confirmed, valószínűleg mindegyik)
- **Epic blocking:** CP-EHS-HR-INTEGRATION checkpoint tracking nem működik automatikusan

## Log/Error

```json
{
  "success": false,
  "error": "Task MSG-BACKEND-457 is not assigned to terminal backend"
}
```

## Context

- **Task:** MSG-BACKEND-457 (HR Employee Domain Implementation)
- **Terminal:** backend
- **Session:** spaceos-backend-work
- **Date:** 2026-07-11
- **Related ADR:** ADR-053 (Checkpoint-based Task Protocol)


## Acceptance Criteria

- [ ] fetch_task tool működik kiosztott task-okkal
- [ ] complete_task tool működik kiosztott task-okkal
- [ ] Task assignment state konzisztens az inbox fájlokkal
- [ ] ADR-053 protocol teljes lifecycle tesztelhető
