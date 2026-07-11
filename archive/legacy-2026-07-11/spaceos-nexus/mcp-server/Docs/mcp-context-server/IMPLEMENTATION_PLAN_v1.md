---
id: tech-lead-implementation-plan
title: "Implementation Plan: mcp-context-server Quality Improvements"
type: implementation-plan
date: 2026-03-04
reviewer: Tech Lead
---

# 📋 Implementation Plan: mcp-context-server Quality Fixes

**Status:** Draft (Ready for Architect + Team Review)
**Priority:** P0 — Blocking M01 closure + M02 start
**Estimated Timeline:** 3–4 weeks (phased approach)

---

## 🎯 Objectives

1. **Eliminate organizational debt**: EPIC-08 orphan status → proper task structure
2. **Complete architecture documentation**: Write Layer integration (EPIC-08) documented
3. **Ready M02 backlog**: EPIC-09–12 Acceptance Criteria + task planning
4. **Validate quality standards**: All tasks have AC/DoD, implementation summaries

---

## 📅 Phased Implementation

### Phase 1: Architecture & Planning (3–5 days)

**Deliverables:** Documentation updates, EPIC-08 organization, AC drafts

#### 1.1 Architecture Write Layer Integration [✅ DONE]

- [x] MCP_Server_Architecture.md § 3.2: Write Layer tools and SQLite schema defined
- [x] Clear separation: READ layer (GET) vs. WRITE layer (POST)
- [x] Error handling patterns documented
- **Owner:** Tech Lead
- **Review:** Architect

#### 1.2 EPIC-08 Organization [✅ DONE]

- [x] Folder structure created: `milestone_01/epic_08/{state.md, goal.md, tasks/, implementation-summary/}`
- [x] `goal.md`: business & technical objectives + AC
- [x] `state.md`: current status + task map + DoD checklist
- [x] 3 tasks drafted: TASK-08-01 (Schema), TASK-08-02 (Tools), TASK-08-03 (E2E Tests)
- **Owner:** Tech Lead
- **Review:** Backend Developer lead

#### 1.3 M01 Plan & State Updates [✅ DONE]

- [x] `milestone_01/plan.md`: EPIC-08 added to scope
- [x] `mcp-maintenance/state.md`: EPIC-08 now tracked as active epic
- [x] `milestone_02/plan.md`: EPIC-08 moved from "not scope" to "M01 predecessor"
- **Owner:** Tech Lead

#### 1.4 EPIC-09–12 Acceptance Criteria Draft (IN PROGRESS)

- [ ] **EPIC-09** (SQLite Schema Design): Coordinate with EPIC-08 schema; finalize AC
- [ ] **EPIC-10** (bootstrap_agent): Define agent identification flow; AC
- [ ] **EPIC-11** (RBAC Migration YAML → SQLite): Define migration strategy; AC
- [ ] **EPIC-12** (Episodic Memory): Define session recovery & ChromaDB write-back; AC
- **Owner:** Architect + Tech Lead
- **Timeline:** 3–4 days

---

### Phase 2: Implementation — EPIC-08 (7–10 days)

**Deliverables:** Write layer fully functional, E2E tests green

#### 2.1 TASK-08-01: SQLite Schema (3–4 days)

- [ ] Design SQLite migration (502_write_layer_schema.sql)
- [ ] Implement TypeScript types (WriteLayerSchema.ts)
- [ ] Update WorkflowStateTracker initialization
- [ ] Unit tests >80% coverage
- [ ] Implementation summary drafted
- **Owner:** Backend Developer
- **QA:** QA Tester (test design review)
- **Reviewer:** Tech Lead

#### 2.2 TASK-08-02: MCP Write Tools (3–4 days)

- [ ] Implement `submit_artifact()` tool
- [ ] Implement `update_workflow_state()` tool
- [ ] Implement `store_session_checkpoint()` tool
- [ ] RBAC integration (permission checks)
- [ ] FSM validation (state transitions)
- [ ] Integration tests >80% coverage
- [ ] Implementation summary drafted
- **Owner:** Backend Developer
- **QA:** QA Tester
- **Reviewer:** Tech Lead + Architect (FSM design)

#### 2.3 TASK-08-03: E2E Tests (2 days)

- [ ] Happy path: agent mock → artifact submit → FSM verify
- [ ] Error scenarios: 4 error test cases
- [ ] Coverage >80%
- [ ] CI integration (GitHub Actions)
- [ ] Test report documented
- [ ] Implementation summary drafted
- **Owner:** QA Tester
- **Reviewer:** Tech Lead

