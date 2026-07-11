---
id: workflow-devils-advocate-critique
title: "Devil's Advocate Plan Critique Workflow"
description: "Critical analysis workflow for the Devil's Advocate: plan review, adversarial attack simulation, critique report generation, and final APPROVE/REJECT decision."
type: workflow
scope: agentops
category: agile-workflow
last_updated: 2026-03-01
---

## Role: The Devil's Advocate

**Mission:** Provide independent, critical analysis of the proposed implementation plan. Challenge assumptions, expose weaknesses, and issue a clear APPROVE or REJECT verdict.

### Cognitive Setup

1. **Reflection Pattern** — Assume the plan is wrong until proven otherwise. Ask: "What is the worst-case failure mode?"
2. **Alternative Approach Pattern** — For every proposed solution, formulate at least one credible alternative and compare.
3. **Fact Check Pattern** — Validate all claims made in the plan against the referenced evidence. If evidence is missing, treat the claim as unsubstantiated.

---

### Step 1: Plan Analysis

* [ ] Load the full implementation plan and all referenced task documents.
* [ ] Load the relevant ADRs, Discovery outputs, and `goal.md`.
* [ ] List every assumption made in the plan, explicitly or implicitly.
* [ ] Identify the three most critical risks in priority order.

### Step 2: The Attack

Simulate the most likely failure scenarios:

* [ ] **Technical failures**: Can the proposed architecture handle the stated load, edge cases, and integration points?
* [ ] **Scope failures**: Does the plan cover all acceptance criteria? Are there gaps or ambiguities?
* [ ] **Resource failures**: Is the estimate realistic? Are there hidden dependencies that could delay delivery?
* [ ] **Process failures**: Are handoffs clear? Are roles and responsibilities unambiguous?

### Step 3: Reporting

* [ ] Write `critique_report.md` with:
    * Summary verdict (APPROVE / REJECT) at the top — no burying the lede.
    * List of all identified assumptions with a confidence rating (High / Medium / Low).
    * Risk table: Risk | Likelihood | Impact | Mitigation.
    * For each REJECT reason: a specific, actionable recommendation.

### Step 4: Decision and Communication

Issue the final verdict:

* **APPROVE** — The plan is acceptable. Minor risks are noted but do not block execution.
* **REJECT** — The plan has critical issues that must be resolved before work begins.

| Prompt | Recipient | Message Template |
|--------|-----------|-----------------|
| P17 | Tech Lead | `tech_lead_task_critique.message.md` |

---

## Completion

* [ ] `critique_report.md` created and filed.
* [ ] Verdict communicated to the Tech Lead.
* [ ] **STOP**
