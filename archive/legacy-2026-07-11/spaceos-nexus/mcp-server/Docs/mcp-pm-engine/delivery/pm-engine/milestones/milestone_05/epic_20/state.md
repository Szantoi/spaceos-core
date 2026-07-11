---
id: epic-pm-engine-20
title: "Epic 20: Operational Hardening - Audit, Metrics, Backup and Restore"
type: epic
milestone: M05
project: pm-engine
project_id: mcp-pm-engine
track: delivery
status: planned
fsm_workflow_id: "agile-epic-lifecycle-v1"
fsm_state: "BACKLOG_READY"
fsm_retry_count: 0
created: 2026-03-04
assignee: backend_developer
depends_on: EPIC-16
---

# EPIC-20: Operational Hardening - Audit, Metrics, Backup and Restore

## Objective

Add production-grade operational safeguards to PM and Context services.

## Standards Binding

- `database/standards/02-delivery/delivery.process.md`
- `database/standards/02-delivery/epic.fsm-schema.md`

## Scope

- Automated backup/restore for `agent.db` and vector memory
- Unified audit logging for MCP and REST state mutations
- Metrics export and detailed health endpoints
- Recovery runbook for common incident patterns

## Tasks

| Type | ID | Task | Estimate | Status |
|:-:|:---|:-----|:---------|:-------|
| Arch | T20-01 | Reliability architecture and SLO targets | 0.5 day | Planned |
| Dev | T20-02 | Backup and restore automation | 1 day | Planned |
| Dev | T20-03 | Unified audit logging pipeline | 1 day | Planned |
| Dev | T20-04 | Metrics and health detail implementation | 1 day | Planned |
| QA | T20-05 | Restore drill and failure injection tests | 1 day | Planned |

## Definition of Done

- [ ] Restore drill meets target RTO.
- [ ] Write actions are traceable to actor/session.
- [ ] Metrics are exportable for external dashboards.
- [ ] Recovery checklist is tested and documented.
