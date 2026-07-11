---
type: agent-initialization
name: Backend Developer Agent — EPIC-10 Phase 2 Coordinator
version: 1.0
date: 2026-03-07
target: VS Code Agent Invocation
status: READY
---

# 🚀 AGENT INITIALIZATION INSTRUCTION

## Quick Invoke Command

**Copy-paste this into VS Code Command Palette or terminal:**

```bash
@Backend Developer Agent

EPIC-10 Phase 2 coordination is active.
I have 3 developers (Dev A, B, C) with parallel tasks (6h + 5h + 8h).
Please coordinate task distribution, monitor daily standups, and validate merge readiness.

Key coordination system paths:
- Task assignments: Docs/mcp-context-server/delivery/mcp-maintenance/devs/
- Dev A (TASK-10-06): dev-a/TASK-10-06/TASK-10-06.md
- Dev B (TASK-10-07): dev-b/TASK-10-07/TASK-10-07.md
- Dev C (TASK-10-08): dev-c/TASK-10-08/TASK-10-08.md
- Feedback channels: coordinator/feedback/dev-[a/b/c]/
- Coordinator dashboard: coordinator/COORDINATOR-DASHBOARD.md

Start with DEVS-COORDINATION-ROUTER.md for system overview.
```

---

## Alternative: Full Context Invoke

```bash
@Backend Developer Agent mode:default

You are coordinating EPIC-10 Phase 2 (3 parallel developers).

**System Status:**
- Phase 1: Complete (merged 2026-03-08 awaiting)
- Phase 2: Planned execution 2026-03-09 → 2026-03-11
- 3 developers assigned (Dev A, B, C)
- Total AC: 50 (20+22+8)
- Total hours: 19 (6h+5h+8h parallel = 1.5 days critical path)

**Your Tasks:**
1. Route task assignments to each developer
2. Monitor daily standups (09:00, 12:00, 18:00 UTC)
3. Collect feedback from coordinator/feedback/dev-[a/b/c]/
4. Validate AC completion & test coverage
5. Approve/reject PRs before merge
6. Escalate blockers immediately

**Coordination Center:**
- Main hub: Docs/mcp-context-server/delivery/mcp-maintenance/devs/DEVS-COORDINATION-ROUTER.md
- Dashboard: Docs/mcp-context-server/delivery/mcp-maintenance/devs/coordinator/COORDINATOR-DASHBOARD.md
- Task templates: Each dev has dev-[a/b/c]/TASK-10-0X/TASK-10-0X.md

**Status:** Ready to receive standups beginning 2026-03-09 09:00 UTC
```

---

## Terminal Command (Direct Invocation)

**If using MCP CLI directly:**

```powershell
# PowerShell
$env:AGENT = "Backend Developer Agent"
$env:MODE = "coordinator"
$env:PHASE = "EPIC-10-Phase-2"

# Then invoke your MCP server with context
npx mcp invoke --agent "$env:AGENT" --mode "$env:MODE"
```

---

## VS Code Keyboard Shortcut

**Press Ctrl+Shift+I** (or Cmd+Shift+I on Mac) to open Agent Invoke, then paste:

```
@Backend Developer Agent

Start EPIC-10 Phase 2 coordination now.
System ready at: Docs/mcp-context-server/delivery/mcp-maintenance/devs/
```

---

## Slack/Chat Invoke

```
@Backend Developer Agent Start EPIC-10 Phase 2 coordination.
Dev A, B, C assigned.
Coordination center: Docs/mcp-context-server/delivery/mcp-maintenance/devs/
Standups active 09:00, 12:00, 18:00 UTC starting 2026-03-09.
```

---

## With Codebase Context (Full State)

**For maximum context, use this comprehensive invoke:**

