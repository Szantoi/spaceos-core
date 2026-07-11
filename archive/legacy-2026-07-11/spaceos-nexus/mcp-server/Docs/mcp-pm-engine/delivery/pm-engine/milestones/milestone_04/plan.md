---
id: plan-pm-engine-m04
title: "Milestone 04: Two-Track Completeness and Multi-Domain Demonstration"
type: milestone
project: pm-engine
project_id: mcp-pm-engine
status: planned
depends_on: M03
created: 2026-03-04
track: both
---

# M04 (PROJ-A+B): Two-Track Completeness and Multi-Domain Demonstration

## Context

After M03, delivery-side PM capabilities are available.
M04 completes the two-track model and proves domain portability.

## Mandatory Standards

- `database/standards/00-foundation/two-track.meta-framework.md`
- `database/standards/01-discovery/discovery.work-item.standard.md`
- `database/standards/02-delivery/delivery.process.md`
- `database/standards/02-delivery/epic.fsm-schema.md`

## Epics

| ID | Title | Track | Priority | Status |
|:---|:------|:------|:---------|:-------|
| EPIC-17 | Multi-domain demonstration | both | P0 | Planned |
| EPIC-18 | Discovery PM Extension (DWI state in DB) | discovery | P0 | Planned |
| EPIC-19 | AgentOps Evaluator Loop | agentops | P1 | Planned |

## Success Criteria

- [ ] Discovery DWI states are queryable from DB (`current_phase`, `next_action`, `verdict`, `status`).
- [ ] Delivery epic/task lifecycle remains FSM-compliant.
- [ ] `bootstrap_agent(domain="marketing", role="campaign_manager")` returns full context.
- [ ] At least one Discovery to Delivery DoR handoff is demonstrated.
