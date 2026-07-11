---
id: goal-joinerytech-mcpserver
title: "Program Goal: JoineryTech MCP Server"
type: goal
scope: program
project: joinerytech-mcpserver
created: 2026-03-04
updated: 2026-03-04
---

# Program Goal: JoineryTech MCP Server

## Summary

The JoineryTech MCP Server is the single trusted context source for the agent system.
Its goal is to let any AI agent (VS Code Copilot session, external client, or automated pipeline)
receive complete working context through one entry point: identity, task, rules, templates, and knowledge.

**The agent does not search. The agent executes.**

> **Guiding Philosophy:** The two-track meta-framework (`database/standards/00-foundation/two-track.meta-framework.md`) is the overarching guiding principle for this program — governing both what the server delivers to agents and how the server itself is built and operated.

---

## Strategic Direction

JoineryTech.Flow is an adaptable role-based framework with the MCP server as its core.
The value is domain portability: software engineering, CAD, marketing, legal analysis, and more,
by swapping role and workflow configuration.

The program follows five strategic goals:

| # | Strategic Goal | Why it matters |
|:--|:---------------|:---------------|
| 1 | Identity-based context serving | `bootstrap_agent` returns all required context in one call. No file path lookup and no filesystem knowledge required. |
| 2 | Database-first runtime | `database/` remains authoring input, while runtime serving is SQLite-backed, testable, and domain-agnostic. |
| 3 | Unified two-track model | Discovery (hypothesis-driven) and Delivery (FSM-driven) are supported consistently via `track`-aware workflows and templates. |
| 4 | Queryable PM state | Program, milestone, epic, and task state live in SQLite and are queried by MCP tools, not by reading markdown. |
| 5 | Self-improving memory | Session outcomes are stored in episodic memory (SQLite + ChromaDB) and reused by future agents. |

---

## Program Hierarchy

```text
Program: JoineryTech MCP Server  (joinerytech-mcpserver)
├── Project A: mcp-context-server  - Agent Context Server (Modern MCP Architecture)
│   ├── M01 - RBAC and Server Hygiene                     (✅ Closed)
│   │   ├── EPIC-00: Architect Coordination & Audit
│   │   ├── EPIC-01: RBAC Schema Update & Root Cleanup    (✅ Closed)
│   │   ├── EPIC-02: Dead Code Elimination & Static Anal. (🔨 In Progress)
│   │   └── EPIC-08: Write Layer (submit_artifact, FSM)   (🔨 In Progress)
│   │
│   ├── M02 - Modern MCP Architecture & Two-Track Backend  (📋 Planned)
│   │   ├── EPIC-09:  SQLite Context Schema & Seeder
│   │   ├── EPIC-10:  bootstrap_agent() Aggregate Tool
│   │   ├── EPIC-11:  Request Context Middleware & Error Standardization
│   │   ├── EPIC-12:  Episodic Memory (SQLite+FTS5+ChromaDB)
│   │   ├── EPIC-13:  Discovery Track Tools (DWI state support)
│   │   └── EPIC-14:  Modern MCP Transports & Tool Plugin System
│   │
│   └── M03 - Full Maturity & Multi-Domain Portability    (🔮 Future)
│       ├── EPIC-15:  PM Query Tools (read-only in context-server)
│       ├── EPIC-16:  Legacy Tools DB Wrapper Refactor
│       ├── EPIC-17:  Multi-Domain Config & Onboarding
│       └── EPIC-18:  Self-Reflection & Memory Quality
│
└── Project B: mcp-pm-engine  - Project Management Engine (Future, not M01-M03 scope)
    ├── M03 - PM Engine and API service                   (🔮 Future)
    │   ├── EPIC-19: PM Schema and State.md Seeder
    │   ├── EPIC-20: PM MCP tools (get_project_state, get_next_tasks, update_task_status)
    │   └── EPIC-21: PM REST API
    ├── M04 - Two-track completeness and Multi-domain      (🔮 Future)
    │   ├── EPIC-22: Multi-domain PM demonstration
    │   ├── EPIC-23: Discovery PM Extension (DWI state in DB)
    │   └── EPIC-24: AgentOps Evaluator Loop
    └── M05 - Operational hardening and reliability        (🔮 Future)
        └── EPIC-25: Audit, Metrics, Backup and Restore
```

