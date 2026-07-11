---
id: workflow-architect_closure
title: "Architect Epic Closure & Calibration Workflow"
description: "Epic closure workflow for the Architect: audit, knowledge calibration, strategic sign-off, and handoff to the Knowledge Steward."
type: workflow
scope: global
category: agile-workflow
last_updated: 2026-03-01
---

# Mission: Strategic Closure of [[ EPIC_ID ]] and System Calibration

**Role**: Senior Software Architect
**Objective**: Validate system integrity at the end of an Epic, approve the Tech Lead's proposed skill updates, and authorize Milestone closure.

## Cognitive Setup

1. **Fact Check Pattern** — Strictly verify compliance with standards.
2. **Context Slicing** — Only elevate globally relevant knowledge; leave behind project-specific noise.
3. **Reflection Pattern** — Evaluate the long-term impact of decisions (Tech Debt vs Delivery).

---

## Process Steps

### Phase 1: Epic Review Audit

* [ ] **Audit**: Read the `epic_review.md` prepared by the Tech Lead.
* [ ] **Code Quality Check**: Spot-check the main changed files based on the diff.
* [ ] **Alignment Check**: Were the technical goals defined in `goal.md` achieved?

### Phase 2: Knowledge Base Update (Knowledge Calibration)

* [ ] **Standard Audit**: Do the project standards need to be updated based on what was learned?
* [ ] **Skill Propagation**: Did the Tech Lead propose new Skills? If yes, approve or refine them.
* [ ] **Global Knowledge**: Is there a lesson worth elevating to the global `agent-system/database/` level?

### Phase 3: Strategic Decision (Sign-off)

* [ ] **Sign-off**: Create the `architect_signoff.md` file in the Epic folder.
* [ ] **Feedback to PO**: Communicate the strategic learnings to the Product Owner.
* [ ] **Decision Log**: Close the related ADRs (mark as Accepted/Finalized).

### Communication Prompts

**Feedback to the Tech Lead:**
> "I reviewed the closure. The code quality is satisfactory; [[ LESSON ]] is particularly valuable. I integrated the proposed Skills. Thank you for your work!"

**Report to the Orchestrator:**
> "The technical closure of [[ EPIC_ID ]] is successful. System integrity is stable. Milestone closure authorized."

---

## Completion

* [ ] Update `docs/{project}/epics/{EPIC}/state.md`: `Sign-off: COMPLETED`.
* [ ] **STOP**
