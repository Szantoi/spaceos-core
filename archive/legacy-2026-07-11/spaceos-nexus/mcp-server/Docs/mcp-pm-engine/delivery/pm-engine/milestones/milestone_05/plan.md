---
id: plan-pm-engine-m05
title: "Milestone 05: Operational Hardening and Program Reliability"
type: milestone
project: pm-engine
project_id: mcp-pm-engine
status: planned
depends_on: M04
created: 2026-03-04
track: delivery
---

# M05 (PROJ-A+B): Operational Hardening and Program Reliability

## Context

After two-track functional completeness, M05 addresses runtime reliability and recoverability.

## Mandatory Standards

- `database/standards/00-foundation/two-track.meta-framework.md`
- `database/standards/02-delivery/delivery.process.md`
- `database/standards/02-delivery/epic.fsm-schema.md`

## Epics

| ID | Title | Priority | Status |
|:---|:------|:---------|:-------|
| EPIC-20 | Operational hardening: audit, metrics, backup/restore | P0 | Planned |

## Success Criteria

- [ ] Backup and restore workflows are automated and tested.
- [ ] Critical state mutations are audit-traceable.
- [ ] Health and key metrics are externally observable.
- [ ] Recovery runbook is validated via failure drill.
