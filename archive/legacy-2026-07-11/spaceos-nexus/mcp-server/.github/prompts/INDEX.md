# Backend Developer Prompts & Agents — Usage Guide

**Version:** 2.0 (Modular Architecture)
**Purpose:** Guide developers to correct prompt & agent for their task
**Architecture:** Agents = General runbook (CORE), Prompts = Milestone/Task-specific context

---

## 📐 Architecture Principle

| Component | Scope | Reusable? | Contains |
|:----------|:------|:----------|:---------|
| **Agents** | General runbook | ✅ YES | HOW: 8-step process, output formats, constraints, placeholders |
| **Prompts** | Milestone/Task context | ❌ NO | WHAT & WHY: EPICs, paths, deadlines, domain knowledge |

**Connection:** Prompt provides context to Agent

- Prompt says: "You are working on M02, your EPICs are 09-14, located at `${PATH}`"
- Agent says: "Use placeholder `${MILESTONE_ROOT}` for paths; follow 8-step runbook"

---

## File Structure

```
.github/
  prompts/
    backend-developer.core.prompt.md    ← Reusable patterns (applies to ALL milestones)
    backend-developer.m02.prompt.md     ← M02 task context (EPICs 09-14)
    backend-developer.m03.prompt.md     ← M03 task context (EPICs 15-18, Multi-Domain & Self-Reflection)
    tech_lead.m01.prompt.md             ← M01 closure context (EPIC-00, TASK-00-0X)
    qa-tester.m01.epic08.prompt.md      ← M01 EPIC-08 QA context (Write Layer testing, 8 test cases)

  agents/
    backend_developer.core.agent.md     ← Backend dev runbook (REUSABLE, placeholder paths)
    tech_lead.coordination.agent.md     ← Tech lead coordination runbook (REUSABLE, placeholder paths)
    qa_tester.agent.md                  ← QA tester runbook (REUSABLE, placeholder paths)
```

---

## 📋 Which File to Use?

### ⚠️ M01 Closure Tasks (EPIC-00)

**Tech leads coordinating M01 closure (TASK-00-01 through TASK-00-05):**

- 📖 Read prompt: `tech_lead.m01.prompt.md`
  - Purpose: M01-specific context (5 closure tasks, EPIC-08/09-14 scope clarity)
  - Contains: Blocker descriptions, stakeholder matrix, file paths, decisions needed

- 🔧 Use agent: `tech_lead.coordination.agent.md`
  - Purpose: Generalized 8-step runbook for coordination
  - Contains: Placeholder paths (`${MILESTONE_ROOT}`), decision template, escalation rules

- ✅ Workflow: Read m01 prompt → Follow coordination agent steps → Generate M01 closue outputs

### For M01 EPIC-08 QA Testing (Write Layer)

**QA engineers validating M01 EPIC-08 Write Layer during active development:**

- 📖 Read prompt: `qa-tester.m01.epic08.prompt.md`
  - Purpose: EPIC-08-specific test strategy (Write Layer: submit artifacts, update FSM state, RBAC validation)
  - Contains: 8 test cases (TC-01 through TC-08), coverage matrix, golden rules, commands

- 🔧 Use agent: `qa_tester.agent.md`
  - Purpose: Generalized 8-step runbook for QA test execution
  - Contains: Test design, execution, reporting, sign-off workflow

- ✅ Workflow: Read epic08 prompt → Follow QA agent steps → Execute test cases (TC-01-TC-08) → Generate QA Sign-Off Memo

### For M02 Development

**Backend developer starting M02 task (EPIC-09 through EPIC-14):**

- 📖 Read prompt: `backend-developer.m02.prompt.md`
  - Purpose: M02-specific context (6 EPICs, 51 tasks, SQLite mandate, dependency chain)
  - Contains: EPIC descriptions, module structure, file paths, constraints

- 🔧 Use agent: `backend_developer.core.agent.md`
  - Purpose: Generalized 8-step runbook for task implementation
  - Contains: Placeholder paths, output formats, test strategy template

- ✅ Workflow: Read m02 prompt → Follow backend dev agent steps → Generate Implementation Summary