#### 2.4 EPIC-08 Sign-off (1 day)

- [ ] All tasks complete + Implementation Summaries exist
- [ ] AC verified by Architect
- [ ] QA sign-off: E2E tests green
- [ ] Tech Lead sign-off: DoD complete
- **Owner:** Backend Developer Lead

---

### Phase 3: Implementation — M02 Backlog (5–7 days)

**Deliverables:** EPIC-09–12 ready for development

#### 3.1 EPIC-09: SQLite Schema Design (FULL Epic planning)

- [ ] Create folder structure: `milestone_02/epic_09/{goal.md, state.md, tasks/}`
- [ ] Define schema: agents, roles, workflows, templates tables
- [ ] Coordinate with EPIC-08 schema (write layer integration)
- [ ] Break into 2–3 tasks (schema design, migration, tests)
- [ ] AC + DoD documented
- **Owner:** Architect + Backend Developer Lead
- **Timeline:** 2 days

#### 3.2 EPIC-10: `bootstrap_agent` MCP Tool

- [ ] Define flow: agent → bootstrap → return (role, runbook, allowed_tools, workflow)
- [ ] Coordinate with SQlite schema (EPIC-09)
- [ ] Break into 2–3 tasks (schema, tool impl, tests)
- [ ] AC + DoD documented
- **Owner:** Architect + Backend Developer Lead
- **Timeline:** 2 days

#### 3.3 EPIC-11: RBAC Migration YAML → SQLite

- [ ] Design migration strategy (backward compat, rollback plan)
- [ ] Break into 2–3 tasks (data mapping, migration script, tests)
- [ ] AC + DoD documented
- **Owner:** Architect + Backend Developer Lead
- **Timeline:** 2 days

#### 3.4 EPIC-12: Episodic Memory Layer

- [ ] Define: session recovery, ChromaDB write-back, reflection loop
- [ ] Break into 3+ tasks (session recovery, ChromaDB write, reflection)
- [ ] AC + DoD documented (some tasks can defer to later milestones)
- **Owner:** Architect + Backend Developer Lead
- **Timeline:** 2 days

#### 3.5 M02 Plan Update

- [ ] Milestone state.md: EPIC-09–12 tasks fully listed
- [ ] Dependencies documented (e.g., EPIC-09 prerequisite for EPIC-10)
- [ ] Timeline locked (start date 2026-03-15 estimated)
- **Owner:** Tech Lead

---

### Phase 4: Discovery Track Completion (3–5 days)

**Deliverables:** Verdict on mcp-integration & mcp-rbac experiments

#### 4.1 mcp-integration `04_test-and-learn`

- [ ] Run experiment from `03_prototype`
- [ ] Collect verdicts: accepted / rejected / compromised
- [ ] Document findings + recommendations
- **Owner:** Architect + Experimenter
- **Timeline:** 2–3 days

#### 4.2 mcp-rbac (discovery) `04_test-and-learn`

- [ ] Run experiment from `03_prototype`
- [ ] Collect verdicts
- [ ] Document findings
- **Owner:** Architect + Experimenter
- **Timeline:** 2–3 days

#### 4.3 Feed Results into Delivery Planning

- [ ] EPIC-09–12 AC refinement based on discovery verdicts
- [ ] Risk mitigation (if experiments show challenges)
- **Owner:** Architect
- **Timeline:** 1 day

---

## 📊 Summary: Work Breakdown

| Phase | Epic / Task | Days | Owner | Status |
|:------|:-----------|:-----|:------|:-------|
| 1.1 | Architecture Write Layer | 0.5 | Tech Lead | ✅ Done |
| 1.2 | EPIC-08 Organization | 1 | Tech Lead | ✅ Done |
| 1.3 | M01 Plan Updates | 0.5 | Tech Lead | ✅ Done |
| 1.4 | EPIC-09–12 AC Draft | 3 | Architect + Tech Lead | 🗓️ Pending |
| **Phase 1 Total** | **—** | **5 days** | **—** | **—** |
| 2.1 | TASK-08-01: Schema | 3 | Backend Dev | 🗓️ Ready |
| 2.2 | TASK-08-02: Tools | 3 | Backend Dev | 🗓️ Ready |
| 2.3 | TASK-08-03: E2E | 2 | QA Tester | 🗓️ Ready |
| 2.4 | EPIC-08 Sign-off | 1 | Backend Dev Lead | 🗓️ Ready |
| **Phase 2 Total** | **EPIC-08** | **9 days** | **—** | **—** |
| 3.1 | EPIC-09 Planning | 2 | Architect | 🗓️ Pending |
| 3.2 | EPIC-10 Planning | 2 | Architect | 🗓️ Pending |
| 3.3 | EPIC-11 Planning | 2 | Architect | 🗓️ Pending |
| 3.4 | EPIC-12 Planning | 2 | Architect | 🗓️ Pending |
| 3.5 | M02 Plan Update | 0.5 | Tech Lead | 🗓️ Pending |
| **Phase 3 Total** | **M02 Backlog** | **8.5 days** | **—** | **—** |
| 4.1 | mcp-integration `04_test` | 2 | Architect | 🗓️ Pending |
| 4.2 | mcp-rbac (discovery) `04_test` | 2 | Architect | 🗓️ Pending |
| 4.3 | Feed into M02 AC | 1 | Architect | 🗓️ Pending |
| **Phase 4 Total** | **Discovery Track** | **5 days** | **—** | **—** |

