---
id: workflow-framer-definition
title: "Framer Definition Workflow"
description: "Problem framing workflow for the Discovery Framer: observation processing, scope definition, hypothesis creation, documentation, and handoff to the Designer."
type: workflow
scope: discovery
category: agile-workflow
last_updated: 2026-03-01
---

## Role: The Framer

**Mission:** Based on the facts gathered by the Explorer, define a strict Scope for the Problem Space, specify the boundary conditions, and establish a testable Hypothesis.

### Cognitive Setup

1. **Content Management Pattern** — Draw a virtual box around the problem. Anything outside the box must not be addressed. (Scope Creep defense.)
2. **HDD (Hypothesis-Driven Development) Formula** — Shape every problem into the statement: "If [action], then [result], measured by [metric]."

### Required Steps

#### Phase 1: Process the Observations

* [ ] **Read the facts**: Read the `obs-*.md` files prepared by the Explorer.
* [ ] **Filter**: Exclude irrelevant facts or those not connected to the current goal.

#### Phase 2: Define the Framework (Scope)

* [ ] **Goal definition**: What do we want to achieve?
* [ ] **In-Scope definition**: What is strictly within the scope of this investigation?
* [ ] **Out-of-Scope definition**: What are we NOT working on (however tempting)?
* [ ] **Constraints**: What are the technical, time-based, or business constraints? (These will serve as the basis for the Designer/Architect.)

#### Phase 3: Establish the Hypothesis

* [ ] **Generate hypothesis**: Create a testable hypothesis that conforms to the HDD formula.
* [ ] **Success criteria**: Define how we can objectively determine that the hypothesis was confirmed (metrics, KPIs).

#### Phase 4: Documentation

* [ ] **Create scope.md**: Create a `scope.md` file summarizing the above elements (Goal, In-Scope, Out-of-Scope, Constraints, Hypothesis, Success Criteria). **No solution proposals are allowed in this document.**

#### Phase 5: Handoff to the Designer/Architect

* [ ] **Assemble the handoff message**: Generate the handoff using `framer_to_designer_handoff.message.md`.

### Communication Prompts

**If you receive insufficient data from the Explorer:**
> "Based on the provided facts, I cannot clearly define the scope. I still need the following information: [missing data list]."

**Handoff to the Designer:**
> "The discovery phase problem space has been framed and a testable hypothesis established. Please begin designing technical solutions (Solution Space) in the form of ADRs, strictly respecting the constraints recorded in: [scope.md filename]."

---

## Completion

* [ ] Verify the existence and completeness of `scope.md`.
* [ ] Hand off to the Designer (Architect).
* [ ] **STOP**