### For M03 Development (Current)

**Backend developer starting M03 task (EPIC-15 through EPIC-18):**

- 📖 Read prompt: `backend-developer.m03.prompt.md`
  - Purpose: M03-specific context (4 EPICs, ~35 tasks, multi-domain + legacy modernization)
  - Contains: EPIC descriptions, legacy tool migration pattern, multi-domain guidelines, episodic reflection

- 🔧 Use agent: `backend_developer.core.agent.md`
  - Purpose: Generalized 8-step runbook for task implementation (same as M02)
  - Contains: Placeholder paths, output formats, test strategy template

- ✅ Workflow: Read m03 prompt → Follow backend dev agent steps → Generate Implementation Summary

### For Generic Patterns (Any Milestone)

**Reference patterns, code conventions, golden rules:**

- 📖 Reference: `backend-developer.core.prompt.md`
  - Purpose: Reusable patterns & standards (TypeScript conventions, testing rules, etc.)
  - When: You need guidance on "how should I structure this code?" (applies to all milestones)

---

### For Future Milestones (M04, M05+)

**Creating M04 backend developer prompt:**

1. Copy `backend-developer.m03.prompt.md` → `backend-developer.m04.prompt.md`
2. Update [3. Context] section with M04 EPICs (19-22 or similar), paths, timelines
3. Keep all [1-2], [4-9] sections unchanged (generic content)
4. **Agent:** Use existing `backend_developer.core.agent.md` (no M04 agent needed)

**Creating M03+ tech lead closure prompt (if needed):**

1. Copy `tech_lead.m01.prompt.md` → `tech_lead.m04.prompt.md` (adjust for M03→M04 context)
2. Update blocker statements, EPIC references, paths
3. **Agent:** Use existing `tech_lead.coordination.agent.md` (no new agent needed)

---

### For Future Roles (QA, DevOps, etc.)

**Create new role support:**

1. Copy `backend_developer.core.agent.md` → `qa_tester.core.agent.md` (generalize runbook)
2. Create `qa_tester.core.prompt.md` (patterns, standards for QA)
3. Create `qa_tester.m02.prompt.md` (M02-specific QA context)
4. **Principle:** Agent = general 8-step process, Prompts = role/milestone-specific context

---

## 📚 Architecture Deep Dive

### Agent = Reusable Runbook (No Hardcoded Paths)

**backend_developer.core.agent.md**

- Section: Step 1 → "Open file: `${MILESTONE_ROOT}/epic_XX/tasks/TASK-${TASK_ID}.md`"
- Section: Step 4 → "Update: `${MILESTONE_ROOT}/epic_${EPIC_ID}/`"
- Output templates use `TASK-XX-YY` (not TASK-09-01 hardcoded)
- Reusable by: M02, M03, M04, etc.

**tech_lead.coordination.agent.md**

- Section: Step 1 → "Read: `${MILESTONE_ROOT}/epic_00/tasks/TASK-${TASK_ID}.md`"
- Decision template uses generic placeholders
- Output formats: Generic decision memo, critical path, risk matrix (no epic numbers hardcoded)
- Reusable by: M01 closure, M02 closure (future), M03 closure (future)

### Prompt = Milestone/Task-Specific Context

**backend-developer.m02.prompt.md**

- Section [3]: "M02 Context: 6 EPICs (EPIC-09 through EPIC-14), 51 tasks"
- Section [3]: "Task Paths: `Docs/.../milestone_02/epic_09/tasks/`"
- Section [4]: "M02 Specifics: SQLite-first, RBAC mandatory, bootstrap_agent() pattern"
- Tells agent: "When you see `${MILESTONE_ROOT}`, replace with `.../milestone_02/`"

**tech_lead.m01.prompt.md**

- Section [3]: "M01 Closure Tasks: TASK-00-01 through TASK-00-05 (EPIC-00)"
- Section [3]: "Stakeholder Matrix: TASK-00-01 owner is EPIC-08 Tech Lead, etc."
- Section [4]: "M01 Blockers: [specific ambiguities]"
- Tells agent: "When you see `${MILESTONE_ROOT}`, replace with `.../milestone_01/`"

