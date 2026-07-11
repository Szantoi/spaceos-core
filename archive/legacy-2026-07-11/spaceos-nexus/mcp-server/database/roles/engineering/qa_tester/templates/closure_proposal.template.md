---
id: template-closure-proposal
title: "Epic Closure Proposal Template"
description: "Formal Epic closure proposal prepared by the QA agent or Tech Lead. Includes completion status, QA report, risks, technical debt, and a clear recommendation to close or reopen the Epic."
type: template
scope: global
last_updated: 2026-03-01
---

# Epic Closure Proposal: [[ EPIC_ID ]]

**Author**: {QA Tester / Tech Lead Agent ID}
**Date**: {YYYY-MM-DD}

---

## 1. Executive Summary (Fact Summary)

- **Epic Goal**: {1 sentence — what the Epic was meant to achieve}
- **Outcome**: {1 sentence — what was actually delivered}
- **Main Blockers**: {Were there delays or scope changes? Briefly explain.}

---

## 2. Completion Status (Fact Check)

| Task ID | Title | Dev Status | QA Status |
|:--------|:------|:-----------|:----------|
| TASK-01 | {Title} | Done / In Progress / Blocked | Passed / Failed / Skipped |
| TASK-02 | {Title} | Done / In Progress / Blocked | Passed / Failed / Skipped |

---

## 3. Quality Assurance Report

- **Unit Tests**: {X} passing / {Y} failing / {Z} skipped
- **Integration Tests**: {X} passing / {Y} failing
- **Critical Bugs**: {Open / None}
- **Security Review**: Passed / Pending / Not required

---

## 4. Risks & Technical Debt (Reflection)

*What remains that could cause future problems?*

- [ ] {Debt or risk item 1}
- [ ] {Debt or risk item 2}

---

## 5. Recommended Next Steps

**Recommendation**:
- [ ] **Close Epic** — All criteria met; safe to close.
- [ ] **Do NOT Close** — Outstanding issues prevent closure (see above).

**Follow-up Tasks** (if any):
- {New task to address remaining debt}

---

**Proposer**: {Agent ID}
**Date**: {YYYY-MM-DD}
