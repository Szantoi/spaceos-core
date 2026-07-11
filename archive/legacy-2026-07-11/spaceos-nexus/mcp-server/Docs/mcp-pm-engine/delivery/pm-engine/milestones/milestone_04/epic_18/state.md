---
id: epic-pm-engine-18
title: "Epic 18: Discovery PM Extension - DWI State in DB"
type: epic
milestone: M04
project: pm-engine
project_id: mcp-pm-engine
track: discovery
status: planned
fsm_workflow_id: "agile-epic-lifecycle-v1"
fsm_state: "BACKLOG_READY"
fsm_retry_count: 0
created: 2026-03-04
assignee: backend_developer
depends_on: EPIC-15
---

# EPIC-18: Discovery PM Extension - DWI State in DB

## Objective

Make Discovery state DB-first so Orchestrator can query Discovery progress without reading markdown files at runtime.

## Standards Binding

- `database/standards/00-foundation/two-track.meta-framework.md`
- `database/standards/01-discovery/discovery.work-item.standard.md`

## Scope

- Persist DWI fields and phase history in SQLite
- Extend seeding from Discovery `dwi-state.md`
- Add track-aware PM queries (`track=discovery`)
- Keep delivery query behavior backward-compatible

## Tasks

| Type | ID | Task | Estimate | Status |
|:-:|:---|:-----|:---------|:-------|
| Arch | T18-01 | DWI-to-DB schema design | 0.5 day | Planned |
| Dev | T18-02 | AgentDb migration and query methods | 1 day | Planned |
| Dev | T18-03 | Seeder extension for DWI parsing | 1 day | Planned |
| Dev | T18-04 | PM tool response extension with `track` | 0.5 day | Planned |
| QA | T18-05 | E2E DB-only Discovery query scenario | 1 day | Planned |

## Definition of Done

- [ ] At least one Discovery topic is fully queryable from DB.
- [ ] `next_action` role+artifact structure remains compliant with DWI standard.
- [ ] Delivery query contracts remain stable.
- [ ] Seeder remains idempotent.