### How They Connect

**Workflow (M02 Backend Developer):**

1. Dev reads `backend-developer.m02.prompt.md`
   - Learns: M02 has EPICs 09-14, SQLite mandate, specific patterns
   - Learns: Task files are in `.../milestone_02/epic_09/tasks/TASK-09-XX.md`
2. Dev invokes `backend_developer.core.agent.md`
   - Step 1: "Load Task Context from `${MILESTONE_ROOT}/epic_XX/`"
   - Dev substitutes: `${MILESTONE_ROOT}` = `.../milestone_02/`
   - Dev opens: `.../milestone_02/epic_09/tasks/TASK-09-01.md`
3. Dev follows 8-step runbook → produces Implementation Summary

**Workflow (M01 Tech Lead Closure):**

1. Tech lead reads `tech_lead.m01.prompt.md`
   - Learns: M01 has 5 closure tasks, EPIC-00, specific decisions needed
   - Learns: Task files are in `.../milestone_01/epic_00/tasks/TASK-00-0X.md`
2. Tech lead uses `tech_lead.coordination.agent.md`
   - Step 1: "Load Task Context from `${MILESTONE_ROOT}/epic_00/`"
   - Tech lead substitutes: `${MILESTONE_ROOT}` = `.../milestone_01/`
   - Tech lead opens: `.../milestone_01/epic_00/tasks/TASK-00-01.md`
3. Tech lead follows 8-step runbook → produces Decision Summary, Critical Path, Risk Matrix

---

## 🎯 Quick Decision Tree

```
Are you executing a BACKEND DEVELOPMENT TASK (code/tests)?
  YES → Read: backend-developer.m02.prompt.md
        Use: backend_developer.core.agent.md
  NO  → Are you COORDINATING A MILESTONE CLOSURE?
        YES → Read: tech_lead.mXX.prompt.md (where XX = milestone)
              Use: tech_lead.coordination.agent.md
        NO  → Are you making a CODING DECISION (patterns)?
              YES → Reference: backend-developer.core.prompt.md
              NO  → Ask in #dev-help (not handled by this framework)
```

---

## 🎭 Available Agents (Professional Template Standard)

All agents follow a **uniform professional pattern** with: YAML frontmatter | Persona & Expertise (10+ areas) | Approach (8+ principles) | Guidelines (10+ rules) | Common Scenarios (8 examples) | Response Style | Advanced Capabilities (10+ areas) | 8-Step Runbook | Constraints | Permissions | Communication Style.

### Agent Inventory

| Agent | Purpose | Runbook | Use When |
|:------|:--------|:--------|:---------|
| **backend_developer.core.agent.md** | Backend dev task implementation | Load Context → Validate AC → Extract Files → Generate Checklist → Test Strategy → Security Review → Summary → Escalate | Implementing feature tasks (database, integration, schema). Reusable across M02, M03, M04+ |
| **tech_lead.coordination.agent.md** | Milestone closure coordination | Load Context → Gather Stakeholders → Facilitate Decision → Document → Build Architect Package → Respond Feedback → Cascade to Team → Handoff | Coordinating M01/M02/M03+ closure tasks; resolving scope ambiguity; managing dependencies |
| **architect.agent.md** | Strategic architecture & ADRs | Load Context → Identify Question → Gather Alternatives → Document Rationale → Draft ADR → Validate Release Criteria → Risk Assessment → Summary & Sign-Off | Designing major architectural changes; drafting ADRs; validating Epic closure readiness; security/infrastructure risk review |
| **qa_tester.agent.md** | QA test planning & validation | Load Context → Validate AC Completeness → Design Test Strategy → Execute Tests → Analyze Failures → Generate Report → Create Follow-Up Tasks → QA Sign-Off | Validating Tasks against AC/DoD; triaging flaky tests; coverage regression analysis; security testing |
| **tech_lead.agent.md** | Task planning & team guidance | Load Epic Context → Identify Boundaries → Design Sequence → Write Task Files → Identify Design Gates → Validate with Stakeholders → Create Support Materials → Summary | Decomposing EPICs into Tasks; task sequencing; dependency mapping; design review gating |
| **devils_advocate.agent.md** | Critical design review & risk challenge | Load Context → Identify Assumptions → Completeness Review → Security Analysis → Edge Case Analysis → Performance Critique → Generate Alternatives → Critique Memo | Challenging design decisions; threat modeling; edge case identification; exploring alternatives; identifying risks |