```
@Backend Developer Agent

**EPIC-10 Phase 2 — Full Coordination Activation**

CONTEXT:
- Milestone: M02 (Delivery Sprint 02)
- Phase: EPIC-10 Phase 2 (Tasks 10-06, 10-07, 10-08)
- Duration: 2.5 calendar days (2026-03-09 → 2026-03-11)
- Team: 3 developers (Dev A, B, C) working in parallel
- Parallelization: 6h + 5h + 8h sequential = 19h → 1.5 days parallel = 12h savings

ASSIGNMENTS:
- Dev A (TASK-10-06): Error Handling & OWASP (6h, 20 AC)
- Dev B (TASK-10-07): Performance & Load Testing (5h, 22 AC)
- Dev C (TASK-10-08): Documentation (8h, 4 deliverables)

COORDINATION SYSTEM:
- Hub: Docs/mcp-context-server/delivery/mcp-maintenance/devs/DEVS-COORDINATION-ROUTER.md
- Tasks: dev-[a/b/c]/TASK-10-0X/TASK-10-0X.md
- Feedback: coordinator/feedback/dev-[a/b/c]/ (receive daily standups)
- Dashboard: coordinator/COORDINATOR-DASHBOARD.md (daily tracking)

STANDUP SCHEDULE:
- Daily: 09:00, 12:00, 18:00 UTC
- Format: DEV-[DATE]-STANDUP-[TIME].md using STANDUP-TEMPLATE.md
- Completion: DEV-[DATE]-COMPLETION.md using COMPLETION-REPORT-TEMPLATE.md

MERGE SEQUENCE:
1. Dev B (TASK-10-07) → 2026-03-10 14:00 UTC
2. Dev A (TASK-10-06) → 2026-03-10 15:00 UTC
3. Dev C (TASK-10-08) → 2026-03-12 12:00 UTC

YOUR ROLE:
1. Receive daily standup reports from 3 devs (9 reports/day)
2. Track progress against COORDINATOR-DASHBOARD.md (update real-time)
3. Validate AC completion & test coverage (≥85%)
4. Approve/reject merge readiness
5. Execute git merge when approved
6. Escalate blockers immediately

ACTIVATION:
Ready for Phase 2 kickoff 2026-03-09 09:00 UTC (pending Phase 1 merge 2026-03-08 09:00 UTC)

BEGIN COORDINATION NOW.
```

---

## Minimal Quick Start

**Shortest command:**

```
@Backend Developer Agent Start coordinating EPIC-10 Phase 2.
```

---

## Pre-Execution Checklist

Before invoking agent, verify:

- [ ] Phase 1 merging (due 2026-03-08 09:00 UTC)
- [ ] Folder structure exists: `Docs/mcp-context-server/delivery/mcp-maintenance/devs/`
- [ ] Task files created: `dev-a/TASK-10-06.md`, `dev-b/TASK-10-07.md`, `dev-c/TASK-10-08.md`
- [ ] Feedback channels ready: `coordinator/feedback/dev-[a/b/c]/`
- [ ] Templates ready: `STANDUP-TEMPLATE.md`, `COMPLETION-REPORT-TEMPLATE.md`
- [ ] Dashboard active: `COORDINATOR-DASHBOARD.md`
- [ ] All 3 devs notified with task links

---

## Post-Invocation Actions

Once agent is active (responds with acknowledgment):

1. **Send Day 1 standups** at 09:00 UTC: `DEV-2026-03-09-STANDUP-MORNING.md`
2. **Send Day 1 standups** at 12:00 UTC: `DEV-2026-03-09-STANDUP-MIDDAY.md`
3. **Send Day 1 standups** at 18:00 UTC: `DEV-2026-03-09-STANDUP-EVENING.md`
4. **Repeat** for Days 2-3 until completion
5. **Send completion reports** when each dev finishes task

---

## Troubleshooting

**If agent doesn't respond:**

- Verify Backend Developer mode is active
- Check `.github/copilot-instructions.md` is loaded
- Ensure agent context includes EPIC-10 details
- Restart VS Code if needed

**If coordination dropdown appears:**

- Select: "Backend Developer Agent"
- Choose: Mode "Coordinator"
- Confirm: "EPIC-10 Phase 2"

---

## Example Dialog Flow

**You invoke:**

```
@Backend Developer Agent Start EPIC-10 Phase 2 coordination.
```

**Agent responds:**

```
✅ Backend Developer Agent — Coordinator Mode ACTIVE

Ready to manage EPIC-10 Phase 2 (Dev A, B, C parallel execution).

System components verified:
✅ DEVS-COORDINATION-ROUTER.md (routing center active)
✅ Task files (TASK-10-06, 10-07, 10-08 ready)
✅ Feedback channels (coordinator/feedback/dev-a/b/c set up)
✅ Templates (standup, completion reports available)
✅ Dashboard (real-time tracking active)

Standup schedule confirmed: 09:00, 12:00, 18:00 UTC daily
Merge gates: Dev B (14:00) → Dev A (15:00) → Dev C (12:00 next day)

STATUS: ✅ Ready for Phase 2 kickoff 2026-03-09 09:00 UTC

What's your first request? (e.g., "Start day 1 standups" or "Check blocker")
```

---

## 🚀 READY TO LAUNCH

**Copy any command above and paste into VS Code Command Palette** to start coordinating Phase 2!

```
Press: Ctrl+Shift+I (or Cmd+Shift+I)
Paste: @Backend Developer Agent [command above]
Hit: Enter
```

---

**Agent Initialization Complete** ✅
**Coordination System LIVE** 🎯
**Ready for 2026-03-09 09:00 UTC kickoff** 🚀
