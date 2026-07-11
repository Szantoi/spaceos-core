---
title: "Dev E — EPIC-13 Discovery Track Tools — Kickoff Overview"
created: 2026-03-09
updated: 2026-03-09
assigned_to: "Dev E"
epic: "EPIC-13"
status: "🟢 READY TO START 2026-03-18"
---

# 🚀 Dev E — Welcome to EPIC-13: Discovery Track Tools

**Epic:** EPIC-13 (Discovery Track Tools — DWI State Support)
**Milestone:** M02
**Start Date:** 2026-03-18 (same day as EPIC-12 kickoff)
**Duration:** ~3.5 weeks (7 tasks, 100h total effort)
**Impact:** Realizes Vision Goal #3 — Unified Two-Track Model

---

## 🎯 What You're Building

The **discovery track** is now a **first-class citizen** alongside the delivery track. Discovery agents need:

✅ **Discovery-specific roles** (`architect`, `researcher`)
✅ **DWI workflow templates** (Discover → Why → Implement)
✅ **Discovery tools** (request context, reference prior discoveries, submit outcomes)
✅ **Track-based RBAC** (only discovery agents see discovery tools)
✅ **Episodic memory integration** (store + retrieve discovery episodes)
✅ **Phase validation gates** (enforce workflow order: ideation → validation → iteration → handoff)
✅ **Full E2E validation** (entire discovery workflow tested end-to-end)

---

## 📋 Your 7 Tasks (Sequential Dependency Chain)

```
TASK-13-01 (17h) — Discovery Roles & DWI Workflow Template
    ↓
TASK-13-02 (14h) — RBAC Discovery Filter & Track Routing
    ↓
TASK-13-03 (16h) — Discovery Tools (request_context, reference_prior_discovery)
    ↓
TASK-13-04 (12h) — Submit Discovery Outcome Tool
    ↓
TASK-13-05 (13h) — Phase-Specific Tools & Validation Gates
    ↓
TASK-13-06 (11h) — Blocker Tracking & Search Fallback
    ↓
TASK-13-07 (14h) — E2E Integration & Full Workflow Validation
```

**Total Effort:** ~100 hours
**Timeline:** 2026-03-18 through 2026-04-01 (3.5 weeks)

---

## ⏰ Timeline & Kickoff

| Date | Task | Status | Duration |
|:---|:---|:---|:---|
| 2026-03-18 | TASK-13-01 | ✅ DONE | 17h (3 days) |
| 2026-03-21 | TASK-13-02 | � READY | 14h (2 days) |
| 2026-03-23 | TASK-13-03 | 🟡 BLOCKED | 16h (2.5 days) |
| 2026-03-26 | TASK-13-04 | 🟡 BLOCKED | 12h (2 days) |
| 2026-03-28 | TASK-13-05 | 🟡 BLOCKED | 13h (2 days) |
| 2026-03-30 | TASK-13-06 | 🟡 BLOCKED | 11h (1.5 days) |
| 2026-03-31 | TASK-13-07 | 🟡 BLOCKED | 14h (2 days) |
| 2026-04-01 | EPIC-13 Complete | ✅ | Peer review + merge |

---

## 🔗 Integration Points

**EPIC-11 Dependency (TASK-11-01 BLOCKED):**

- TASK-13-02 needs `agent_sessions` table from EPIC-11 (already exists)
- RBAC middleware from EPIC-11 extended for discovery track

**EPIC-12 Dependency (Asynchronous, no blocker):**

- TASK-13-03 uses EPIC-12 ChromaDB for `reference_prior_discovery()`
- TASK-13-04 stores discovery outcomes into EPIC-12 episodic memory
- TASK-13-06 uses EPIC-12 FTS5 as search fallback

---

## 🎓 Key Concepts You'll Learn

### 1. **DWI Model = Discovery Workflow**

- **Discover:** Generate hypotheses (ideation phase)
- **Why:** Test hypotheses (validation phase)
- **Implement:** Refine & prepare for handoff (iteration + delivery_handoff phases)

### 2. **Track-Based Routing**

- Sessions have `track = "discovery" | "delivery"`
- RBAC filters tools by track (discovery agents can't access delivery tools)
- Middleware routes requests based on track + role

### 3. **Phase Gates**

- Discovery workflow has 4 phases with entrance/exit criteria
- Agents must complete each phase before proceeding
- Tools available per phase (researcher can't access ideation phase)

### 4. **Episodic Memory as RAG**

- Discovery episodes stored in EPIC-12 with `track="discovery"`
- Future discovery agents retrieve similar past episodes
- Semantic + keyword search for flexible retrieval

---

## 📚 Key Files to Read Before Starting

1. **EPIC-13 Goal:** `milestones/milestone_02/epic_13/goal.md`
2. **EPIC-11 Reference:** `milestones/milestone_02/epic_11/goal.md` (middleware, RBAC patterns)
3. **EPIC-12 Reference:** `milestones/milestone_02/epic_12/goal.md` (episodic memory, ChromaDB)
4. **Database Schema:** `database/standards/02-delivery/` (schema patterns, migration best practices)

---

## 💡 Tips for Success

✅ **Start with TASK-13-01:** Role definitions are the foundation for everything else
✅ **Keep tight with EPIC-12:** ChromaDB integration starts in TASK-13-03, coordinate with Dev D
✅ **Test phase gates early:** TASK-13-05 blocker validation is critical for workflow integrity
✅ **E2E test constantly:** Don't wait for TASK-13-07 to test end-to-end — build tests incrementally
✅ **Use TypeScript strict mode:** All types non-optional, no `any` allowed
✅ **Document assumptions:** Each task has different role/phase constraints — document clearly

---

## 🤝 Who's Around

- **Tech Lead:** For architectural questions, RBAC design, track routing
- **Dev D:** EPIC-12 (episodic memory) — coordinate on ChromaDB integration
- **Dev A/B/C:** EPIC-11 (middleware, RBAC) — leverage their middleware patterns
- **QA1:** Test strategy, E2E test design help

---

## 📞 Questions Before Starting?

- What is "track routing"? → Middleware uses `session.track` to filter tools
- When do I need EPIC-12? → TASK-13-03 (ChromaDB semantic search)
- What about EPIC-14? → Separate track, don't worry about it (Dev B/C handling)
- Can tasks run in parallel? → No, sequential dependency chain (TASK-13-01 → 13-02 → ... → 13-07)

---

## 🎯 Success Criteria for EPIC-13

**You win when:**

- ✅ All 7 tasks complete with 100% AC passing
- ✅ 100+ unit tests passing (E2E + integration)
- ✅ Full DWI workflow tested end-to-end (ideation → handoff)
- ✅ Discovery agents can call discovery tools, delivery agents get UNAUTHORIZED
- ✅ Discovery episodes stored in EPIC-12 episodic memory + retrievable
- ✅ Phase gates enforced (can't skip phases)
- ✅ Code merged + EPIC-13 deployment ready

---

Good luck! 🚀 You've got this. Start with TASK-13-01 on 2026-03-18, and let's build the unified two-track model!

— Backend Team
