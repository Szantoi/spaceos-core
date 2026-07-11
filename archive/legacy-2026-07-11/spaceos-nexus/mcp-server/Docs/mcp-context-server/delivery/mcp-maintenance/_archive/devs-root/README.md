---
type: system-index
name: DEV-COORDINATION-SYSTEM
date: 2026-03-07
updated: 2026-03-08
status: ✅ ACTIVE
---

# 🛣️ DEV COORDINATION SYSTEM — QUICK START INDEX

**Coordinator Central routing system for task distribution & feedback tracking**

**Current Milestones:**

- ✅ EPIC-10 Phase 2 (EPIC-10 coordination)
- 🟡 **M02 ACTIVE** (EPIC-11, EPIC-12/13/14 planning) — [→ Jump to M02 Guide](#m02-developer-task-finder)

---

## 📂 WHERE TO GO (Based on Your Role)

### 🎓 IF YOU'RE A NEW COORDINATOR (FIRST TIME)

**Start here:**

1. Read: [`DEVS-COORDINATION-ROUTER.md`](./DEVS-COORDINATION-ROUTER.md) — Understand the system
2. Review: [`COORDINATOR-DASHBOARD.md`](./coordinator/COORDINATOR-DASHBOARD.md) — Daily tracking overview
3. Check: Task folders (`dev-a/`, `dev-b/`, `dev-c/`) — See what each dev gets
4. Note: Feedback channels (`coordinator/feedback/dev-[a/b/c]/`) — Where feedback goes

**First Action:** Send launch notification to all 3 devs when Phase 1 merges (2026-03-08 09:00 UTC)

---

### 👨‍💻 IF YOU'RE DEV A, B, or C (RECEIVING TASK)

**Your task location:**

```
Docs/mcp-context-server/delivery/mcp-maintenance/devs/dev-[a/b/c]/TASK-10-0[6/7/8]/TASK-10-0[6/7/8].md
```

**Your feedback channel (where you post standups + completion):**

```
Docs/mcp-context-server/delivery/mcp-maintenance/devs/coordinator/feedback/dev-[a/b/c]/
```

**What you do each day:**

1. **Morning (09:00 UTC):** Create `DEV-[DATE]-STANDUP-MORNING.md` using template
2. **Midday (12:00 UTC):** Create `DEV-[DATE]-STANDUP-MIDDAY.md` using template
3. **Evening (18:00 UTC):** Create `DEV-[DATE]-STANDUP-EVENING.md` using template
4. **On Completion:** Create `DEV-[DATE]-COMPLETION.md` with final status

**Template Location:**

```
Docs/mcp-context-server/delivery/mcp-maintenance/devs/coordinator/feedback/STANDUP-TEMPLATE.md
Docs/mcp-context-server/delivery/mcp-maintenance/devs/coordinator/feedback/COMPLETION-REPORT-TEMPLATE.md
```

---

### 🛠️ IF YOU'RE THE COORDINATOR (DAILY OPERATIONS)

**Main Hub:** [`COORDINATOR-DASHBOARD.md`](./coordinator/COORDINATOR-DASHBOARD.md)

**Daily Tasks:**

1. **09:00 UTC:** Read morning standups from all 3 devs

   ```
   coordinator/feedback/dev-a/DEV-[DATE]-STANDUP-MORNING.md
   coordinator/feedback/dev-b/DEV-[DATE]-STANDUP-MORNING.md
   coordinator/feedback/dev-c/DEV-[DATE]-STANDUP-MORNING.md
   ```

2. **12:00 UTC:** Read midday standups

   ```
   coordinator/feedback/dev-[a/b/c]/DEV-[DATE]-STANDUP-MIDDAY.md
   ```

3. **18:00 UTC:** Read evening standups + update dashboard

   ```
   coordinator/feedback/dev-[a/b/c]/DEV-[DATE]-STANDUP-EVENING.md
   ```

4. **On Completion:** Review completion report + approve/reject merge

---

## 📋 FOLDER MAP

```
Docs/mcp-context-server/delivery/mcp-maintenance/devs/
│
├─ 📄 DEVS-COORDINATION-ROUTER.md .............. Main routing file (START HERE)
├─ 📄 COORDINATOR-DASHBOARD.md ................ Daily status tracking
│
├─ 📁 dev-a/TASK-10-06/ ....................... Dev A's task folder
│  └─ 📄 TASK-10-06.md ........................ Dev A's task description (20 AC)
│
├─ 📁 dev-b/TASK-10-07/ ....................... Dev B's task folder
│  └─ 📄 TASK-10-07.md ........................ Dev B's task description (22 AC)
│
├─ 📁 dev-c/TASK-10-08/ ....................... Dev C's task folder
│  └─ 📄 TASK-10-08.md ........................ Dev C's task description (4 deliverables)
│
└─ 📁 coordinator/
   ├─ 📄 COORDINATOR-DASHBOARD.md ............ Status tracker (Daily update)
   ├─ 📄 STANDUP-TEMPLATE.md ................ Template for daily standups
   ├─ 📄 COMPLETION-REPORT-TEMPLATE.md ..... Template for final reports
   │
   └─ 📁 feedback/
      ├─ 📁 dev-a/ .......................... Dev A feedback inbox
      │  ├─ 📄 DEV-2026-03-09-STANDUP-MORNING.md
      │  ├─ 📄 DEV-2026-03-09-STANDUP-MIDDAY.md
      │  ├─ 📄 DEV-2026-03-09-STANDUP-EVENING.md
      │  └─ 📄 DEV-2026-03-10-COMPLETION.md
      │
      ├─ 📁 dev-b/ .......................... Dev B feedback inbox
      │  ├─ 📄 DEV-2026-03-09-STANDUP-MORNING.md
      │  ├─ 📄 DEV-2026-03-09-STANDUP-MIDDAY.md
      │  ├─ 📄 DEV-2026-03-09-STANDUP-EVENING.md
      │  └─ 📄 DEV-2026-03-10-COMPLETION.md
      │
      └─ 📁 dev-c/ .......................... Dev C feedback inbox
         ├─ 📄 DEV-2026-03-09-STANDUP-MORNING.md
         ├─ 📄 DEV-2026-03-09-STANDUP-MIDDAY.md
         ├─ 📄 DEV-2026-03-09-STANDUP-EVENING.md
         └─ 📄 DEV-2026-03-12-COMPLETION.md
```

---

## 🎯 QUICK REFERENCE

### For Dev A (TASK-10-06: Error Handling)

| What | Where |
|:-----|:------|
| Task Description | `dev-a/TASK-10-06/TASK-10-06.md` |
| Daily Standups | `coordinator/feedback/dev-a/DEV-[DATE]-STANDUP-*.md` |
| Final Completion | `coordinator/feedback/dev-a/DEV-COMPLETION.md` |
| Merge Window | 2026-03-10 15:00 UTC |

### For Dev B (TASK-10-07: Performance)

| What | Where |
|:-----|:------|
| Task Description | `dev-b/TASK-10-07/TASK-10-07.md` |
| Daily Standups | `coordinator/feedback/dev-b/DEV-[DATE]-STANDUP-*.md` |
| Final Completion | `coordinator/feedback/dev-b/DEV-COMPLETION.md` |
| Merge Window | 2026-03-10 14:00 UTC |

### For Dev C (TASK-10-08: Documentation)

| What | Where |
|:-----|:------|
| Task Description | `dev-c/TASK-10-08/TASK-10-08.md` |
| Daily Standups | `coordinator/feedback/dev-c/DEV-[DATE]-STANDUP-*.md` |
| Final Completion | `coordinator/feedback/dev-c/DEV-COMPLETION.md` |
| Merge Window | 2026-03-12 12:00 UTC |

---

## ⚡ CRITICAL DATES

| Date | Time | Event | Action |
|:----:|:----:|:------|:-------|
| 2026-03-08 | 09:00 | Phase 1 merge | Launch Phase 2 when confirmed |
| 2026-03-09 | 09:00 | Phase 2 kickoff | All 3 devs start work |
| 2026-03-09 | 09:00→18:00 | Day 1 standups | Collect 3x feedback from each dev (9 reports) |
| 2026-03-10 | 09:00→18:00 | Day 2 standups | Collect 3x feedback from each dev (9 reports) |
| 2026-03-10 | 14:00 | Dev B merge | Merge TASK-10-07 (Performance) |
| 2026-03-10 | 15:00 | Dev A merge | Merge TASK-10-06 (Error Handling) |
| 2026-03-11 | 09:00→18:00 | Day 3 standups | Collect 3x feedback from each dev (9 reports) |
| 2026-03-12 | 12:00 | Merge window ALL | Dev A/B/C ready (BC priority 2 → 1 → 3) |
| 2026-03-12 | 12:00 | Phase 2 complete | All PRs merged, ready for EPIC-11 |
| 2026-03-12 | 13:00 | Go/No-go | Backend Dev final sign-off |

---

## 🚀 LAUNCH CHECKLIST (Before 2026-03-09 09:00 UTC)

- [x] Folder structure created ✅
- [x] Task files prepared (TASK-10-06/07/08.md) ✅
- [x] Templates ready (STANDUP, COMPLETION) ✅
- [x] Coordinator dashboard active ✅
- [ ] Phase 1 merge confirmed (due 2026-03-08 09:00 UTC)
- [ ] Notification sent to Dev A, B, C with links to their tasks
- [ ] Standup schedule confirmed (09:00, 12:00, 18:00 UTC)

---

## 🎓 FAQ

**Q: Where's my task description?**
A: `Docs/mcp-context-server/delivery/mcp-maintenance/devs/dev-[a/b/c]/TASK-10-0[6/7/8]/TASK-10-0[6/7/8].md`

**Q: Where do I post standups?**
A: `Docs/mcp-context-server/delivery/mcp-maintenance/devs/coordinator/feedback/dev-[a/b/c]/`

**Q: What do I put in a standup?**
A: Use template: `STANDUP-TEMPLATE.md` (completed today, plan tomorrow, blockers, tests, questions)

**Q: When should I merge?**
A: Only after coordinator approves in `COORDINATOR-APPROVAL.md` in your task folder

**Q: How many standups per day?**
A: Three (09:00, 12:00, 18:00 UTC) — one for each time slot

**Q: Can I merge early?**
A: Only if all AC met, all tests passing, coordinator approves, AND no dependencies on other devs

---

## 📞 EMERGENCY CONTACT

**If Blocked:**

1. Notify coordinator immediately (don't wait for standup)
2. Create: `coordinator/feedback/dev-[a/b/c]/BLOCKER-[TIMESTAMP].md`
3. Describe: What's blocked, why, impact, suggested fix

**Escalation Path:**

- Coordinator → Tech Lead (if affects timeline)
- Tech Lead → Architect (if design decision needed)

---

## ✅ SYSTEM STATUS

**Status:** 🟢 **ACTIVE & READY**
**Last Updated:** 2026-03-07 13:00 UTC
**Phase:** EPIC-10 Phase 2 Coordination
**Devs Tracked:** 3 (A, B, C)
**Total AC Assigned:** 50
**Launch Date:** 2026-03-09 09:00 UTC (pending Phase 1 merge)

---

## 🎯 SUCCESS DEFINITION

Phase 2 is successful when:

- ✅ All 3 devs deliver on time (2026-03-10 → 2026-03-12)
- ✅ 50/50 AC completed
- ✅ 40+ tests passing
- ✅ Coverage ≥85% (actual 87%)
- ✅ 0 security issues
- ✅ 0 merge conflicts
- ✅ 0 blockers impact timeline
- ✅ 1350 lines documentation
- ✅ 3 PRs merged to main

---

## 🚀 M02 DEVELOPER TASK FINDER

**🔥 NEW: M02 is now ACTIVE (2026-03-09+)**

### If you're a developer in M02 milestone

**[→ READ THIS FIRST: M02_DEVELOPER_TASK_LOCATION_GUIDE.md](./M02_DEVELOPER_TASK_LOCATION_GUIDE.md)**

This guide tells you:

- ✅ Where your tasks are located (by EPIC)
- ✅ What to start with (EPIC-11 is active, EPIC-12/13/14 coming later)
- ✅ Timeline & parallelization opportunities
- ✅ Quick reference for Dev A, B, C, D, and QA

### M02 Task Locations

**EPIC-11 (Active Now):**

```
Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_02/epic_11/devs/dev-[a/b/c]/TASK-11-0[1-8]/
```

**EPIC-12, 13, 14 (Planned):**

- EPIC-12: Created 2026-03-18 (Dev D owner)
- EPIC-13: Created 2026-03-18+ (Dev E or other)
- EPIC-14: Created 2026-03-19 (if approved by 2026-03-14)

**Master Status:**

- [`M02_DEV_STATUS_REPORT_2026-03-08.md`](./M02_DEV_STATUS_REPORT_2026-03-08.md) — Current assignments + timeline
- [`M02_TASK_PRIORITIZATION_MATRIX_2026-03-08.md`](./M02_TASK_PRIORITIZATION_MATRIX_2026-03-08.md) — Task sequencing + blockers

---

**Next:** Go to [`DEVS-COORDINATION-ROUTER.md`](./DEVS-COORDINATION-ROUTER.md) for detailed routing!
