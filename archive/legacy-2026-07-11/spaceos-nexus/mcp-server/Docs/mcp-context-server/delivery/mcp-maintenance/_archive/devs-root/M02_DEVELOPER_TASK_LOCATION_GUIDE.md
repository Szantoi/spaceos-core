---
type: developer-guide
name: M02_DEVELOPER_TASK_LOCATION_GUIDE
date: 2026-03-08
status: ✅ ACTIVE
---

# 🎯 M02 Developer Task Location Guide

**Quick Answer:** Hol találd meg a feladataidat az M02 milestone alatt?

---

## 📂 Task Location by EPIC

### **EPIC-11 (Context Middleware + RBAC)**

**Status:** ✅ DEV‑A WORK COMPLETE (Dev B, C still active)
**Task Location:**

```
📁 Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_02/epic_11/devs/dev-[a/b/c]/
   ├─ EPIC-11-INSTRUCTIONS.md           ← Read this first
   ├─ TASK-11-0[1-8]/
   │  ├─ TASK-11-0[1-8]-ASSIGNMENT.md   ← Task specification
   │  ├─ TASK-11-0[1-8]-KICKOFF.md      ← Implementation guide
   │  └─ IMPLEMENTATION-SUMMARY.md      ← Your completion report (fill after)
   └─ [Additional task folders...]
```

**For each task:**

1. Read `TASK-XX-ASSIGNMENT.md` — AC, scope, deliverables
2. Read `TASK-XX-KICKOFF.md` — Implementation roadmap
3. Implement
4. Write `IMPLEMENTATION-SUMMARY.md` — Document what you built

**Start:** [`epic_11/devs/README.md`](../../../milestones/milestone_02/epic_11/devs/README.md) (if exists)

---

### **EPIC-12 (Episodic Memory Layer)**

**Status:** � COMPLETE (Dev D finished Phase 1 tasks)
**Task Location:**

```
📁 Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_02/epic_12/
   ├─ goal.md                    ← Epic goal + scope
   ├─ state.md                   ← Current state (Phase 1 done)
   ├─ EPIC-12-REFACTORED-SPECIFICATION-v2_2026-03-06.md
   ├─ EPIC-12-AC-UPDATES-ONLINE-RESEARCH.md
   └─ [task folders completed]
      ├─ TASK-12-01/ ✅
      ├─ TASK-12-02/ ✅
      ├─ TASK-12-03/ ✅
      └─ TASK-12-04/ ✅
```

**Phase 1 delivered:** Episode storage, FTS5 & semantic search, hybrid E2E workflow.

---

```
📁 Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_02/epic_12/
   ├─ goal.md                    ← Epic goal + scope
   ├─ state.md                   ← Current state
   └─ [tasks/ — TO BE CREATED]
      ├─ TASK-12-01/
      ├─ TASK-12-02/
      ├─ TASK-12-03/
      └─ TASK-12-04/
```

**For Now (Prep Phase 2026-03-09 to 2026-03-17):**

- [ ] Read [`epic_12/goal.md`](../../../milestones/milestone_02/epic_12/goal.md) — Understand episodic memory vision
- [ ] Read [`epic_12/state.md`](../../../milestones/milestone_02/epic_12/state.md) — Current status
- [ ] Review spec: [`epic_12/EPIC-12-REFACTORED-SPECIFICATION-v2_2026-03-06.md`](../../../milestones/milestone_02/epic_12/EPIC-12-REFACTORED-SPECIFICATION-v2_2026-03-06.md)
- [ ] Prepare design mockup + task breakdown (Dev D responsibility)

**Task will be available:** 2026-03-18 (when task folder + files created)

---

### **EPIC-13 (Discovery Track Tools)**

**Status:** 🔴 PLANNED (starts 2026-03-18+)
**Task Location:** 🚧 **NOT YET CREATED** — Prepared once Tech Lead assigns EPIC-13 owner (Dev E or other)

```
📁 Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_02/epic_13/
   ├─ goal.md                    ← Epic goal + scope
   ├─ state.md                   ← Current state
   └─ [tasks/ — TO BE CREATED]
      ├─ TASK-13-01/
      ├─ TASK-13-02/
      ├─ [... 7 tasks total ...]
      └─ TASK-13-07/
```

**For Now (Prep Phase 2026-03-09 to 2026-03-17):**

- [ ] Read [`epic_13/goal.md`](../../../milestones/milestone_02/epic_13/goal.md) — DWI phase understanding
- [ ] Read [`epic_13/state.md`](../../../milestones/milestone_02/epic_13/state.md) — Current discovery readiness
- [ ] Skim spec for DDD + RBAC patterns

