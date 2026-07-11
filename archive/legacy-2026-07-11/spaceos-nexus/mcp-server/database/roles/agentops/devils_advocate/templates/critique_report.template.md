---
id: template-critique_report
title: "Devil's Advocate Critique Report"
description: "Structured critique report template for Devil's Advocate analysis of plans, architectures, and task implementations. Identifies blockers, risks, and alternative approaches."
type: template
scope: global
last_updated: 2026-03-01
---

# **Critique Report: [[ PLAN_NAME / TASK_ID ]]**

| Field | Details |
| :---- | :---- |
| **Reviewer (Advocate)** | Devil's Advocate Agent |
| **Date** | {YYYY-MM-DD} |
| **Target** | {Architect / Tech Lead} |

## **1. Critical Issues (Blockers)**

*Every issue that prevents the plan from being implemented.*

* 🔴 **Issue**: {Description} | **Why?**: {Argument/Rule} | **Suggestion**: {Solution}

## **2. Weak Points and Risks**

*Issues that are not blockers but may cause problems in the future.*

* 🟡 **Risk**: {Description} | **Impact**: {What happens if it materializes?} | **Mitigation**: {How to avoid it?}

## **3. Alternative Proposals**

*"Why not..." questions and suggestions.*

* **Question**: {Why did we choose X instead of Y?}
* **Suggestion**: {Alternative approach}

## **4. Standards Compliance (DoD & Architecture)**

* [ ] Clean Architecture respected?
* [ ] Error Recovery covered?
* [ ] Testability ensured?

## **5. Final Verdict**

* [ ] **ACCEPTED**: The plan is ready for implementation.
* [ ] **NEEDS REVISION**: The 🔴 issues above must be fixed first.
* [ ] **REJECTED**: The concept is flawed; a new design is required.

*This report is written with constructive intent to improve the quality of the project.*
