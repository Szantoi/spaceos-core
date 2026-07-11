# JoineryTech MCP Server - Program Vision and Strategy

**Type:** Program Vision
**Last Updated:** 2026-03-04
**Related:** [goal.md](goal.md)

---

> **Guiding Philosophy**
>
> The **two-track meta-framework** ([`database/standards/00-foundation/two-track.meta-framework.md`](../database/standards/00-foundation/two-track.meta-framework.md)) is the overarching guiding principle of this entire program.
> It governs not only the projects and workflows the MCP server manages — it also governs how the MCP server itself is **built, operated, and evolved**.
> All epics, tooling decisions, operational processes, and governance rules derive from this framework.

---

## 1. Core Objective

Build an adaptable role-based framework with the MCP server as backbone,
so AI agents operate through clear role identities and deterministic workflows.

The platform is not software-only. Roles and workflows are configurable,
so the same server can support engineering, CAD, marketing, legal, and other domains.

**Core principle:** The agent does not search. The agent receives identity and task context, executes, and submits results.

---

## 2. Two Program Subsystems

The program has two logically separated subsystems running on the same MCP server.
They can be developed and tested independently.

### Subsystem A - Agent Context Server

**Project ID:** `mcp-context-server`
**Milestones:** M01, M02, M03

Responsibilities:

- Single entry point via `bootstrap_agent`
- SQLite-based RBAC with fail-closed behavior
- Role/runbook/workflow/template payloads served from DB
- Knowledge RAG integration (ChromaDB)
- Session creation and lifecycle tracking
- Template-based artifact submission

Target state: one `bootstrap_agent` call is enough to start work.

### Subsystem B - Project Management Engine

**Project ID:** `mcp-pm-engine`
**Milestones:** M03, M04, M05

Responsibilities:

- PM hierarchy in SQLite (`programs`, `projects`, `milestones`, `epics`, `tasks`)
- Queryable state via `get_project_state`
- Role-aware next task retrieval via `get_next_tasks`
- Controlled status mutation via `update_task_status`
- Transitional seeding from docs

Target state: Orchestrator can query and dispatch work without reading markdown status files.

---

## 3. Three Operational Tracks

### Engineering (Delivery)

- Input: epic and task identifiers
- Context source: Context Server + PM Engine
- Output: code and delivery evidence

### Discovery

- Input: observation, hypothesis, research focus
- Context source: Context Server + DWI state model
- Output: validated learning and handoff artifacts

### AgentOps

- Input: session and evaluator traces
- Context source: Context Server + PM evaluator tasks
- Output: role calibration and policy improvements

---

## 4. Portability by Role Configuration

The core server and DB schema remain stable.
Domain behavior is changed by role and knowledge content.

```text
Domain-specific layer:
  database/roles/<domain>/<role>/
  database/knowledge/<domain>/

Domain-agnostic layer:
  MCP server runtime (TypeScript)
  agent.db schema
  bootstrap_agent and PM tooling
```

To onboard a new domain:

1. Add `database/roles/<domain>/`
2. Add `database/knowledge/<domain>/`
3. Run seeders
4. Call `bootstrap_agent(domain="...", role="...")`

---

## 5. Role of Markdown During Transition

`Docs/` currently preserves human-readable state during migration.
Final target is machine-centric operation with DB and API as runtime truth.

| Stage | Role of markdown | Human observation |
|:------|:------------------|:------------------|
| Transition now | Primary human mirror | Read docs |
| After M03 | DB authoritative, docs optional mirrors | API and DB |
| Final state | Runtime independent from markdown | External app through API |

---

## 6. Organizational Memory Model

Memory is split by type and stored in fit-for-purpose layers:

| Memory Type | Source | Access |
|:------------|:-------|:-------|
| Identity memory | roles, schemas, runbooks in SQLite | `bootstrap_agent` |
| Process memory | workflows and templates in SQLite | `bootstrap_agent`, intent tools |
| Reference memory | ChromaDB chunks from `.knowledge.md` | `search_knowledge` |
| Episodic memory | SQLite + ChromaDB episodic store | `store_experience`, `search_experience` |
| PM state memory | PM hierarchy in SQLite | PM tools and API |
| Session memory | sessions and workflow state | session tools |

---

## 7. RAG as a Growing Memory Layer

RAG is not static documentation lookup only.
It is a growing organizational memory with semantic, episodic, and procedural dimensions.

- Semantic memory: curated knowledge files
- Episodic memory: outcome summaries from sessions
- Procedural memory: recurring patterns distilled from history

Selected architecture: SQLite (FTS5) + ChromaDB hybrid.

---

## 8. Program Roadmap

| Milestone | Project | Focus | Key Result |
|:----------|:--------|:------|:-----------|
| **M01** (Closed) | `mcp-context-server` | RBAC and hygiene | Role schemas standardized, RBAC tests passing |
| **M02** (Planned) | `mcp-context-server` | SQLite context backbone | Bootstrap active, RBAC from DB, episodic tools active |
| **M03** (Future) | `mcp-context-server` | Legacy tools DB-first refactor | Runtime no longer depends on filesystem tool paths |
| **M03** (Future) | `mcp-pm-engine` | PM Engine and API | PM tools and external API replace markdown state reading |
| **M04** (Future) | PROJ-A+B | Two-track completeness and multi-domain | EPIC-17, EPIC-18, EPIC-19 delivered |
| **M05** (Future) | PROJ-A+B | Operational reliability | EPIC-20 delivered (audit, metrics, backup/restore) |

---

## 9. Two-Track Standards-Driven Governance

The `two-track.meta-framework.md` is the single governing philosophy. This applies **bidirectionally**:

| Direction | Scope |
|:----------|:------|
| **Outward** | The MCP server serves Discovery and Delivery contexts to agent sessions. |
| **Inward** | The MCP server's own development, epics, and operational processes follow the same two-track model. |

Mandatory standards binding:

- **[`database/standards/00-foundation/two-track.meta-framework.md`](../database/standards/00-foundation/two-track.meta-framework.md)** — overarching guiding philosophy
- `database/standards/01-discovery/discovery.work-item.standard.md`
- `database/standards/02-delivery/delivery.process.md`
- `database/standards/02-delivery/epic.fsm-schema.md`

Operating model:

- Discovery track: DWI phase/state model (outward: served; inward: own spikes/research use DWI)
- Delivery track: deterministic FSM lifecycle (outward: served; inward: all server epics use FSM)
- AgentOps track: evaluator loop with persistent evidence
- Discovery to Delivery handoff: strict DoR gate — both for managed projects and for the server's own epics
