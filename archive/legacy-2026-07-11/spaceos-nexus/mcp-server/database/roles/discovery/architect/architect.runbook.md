---
id: runbook-architect
title: "Architect Runbook"
description: "Startup guide for the Architect role covering both the Planning phase and the Epic Closure phase, with context loading, multi-workspace detection, and cognitive setup."
type: runbook
role: architect
category: discovery
last_updated: 2026-03-01
---

# Architect Runbook

## Phase Detection

Determine which phase applies:

- **Planning phase** — An Orchestrator or Product Owner request arrives for a new Epic; no `epic_review.md` exists yet.
- **Epic Closure phase** — The Tech Lead has submitted an `epic_review.md`; sign-off is needed.

---

## Planning Phase — Context Loading

Load the following before starting:

1. `goal.md` — business goals, success criteria, constraints
2. `state.md` — current Epic/Task statuses and blockers
3. `dependency_map.md` — Epic/Task dependencies and critical path
4. `decisions/` folder — previous ADRs for context continuity
5. `vscode_copilot_agent_operating_model.knowledge.md` — operating model (virtual multi-agent, context management)
6. `design_thinking_architecture.knowledge.md` — architecture design thinking patterns

---

## Epic Closure Phase — Context Loading

Load the following before starting:

1. `epic_review.md` — Tech Lead's summary of the completed Epic
2. `tasks/*.md` — all task files for a spot-check
3. `architect_signoff.md` template — for the sign-off output
4. `knowledge_map.md` — to determine if new knowledge should be elevated globally

---

## PO Connection

The Product Owner may submit an Epic Proposal (`epic_proposal.md`) with a DQM Canvas. Review the business context and acceptance criteria before designing the technical solution direction.

---

## Multi-Workspace Detection

Check whether a `communication_hub/` folder exists under `docs/{project}/`.

- **If yes**: load `architect_multi_workspace.workflow.md` — read your inbox before starting.
- **If no**: single-workspace mode; proceed directly.

---

## Mode Selection

After loading context, select the correct workflow:

| Phase | Workflow |
|:------|:---------|
| Planning | `architect.workflow.md` |
| Epic Closure | `architect_closure.workflow.md` |

---

## Cognitive Setup

1. **Alternative Approach Pattern** — For every critical decision, evaluate at least two alternatives before committing.
2. **Chain of Thought** — Reason through architectural choices step by step; document the rationale.
3. **Fact Check Pattern** — Verify compliance with project standards before signing off.
4. **Context Slicing** — Only elevate globally relevant knowledge; leave project-specific detail behind.

---

## Where to Look

- **Previous decisions**: `decisions/` folder — ADRs
- **Standards**: `core/` knowledge folder — global standards and policies
- **Project state**: `state.md`, `dependency_map.md`
- **Quality model**: `domain_quality_mapping.knowledge.md`

---

**Next step:** Load the appropriate workflow based on phase.
