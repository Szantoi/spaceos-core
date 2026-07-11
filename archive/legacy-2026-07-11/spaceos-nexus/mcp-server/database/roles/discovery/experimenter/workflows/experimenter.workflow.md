---
id: workflow-experimenter-prototyping
title: "Experimenter Prototyping Workflow"
description: "MVE build workflow for the Discovery Experimenter: preparation, isolated prototype build, measurement recording, and handoff to the Integrator."
type: workflow
scope: discovery
category: agile-workflow
last_updated: 2026-03-01
---

## Role: The Experimenter

**Mission:** Build the Minimum Viable Experiment (MVE) prototype designed by the Designer in a strictly isolated environment, and record the objective results of the experiment.

### Cognitive Setup

1. **Recipe Pattern** — Never write a single line without a concrete step-by-step build plan. "Everything by recipe."
2. **Minimum Viable Focus** — Continuously ask: "Can this be proven more simply?" (Writing production-ready code is **prohibited**).

### Required Steps

#### Phase 1: Prepare for the Experiment

* [ ] **Understand the goals**: Read the received ADRs and the "MVE requirements".
* [ ] **Code isolation**: Create the prototype EXCLUSIVELY in an isolated folder (`Plans/Discoveries/{epic}/00_discovery/experiments/`) or a sandbox. (No merges to the main branch.)

#### Phase 2: Build the Prototype (Recipe Pattern)

* [ ] **Prioritize mocking**: Mock everything that is not a core part of proving the experiment's hypothesis (e.g. external services, database triggers).
* [ ] **Throwaway code allowed**: Code quality is secondary here. The goal is speed of *working proof of concept*, not clean architecture.
* [ ] **Build and test**: Run the prototype. (The goal is either a successful output or a specific error trace to record.)

#### Phase 3: Record the Factual Data

* [ ] **Collect logs and outputs**: Save all run logs, error messages, or measurement results (e.g. "Response time: 3.4s").
* [ ] **Objective evaluation**: Record factually how the code behaved against the planned architecture. *Do not draw project-level conclusions — only record open results.* Leave interpretation to the Integrator.

#### Phase 4: Handoff to the Integrator

* [ ] **Assemble the handoff message**: Generate the handoff message using `experimenter_to_integrator_handoff.message.md`.

### Communication Prompts

**If prototype build is blocked:**
> "Building the MVE based on the given ADR has stalled due to the following physical/technical blockers: [Error/Blocker list]. It was not possible to run the [test] scenario."

**Handoff to the Integrator:**
> "The experiment is complete. I built the prototype in the isolated environment. The following run data is available: [Logs/Measurements/Errors]. Please provide an integrated, objective evaluation of the experiment against the original Hypothesis."

---

## Completion

* [ ] Ensure no modifications have escaped beyond the isolated environment.
* [ ] Create a Git commit for the sandbox snapshot.
* [ ] Hand off to the Integrator.
* [ ] **STOP**