**Task will be available:** 2026-03-18+ (when task folder + files created)

---

### **EPIC-14 (Modern MCP Transports)**

**Status:** 🟡 REFINEMENT + DECISION GATE
**Task Location:** 🚧 **NOT YET CREATED** — Created after Tech Lead go/no-go (2026-03-14 EOD)

```
📁 Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_02/epic_14/
   ├─ goal.md                    ← Epic goal + scope
   ├─ state.md                   ← Current state
   └─ [tasks/ — TO BE CREATED AFTER DECISION]
      ├─ TASK-14-01/
      ├─ TASK-14-02/
      ├─ [... 12 tasks total ...]
      └─ TASK-14-12/
```

**Development Prep (Dev B + C, 2026-03-09 to 2026-03-10):**

**File locations for parallel refinement work:**

- [ ] [`dev-b/EPIC-14-REFINEMENT-STUDY-T14-02.md`](../devs/dev-b/EPIC-14-REFINEMENT-STUDY-T14-02.md) — HTTP Transport design study
- [ ] [`dev-c/EPIC-14-REFINEMENT-STUDY-T14-03.md`](../devs/dev-c/EPIC-14-REFINEMENT-STUDY-T14-03.md) — Plugin System design study

**For Now (Refinement Phase 2026-03-09 to 2026-03-10 EOD):**

- [ ] **Dev B:** Complete EPIC-14-REFINEMENT-STUDY-T14-02.md (design + PoC)
- [ ] **Dev C:** Complete EPIC-14-REFINEMENT-STUDY-T14-03.md (design + PoC)
- [ ] Tech Lead reviews + makes go/no-go decision (2026-03-14)

**Full tasks available:** 2026-03-19 (if approved)

---

## 🚀 Central Navigation

### **Milestone Root (Start Here)**

```
📁 Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_02/
   ├─ INDEX.md                                      ← Filing guide
   ├─ goal.md                                       ← M02 strategic goals
   ├─ state.md                                      ← M02 current status
   ├─ plan.md                                       ← M02 high-level roadmap
   ├─ M02_MILESTONE_STATUS_REPORT_2026-03-08.md    ← Live status + assignments
   ├─ M02_TASK_PRIORITIZATION_MATRIX_2026-03-08.md ← Task sequencing + blockers
   │
   ├─ epic_09/                                      ← EPIC-09 (100% complete)
   ├─ epic_10/                                      ← EPIC-10 (merge pending)
   ├─ epic_11/                                      ← EPIC-11 (active development)
   ├─ epic_12/                                      ← EPIC-12 (planned 2026-03-18)
   ├─ epic_13/                                      ← EPIC-13 (planned 2026-03-18+)
   ├─ epic_14/                                      ← EPIC-14 (pending decision)
   │
   ├─ 01-goals/                                     ← Strategic documentation
   ├─ 02-planning/                                  ← Roadmap + parallelization
   ├─ 03-kickoff/                                   ← Launch day materials
   ├─ 06-qa/                                        ← QA strategies + test docs
   └─ 08-deployment/                                ← Deployment guides
```

### **Developer Central (Task Assignments)**

```
📁 Docs/mcp-context-server/delivery/mcp-maintenance/devs/
   ├─ README.md                                     ← Start here for dev navigation
   ├─ M02_DEV_STATUS_REPORT_2026-03-08.md           ← Your assignment + timeline
   ├─ M02_TASK_PRIORITIZATION_MATRIX_2026-03-08.md ← Task sequencing
   ├─ DEVS_M02_CONSOLIDATED_REFERENCE.md           ← Quick reference
   │
   ├─ dev-a/                                        ← Dev A's task folder (ALL TASKS COMPLETE)
   │  ├─ EPIC-11-INSTRUCTIONS.md
   │  ├─ TASK-11-01/    (schema migration)
   │  ├─ TASK-11-03/    (FSM validator)
   │  ├─ TASK-11-06/    (RBAC migration)
   │  ├─ TASK-11-07/    (context middleware)
   │  └─ [follow-up items]
   │
   ├─ dev-b/                                        ← Dev B's task folder
   │  ├─ EPIC-11-INSTRUCTIONS.md
   │  ├─ EPIC-14-REFINEMENT-STUDY-T14-02.md        ← Parallel refinement work
   │  ├─ TASK-11-06/
   │  ├─ TASK-11-07/
   │  ├─ TASK-11-08/
   │  ├─ TASK-14-02/                                ← Created after go/no-go
   │  └─ [additional task folders]
   │
   ├─ dev-c/                                        ← Dev C's task folder
   │  ├─ EPIC-11-INSTRUCTIONS.md
   │  ├─ EPIC-14-REFINEMENT-STUDY-T14-03.md        ← Parallel refinement work
   │  ├─ TASK-11-02/
   │  ├─ TASK-11-04/
   │  ├─ TASK-11-05/
   │  ├─ TASK-14-03/                                ← Created after go/no-go
   │  └─ [additional task folders]
   │
   ├─ dev-d/                                        ← Dev D's task folder (EPIC-12 lead)
   │  ├─ TASK-12-01/                                ← pending start 2026-03-18
   │  ├─ TASK-12-02/                                ← pending
   │  ├─ TASK-12-03/                                ← pending
   │  ├─ TASK-12-04/                                ← pending
   │  └─ [prep items pre-2026-03-18]
   │
   └─ coordinator/                                  ← Coordination hub
      ├─ COORDINATOR-DASHBOARD.md                  ← Daily tracking
      └─ feedback/                                  ← Standup + completion reports
         ├─ dev-a/
         ├─ dev-b/
         ├─ dev-c/
         └─ [... feedback files]
```