Context DB schema (`agent.db`):

| Entity | Table | Timing | Notes |
|:-------|:------|:-------|:------|
| Roles | `roles`, `permissions` | M02 EPIC-09 | RBAC SQLite backend |
| Workflows | `workflows`, `workflow_steps` | M02 EPIC-10 | Delivery + Discovery templates |
| Templates | `templates`, `template_variables` | M02 EPIC-10 | Artifact + context templates |
| Sessions | `sessions` | M01 EPIC-08 | Agent session tracking |
| Episodes | `episodes`, `episode_highlights` | M02 EPIC-12 | Episodic memory store |
| Learning | `learning_items`, `dwi_phases` | M02 EPIC-13 | Discovery track learning |
| Knowledge | `knowledge_index` | M02 EPIC-09 | ChromaDB sync metadata |
| Audit | `audit_log` | M02 EPIC-11 | Request + tool call history |

---

## Three Core Tracks

### 1. Engineering (Delivery)

- Input: task contract (`epic_id`, `task_id`)
- Context: delivery workflow, template set, FSM state
- Output: code, implementation report, task status update in DB

### 2. Discovery

- Input: observation, hypothesis, research question
- Context: discovery workflow, phase guidance, DWI state
- Output: decisions, validated learning, handoff artifacts

### 3. AgentOps

- Input: session logs, evaluator traces, role performance data
- Context: evaluator workflow + PM dataset tasks
- Output: role calibration, policy updates, evaluation datasets

---

## Principles

| Principle | Description |
|:----------|:------------|
| **Two-track governance** | `two-track.meta-framework.md` is the single overarching guiding philosophy. It governs both what the server delivers to agents and how the server itself is built and operated. |
| Zero-path | Agents receive context, never filesystem paths. |
| Fail-closed RBAC | Unknown role gets no privileged tools. |
| Template-driven output | Agents submit artifacts; they do not write docs directly. |
| Idempotent seeding | Seeders can run repeatedly without duplication. |
| Domain portability | Role and knowledge config can be swapped without core server changes. |
| DB as PM source of truth | Runtime state is authoritative in SQLite; markdown mirrors are transitional. |
| Markdown is transitional | `Docs/` supports transition only; final operation is machine-centric and API-first. |
| Living memory | Episodic tools continuously enrich organizational memory. |

---

## Program Success Criteria

### M01 - Closed (RBAC & Server Hygiene)

Project A (`mcp-context-server`):

- [x] RBAC schema standardized (`RbacFilter` + role permissions)
- [x] Write-layer tools stubbed (EPIC-08: `submit_artifact()`, `update_workflow_state()`)
- [x] SQLite metadata.db initialized with schema (sessions, artifacts, workflow_events, checkpoints)
- [x] Session management (`SessionManager`, `WorkflowStateTracker`)
- [x] No dead code or lint errors (EPIC-02)
- [x] E2E tests passing for RBAC tool filtering + write-layer operations

### M02 - Minimum Maturity (Modern MCP Architecture)

Project A (`mcp-context-server`) — **Focus Area**:

#### EPIC-09: SQLite Context Schema & Seeder

- [ ] `agent.db` schema: roles, permissions, workflows, templates, knowledge_index
- [ ] Idempotent seeder: YAML (`database/roles/`, `database/standards/`) → SQLite at startup
- [ ] RBAC migrated: `RbacFilter` queries SQLite instead of filesystem scanning
- [ ] Performance: <100ms for RBAC check on subsequent calls (cached)
- [ ] E2E: seeder runs clean, verify data integrity (unique constraints, FKs)

#### EPIC-10: `bootstrap_agent()` Aggregate Tool (Modern MCP Best Practice)

- [ ] Single tool call returns complete agent context:

  ```
  bootstrap_agent(domain, role, intent?, track?)
  → { identity, workflow, templates, permissions, session_id, knowledge_chunks, next_steps }
  ```

