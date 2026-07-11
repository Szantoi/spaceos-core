---
id: template-strategic-directive
title: "PO Strategic Directive Template"
description: "Product Owner strategic directive for the Orchestrator. Defines the current strategic focus, Epic priority order, scope guardrails, and quality thresholds for the active project phase."
type: template
scope: global
last_updated: 2026-03-01
---

# PO Strategic Directive: {DATE}

**Project**: {PROJECT_NAME}
**Status**: ACTIVE
**Source**: Product Owner

---

## 1. Strategic Focus

The most important priority for the current phase of the project:
> {Describe in 1 sentence the primary business goal right now.}

## 2. Epic Priority (Product Backlog)

| Rank | Epic ID | Title | Priority | Business Rationale |
|:-----|:--------|:------|:---------|:------------------|
| 1 | {EPIC-00X} | {Title} | MUST (P1) | {Why?} |
| 2 | {EPIC-00X} | {Title} | SHOULD (P2) | {Why?} |
| 3 | {EPIC-00X} | {Title} | COULD (P3) | {Why?} |

## 3. Scope Guidelines & Guardrails

- **MUST HAVE**: {MVP-critical features}
- **DEFER**: {Features that can wait for the next phase (nice to have)}
- **NO-GO**: {Directions to avoid due to scope creep risk}

## 4. Quality Thresholds (Orchestrator Control)

Business expectations for execution:

- **Quality Level**: High / Medium / Low
- **Acceptance Rule**: {e.g., "No feature Epic can start while critical bugs remain unresolved"}
- **Required DQM Context**: {Which DQM attribute should the Architect focus on?}

---

## Note to Orchestrator
{Actionable instruction, e.g., "Start planning EPIC-003 with the Architect."}
