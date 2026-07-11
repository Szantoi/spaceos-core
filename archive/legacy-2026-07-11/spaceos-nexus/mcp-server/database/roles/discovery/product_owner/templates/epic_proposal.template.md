---
id: template-epic-proposal
title: "Epic Proposal Template"
description: "Structured proposal for a new Epic. Covers executive summary, business value, user stories, scope definition, acceptance criteria, and quality constraints (DQM summary)."
type: template
scope: global
last_updated: 2026-03-01
---

# Epic Proposal: {EPIC_TITLE}

**ID**: {EPIC_ID}
**Status**: DRAFT
**Owner**: Product Owner
**Proposed Date**: {DATE}

---

## 1. Executive Summary

Briefly describe the business need and the primary goal of this Epic.

## 2. Business Value (Value vs Effort)

- **Problem Statement**: What specific problem are we solving?
- **User Personas**: Who benefits from this change?
- **Expected Outcome**: What is the measurable success?
- **Value Score (1-5)**: {X}
- **Assumed Effort (1-5)**: {Y}

## 3. User Stories / Requirements

1. **AS A** {role} **I WANT** {action} **SO THAT** {benefit}.
2. **AS A** {role} **I WANT** {action} **SO THAT** {benefit}.

## 4. Scope Definition

### In Scope
- {Item 1}
- {Item 2}

### Out of Scope (Scope Guard)
- {Item 1}
- {Item 2}

## 5. Acceptance Criteria

- [ ] {Business AC 1}
- [ ] {Business AC 2}
- [ ] {Business AC 3}

## 6. Quality & Constraints (DQM Summary)

Summarize the key findings from the DQM Canvas (`dqm_canvas.template.md`).

- **Primary Quality Goal**: {e.g., Performance, Reliability}
- **Fitness Functions**: {Link to specific automated checks}

---

## Status History

| Date | Status | Note |
|:-----|:-------|:-----|
| {DATE} | DRAFT | Initial proposal |