- [ ] Combines internal calls: `get_role_context()` + `get_workflow()` + `get_templates()` + `new_session()`
- [ ] Response includes `nextSteps`: ordered list of recommended tool calls for agent to follow
- [ ] Session lifecycle: `active` → (agent works) → `submitted` (via `submit_artifact()`) → `completed`
- [ ] Modern pattern: context injection in one call (vs 3–5 RPC calls)
- [ ] E2E: agent can complete minimal workflow with only `bootstrap_agent()` + `submit_artifact()`

#### EPIC-11: Request Context Middleware & Error Standardization (Modern MCP Best Practice)

- [ ] `RequestContext` object: `{ sessionId, role, domain, permissions, timestamp }`
- [ ] Middleware extracts context once per request, passes to all tool handlers (dependency injection)
- [ ] Standardized error response format: `{ isError: true, error: { code, message, details } }`
- [ ] Error codes: `SESSION_NOT_FOUND`, `PERMISSION_DENIED`, `SCHEMA_VALIDATION_ERROR`, `FSM_INVALID_TRANSITION`, etc.
- [ ] Audit logging: every tool call logged with `{ sessionId, role, toolName, timestamp, result }`
- [ ] Response format unified: `{ content: [...], structuredContent?: {...} }` (all tools)
- [ ] E2E: test error propagation and audit trail

#### EPIC-12: Episodic Memory Layer (Self-Improving Memory — Vision Goal #5)

- [ ] SQLite tables: `episodes`, `episode_highlights`, `artifact_relations`
- [ ] ChromaDB collection: `episodic` (semantic search over episode summaries)
- [ ] Tools:
  - `store_experience(session_id, artifact_id, summary, tags?)` → SQLite + ChromaDB
  - `search_experience(query, topK?)` → hybrid FTS5 + semantic search
  - `reflect_session(session_id)` → auto-generate episode highlights + add to ChromaDB
- [ ] Reuse example: when agent sees similar problem, `search_experience()` surfaces past solutions
- [ ] E2E: submit artifact → auto-store as episode → retrieve in future similar task

#### EPIC-13: Discovery Track Tools (Unified Two-Track Model — Vision Goal #3)

- [ ] Discovery workflow support (`dwi-state.md` semantics: Observe → Define → Ideate → Prototype → Learn)
- [ ] Tools:
  - `store_learning(learning_id, phase, findings, evidence_artifact_id)` → SQLite + artifact relations
  - `search_learning(query, phase?)` → find validated learning in Discovery track
  - `get_dwi_context(learning_id)` → current DWI state + phase gates
- [ ] Track-aware context: `bootstrap_agent(..., track="discovery")` returns Discovery workflow (not Delivery FSM)
- [ ] Definition of Ready gate: learning must be `validated` to handoff to Delivery (EPIC-14 dependency)
- [ ] E2E: Discovery agent stores learning → validated → Delivery agent retrieves via `get_dwi_context()`

#### EPIC-14: Modern MCP Transport & Tool Architecture (Best Practices)

- [ ] Transport auto-detection: client capability → auto-select Streamable HTTP or SSE
- [ ] Tool plugin system: modular registration (avoid monolithic `buildMcpServer()`)

  ```typescript
  interface ToolPlugin {
    register(server: McpServer, context: RequestContext): void;
  }
  // Usage: plugins = [RolePlugin, WriteLayerPlugin, EpisodePlugin, ...]
  ```

- [ ] Resource templates for dynamic access:

  ```
  artifact://{artifact_id} (stream artifact content)
  learning://{learning_id} (stream learning findings)
  episode://{episode_id} (stream episode summary)
  ```

- [ ] Notification-based bulk operations: batch artifact submissions (debounce 100ms)
- [ ] Sampling for LLM-assisted decisions: `recommend_next_task()` uses server.createMessage()
- [ ] E2E: test new transport detection, resource streaming, bulk notification

### M03 - Full Maturity & Multi-Track Completeness

Project A (`mcp-context-server`):

#### EPIC-15: PM Query Tools (Database-First State — Vision Goal #4)

- [ ] NOT pm-engine server; read-only query tools in context-server for Delivery agents
- [ ] Tools:
  - `get_epic_state(epic_id)` → current FSM state (from PM DB, not markdown)
  - `list_active_tasks(role, epic_id?)` → paginated, filtered
  - `get_task_dependencies(task_id)` → dependency graph (no markdown parsing)