### Which Agent for Which Task?

**Scenario: Implementing a backend database feature**
→ **Agent:** `backend_developer.core.agent.md`
→ **Prompt:** `backend-developer.m02.prompt.md` (M02 context)
→ **Workflow:** Load prompt (provides EPIC context, file paths) → Follow agent 8-step runbook → Generate Implementation Summary

**Scenario: Coordinating M01 Epic closure (scope ambiguity)**
→ **Agent:** `tech_lead.coordination.agent.md`
→ **Prompt:** `tech_lead.m01.prompt.md` (M01 blocker context, stakeholders)
→ **Workflow:** Load prompt (identifies decision blockers) → Follow agent 8-step runbook → Generate Decision Summary, Critical Path

**Scenario: Designing FSM state machine for new Epic**
→ **Agent:** `architect.agent.md`
→ **Prompt:** Related ADRs + standards
→ **Workflow:** Load Epic context, challenge assumptions, draft ADR, validate release criteria

**Scenario: Validating Task test coverage before merge**
→ **Agent:** `qa_tester.agent.md`
→ **Prompt:** Task AC/DoD
→ **Workflow:** Load Task AC → Design test strategy → Execute → Generate Test Report

**Scenario: Breaking Epic into implementation Tasks**
→ **Agent:** `tech_lead.agent.md`
→ **Prompt:** Epic goal.md + standards
→ **Workflow:** Identify Task boundaries → Design sequence → Write TASK files → Validate dependencies

**Scenario: Pre-implementation design review (security, edge cases)**
→ **Agent:** `devils_advocate.agent.md`
→ **Prompt:** Task implementation plan
→ **Workflow:** Load plan → Threat model → Edge case analysis → Propose alternatives → Critique memo

---

Contains **M02-M03 specific content**:

| Section | Content | Purpose |
|:--------|:--------|:--------|
| [3] Context | M02-M03 EPIC structure (EPIC-09 to EPIC-14) | Task context |
| [4] Specifics | SQLite-first mandate, database paths, integration | M02 patterns |
| [6] M02 Constraints | Database-first, performance targets (< 50ms) | M02 standards |
| [9] Commands | M02-specific npm scripts, database setup | M02 development |
| Quick Start | EPIC-09 start here, dependency chain | M02 onboarding |

### Core Agent (`backend_developer.core.agent.md`)

Contains **reusable runbook** across all milestones:

| Section | Content | Used By |
|:--------|:--------|:--------|
| Runbook | 8-step execution process | All milestones |
| Constraints | NO_CODE_COMMITS, escalation rules | All milestones |

### Core Agent (`backend_developer.core.agent.md`)

Contains **reusable runbook** across all milestones:

| Section | Content | Used By |
|:--------|:--------|:--------|
| Runbook | 8-step execution process | All milestones (M02, M03, M04+) |
| Constraints | NO_CODE_COMMITS, escalation rules | All milestones |
| Output Formats | Checklist, test strategy, security, summary | All milestones |

---

## ✅ Checklist: Setting Up for M02 Development

- [ ] Read `.github/prompts/backend-developer.m02.prompt.md` (Execution Guide)
- [ ] Bookmark `.github/agents/backend_developer.m02.agent.md` (Implementation Support)
- [ ] Review `.github/prompts/backend-developer.core.prompt.md` (Golden Rules + Patterns)
- [ ] Create first TASK: Pick from `Docs/.../milestone_02/epic_09/tasks/TASK-09-01.md`
- [ ] Follow M02 Workflow: Plan → Code → Test → Summary → Review → Merge

---

## 🔄 Migration Path for Future Milestones

### When Upgrading from M02 to M03

**Step 1: Create M03 Prompt Only (NO NEW AGENT)**

