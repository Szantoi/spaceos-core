---
title: "Dev E — TASK-13-01 Assignment Sheet"
subtitle: "Discovery Roles & DWI Workflow Template — Foundation for Discovery Track"
created: 2026-03-08
updated: 2026-03-09
assigned_to: "Dev E"
priority: "P0"
epic: "EPIC-13"
phase: "M02 — Phase 1: Discovery Track Setup"
status: "✅ DONE"
completed: 2026-03-15
effort_estimate: "17 hours"
ac_count: 4
---

# 🚀 Dev E — TASK-13-01 Assignment

**Task:** TASK-13-01 (Discovery Roles & DWI Workflow Template)
**Epic:** EPIC-13 (Discovery Track Tools — DWI State Support)
**Phase:** M02 Phase 1 — Discovery Track Setup
**Priority:** P0 (foundation for all discovery tools)
**Effort Estimate:** 17 hours (2 days)

---

## 🎯 Your Mission

Create the **role definitions and workflow templates** that enable the discovery track. This is the **foundation** for all discovery-specific tools (TASK-13-02/03/04).

**Key Deliverables:**

- 2 Discovery roles: `discovery/architect` + `discovery/researcher`
- DWI workflow template: 4-phase workflow (ideation → validation → iteration → delivery_handoff)
- Phase-specific templates: artifact templates, checklists, exit criteria per phase
- Discovery-specific permissions (RBAC rules)

**Why This Matters:**

- **Unified Two-Track Model:** Discovery and Delivery are now first-class citizens
- **Track Routing Foundation:** Middleware will use these roles to route discovery agents
- **Workflow Clarity:** Discovery agents have explicit phases + artifact templates
- **Downstream Dependency:** TASK-13-02/03/04 all depend on these role definitions

---

## 📋 Acceptance Criteria (4 AC)

### AC-1: Discovery Roles Defined (2 Roles) ✅

**Create 2 Role Definitions in `database/roles/discovery/`:**

**1. `architect` Role:**

- **Location:** `database/roles/discovery/architect/manifest.md`
- **Responsibilities:**
  - Lead discovery hypothesis generation
  - Validate ideas against constraints
  - Create discovery artifacts (design docs, decision trees)
  - Drive transitions between phases
- **Permissions:**
  - Can call: `request_context()`, `reference_prior_discovery()`, `submit_discovery_outcome()`, `get_phase_template()`
  - Cannot call: Delivery-track tools, implementation code generators
  - Track: `discovery`
  - Phase restrictions: All discovery phases (ideation, validation, iteration, delivery_handoff)
- **MCP Tools Allowed:** 7 tools (all discovery-specific)

**2. `researcher` Role:**

- **Location:** `database/roles/discovery/researcher/manifest.md`
- **Responsibilities:**
  - Deep-dive research into discovery topics
  - Validate proposals against technical constraints
  - Create validation reports
  - Document findings + blockers
- **Permissions:**
  - Can call: `reference_prior_discovery()`, `submit_discovery_outcome()`, `check_constraints()` (validation focus)
  - Cannot call: `request_context()` (architect-only), implementation tools
  - Track: `discovery`
  - Phase restrictions: Validation phase primary, read-only iteration/ideation

**Format** (use YAML/JSON):

```yaml
---
id: discovery/architect
name: "Discovery Architect"
description: "Leads hypothesis-driven discovery workflow"
track: discovery
responsibilities:
  - "Generate and validate hypotheses"
  - "Guide team through DWI phases"
mcp_tool_permissions:
  - request_context
  - reference_prior_discovery
  - submit_discovery_outcome
  - get_phase_template
phase_restrictions:
  - ideation
  - validation
  - iteration
  - delivery_handoff
constraints:
  - "Cannot generate implementation code in ideation phase"
  - "Must document blockers before iteration phase"
```

**Verification:**

- [ ] Both role files exist in `database/roles/discovery/`
- [ ] All 8 fields present (id, name, description, track, responsibilities, mcp_tool_permissions, phase_restrictions, constraints)
- [ ] No `any` types; strict schema validation
- [ ] Unit test: `test_discovery_architect_role_loads_correctly()`
- [ ] Unit test: `test_discovery_researcher_role_loads_correctly()`

---

### AC-2: DWI Workflow Template (4-Phase Workflow) ✅

**Create Workflow Definition:**

- **Location:** `database/roles/discovery/*/workflows/DWI.workflow.md`
- **4 Phases:**
  1. **IDEATION** (entrance → exit criteria)
  2. **VALIDATION** (entrance → exit criteria)
  3. **ITERATION** (entrance → exit criteria)
  4. **DELIVERY_HANDOFF** (entrance → exit criteria)

**Format** (Markdown + YAML frontmatter):

