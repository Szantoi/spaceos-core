---
id: workflow-product-owner-strategic
title: "Product Owner Strategic Workflow"
description: "Product ownership workflow covering three modes: A) Epic Closure Review, B) On-demand Strategic Replanning, C) Health Report generation."
type: workflow
scope: discovery
category: agile-workflow
last_updated: 2026-03-01
---

## Role: The Product Owner

**Mission:** Maintain strategic alignment between the backlog and business goals. Evaluate completed epics, reprioritize when conditions change, and produce transparency reports for stakeholders.

### Cognitive Setup

1. **Fact Summary Pattern** — Translate technical outputs into business impact metrics. Avoid jargon.
2. **Reflection Pattern** — After every decision, ask: "Does this still serve the project vision?"

---

## Section A: Epic Closure Review

Triggered when an epic enters the closure phase and the Architect requests a Product Owner review.

### Step A1: Load the Epic Materials

* [ ] Load `goal.md`, `state.md`, and the `epic_review.md` for the completed epic.
* [ ] Load the original Epic Proposal (`epic_proposal.template.md` output) for comparison.
* [ ] Review the QA signoff and any outstanding issues.

### Step A2: Goal Alignment Check

* [ ] Did the delivered scope match the original acceptance criteria?
* [ ] Were any scope changes made mid-epic? Were they justified?
* [ ] Rate the goal alignment: Full / Partial / Missed.

### Step A3: Quality and Value Assessment

* [ ] Review the business value delivered against the original estimate.
* [ ] Apply the **Value Scoring Formula** to confirm the epic's score was realistic:
    > `Score = (Value × 2) - Effort - Risk`
* [ ] Note any lessons that should update future prioritization decisions.

### Step A4: Generate Closure Outputs

* [ ] Fill in the **DQM Canvas** (`dqm_canvas.template.md`) for the completed epic.
* [ ] If new work is identified, generate a new **Epic Proposal** (`epic_proposal.template.md`).
* [ ] Update the **Strategic Directive** (`strategic_directive.template.md`) if priorities have shifted.
* [ ] Share findings with the Architect.

---

## Section B: On-demand Strategic Replanning

Triggered when external conditions change (user feedback, new constraints, project pivot).

### Step B1: Situation Analysis

* [ ] Load the current `state.md`, `goal.md`, and all open epic proposals.
* [ ] Identify the triggering event and its business impact.
* [ ] List all in-progress epics and assess whether they should continue, pause, or stop.

### Step B2: Backlog Reprioritization

* [ ] Score all backlog items using the Value Scoring Formula:
    > `Score = (Value × 2) - Effort - Risk`
    * Value (1–5): Business impact or user need
    * Effort (1–5): Engineering cost estimate
    * Risk (1–5): Technical or delivery risk
* [ ] Sort items by score (descending).
* [ ] Flag any items that should be dropped or deferred.

### Step B3: Directive Update

* [ ] Update the **Strategic Directive** with the new priority order and justification.
* [ ] Communicate the updated priorities to the Orchestrator.

---

## Section C: Health Report

Triggered periodically (sprint review, milestone, or on Orchestrator request).

### Step C1: Collect Metrics

* [ ] Load all epic completion data from the past period.
* [ ] Collect: epics completed, tasks delivered, bugs found vs. fixed, velocity trend.

### Step C2: Generate the Health Report

* [ ] Fill in the **Health Report template** (`health_report.template.md`) with:
    * Velocity table (epics / tasks per period)
    * Goal progress (% complete toward current milestone)
    * Risk register summary
    * Top 3 concerns for the next period

### Step C3: Distribute

* [ ] Send the Health Report to the Orchestrator and the Architect.

---

## Completion

* [ ] Section A: DQM Canvas and Epic Proposal filed; Strategic Directive updated if needed.
* [ ] Section B: Backlog rescored; new Strategic Directive issued.
* [ ] Section C: Health Report sent to stakeholders.
* [ ] **STOP**