---

## 📌 Quick Reference by Developer Role

### **If You're Dev A (EPIC-11 Lead)**

1. **Your Tasks:** [`Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_02/epic_11/devs/dev-a/`](../../../milestones/milestone_02/epic_11/devs/dev-a/)
2. **Task 1 (Critical):** [`TASK-11-01/TASK-11-01-ASSIGNMENT.md`](../../../milestones/milestone_02/epic_11/devs/dev-a/TASK-11-01/TASK-11-01-ASSIGNMENT.md)
3. **Status:** Use [`M02_DEV_STATUS_REPORT_2026-03-08.md`](./M02_DEV_STATUS_REPORT_2026-03-08.md) to track progress
4. **Report:** Create IMPLEMENTATION-SUMMARY.md after each task
5. **Follow-up:** Role validation error message (30 min, integrate into TASK-11-01)

**Timeline:**

- 2026-03-11: Start TASK-11-01
- 2026-03-12 EOD: Complete TASK-11-01 (unblocks Dev B + C)
- 2026-03-12+: Continue with TASK-11-03, TASK-11-06, TASK-11-07

---

### **If You're Dev B (EPIC-11 + EPIC-14)**

1. **Urgent (This Week!):** [`dev-b/EPIC-14-REFINEMENT-STUDY-T14-02.md`](./dev-b/EPIC-14-REFINEMENT-STUDY-T14-02.md)
2. **EPIC-11 Tasks:** [`Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_02/epic_11/devs/dev-b/`](../../../milestones/milestone_02/epic_11/devs/dev-b/)
3. **Current Status:** Waiting for Dev A TASK-11-01 completion (2026-03-12 EOD)
4. **Next:** TASK-11-02 (StateTracker) starts 2026-03-12
5. **Parallel:** EPIC-14 Refinement Study (Sat 2026-03-09 → Mon 2026-03-10 EOD)

**Timeline:**

- 2026-03-09-10: EPIC-14 refinement work
- 2026-03-12: Start TASK-11-02 (once Dev A finishes)
- 2026-03-13: Start TASK-11-08 (Two-Track Routing)
- 2026-03-19 (if approved): Start EPIC-14 full implementation

---

### **If You're Dev C (EPIC-11 + EPIC-14)**

1. **Urgent (This Week!):** [`dev-c/EPIC-14-REFINEMENT-STUDY-T14-03.md`](./dev-c/EPIC-14-REFINEMENT-STUDY-T14-03.md)
2. **EPIC-11 Tasks:** [`Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_02/epic_11/devs/dev-c/`](../../../milestones/milestone_02/epic_11/devs/dev-c/)
3. **Current Status:** Waiting for Dev A TASK-11-01 completion (2026-03-12 EOD)
4. **Next:** TASK-11-04 (Resumption Logic) starts 2026-03-13
5. **Parallel:** EPIC-14 Refinement Study (Sat 2026-03-09 → Mon 2026-03-10 EOD)

**Timeline:**

- 2026-03-09-10: EPIC-14 refinement work
- 2026-03-13: Start TASK-11-04 (once Dev A finishes T11-01 + Dev B starts T11-02)
- 2026-03-14: Start TASK-11-05 (FSM E2E Testing)
- 2026-03-19 (if approved): Start EPIC-14 full implementation

---

### **If You're Dev D (EPIC-12 Lead)**