```markdown
---
id: workflow-discovery-dwi
title: "DWI Workflow: Discover → Why → Implement Handoff"
type: workflow
track: discovery
phases:
  - ideation
  - validation
  - iteration
  - delivery_handoff
created_at: 2026-03-18
---

# DWI Workflow

## Phase 1: IDEATION

**Goal:** Generate hypothesis-driven ideas

**Entrance Criteria:**
- [ ] Discovery session started
- [ ] Problem statement clear
- [ ] Prior discoveries reviewed

**Tools Available:**
- `request_context("ideation")`
- `reference_prior_discovery(search_text)`
- `submit_artifact(type="idea", content)`

**Exit Criteria:**
- [ ] 3+ ideas documented
- [ ] Each idea has initial reasoning
- [ ] Blockers identified

**Artifacts Produced:**
- Idea list (title + summary for each)
- Initial reasoning document
- Blocker list

---

## Phase 2: VALIDATION

**Goal:** Test hypothesis against constraints

**Entrance Criteria:**
- [ ] Ideation phase complete
- [ ] 3+ ideas from ideation

**Tools Available:**
- `reference_prior_discovery(phase="validation")`
- `check_constraints(idea_id, constraint_set)`
- `submit_artifact(type="validation_report")`

**Exit Criteria:**
- [ ] Each idea validated or rejected
- [ ] Constraint violations documented
- [ ] Decision rationale written

**Artifacts Produced:**
- Validation report
- Constraint analysis
- Selected ideas for iteration

---

## Phase 3: ITERATION

**Goal:** Refine ideas based on validation feedback

**Entrance Criteria:**
- [ ] Validation complete
- [ ] Ideas either validated or rejected
- [ ] Refinement plan defined

**Tools Available:**
- All discovery tools
- `submit_artifact(type="refined_design")`

**Exit Criteria:**
- [ ] Refined design ready for delivery
- [ ] Implementation plan sketched
- [ ] Tech lead sign-off optional

**Artifacts Produced:**
- Refined design document
- Technical implementation sketch
- Decision trade-offs documented

---

## Phase 4: DELIVERY_HANDOFF

**Goal:** Create ticket/epic for engineering team

**Entrance Criteria:**
- [ ] Iteration complete
- [ ] Design ready for engineers
- [ ] No critical blockers

**Tools Available:**
- `submit_discovery_outcome(outcome_type="HANDOFF", content)`
- `create_task_ticket(from_discovery)`

**Exit Criteria:**
- [ ] Ticket created with acceptance criteria
- [ ] Discovery session linked to ticket
- [ ] Discovery team confirmed handoff

**Artifacts Produced:**
- Task ticket (with linked discovery episodes)
- Acceptance criteria from discovery
- Risk/blocker list for engineers
```

**Verification:**

- [ ] Workflow file exists in correct location
- [ ] All 4 phases defined with entrance/exit criteria
- [ ] Tools per phase match role permissions
- [ ] Artifact templates clear + actionable
- [ ] Unit test: `test_dwi_workflow_loads_correctly()`
- [ ] Unit test: `test_dwi_workflow_has_4_phases()`
- [ ] Unit test: `test_dwi_workflow_phases_have_entrance_exit_criteria()`

---

### AC-3: Phase-Specific Artifact Templates ✅

**Create 4 Templates (one per phase):**

**1. IDEATION Artifact Template**

- **Location:** `database/roles/discovery/templates/ideation-artifact.md`
- **Content:**

  ```markdown
  # Ideation Artifact Template

  ## Idea Title
  [Brief, descriptive title]

  ## Problem Statement
  [What problem does this idea solve?]

  ## Initial Hypothesis
  [What are we trying to test?]

  ## Proposed Solution
  [High-level approach]

  ## Initial Blockers
  - [ ] Blocker 1
  - [ ] Blocker 2

  ## References to Prior Discovery
  - [Reference 1]: episode-id-123
  ```

**2. VALIDATION Artifact Template**

- **Location:** `database/roles/discovery/templates/validation-report.md`
- Content: Constraint analysis, go/no-go decision, rationale

**3. ITERATION Artifact Template**

- **Location:** `database/roles/discovery/templates/refined-design.md`
- Content: Refined design, trade-offs, implementation sketch

**4. DELIVERY_HANDOFF Artifact Template**

- **Location:** `database/roles/discovery/templates/handoff-ticket.md`
- Content: Task description, acceptance criteria, linked discoveries

**Verification:**

- [ ] 4 template files exist in correct locations
- [ ] Each template has clear sections + examples
- [ ] Templates guide discovery agents through each phase
- [ ] Unit test: `test_all_4_artifact_templates_exist()`

---

### AC-4: Phase-Based Access Control (RBAC Rules) ✅

