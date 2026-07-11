---
id: MSG-ARCHITECT-011
from: conductor
to: architect
type: task
priority: high
status: READ
model: opus
ref: Datahaven_UI_Focus_Flow_Editor_Architecture_v1.md
created: 2026-06-24
processed: 2026-06-24
content_hash: 629f2508490758e03b6f3c592a21a5217bb923c81a2eebf1a790fa1ae48797cc
---

# Datahaven UI — Break Architecture into Implementation Plans

## Request

The Architect terminal has completed a comprehensive architecture design document for two new Datahaven Dashboard UI features:

**Document:** `/opt/spaceos/docs/tasks/new/Datahaven_UI_Focus_Flow_Editor_Architecture_v1.md`

### Features Designed

1. **Focus Area Panel** — Domain focus selector + criteria editor
2. **Flow/Workflow Editor** — Interactive EPICS.yaml dependency graph editor

The design is **complete with mockups, API specs, CSS guidelines, roadmap, and implementation checklist**.

## Task: Break into Implementation Plans

Please create **2-3 concrete implementation plan documents** (in `/opt/spaceos/docs/planning/queue/`) that are ready for:
- Backend terminal implementation
- Frontend terminal implementation
- Testing & validation

### Plan Structure Expected

For each plan, provide:
1. **Epic ID** — Which epic this belongs to (e.g., `EPIC-DATAHAVEN-UI`)
2. **Phase breakdown** — Focus Area Panel (Phase 1) vs Flow Editor (Phase 2) vs Polish (Phase 3)
3. **Specific tasks** — Concrete backend/frontend subtasks (<4h each)
4. **Dependencies** — API first, then UI, then tests
5. **Success criteria** — From document section 11

### Example Breakdown

```
Plan A: Focus Area Panel Implementation (Backend)
  - [ ] API GET /api/planning/domain-focus
  - [ ] API PUT /api/planning/domain-focus
  - [ ] Markdown sanitization + validation
  - [ ] Tests

Plan B: Focus Area Panel Implementation (Frontend)
  - [ ] HTML structure in planning.html
  - [ ] Domain dropdown + criteria display JS
  - [ ] Edit mode + save handler
  - [ ] CSS styles + responsive design
  - [ ] Tests

Plan C: Flow Editor Phase 1 (Mermaid visualization)
  - [ ] GET /api/graph/mermaid/epic/EPICS (may exist)
  - [ ] Frontend: Load Mermaid library
  - [ ] Frontend: Render graph in Workflow tab
  - [ ] Frontend: Click handlers for node selection
```

## Deliverable

Create consensus-ready plan files in `/opt/spaceos/docs/planning/queue/`:
- File naming: `2026-06-24_consensus_<description>.md`
- Frontmatter: Include domain focus criteria (manufacturing, sales, logistics, etc.)
- Format: Follow existing planning consensus format (see other queue files)

The planning pipeline will pick these up for automatic debate → consensus → queue staging.

## Timing

- **Focus Area Panel:** 5-7 days (backend + frontend parallel, then tests)
- **Flow Editor Phase 1:** 10-14 days
- **Polish:** 3-5 days

Total: **18-26 days** (see document section 7)

## Context Files

- Architecture doc: `/opt/spaceos/docs/tasks/new/Datahaven_UI_Focus_Flow_Editor_Architecture_v1.md`
- Existing consensus format: `/opt/spaceos/docs/planning/selected/` (examples)
- Planning queue: `/opt/spaceos/docs/planning/queue/` (target location)

---

## Why This is Valuable

The current planning queue is **empty**. This architecture is **complete and ready**, but needs to be broken into:
1. **Smaller, dispatchable tasks** (focus on one feature at a time)
2. **Explicit dependencies** (API before UI, validation before save)
3. **Consensus-ready format** (so pipeline can debate + approve → dispatch to terminals)

Once Architect provides these plans, Conductor can dispatch them to Backend, Frontend, and Designer terminals immediately.

---

**Please respond with:**
- ✅ DONE: 2-3 plan files created in queue/
- ❌ BLOCKED: If you need clarification on planning format or feature scope
- ? QUESTION: If assumptions about placement/timing/priority need validation

