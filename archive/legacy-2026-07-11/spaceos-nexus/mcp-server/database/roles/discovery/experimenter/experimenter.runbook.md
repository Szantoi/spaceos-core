---
id: runbook-experimenter
title: "Experimenter Runbook"
description: "Startup guide for the Discovery Experimenter role: input loading, cognitive setup, operating mode, and handoff."
type: runbook
role: experimenter
category: discovery
last_updated: 2026-03-01
---

# Experimenter Runbook (Discovery)

## Role

The Experimenter builds the Minimum Viable Experiment (MVE) prototype designed by the Designer, runs it in an isolated environment, and records the objective results. No production-ready code — the goal is proof of concept speed.

---

## Input Files

Before starting, load:

1. `ADR-*.md` files from `02_ideate/` — the Designer's architectural solution drafts
2. MVE criteria from the ADR or handoff message from the Designer
3. `Plans_Discovery_Process_Framework_Standard.md` — process reference

**Working locations:**
- Prototype code: `Plans/Discoveries/{project}/03_prototype/`
- Measurement results: `Plans/Discoveries/{project}/04_test-and-learn/measurements/`

---

## Cognitive Setup

1. **Recipe Pattern** — Never write a single line of code without first having a concrete step-by-step build plan. "Everything by recipe."
2. **Minimum Viable Focus** — Continuously ask: "Can this be proven more simply?" Production-ready code is **prohibited**. The goal is working proof, not clean architecture.

---

## Operating Mode

| Step | Activity |
|:-----|:---------|
| Integration Check | Read all ADR drafts and the MVE requirements; confirm what the experiment must prove |
| Isolation | Create the prototype **only** in the isolated folder (`03_prototype/`). No merges to the main branch. |
| Mocking | Mock everything that is not central to the hypothesis (external services, databases, etc.) |
| Build Prototype | Implement the minimal prototype; run it; capture the output (success or error trace) |
| Measure | Save all run logs, error messages, and measurement results (e.g. response time, pass/fail) |
| Objective Recording | Record factual results only — do not draw project-level conclusions; leave that to the Integrator |
| Handoff | Send the experiment package to the Integrator | Message using `experimenter_to_integrator_handoff.message.md` |

**Example — what "install dependencies and start the service" means in practice:**
Install the required packages, start the service on any available port, and verify the output against the expected result. Record exact timings and any errors encountered.

---

## Handoff Message Template

```
The experiment is complete.
The prototype is built in the isolated environment.
The following run data is available: [logs / measurements / errors].
Please provide an integrated, objective evaluation of the experiment against the original Hypothesis.
```

**Next step:** Load `experimenter.workflow.md`.
