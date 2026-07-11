---
id: tech-lead-task-template
title: "Implementation Plan Template (Task)"
description: "Complete task implementation plan filled by the Tech Lead. Sections 1-10 define the technical strategy; Section 11 is filled by the Developer during and after implementation."
type: template
scope: global
last_updated: 2026-03-01
---

> **Workflow**: Tech Lead fills sections 1–10 → Developer implements → Developer fills section 11.

# Implementation Plan: [BACKLOG_ITEM_ID]

---

## Context Loading (Auto-required)

```
default_role: tech_lead (or developer — see frontmatter)
project_override: docs/{project}/_program.md (if available)

Required skills to load:
- tech_lead.skill.md (or developer.skill.md)
- Any relevant domain skill (e.g., clean_architecture.skill.md)
```

---

## Section 1 — Goal

**What needs to be built and why?**

{1-2 sentence clear goal statement}

**Context**: {Why is this task necessary? What will it unblock?}

---

## Section 2 — Inputs and References

- **Task**: {BACKLOG_ITEM_ID}
- **Epic**: {EPIC_ID}
- **Related files**:
  - `{path/to/file}` — {why it's relevant}
- **ADR references**: {Which prior architectural decisions apply?}
- **Dependencies**: {Which Tasks must be completed first?}

---

## Section 3 — Execution Strategy

**Approach**: {Which pattern/technique to use and why?}

**Why this approach?** {Brief justification}

**What to avoid?** {Alternative approaches ruled out and why}

---

## Section 4 — Affected Files

| File | Change Type | Notes |
|:-----|:-----------|:------|
| `{path/file}` | Create / Modify / Delete | {Brief description} |

---

## Section 5 — Steps

> Step-by-step breakdown for the Developer.

1. **{Step Name}**
   - What: {Exact action}
   - Where: `{file path}`
   - How: {Instruction or code snippet}

2. **{Step Name}**
   - What: …

---

## Section 6 — Edge Cases and Risks

| Case | Risk Level | Handling |
|:-----|:----------|:---------|
| {Case 1} | High/Med/Low | {How to handle} |
| {Case 2} | High/Med/Low | {How to handle} |

---

## Section 7 — Testing Strategy

- **Unit Tests**: {What to cover and where?}
- **Integration Tests**: {What scenario to test?}
- **Manual Check**: {What to verify in the UI or API explorer?}

---

## Section 8 — Definitions (Glossary)

| Term | Definition |
|:-----|:----------|
| `{Term}` | {Short definition in context of this task} |

---

## Section 9 — Dependencies

- **Blocks**: {Which Tasks does this task unblock?}
- **Blocked by**: {Which Tasks must be completed before this one?}
- **External**: {Any external service or config dependency?}

---

## Section 10 — Tech Lead Pre-Flight Checklist

- [ ] Goal is unambiguous
- [ ] All references and dependencies are documented
- [ ] Steps are complete and implementable
- [ ] Edge cases covered
- [ ] Testing strategy defined
- [ ] Glossary filled (if new terms introduced)

---

## Section 11 — Developer Section (Fill During Implementation)

### Summary of Changes
{1-2 sentence description of what was actually implemented}

### Files Changed
| File | Type | Summary |
|:-----|:-----|:--------|
| `{path/file}` | Create / Modify / Delete | {What changed?} |

### Bugs and Solutions
| Bug | Root Cause | Solution |
|:----|:----------|:---------|
| {Description} | {Cause} | {Fix applied} |

### QA Sign-off Request

- [ ] Unit tests written and passing
- [ ] No linting/build errors
- [ ] Manual test done
- [ ] Reviewed by another agent / peer if possible
- [ ] Ready for QA Agent review
