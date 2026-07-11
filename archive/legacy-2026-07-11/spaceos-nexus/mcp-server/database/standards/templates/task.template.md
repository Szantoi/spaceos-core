---
id: TASK-XX-YY
title: "[Concise Task Title]"
type: task
epic: EPIC-XX
milestone: MXX
project: mcp-maintenance
scope: [scope]
status: pending
priority: P0|P1|P2
assignee: [role: backend_developer, qa_tester, architect]
created: [YYYY-MM-DD]
fsm_state: "BACKLOG_READY"
fsm_retry_count: 0
depends_on:
  - TASK-XX-YY
  - EPIC-XX (if dependency on entire epic)
related_tasks:
  - TASK-XX-YY
blockers: []
---

# TASK-XX-YY: [Full Task Title]

## Purpose

1-3 sentence overview of task purpose — what problem does it solve?
What is the strategic context from the EPIC goal.md?

---

## Acceptance Criteria

Clear, testable checkboxes in the form "[ ] System shall..."

### Functional Requirements

- [ ] [Specific requirement 1]
- [ ] [Specific requirement 2]
- [ ] [Specific requirement 3]

### Quality Requirements

- [ ] [Performance / security / reliability criterion]
- [ ] Code follows project conventions (see: database/standards/00-foundation/)
- [ ] Documentation updated

### Delivery

- [ ] Deliverable artifact(s) created (see Output section below)
- [ ] Peer review completed
- [ ] Ready for handoff to downstream task

---

## Input / Output

### Input

| Source | Format | Notes |
|:-------|:-------|:------|
| [Artifact] | [Format] | [Description] |
| EPIC-XX state.md | Markdown | Specification reference |
| database/standards/ | Markdown | Standards & conventions |

### Output (Delivery Artifacts)

| Artifact | Format | Location | Consumed By |
|:---------|:-------|:---------|:------------|
| [Primary deliverable] | [File type] | `path/.../[name].md` | [Next task] |
| [Secondary deliverable] | [File type] | `path/.../[name].ts` | [Next task] |

---

## Technical Specification

### Design / Implementation Approach

Brief description of HOW the task will be accomplished.

### Data Structures / Interfaces

```typescript
// Example schema, types, or interfaces
interface Example {
  field: string;
}
```

### Database Changes (if applicable)

```sql
-- SQL DDL statements, migrations, schema changes
ALTER TABLE examples ADD COLUMN new_field TEXT;
```

### API Endpoints / Tool Interfaces (if applicable)

```
POST /api/endpoint
Request: { ... }
Response: { success: bool, data: {...} }
```

---

## Test Scenarios

Clear scenarios for validation (unit, integration, or E2E).

| # | Scenario | Input | Expected Output | Pass? |
|:--|:---------|:------|:---------------:|:-----:|
| 1 | Happy path — normal case | [input] | [output] | ☐ |
| 2 | Edge case — boundary | [input] | [output] | ☐ |
| 3 | Error case — validation | [invalid input] | [error response] | ☐ |

---

## Blockers / Dependencies

| Item | Status | Risk | Mitigation |
|:-----|:-------|:-----|:-----------|
| [Blocker/dependency] | ⏳ Pending / ✅ Ready | 🟡 / 🔴 | [Action if blocked] |

---

## Effort Estimate

- **Design / Analysis:** X hours
- **Implementation:** X hours
- **Testing / QA:** X hours
- **Documentation:** X hours
- **Total:** X-Y hours (Y-Z day sprint)

---

## Related Documentation

- `Docs/goal.md` — Strategic Vision alignment
- `EPIC-XX/goal.md` — Epic-level requirements
- `EPIC-XX/state.md` — Detailed spec / reference
- [Other standards, templates, or adjacent work]

---

## Notes / Implementation Hints

- [Non-binding guidance for developer]
- [Common pitfalls to avoid]
- [Code patterns or examples from similar tasks]
- [Security / performance considerations]

---

## Success Checklist (Definition of Done)

- ✅ All Acceptance Criteria passed
- ✅ Tests written and passing (unit + integration)
- ✅ Code peer-reviewed
- ✅ Documentation updated (README, API docs, etc.)
- ✅ Security review (if applicable)
- ✅ Database migrations tested (if applicable)
- ✅ Related EPIC state.md references updated
- ✅ Ready for downstream tasks / deployment

---

## Implementation Notes (Updated During Work)

[Space for developer notes, learnings, or mid-stream updates]
