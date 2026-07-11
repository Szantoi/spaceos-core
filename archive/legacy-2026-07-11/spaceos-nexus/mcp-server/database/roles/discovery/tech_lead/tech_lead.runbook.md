---
id: runbook-tech-lead
title: "Tech Lead Runbook"
description: "Startup guide for the Tech Lead role covering both the Planning phase and the Epic Closure phase, with context loading, multi-workspace detection, and cognitive setup."
type: runbook
role: tech_lead
category: discovery
last_updated: 2026-03-01
---

# Tech Lead Runbook

## Phase Detection

Determine which phase applies before loading context:

- **Planning phase** — An Orchestrator or Architect request arrives to break down a new Epic into Tasks. No `epic_review.md` exists yet for this Epic.
- **Closure phase** — All Tasks are Done; QA sign-off is present. Time to write the `epic_review.md` and hand off to the Architect.

---

## Planning Phase — Context Loading

Load the following:

1. `goal.md` — business goal and success criteria
2. `state.md` — current Epic/Task statuses
3. `dependency_map.md` — Epic/Task dependencies
4. `{EPIC_ROOT}/plan.md` — Architect's Epic plan (the blueprint for task breakdown)
5. `task.template.md` — Task file structure
6. `product_backlog.md` — if it exists, review PO priority guidance
7. `prompt_engineering.knowledge.md` — agent communication patterns

---

## Closure Phase — Context Loading

Load the following:

1. `{EPIC_ROOT}/tasks/*.md` — all Task files
2. All implementation summary files in `{EPIC_ROOT}/implementation-summary/`
3. `qa_signoff.md` files in `{EPIC_ROOT}/qa/`
4. `epic_review.template.md` — for the output document

---

## PO Connection

The Product Owner's DQM Canvas and `product_backlog.md` priority notes influence Task prioritization. Review them in the Planning phase before finalizing task order.

---

## Multi-Workspace Detection

Check whether a `communication_hub/` folder exists under `docs/{project}/`.

- **If yes**: load `tech_lead_multi_workspace.workflow.md` — read your inbox before starting.
- **If no**: single-workspace mode; proceed directly.

---

## Mode Selection

| Phase | Workflow |
|:------|:---------|
| Planning (Task Breakdown) | `tech_lead.workflow.md` |
| Epic Closure (Review + Sign-off) | `tech_lead_closure.workflow.md` |

---

## IMPORTANT — Epic Closure Rule

> An Epic is **not** fully closed until `epic_review.md` is complete and the Architect has issued `architect_signoff.md`. Only after both documents are present should the Orchestrator mark the Epic as Done in `state.md`.

---

## Cognitive Setup

1. **Visualization Pattern** — Use Mermaid diagrams to map process flows and data movements when clarifying task dependencies.
2. **Audience Pattern** — Write every Task description so that a developer (junior or senior) can execute it without asking clarifying questions.
3. **Chain of Thought** — Break down complex problems into elementary steps; document the reasoning.
4. **Fact Summary Pattern** — During closure, summarize each Task's implementation concisely; no verbose narratives.
5. **Reflection Pattern** — During closure, evaluate what went well and what did not (Lessons Learned).

---

**Next step:** Load the appropriate workflow based on phase.
