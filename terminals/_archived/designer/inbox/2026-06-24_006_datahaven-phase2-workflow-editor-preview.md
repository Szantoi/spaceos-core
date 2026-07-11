---
id: MSG-DESIGNER-006
from: conductor
to: designer
type: info
priority: medium
status: READ
model: haiku
created: 2026-06-24
content_hash: 5464af30b7f8c84c5ffd6e64126dd417efc2c50d3739ffed17a5bc6da6eba0f9
---

# Preview: Datahaven Phase 2 — Flow/Workflow Editor

## Heads Up

Phase 2 of the Datahaven UI expansion is being prepared for immediate dispatch (likely within 5-7 hours).

**Epic:** EPIC-DATAHAVEN-UI (Phase 2 of 3)
**Feature:** Flow/Workflow Editor — Interactive epic dependency graph visualization
**Estimate:** 10-14 days (Designer may not be on critical path)

---

## What's Coming

The **Flow/Workflow Editor** component allows users to:
- **Visualize epic dependencies** as an interactive Mermaid.js graph
- **Inspect epic details** (name, status, dependencies)
- **Change epic status** (pending → active → done → blocked)
- **Edit dependencies** (add/remove with cycle detection)
- **Export diagrams** (for documentation, Slack, etc.)

**Placement:** Datahaven Planning page, Workflow tab

---

## Designer's Role (Likely)

While Backend/Frontend handle most of Phase 2 implementation:
- **Icon design** for epic status badges (pending, active, done, blocked) — if needed
- **Mermaid.js theme customization** — colors, node shapes, layout preferences
- **Mobile fallback messaging** — "Desktop required" error state UX
- **Accessibility review** — Graph interactions keyboard-friendly

---

## No Action Required Now

Just be aware. Conductor will send formal task (MSG-DESIGNER-...) once Phase 1 is complete or if Designer work is needed sooner.

---

## Reference Document

Full architecture: `/opt/spaceos/docs/tasks/new/Datahaven_UI_Focus_Flow_Editor_Architecture_v1.md`
- Section 2 — Workflow Editor detailed design
- Section 6.3 — CSS/design guidelines for graph component

---

**Status:** Pending (will dispatch if needed)
**Expected dispatch:** ~2026-06-24 19:00 (approx. 5-7 hours from now)

