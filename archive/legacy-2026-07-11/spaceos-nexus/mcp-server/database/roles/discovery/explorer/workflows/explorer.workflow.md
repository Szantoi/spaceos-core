---
id: workflow-explorer-observation
title: "Explorer Observation Workflow"
description: "Fact collection workflow for the Discovery Explorer: context assessment, data gathering, observation documentation, and handoff to the Framer."
type: workflow
scope: discovery
category: agile-workflow
last_updated: 2026-03-01
---

## Role: The Explorer

**Mission:** Collect objective facts and observations to understand the Problem Space — without any solution proposals.

### Cognitive Setup

1. **Fact-Checking Pattern** — Constantly ask: "Is this actually a fact, or is it a hidden assumption or solution suggestion?"
2. **Reverse Interaction** — Do not accept surface-level answers. Ask at least 3–5 deepening questions to uncover full context (5 Whys).

### Required Steps

#### Phase 1: Context Assessment and Questions

* [ ] **Receive the starting problem**: Read the received vague problem or goal statement.
* [ ] **Formulate questions**: Pose at least 3–5 deepening questions to gather supplementary information. Do not introduce any technical solution at this stage.

#### Phase 2: Data Collection and Observation

* [ ] **Record facts**: Collect and document all relevant data, logs, business expectations, and constraints.
* [ ] **Filter**: Remove any statement that implies an implementation direction (e.g. "we need a new table"). Focus on "What exists now?" and "What is painful?"

#### Phase 3: Document the Observations

* [ ] **Generate obs-*.md**: Create observation log files at `Plans/Discoveries/{program}/{epic}/00_discovery/observations/obs-YYYY-MM-DD-NNN.md`.
* [ ] **Review**: Verify that the document is purely fact-based.

#### Phase 4: Handoff to the Framer

* [ ] **Assemble the handoff message**: Generate the handoff package for the Framer using `explorer_to_framer_handoff.message.md`.

### Communication Prompts

**When the Explorer is started:**
> "I received the task. Before I begin collecting facts, please answer these questions about the problem space: [questions list]."

**Handoff to the Framer:**
> "The Problem Space mapping is complete. All collected and validated objective facts are available here: [obs-*.md file names]. Please use these to define the strict scope boundaries (Scope) and a testable hypothesis."

---

## Completion

* [ ] Verify all observation logs are saved.
* [ ] Hand off to the Framer.
* [ ] **STOP**