- [ ] No write tools here (write layer stays in EPIC-08 submit/update)
- [ ] E2E: query PM state without reading any `state.md` file

#### EPIC-16: Legacy Tools DB Wrapper Refactor

- [ ] Existing tools (`get_role`, `get_role_context`, `search_knowledge`) → backward-compatible
- [ ] Internal implementation: SQL queries instead of filesystem ops
- [ ] Performance: cached responses + TTL invalidation
- [ ] Deprecation path: mark old file-based endpoints as legacy (no new dependers)
- [ ] E2E: test backward compatibility + measure improvement

#### EPIC-17: Multi-Domain Configuration & Onboarding (Domain Portability — Vision Goal #1)

- [ ] Config-driven domain onboarding: new domain = new role + knowledge YAML only (no code changes)
- [ ] Domain context: role schema → DB; knowledge.md → ChromaDB index
- [ ] Multi-domain E2E: demonstrate engineering + marketing domain in same server

  ```
  bootstrap_agent(domain="marketing", role="campaign_manager")
  vs
  bootstrap_agent(domain="engineering", role="backend_developer")
  // Same server, different workflows + templates + knowledge
  ```

- [ ] Security validation: fail-closed for unknown domain/role

#### EPIC-18: Self-Reflection & Memory Quality (Episodic + Learning Integration)

- [ ] `reflect_on_session()`: auto-analyze episode + highlight insights

  ```
  input: session_id, outcomes
  → creates: episode_highlights (key patterns, risks, reusable snippets)
  → index: ChromaDB episodic + learning collections
  ```

- [ ] Measurable reuse: track how often `search_experience()` surfaced past solutions
- [ ] Quality feedback: episodic searches rated by agent (thumbs up/down) → training signal
- [ ] E2E: agent solves problem, system learns + improves future recommendations

Project B (`mcp-pm-engine`) — **NOT IN SCOPE FOR M01–M03**:

*See separate planning documents; PM Engine remains independent*

---

## Scope Boundaries

### In Scope

- MCP server TypeScript/Node.js implementation
- Serving `database/roles/**` and `database/standards/**`
- SQLite context DB (`agent.db`)
- SQLite PM DB (`programs -> projects -> milestones -> epics -> tasks`)
- RBAC, session management, and artifact submit write layer
- PM MCP tools: `get_project_state`, `get_next_tasks`, `update_task_status`
- ChromaDB semantic RAG for `.knowledge.md`
- Episodic memory (`episodes`, `episode_highlights`, `episodic` collection)
- RAG tools: `store_experience`, `search_experience`, `reflect_session`
- Unit and E2E tests

### Out of Scope

- Direct LLM invocation inside MCP server runtime
- Frontend UI
- Cloud deployment details
- Direct DB writes by agents outside governed tool workflows

---

## Two-Track Governance (Mandatory Standards Binding)

The `two-track.meta-framework.md` is the **single overarching guiding philosophy** of this program.
It applies **bidirectionally**: to the projects the server manages, and to the server's own development and operational lifecycle.

| Direction | What it governs |
|:----------|:----------------|
| **Outward** | Context served to agent sessions: Discovery DWI state, Delivery FSM workflows, AgentOps evaluator traces. |
| **Inward** | The server's own epics and tasks follow FSM lifecycle; own research/spikes use DWI semantics. |

Mandatory standards:

- **[`database/standards/00-foundation/two-track.meta-framework.md`](../database/standards/00-foundation/two-track.meta-framework.md)** — overarching guiding philosophy
- `database/standards/01-discovery/discovery.work-item.standard.md`
- `database/standards/02-delivery/delivery.process.md`
- `database/standards/02-delivery/epic.fsm-schema.md`

Operational implications:

- Discovery track uses DWI (`dwi-state.md`) semantics and phase gates (EPIC-18).
- Delivery track uses deterministic FSM lifecycle for epics/tasks — including the server's own EPIC-09..20.
- AgentOps uses trace-driven evaluator datasets and run history (EPIC-19).
- Discovery to Delivery transfer is allowed only through Definition of Ready (DoR).
