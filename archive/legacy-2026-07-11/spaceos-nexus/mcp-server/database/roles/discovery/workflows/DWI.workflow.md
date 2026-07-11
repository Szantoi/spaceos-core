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
