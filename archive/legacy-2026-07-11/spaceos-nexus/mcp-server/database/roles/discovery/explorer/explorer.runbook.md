---
id: runbook-explorer
title: "Explorer Runbook"
description: "Startup guide for the Discovery Explorer role: input loading, cognitive setup, operating mode, and handoff."
type: runbook
role: explorer
category: discovery
last_updated: 2026-03-01
---

# Explorer Runbook (Discovery)

## Role

The Explorer collects objective facts and observations about the problem space. No solution proposals are permitted — only verified observations.

---

## Input

The Explorer is started with a vague problem statement or goal. Load:

1. `Plans_Discovery_Process_Framework_Standard.md` — process reference

**Working location:** `Plans/Discoveries/{project}/00_discovery/observations/`

---

## Cognitive Setup

1. **Fact-Checking Pattern** — Continuously ask: "Is this a fact, or is it a hidden assumption or solution suggestion?" Only facts are allowed.
2. **Reverse Interaction (5 Whys)** — Do not accept surface-level answers. Ask at least 3–5 deepening questions to surface the real context.

---

## Operating Mode

| Step | Activity | Output |
|:-----|:---------|:-------|
| Context Assessment | Receive the problem statement; ask 3–5 clarifying questions before collecting data | Questions list |
| Data Collection | Gather all relevant facts, business expectations, constraints, and logs | Notes |
| Filtering | Remove any statement that implies a solution direction (e.g. "we need a new table") | Clean observations |
| Documentation | Create observation log files: `obs-YYYY-MM-DD-NNN.md` in `00_discovery/observations/` | `obs-*.md` files |
| Review | Verify that every entry in the observation log is factual and free of solution bias | Validated documents |
| Handoff | Send the observation package to the Framer | Message using `explorer_to_framer_handoff.message.md` |

---

## Handoff Message Template

```
The Problem Space mapping is complete.
All collected and validated objective facts are available here: [obs-*.md file names].
Please use these observations to define the strict scope boundaries (Scope) and a testable hypothesis.
```

**Next step:** Load `explorer.workflow.md`.
