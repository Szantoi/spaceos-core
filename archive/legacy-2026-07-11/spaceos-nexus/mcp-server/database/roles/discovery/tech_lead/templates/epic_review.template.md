---
id: tech-lead-epic-review-template
title: "Epic Review Report Template"
description: "Post-Epic technical review document filled by the Tech Lead. Covers deviations from plan, tech debt registry, calibration lessons, QA observations, and readiness check before Architect sign-off."
type: template
scope: global
last_updated: 2026-03-01
---

# Epic Review Report: [[ EPIC_ID - NAME ]]

## 1. General Information

- **Tech Lead**: {Agent ID}
- **Date**: {YYYY-MM-DD}
- **Affected Components / Layers**: {API, Core, Infra, UI, …}

## 2. Technical Summary

*2-3 sentence technical overview of what was implemented, what changed, and the overall result.*

{WRITTEN SUMMARY}

## 3. Deviations from Plan

*If there were no deviations, write "None".*

- **[TASK-ID]**: {Description of deviation and reason}

## 4. Technical Debt Registry

| Item | Impact | Action Needed By |
|:-----|:-------|:-----------------|
| [ ] {Debt 1} | High / Med / Low | {Date or Epic} |
| [ ] {Debt 2} | High / Med / Low | {Date or Epic} |

## 5. Calibration & Knowledge Management (Lessons Learned)

### Global Skills to Update
- {Skill file: `{skill_name}.skill.md` — what should change?}

### Standards to Establish / Update
- {New pattern or constraint that should be documented?}

### Template and Workflow Improvements
- {Was a template incomplete or incorrect? Which one?}

## 6. QA & Performance Observations

*Recurring test failures, performance bottlenecks, or verification gaps observed:*

- {Observation 1}
- {Observation 2}

## 7. Readiness Check (before Architect Sign-off)

- [ ] All tasks completed and verified
- [ ] No critical bugs remaining
- [ ] Tech debt documented
- [ ] Lessons learned captured
- [ ] Documentation updated

---

## 8. Calibration Instructions (For Knowledge Steward)

> **This section is completed by the Tech Lead and forwarded to the Knowledge Steward for integration.**

### Global Skills to Update
- {Skill ID / name}: {What to add/change and why}

### Standards to Create / Update
- {Standard name}: {Content or direction}

### Template Refinements
- {Template file}: {Specific change requested}

### KS Action Items
- [ ] Update relevant `.skill.md` files
- [ ] Update or create `.workflow.md` if needed
- [ ] Document new patterns or constraints in knowledge files
- [ ] Notify Orchestrator when calibration is complete