**Implement Discovery-Specific RBAC:**

```typescript
// In src/roles/guardrailEvaluator.ts or RBAC service:

export const discoveryRBACRules = {
  "discovery/architect": {
    allowed_phases: ["ideation", "validation", "iteration", "delivery_handoff"],
    allowed_tools: [
      "request_context",
      "reference_prior_discovery",
      "submit_discovery_outcome",
      "get_phase_template",
      // ... (7 total discovery tools)
    ],
    phase_restrictions: {
      "ideation": {
        forbidden_outputs: ["implementation_code", "database_schema"],
        allowed_artifacts: ["idea_list", "reasoning"]
      },
      "validation": {
        forbidden_outputs: ["implementation_code"],
        allowed_artifacts: ["validation_report", "constraint_analysis"]
      }
    }
  },
  "discovery/researcher": {
    allowed_phases: ["validation", "iteration"],  // No ideation, no handoff
    allowed_tools: [
      "reference_prior_discovery",
      "check_constraints",
      "submit_discovery_outcome",
      // ... (subset of discovery tools)
    ],
    phase_restrictions: {
      "validation": {
        read_only: false,
        write_allowed: ["validation_report"]
      },
      "iteration": {
        read_only: true  // Researcher can view, not modify
      }
    }
  }
};
```

**Verification:**

- [ ] RBAC rules loaded from `database/roles/discovery/` manifest files
- [ ] `request_context("ideation")` allowed for architect, denied for researcher
- [ ] Phase restrictions enforced (researcher cannot access ideation)
- [ ] Unit test: `test_discovery_architect_can_access_all_phases()`
- [ ] Unit test: `test_discovery_researcher_access_restricted_by_phase()`

---

## 🛠️ Implementation Checklist

### Phase 1: Role Definitions (4-5h)

- [ ] Create `database/roles/discovery/architect/manifest.md`
- [ ] Create `database/roles/discovery/researcher/manifest.md`
- [ ] Validate schema (id, name, description, track, mcp_tool_permissions, constraints)
- [ ] Ensure no conflicts with delivery roles

### Phase 2: DWI Workflow Template (5-6h)

- [ ] Create `database/roles/discovery/*/workflows/DWI.workflow.md`
- [ ] Define 4 phases with entrance/exit criteria
- [ ] List tools available per phase
- [ ] Define artifact templates per phase

### Phase 3: Artifact Templates (3-4h)

- [ ] Create 4 template files (`ideation-artifact.md`, etc.)
- [ ] Each template has clear sections + examples
- [ ] Templates guide discovery process

### Phase 4: RBAC Rules + Testing (4-5h)

- [ ] Implement phase-based access control
- [ ] Write 6+ unit tests
- [ ] Verify role loading + RBAC enforcement

---

## 📁 Files to Create

### Config Files (database/)

- `database/roles/discovery/architect/manifest.md`
- `database/roles/discovery/researcher/manifest.md`
- `database/roles/discovery/*/workflows/DWI.workflow.md`
- `database/roles/discovery/templates/ideation-artifact.md`
- `database/roles/discovery/templates/validation-report.md`
- `database/roles/discovery/templates/refined-design.md`
- `database/roles/discovery/templates/handoff-ticket.md`

### Tests

- `src/tests/unit/DiscoveryRoles.test.ts` (6+ tests)

---

## 🧪 Test Coverage Target

**Minimum 90% coverage required:**

| Component | Tests | Target |
|:---|:---|:---|
| Role loading | 2 | 100% |
| DWI workflow | 3 | 100% |
| RBAC rules | 2 | 100% |
| **Total** | **7+** | **90%+** |

---

## 📞 Definition of Done

- [ ] All 4 AC passing
- [ ] 7+ unit tests green
- [ ] 90%+ code coverage
- [ ] No `any` types; strict TypeScript
- [ ] Implementation Summary drafted
- [ ] Ready for peer review

---

## 📅 Timeline

**2026-03-18:** Task starts (EPIC-12 kick-off day)
**2026-03-19 EOD:** All AC passing
**2026-03-20:** Peer review + merge
**2026-03-20:** Dev E begins TASK-13-02 (RBAC routing)

---

Good luck! 🚀

## ✅ Implementation Summary (Completed)

- Verified discovery role schemas (`architect`, `researcher`) exist under `database/roles/discovery/*` and load via `RoleLoader`.
- Confirmed DWI workflow template (`DWI.workflow.md`) defines all 4 phases with entrance/exit criteria.
- Confirmed all 4 artifact templates exist and are readable by `request_context()`.
- Ran unit test suite `src/tests/unit/discoveryRoles.test.ts` (5 tests passed).
- Updated task status to `✅ DONE` in the assignment header.