1. **Prep Reading (This Week):** [`epic_12/EPIC-12-REFACTORED-SPECIFICATION-v2_2026-03-06.md`](../../../milestones/milestone_02/epic_12/EPIC-12-REFACTORED-SPECIFICATION-v2_2026-03-06.md)
2. **Specification:** [`epic_12/goal.md`](../../../milestones/milestone_02/epic_12/goal.md)
3. **Status:** [`epic_12/state.md`](../../../milestones/milestone_02/epic_12/state.md)
4. **Your Folder:** [`Docs/mcp-context-server/delivery/mcp-maintenance/devs/dev-d/`](./dev-d/) (will be populated 2026-03-18)
5. **Follow-up:** 5MB episode storage mock validation (1-2h, integrate into TASK-12-01)

**Timeline:**

- 2026-03-09-17: Spec review + prep work
- 2026-03-18: TASK-12-01 starts (Episode Storage)
- 2026-03-20: TASK-12-02 (FTS5)
- 2026-03-22: TASK-12-03 (ChromaDB)
- 2026-03-24: TASK-12-04 (E2E Integration)

---

### **If You're QA1 (Testing Lead)**

1. **E2E Integration (This Week):** [`epic_11/QA-EPIC-11-TASK-VERIFICATION_2026-03-06.md`](../../../milestones/milestone_02/epic_11/QA-EPIC-11-TASK-VERIFICATION_2026-03-06.md)
2. **EPIC-12 Test Strategy:** [`epic_12/QA-EPIC-12-TASK-VERIFICATION_2026-03-06.md`](../../../milestones/milestone_02/epic_12/QA-EPIC-12-TASK-VERIFICATION_2026-03-06.md)
3. **EPIC-14 Test Strategy:** [`epic_14/EPIC-14-QA-TEST-STRATEGY.md`](../../../milestones/milestone_02/epic_14/EPIC-14-QA-TEST-STRATEGY.md)

**Timeline:**

- 2026-03-15-17: EPIC-11 E2E integration testing
- 2026-03-24+: EPIC-12 E2E tests (TASK-12-04)
- 2026-03-26+: EPIC-14 E2E tests (TASK-14-11)

---

## 🔗 Important Links

| Resource | Purpose | Link |
|:---------|:--------|:-----|
| **M02 Status** | Current assignments + timeline | [`M02_DEV_STATUS_REPORT_2026-03-08.md`](./M02_DEV_STATUS_REPORT_2026-03-08.md) |
| **Task Sequencing** | Critical path vs parallel opportunities | [`M02_TASK_PRIORITIZATION_MATRIX_2026-03-08.md`](./M02_TASK_PRIORITIZATION_MATRIX_2026-03-08.md) |
| **Filing Guide** | Directory structure + moving files | [`../milestones/milestone_02/FILING-GUIDE-UPDATED.md`](../../../milestones/milestone_02/FILING-GUIDE-UPDATED.md) |
| **Epic 11 Lead** | EPIC-11 specific coordination | [`../milestones/milestone_02/epic_11/TECH_LEAD_KICKOFF_EPIC_11_2026-03-08.md`](../../../milestones/milestone_02/epic_11/TECH_LEAD_KICKOFF_EPIC_11_2026-03-08.md) |
| **Epic 12 Spec** | Episodic memory system specification | [`../milestones/milestone_02/epic_12/goal.md`](../../../milestones/milestone_02/epic_12/goal.md) |
| **Epic 13 Spec** | Discovery track specification | [`../milestones/milestone_02/epic_13/goal.md`](../../../milestones/milestone_02/epic_13/goal.md) |
| **Epic 14 Spec** | Modern MCP transports | [`../milestones/milestone_02/epic_14/goal.md`](../../../milestones/milestone_02/epic_14/goal.md) |

---

## ❓ Still Can't Find Your Task?

**Troubleshooting:**

1. **Check your name** in [`M02_DEV_STATUS_REPORT_2026-03-08.md`](./M02_DEV_STATUS_REPORT_2026-03-08.md) — confirms your EPIC assignment
2. **Verify your folder** — `Docs/mcp-context-server/delivery/mcp-maintenance/devs/dev-[a/b/c/d]/`
3. **Look for TASK-XX folders** — Each task should have its own folder
4. **Can't find it?** → Likely not created yet (EPIC-12/13/14 tasks created later)
5. **Ask Tech Lead** → Slack #m02-dev or email (details in M02_DEV_STATUS_REPORT)

---

**Last Updated:** 2026-03-08
**Status:** ✅ ACTIVE
**Next Review:** 2026-03-10 (after EPIC-14 refinement checkpoint)
