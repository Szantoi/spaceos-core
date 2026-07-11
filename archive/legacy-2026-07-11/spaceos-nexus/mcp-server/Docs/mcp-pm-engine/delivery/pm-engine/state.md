---
id: state-pm-engine
title: "State: MCP Project Management Engine"
type: state
project: pm-engine
project_id: mcp-pm-engine
track: both
program: joinerytech-mcpserver
program_state: Docs/program-state.md
updated: 2026-03-04
---

# Project State: MCP Project Management Engine

## Project Overview

| Total Milestones | Total Epics | Active Epic | Project Status |
|:-----------------|:------------|:------------|:---------------|
| 3 | 7 | - | Future (M03+) |

## Milestone Map

| ID | Title | Status |
|:---|:------|:-------|
| M03 | PM Engine and API service | Future |
| M04 | Two-track completeness and multi-domain | Future |
| M05 | Operational hardening and reliability | Future |

## Epic State Map

| ID | Title | State | Owner |
|:---|:------|:------|:------|
| EPIC-14 | PM Schema Design and State.md Seeder | BACKLOG_READY (M03) | Backend Developer |
| EPIC-15 | PM MCP tools (`get_project_state`, `get_next_tasks`, `update_task_status`) | BACKLOG_READY (M03) | Backend Developer |
| EPIC-16 | PM REST API for external applications | BACKLOG_READY (M03) | Backend Developer |
| EPIC-17 | Multi-domain demonstration | BACKLOG_READY (M04) | Backend Developer |
| EPIC-18 | Discovery PM Extension (DWI state in DB) | BACKLOG_READY (M04) | Backend Developer |
| EPIC-19 | AgentOps Evaluator Loop | BACKLOG_READY (M04) | Backend Developer |
| EPIC-20 | Operational hardening (audit, metrics, backup/restore) | BACKLOG_READY (M05) | Backend Developer |

## Mandatory Standards Binding

- `database/standards/00-foundation/two-track.meta-framework.md`
- `database/standards/01-discovery/discovery.work-item.standard.md`
- `database/standards/02-delivery/delivery.process.md`
- `database/standards/02-delivery/epic.fsm-schema.md`
