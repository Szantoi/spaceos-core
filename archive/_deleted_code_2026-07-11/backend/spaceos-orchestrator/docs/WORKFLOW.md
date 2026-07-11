# SpaceOS.Orchestrator — Agent Workflow

> This document defines how Claude Code sub-agents execute tasks.  
> Every agent session must start by reading this file + CLAUDE_ORCH.md.

---

## Agent Roles

| Agent | Trigger | Reads | Writes |
|-------|---------|-------|--------|
| **PLAN** | New epic created | EPIC.md, BACKLOG.md, Codebase_Status.md | task/*.md files |
| **CODE** | Task status → `IN_DEV` | task .md, CLAUDE_ORCH.md, existing source | Source files, test files |
| **TEST** | CODE agent completes | task .md, new source files | Test files, updates task status |
| **REVIEW** | TEST agent completes | REVIEW_CHECKLIST_ORCH.md, all changed files | REVIEW_REPORT.md, fixes source |

---

## Phase 1 — PLAN Agent

```
You are the PLAN agent for SpaceOS.Orchestrator.
Read: BACKLOG.md, EPIC.md for [EPIC_ID], Codebase_Status.md

Your job:
1. Break the epic into layer-granularity tasks (one task = one module concern)
2. For each task create a task file in epics/[EPIC_ID]/tasks/
3. Each task file must follow the Task Template in WORKFLOW.md
4. Update BACKLOG.md epic status to IN_DEV
5. Output: list of created task files, nothing else
```

---

## Phase 2 — CODE Agent

```
You are the CODE agent for SpaceOS.Orchestrator.
Read first: CLAUDE_ORCH.md + task file [TASK_FILE]

Your job:
1. Implement exactly what the task Acceptance Criteria require
2. Follow every rule in CLAUDE_ORCH.md — no exceptions
3. Every new service/route → companion test file (mandatory)
4. No TODO/FIXME in committed code
5. Output: summary table (File | Change | Reason)
```

---

## Phase 3 — TEST Agent

```
You are the TEST agent for SpaceOS.Orchestrator.
Read: task file [TASK_FILE], all files changed by CODE agent

Your job:
1. Verify all test requirements in the task file are covered
2. Run: npm test — must be 0 failed
3. If missing coverage: write the missing tests
4. Update task status → CODE_REVIEW
5. Output: test coverage table (Test | Type | Result)
```

---

## Phase 4 — REVIEW Agent

```
You are the REVIEW agent for SpaceOS.Orchestrator.
Read first: REVIEW_CHECKLIST_ORCH.md, then every file changed in this task

Your job:
1. Check every item in REVIEW_CHECKLIST_ORCH.md against the changed files
2. For each violation: FIX IT in place, then log it in REVIEW_REPORT.md
3. After fixes: run npm run build + npm test — must be green
4. Write REVIEW_REPORT.md
5. Update task status → CLOSED_DONE (if clean) or → REVIEW_FAILED (if unfixable)
```

---

## Task File Template

```markdown
# Task: [TASK_ID] — [Title]
**Epic:** [EPIC_ID]  
**Agent:** CODE  
**Status:** BACKLOG_READY  
**Module:** [config | llm | interpreter | proxy | middleware | routes | types]

---

## Context

[Which files the agent must read before starting]  
[What already exists that this task builds on]

## Acceptance Criteria

- [ ] ...

## Test Requirements

| Test | Type | Description |
|------|------|-------------|
| ... | Unit | ... |
| ... | Integration | ... |

## CLAUDE_ORCH.md Gates (auto-checked by REVIEW agent)

- [ ] ILlmProvider never bypassed — all LLM calls through the interface
- [ ] No Kernel URL hardcoded — always from env.KERNEL_BASE_URL
- [ ] No API keys in source code — always from env.*
- [ ] ConfigureAwait equivalent: no unhandled promise rejections
- [ ] Zod validation on every route input
- [ ] Error responses always use the centralized errorHandler
- [ ] No TODO/FIXME in committed code

## Definition of Done

- [ ] All AC checked
- [ ] npm run build → 0 errors
- [ ] npm test → 0 failed
- [ ] REVIEW_REPORT.md → CLOSED_DONE
```

---

## REVIEW_REPORT.md Template

```markdown
# Review Report — [TASK_ID]
**Date:** YYYY-MM-DD  
**Agent:** REVIEW  
**Final status:** CLOSED_DONE | REVIEW_FAILED

## Violations Found & Fixed

| # | File | Violation | Fix Applied |
|---|------|-----------|-------------|

## Unfixable Violations

| # | File | Issue | Why unfixable |
|---|------|-------|---------------|

## Build & Test Result
- Build: ✅ 0 TypeScript errors
- Tests: ✅ [N] passing, 0 failed
```

---

## Executing in Claude Code

Minden session elején illesszük be a Master Prompt tartalmát, majd:

```
[PLAN session]
Read WORKFLOW.md and BACKLOG.md.
Act as PLAN agent. Target epic: E11.
Create task files. Update BACKLOG.md status.

[CODE session]
Read WORKFLOW.md, CLAUDE_ORCH.md.
Act as CODE agent. Target task: epics/E11_ORCH_BOOTSTRAP/tasks/T1_project_setup.md
Implement. Output summary table only.

[TEST session]
Read WORKFLOW.md.
Act as TEST agent. Target task: epics/E11_ORCH_BOOTSTRAP/tasks/T1_project_setup.md
Run npm test. Fill missing coverage.

[REVIEW session]
Read WORKFLOW.md, REVIEW_CHECKLIST_ORCH.md.
Act as REVIEW agent. Target task: epics/E11_ORCH_BOOTSTRAP/tasks/T1_project_setup.md
Fix violations. Write REVIEW_REPORT.md.
```
