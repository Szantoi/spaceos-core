---
id: epic-pm-engine-19
title: "Epic 19: AgentOps Evaluator Loop - Dataset and Evaluation Pipeline"
type: epic
milestone: M04
project: pm-engine
project_id: mcp-pm-engine
track: agentops
status: planned
fsm_workflow_id: "agile-epic-lifecycle-v1"
fsm_state: "BACKLOG_READY"
fsm_retry_count: 0
created: 2026-03-04
assignee: backend_developer
depends_on: EPIC-12
---

# EPIC-19: AgentOps Evaluator Loop - Dataset and Evaluation Pipeline

## Objective

Operationalize trace-driven evaluator cycles with persistent DB evidence for role quality and drift control.

## Standards Binding

- `database/standards/03-agent-system/agent.development-framework.md`
- `database/standards/00-foundation/two-track.meta-framework.md`

## Scope

- DB entities for datasets, runs, and results
- MCP tools for run lifecycle and summary retrieval
- Role+version baseline reports
- Link recurring evaluator failures to memory insights

## Tasks

| Type | ID | Task | Estimate | Status |
|:-:|:---|:-----|:---------|:-------|
| Arch | T19-01 | Evaluation schema and scoring model | 0.5 day | Planned |
| Dev | T19-02 | AgentDb migrations and query layer | 1 day | Planned |
| Dev | T19-03 | Evaluator MCP tool implementation | 1 day | Planned |
| Dev | T19-04 | Evaluation summary/report endpoint | 0.5 day | Planned |
| QA | T19-05 | E2E fail-fix-pass evaluation cycle | 1 day | Planned |

## Definition of Done

- [ ] Versioned evaluator datasets are stored in DB.
- [ ] Evaluation history is queryable by role and date.
- [ ] Red-team failures are visible in reports.
- [ ] At least one role has a complete recorded baseline run.
