---
title: "Execution Model Update — Dependency-Driven (NOT Calendar-Driven)"
type: "tech-lead-directive"
created: 2026-03-11
updated: 2026-03-11
status: "🟢 EFFECTIVE IMMEDIATELY"
---

# 📋 Execution Model Change: Dependency-Driven

**Date:** 2026-03-11
**Change:** Removed calendar-based start dates. Introduced **dependency-driven task triggering**.

---

## ❌ OLD MODEL (Calendar-Driven)

Tasks started by fixed calendar dates:
```
Dev A: Start 2026-03-18 09:00 UTC (regardless of prep)
Dev B: Start 2026-03-20 02:00 UTC (regardless of TASK-14-01 status)
Dev D: Start 2026-03-11, finish by 2026-03-12 17:00 UTC (deadline pressure)
```

**Problem:** Tasks started on calendar dates even if blockers existed. Pressure to meet dates instead of quality.

---

## ✅ NEW MODEL (Dependency-Driven)

Tasks start **when their blockers are complete**, not by calendar date:

```
Dev D: No blocker → Start NOW
  └─ TASK-12-01 ✅ → TASK-12-02 auto-unblocked → Start immediately (no waiting)
  └─ TASK-12-02 ✅ → TASK-12-03 auto-unblocked → Start immediately
  └─ (repeat for TASK-12-04)

Dev A: No blocker → Start NOW (parallel with D)
  └─ TASK-14-01 ✅ → TASK-14-02 auto-unblocked for both Dev A + Dev B

Dev B: Blocked by TASK-14-01
  └─ When TASK-14-01 ✅ → Dev B starts TASK-14-02 immediately

Dev C: No blocker → Start NOW (parallel with A, independent)
  └─ TASK-14-03 ✅ → TASK-14-04 auto-unblocked → Start immediately

Dev E: No blocker → Start NOW (parallel with others)
  └─ TASK-13-01 ✅ → TASK-13-02 auto-unblocked → Start immediately
```

**Benefit:** Developers start as soon as possible. No arbitrary waiting. Quality over deadlines.

---

## 📝 What Changed

### DEVELOPER_ASSIGNMENT_DISPATCH_2026-03-11.md

| Section | Old | New |
|:--------|:---:|:---:|
| Dev A start | "2026-03-18" | "No blocker → Start immediately" |
| Dev B start | "2026-03-18" → "Wait for A" | "When TASK-14-01 complete" |
| Dev C start | "2026-03-19" | "No blocker → Start immediately" |
| Dev D start | "2026-03-11" deadline | "No blocker → Start NOW" |
| Dev E start | "2026-03-18" | "No blocker → Start immediately" |
| TASK trigger | Calendar date | "START when X task complete" |

### PUBLIC_NOTICE_M02_KICKOFF_2026-03-11.md

| Section | Change |
|:--------|:-------|
| Task Table | "Start Date" → "Start Condition" |
| Dev A/B/C/E section | Removed "Start 2026-03-18" deadline |
| Dev D section | Removed "Complete by 2026-03-12 17:00" deadline |
| blocker section | Added "When X ✅, Y starts immediately" logic |

### COORDINATOR_DASHBOARD_2026-03-11.md

| Section | Change |
|:--------|:-------|
| Real-time status | "Waiting for 3-18" → "No blocker → Start now" |
| Dev A | "starts 2026-03-18" → "Start when blocker clear" |
| Dev B | "Start Date 2026-03-20" → "Starts when TASK-14-01 ✅" |
| Dev E | "starts 2026-03-18" → "Start when blocker clear" |
| Milestones | Calendar dates → Dependency triggers |

---

## 🎯 Key Principles

1. **No Waiting:** When a task completes, the next task **immediately** becomes available.
2. **Parallel When Possible:** Independent tasks (A, C, E) start simultaneously; B waits for A only.
3. **Quality Over Dates:** Developers focus on AC + tests, not meeting calendar deadlines.
4. **Transparency:** Everyone sees blocking task → unblocking task relationship.

---

## 📊 Task Dependencies (Updated)

```
┌─ Dev D: TASK-12-01 ────────────────────────────────┐
│         ↓ (when ✅)                                 │
│         TASK-12-02 → TASK-12-03 → TASK-12-04 ✅   │
└────────────────────────────────────────────────────┘

┌─ Dev A: TASK-14-01 ────────────┐
│         ↓ (when ✅)            │
│         TASK-14-02 → TASK-14-05 ✅
│
├─ Dev B: (blocks on TASK-14-01) ─ When A's TASK-14-01 ✅ → Start TASK-14-02
│
├─ Dev C: TASK-14-03 ────────────┐
│         ↓ (when ✅)            │
│         TASK-14-04 → TASK-14-05 ✅
│
└─ Dev E: TASK-13-01 ────────────┐
          ↓ (when ✅)            │
          TASK-13-02 → ... → TASK-13-07 ✅
```

---

## 🚀 Coordination Process (Updated)

### Standup Status

Include in every standup:
```markdown
## Status
- TASK-XX-YY: X% complete
- **Next Task Ready?** [Yes ✅ / Waiting for TASK-X]
```

When next task is ready, developer **starts immediately** (no waiting for calendar date).

### Blocker Escalation

**Minor blocker (< 1h wait):**
- Continue next task, note in standup

**Major blocker (> 1h wait):**
- Escalate → Tech Lead for priority resolution
- Don't wait for calendar date

---

##  ✅ Effective Immediately

This model is **effective now** (2026-03-11). All developers should:

1. ✅ Ignore calendar dates in old documents
2. ✅ Check task dependencies instead (DEVELOPER_ASSIGNMENT_DISPATCH)
3. ✅ Start when **blocker condition met**, not by calendar
4. ✅ Post standup: "Next task ready? Yes/No"
5. ✅ On task completion, notify coordinator → developer unblocks immediately

---

## 📞 Questions?

**Q: When do I start?**
A: When your task has **no blocker**. Check the dependency table above.

**Q: What if my blocker is taking longer?**
A: Post in #architecture. Tech Lead will prioritize if needed. Don't wait for arbitrary calendar date.

**Q: How does coordinator track "ready to unblock"?**
A: Standup includes "Next Task Ready?" field. Coordinator watches completion reports.

---

**Status:** 🟢 Ready for execution
**Model:** Dependency-driven → Zero artificial waiting