```bash
# Create M03 prompt (copy from M02, update context)
cp .github/prompts/backend-developer.m02.prompt.md \
   .github/prompts/backend-developer.m03.prompt.md

# Edit [3. Context] section with M03 EPIC info (EPIC-15-18 instead of EPIC-09-14)
# Edit file paths to point to milestone_03/ (instead of milestone_02/)
# DO NOT create backend_developer.m03.agent.md — agents are reusable!
```

**Step 2: Agents Remain Core (Reusable)**

- Use existing `backend_developer.core.agent.md` for M03 tasks (no M03-specific agent copy needed)
- Agent runbook uses placeholder paths (`${MILESTONE_ROOT}`) → substitutable by any prompt
- All milestones (M02, M03, M04, ...) use the SAME core agent

**Step 3: Update This INDEX**

- Add M03 entries to file status table
- Update "Current" milestone reference
- Link new M03 prompts in decision tree

---

## Principle: Agents Are Templates, Prompts Are Context

## 📞 Questions & Support

**Question:** Which prompt should I use for an M02 task?
**Answer:** `backend-developer.m02.prompt.md` (section [3] has M02-M03 context)

**Question:** How do I get a checklist for TASK-09-02?
**Answer:** Invoke agent: `backend_developer.m02.agent.md` with taskId="TASK-09-02"

**Question:** Can I use M02 prompt for M03 tasks?
**Answer:** No — create `backend-developer.m03.prompt.md` with M03 EPIC paths

**Question:** What if I find a bug in the core prompt?
**Answer:** Fix in `backend-developer.core.prompt.md`, then propagate to m02, m03, etc.

**Question:** How do I add a new pattern (Refusal, Chain of Thought, etc.)?
**Answer:** Add to core prompt, then include in all milestone-specific versions

---

## 📊 File Status

| File | Version | Scope | Type | Status |
|:-----|:--------|:------|:-----|:-------|
| `backend-developer.core.prompt.md` | 1.0 | Patterns | Reusable | ✅ Stable |
| `backend-developer.m02.prompt.md` | 2.0 | M02 Tasks (EPIC-09-14) | Milestone-specific | ✅ Closed |
| `backend-developer.m03.prompt.md` | 1.0 | M03 Tasks (EPIC-15-18) | Milestone-specific | ✅ Active |
| `qa-tester.m01.epic08.prompt.md` | 1.0 | M01 EPIC-08 QA (Write Layer) | Epic-specific | ✅ Active |
| `backend_developer.core.agent.md` | 1.0 | All Tasks | Reusable (placeholder paths) | ✅ Stable |
| `tech_lead.m01.prompt.md` | 1.0 | M01 Closure | Milestone-specific | ✅ Active |
| `tech_lead.coordination.agent.md` | 1.0 | All Closures | Reusable (placeholder paths) | ✅ Stable |
| `qa_tester.agent.md` | 1.0 | All QA Tasks | Reusable (placeholder paths) | ✅ Stable |

---

## Principle: Agents Are Templates, Prompts Are Context

- Agent 8-step runbook uses `${MILESTONE_ROOT}` placeholder
- Each prompt tells agent: "When you see `${MILESTONE_ROOT}`, use `.../milestone_XX/`"
- Result: **One agent, infinite contexts** (M02, M03, M04, ... all use same agent)

This design solves:

1. **No duplication**: Create agent once, reuse across all milestones
2. **Single source of truth**: Fix bugs in agent once, all milestones benefit
3. **Scalability**: Adding M03 means creating ONE prompt, not entire agent + prompt
4. **Flexibility**: Teams can use different agents (backend_developer vs qa_tester) with same milestone prompt

---

**TL;DR:**

- **Backend dev M02:** Read `backend-developer.m02.prompt.md` + use `backend_developer.core.agent.md`
- **Tech lead M01 closure:** Read `tech_lead.m01.prompt.md` + use `tech_lead.coordination.agent.md`
- **Patterns (any role):** Reference `backend-developer.core.prompt.md`
- **Future milestone:** Copy `*.mXX.prompt.md`, use same agent (no agent copies needed)

Let's build! 🚀
