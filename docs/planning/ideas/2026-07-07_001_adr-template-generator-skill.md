# ADR Template Generator Skill

**Date:** 2026-07-07
**Source:** Explorer Terminal Memory (JoineryTech Research Findings)
**Priority:** Medium
**Complexity:** Low (1-2 days)

---

## Concept

Architectural decision pattern template generator with 8-gap analysis structure for consistent, high-quality ADR creation.

---

## Problem

- ADRs written manually are inconsistent
- Quality varies between authors
- Missing sections (alternatives, trade-offs, risk mitigation)
- Time-consuming to create comprehensive ADRs

---

## Solution

Create Claude Code skill that generates ADR template with:

### Core Sections
1. **Context** — Current situation + problem statement
2. **Gap Analysis** — 8 integration gaps identified (structure from ADR-058)
3. **Alternatives** — 3+ options with pros/cons
4. **Trade-offs** — Technical/business/timeline considerations
5. **Risk Mitigation** — For each major risk, mitigation strategy
6. **Success Metrics** — How to measure if decision was good
7. **Implementation Roadmap** — Step-by-step execution plan
8. **References** — Related ADRs, docs, discussions

### Template Structure
```markdown
# ADR-XXX: [Title]

**Date:** YYYY-MM-DD
**Status:** Proposed | Accepted | Superseded
**Deciders:** [Names]
**Related:** ADR-001, ADR-042

## Context
[Background + problem statement]

## Gap Analysis
| Gap | Impact | Priority |
|-----|--------|----------|
| ... | ... | ... |

## Alternatives Considered

### Option 1: [Name]
**Pros:** ...
**Cons:** ...
**Cost:** ...

### Option 2: [Name]
...

## Decision
[Chosen option + rationale]

## Trade-offs
[Technical/business/timeline considerations]

## Risk Mitigation
| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| ... | ... | ... | ... |

## Success Metrics
- Metric 1: [target]
- Metric 2: [target]

## Implementation Roadmap
1. Phase 1: ...
2. Phase 2: ...

## References
- [Link 1]
- [Link 2]
```

---

## Benefits

- **Consistency:** All ADRs follow same structure
- **Quality:** No missing sections
- **Speed:** 15-20 min ADR creation (vs 60+ min manual)
- **Reusability:** Template can be customized per domain

---

## Implementation

**Skill Location:** `/opt/spaceos/.claude/skills/adr-decision-template/`

**Skill Files:**
- `SKILL.md` — Main skill definition
- `template.md` — Base ADR template
- `examples/` — Sample ADRs using template

**Integration:**
- Architect terminal uses skill when creating ADRs
- Root terminal uses skill for strategic decisions
- Conductor uses skill for epic-level architecture decisions

---

## Example Usage

```bash
/adr-decision-template
  title: "JoineryTech Backend Migration Strategy"
  problem: "Current localStorage prototype needs PostgreSQL backend"
  alternatives: ["Rewrite", "Incremental Migration", "Dual-Write Pattern"]
```

**Output:** Complete ADR-XXX.md file with all sections pre-filled

---

## ROI Estimate

- **Time Savings:** 40-45 min/ADR × 20 ADRs/year = **800-900 min/year** (~15 hours)
- **Quality Improvement:** Consistent structure → better decisions
- **Onboarding:** New team members understand decision rationale

---

## Next Steps

1. Create skill definition in `.claude/skills/adr-decision-template/`
2. Test with 2-3 sample ADRs
3. Integrate with Architect terminal workflow
4. Update ADR_CATALOGUE.md to reference template

---

**Status:** Pending Implementation
**Assigned:** Librarian (skill creation)
**Estimated Effort:** 1-2 days
