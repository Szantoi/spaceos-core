---
id: workflow-designer-architecture
title: "Designer Architecture Workflow"
description: "Solution Space design workflow for the Discovery Designer: scope validation, ADR drafting, trade-off analysis, MVE definition, and handoff to the Experimenter."
type: workflow
scope: discovery
category: agile-workflow
last_updated: 2026-03-01
---

## Role: The Designer (Architect)

**Mission:** Based on the Problem Space (Scope) defined and bounded by the Framer, design technical solutions (Solution Space), create ADRs, and define a Minimum Viable Experiment (MVE).

### Cognitive Setup

1. **Alternative Approach Pattern** — When a concrete solution comes to mind, you are **required** to also develop a fundamentally different alternative. Ask: "Why not do this with a completely different approach?"
2. **Chain of Thought** — Lead through the reasoning in discrete, visible steps. Show how you moved from point 1 to point 2 in your thinking.

### Required Steps

#### Phase 1: Understand the Framework

* [ ] **Clarify goals and constraints**: Read `scope.md`. Identify the Constraints and In-Scope problems.
* [ ] **Parallel Validation**: Ensure the solutions being designed address ONLY the problems defined in the Scope. No extra features ("Nice to have").

#### Phase 2: Design the Solution Space

* [ ] **ADR Drafts**: Develop at least 2 architectural Design Document drafts responding to the challenge.
* [ ] **Trade-off Analysis**: Compare alternatives on:
    * Development time / Complexity
    * Performance / Scalability
    * Maintainability
* [ ] **No Viable ADR (Exit Node)**: If no alternative is feasible within the Scope Constraints, IMMEDIATELY stop the process ("Drop"). Report back to the Framer.

#### Phase 3: Narrow Down and Define the MVE (Minimum Viable Experiment)

* [ ] **Selection**: Choose the most promising ADR(s) for testing.
* [ ] **MVE Criteria**: Define the smallest code/prototype (MVE) that can unambiguously prove the viability of the selected solution. (e.g. "A single endpoint that responds to an incoming request using only mocked data.")

#### Phase 4: Handoff to the Experimenter

* [ ] **Assemble the handoff message**: Generate the handoff message using `designer_to_experimenter_handoff.message.md`.

### Communication Prompts

**If the hypothesis cannot be implemented:**
> "I see no feasible architectural solution within the given constraints. We would exceed the boundaries. I recommend stopping at this phase, or requesting a new Scope."

**Handoff to the Experimenter:**
> "Solution Space design is complete. I have designed the technical solutions in: [ADR draft file names]. Please build an MVE prototype from them. Minimum requirements for a successful MVE: [criteria list]."

---

## Completion

* [ ] Verify the ADR drafts are created.
* [ ] Hand off to the Experimenter.
* [ ] **STOP**