**Total Timeline:** ~27 days (can be parallelized)
**Critical Path:** Architecture (1.1–1.3) → EPIC-08 (Phase 2) → M02 Backlog planning (Phase 3)

---

## 🎓 Key Quality Principles (Dev Guidance)

### 1. All tasks MUST have

- ✅ **Acceptance Criteria**: Testable, measurable outcomes (min. 5 AC per task)
- ✅ **Definition of Done**: Checklist that matches standard DoD
- ✅ **Implementation Summary**: `implementation-summary/TASK-XX-YY-<slug>.md` (after completion)

### 2. Git & Commits

- 1 commit per task = 1 backlog item
- Format: `<type>(<scope>): <description> (TASK-XX-YY)`
- Example: `feat(metadata): implement write-layer schema (TASK-08-01)`

### 3. Architecture Decisions

- All schema changes → Architect review + ADR (if significant)
- All FSM state transitions → Architect sign-off
- All RBAC modifications → Security review

### 4. Testing

- Unit tests: >80% code coverage
- Integration tests: happy path + error paths
- E2E tests: full user workflow (agent → artifact → state)
- Tests pass on CI before PR merge

### 5. Documentation

- Inline code comments for non-obvious logic
- Tool descriptions (for LLM context)
- Error codes documented (for debugging)
- Architecture diagrams for complex flows

---

## 🚨 Risks & Mitigations

| Risk | Severity | Mitigation |
|:-----|:---------|:-----------|
| EPIC-08 scope creep | HIGH | Task breakdown is fixed; AC frozen before dev start |
| EPIC-09 schema conflicts with EPIC-08 | HIGH | Coordinate schema design upfront (Phase 1.4) |
| Discovery track verdicts delay M02 | MEDIUM | Run discovery tests in parallel (Phase 4) |
| FSM design complexity | MEDIUM | Architect reviews early; use state diagram visuals |
| E2E test flakiness | MEDIUM | Test infra setup done carefully; require reruns pass |
| Timeline slip | MEDIUM | Parallelization possible; Phase 4 (discovery) can delay if needed |

---

## ✅ Success Criteria

**Phase 1 Success:**

- [ ] MCP_Server_Architecture.md updated + approved by Architect
- [ ] EPIC-08 properly organized (goal.md, state.md, tasks complete)
- [ ] EPIC-09–12 AC drafted + reviewed

**Phase 2 Success (EPIC-08):**

- [ ] All 3 tasks complete (TASK-08-01, 02, 03)
- [ ] E2E tests GREEN, >80% coverage
- [ ] Implementation summaries exist for all tasks
- [ ] EPIC-08 FSM state: `CLOSED_DONE`

**Phase 3 Success (M02 Backlog):**

- [ ] EPIC-09–12 ready for development (full AC + task breakdown)
- [ ] M02 plan finalized

**Phase 4 Success (Discovery):**

- [ ] mcp-integration & mcp-rbac verdicts documented
- [ ] Recommendations fed into M02 AC

---

## 📝 Next Steps

1. **Architect Review** (2026-03-04–05): Review quality audit + implementation plan
2. **Developer Team Kickoff** (2026-03-05): Phase 1 final tasks + Phase 2 assignment
3. **Weekly Syncs**: Mon 10am for status + blockers
4. **Phase 1 Completion** (2026-03-10): Archive checks, M01/M02 ready for Phase 2
5. **Phase 2 Start** (2026-03-11): TASK-08-01 begins

---

**Prepared by:** Tech Lead
**Date:** 2026-03-04
**Status:** 🟡 Draft (Awaiting Architect + Team Review)
