# Agent Creation Methodology

**Version:** 1.0
**Date:** 2026-03-05
**Standard:** Professional Template Pattern (typescript-mcp-expert reference)

---

## 📋 Overview

This document describes the **systematic methodology** for creating high-quality, reusable AI agents for the JoineryTech MCP Server. The approach ensures consistency, professionalism, and long-term maintainability across all agents.

**Goal:** Every agent should be production-grade, with clear expertise areas, structured runbooks, and actionable guidance for users.

---

## 🏗️ Architecture Principles

### 1. One Agent, Many Contexts (Placeholder Paths)

- Agents use **placeholder paths** like `${MILESTONE_ROOT}`, `${TASK_ID}`, `${EPIC_ID}`
- Prompts provide **concrete context**: "Substitute `${MILESTONE_ROOT}` = `.../milestone_02/`"
- Result: **Single agent serves M02, M03, M04** without duplication

**Example:**
```markdown
# In Agent Runbook (generic):
"Step 1: Load Task from `${MILESTONE_ROOT}/epic_${EPIC_ID}/tasks/TASK-${TASK_ID}.md`"

# In Prompt (M02-specific):
"For M02 tasks, substitute: `${MILESTONE_ROOT}` = `.../milestone_02/`"

# Team uses:
Developer reads Prompt → Agent tells them → Developer substitutes → Opens real file
```

### 2. Agents = Template, Prompts = Context

| Component | Scope | Content | Reusable? |
|:----------|:------|:--------|:----------|
| **Agent** | HOW | 8-step runbook, output formats, constraints | ✅ YES (all milestones) |
| **Prompt** | WHAT & WHY | EPICs, paths, decisions, domain knowledge | ❌ NO (milestone-specific) |

---

## 📐 Professional Template Structure

Every agent follows this **11-section structure** (adapted from `typescript-mcp-expert.agent.md`):

### Section 1: YAML Frontmatter
```yaml
---
description: "Role — Specific responsibilities and scope"
name: "Agent Name"
model: "claude"
tools: ['vscode', 'read', 'search', 'filesystem/*', ...]
---
```

**Content:** Description, name, model, tools list.

### Section 2: Persona & Title
```markdown
# Agent Name

You are a **expert-level title** responsible for **core responsibilities**. Your role is to **primary function**. You do not **what you avoid**.
```

**Purpose:** Establish credibility, set tone, clarify scope boundaries.

### Section 3: Your Expertise
- **Count:** 10-12 bullet points (domain mastery areas)
- **Format:** Hyphen-bullet, brief description
- **Examples:**
  - Architecture Patterns: Micro-kernel, middleware, CQRS, FSM
  - MCP Protocol Architecture: Routing, tool exposure, RBAC filtering
  - Database Architecture: Schema design, migrations, consistency
  - Performance & Scalability: Load analysis, bottleneck identification

**Purpose:** Build trust; show deep knowledge in specific areas.

### Section 4: Your Approach
- **Count:** 8-9 bullet points (guiding principles)
- **Format:** Numbered list with imperative action + reasoning
- **Example:**
  1. **Start with Context**: Load project goal.md, state.md, architecture baseline before proposing changes
  2. **Challenge Assumptions**: Question whether current design serves future scenarios

**Purpose:** Establish work philosophy; explain how decisions are made.

### Section 5: Guidelines
- **Count:** 10-12 bullet points (concrete rules & checkpoints)
- **Format:** Numbered list with action + validation criteria
- **Example:**
  1. **Load Complete Context**: Read goal.md, state.md, existing ADRs, and related TASK files before drafting
  2. **Validate Release Criteria**: Confirm Implementation Reports, QA sign-offs, Tech Lead approval exist

**Purpose:** Provide operational rules; ensure consistent behavior.

### Section 6: Common Scenarios
- **Count:** 8 typical use cases
- **Format:** Numbered "Scenario: [description]" with bullet steps + output
- **Example:**
  ```markdown
  1. **Scenario: Issue labeled `design` requesting architectural alternatives**
     - Load issue context and related EPIC goal.md
     - Draft 2+ alternatives with performance/security/complexity trade-offs
     - Recommend preferred approach with justification
     - Output: ADR draft at `database/joinerytech-flow/discovery/<topic>/02_ideate/adrs/<slug>-draft.md`
  ```

**Purpose:** Show real-world use cases; help users understand when/how to invoke agent.

### Section 7: Response Style
- **Format:** Bullet points describing communication approach
- **Example:**
  - Technical Depth: Use architecture terminology with precision
  - Clarity for Stakeholders: Explain trade-offs in business terms
  - Language: English for technical, Hungarian for context
  - Format: Markdown tables, Mermaid diagrams, bullet-point checklists

**Purpose:** Set communication tone; guide output quality.

### Section 8: Advanced Capabilities
- **Count:** 10+ areas of deep expertise
- **Format:** Numbered list with brief description
- **Example:**
  1. **ADR Archaeology**: Review superseded ADRs to avoid reinventing decisions
  2. **Cross-Project Patterns**: Identify best practices from JoineryTech portfolio
  3. **Technical Debt Assessment**: Score existing architecture for tech debt

**Purpose:** Show depth; hint at advanced use cases.

### Section 9: 8-Step Runbook

**Core execution process** broken into 8 atomic steps. Each step has:
- **Step N: [Title]**
  - Bullet preconditions or actions
  - **Output**: What artifact/result this step produces

**Example Structure:**
```markdown
### Step 1: Load Complete Project Context
- Read: `database/joinerytech-flow/discovery/*/goal.md` (project vision)
- Read: `program-state.md`, `state.md` (current status)
- Understand: Timeline, team capacity, dependencies on other EPICs
- **Output**: Context document with goals and constraints

### Step 2: Identify Architectural Question or Risk
- Clarify: Is this ADR request, release sign-off, or risk review?
- Load related EPIC goal.md, TASK files, or issue context
- **Output**: Problem statement memo

[... Steps 3-8 follow similar pattern ...]
```

**Purpose:** Provide step-by-step execution guide; ensure repeatable process.

### Section 10: Constraints

**Format:** Bullet list with uppercase constraint names and description.

**Example:**
```markdown
- **NO_CODE_COMMITS**: You do not write production code or commit implementation. You design, review, and approve.
- **MUST_VALIDATE_TEMPLATES**: All ADRs must follow Markdown ADR template.
- **ESCALATE_IMMEDIATELY_ON**: Security risks | Infrastructure changes | Data model conflicts
```

**Purpose:** Set hard boundaries; prevent scope creep.

### Section 11: Permissions & Communication

**Permissions Section:**
```markdown
**Read:**
- `database/joinerytech-flow/discovery/*/goal.md`
- `database/standards/` (all standards)

**Write:**
- `database/joinerytech-flow/discovery/*/02_ideate/adrs/` (ADRs)

**Create PR / Issues:**
- Yes (for ADR review PRs, follow-up tasks)
```

**Communication Style Section:**
- Tone guidance (precision, clarity, respect)
- Format preferences (tables, diagrams, code)
- Language policy (English for technical, Hungarian for explanations)

---

## 🔄 Creation Process (8 Steps)

### Step 1: Define Agent Role & Purpose

**Input:** What problem does this agent solve?

**Questions to answer:**
- What is the agent's primary responsibility? (e.g., "Backend dev task implementation")
- What is NOT this agent's responsibility? (e.g., "We don't write code; don't commit changes")
- Who will use this agent? (e.g., "Backend developers implementing M02 tasks")
- What is the expected workflow? (e.g., "Load prompt → Follow runbook → Produce implementation summary")

**Output:** 2-3 sentence role description + audience definition.

---

### Step 2: Inventory Expertise Areas

**Process:**

1. **Brainstorm:** List all skills this role needs (10-15 areas)
2. **Categorize:** Group into 10-12 coherent expertise blocks
3. **Validate:** Does each area support the core mission?

**Example (for Architect Agent):**

Raw skills: ADR writing, MCP design, security, performance, scalability, databases, team communication, API design, migrations, threat modeling, FSM, RBAC, testing strategies, deployment, resilience...

→ Organized into 10 areas:
- Architecture Patterns
- Design Decision Records (ADRs)
- MCP Protocol Architecture
- Security Architecture
- Database Architecture
- Performance & Scalability
- System Integration
- Cross-Cutting Concerns
- Release Engineering
- Stakeholder Communication

**Output:** 10-12 expertise bullet points (brief descriptions).

---

### Step 3: Define Work Approach (8-9 Principles)

**Process:**

1. **Ask:** How should this agent think and make decisions?
2. **Extract:** From standards, best practices, or project principles
3. **Articulate:** As imperative statements (e.g., "Start with Context", "Challenge Assumptions")

**Example (for Backend Developer):**

1. Understand Task First
2. Assume Nothing
3. File-First Thinking
4. Test-Driven Mindset
5. Security Lens
6. Clarity Over Speed
7. No Code Commits
8. Escalate Early
9. Respect Constraints

**Output:** 8-9 numbered approach principles.

---

### Step 4: Create Guidelines (10-12 Operational Rules)

**Process:**

1. **Ask:** What concrete rules should this agent follow?
2. **Source:** From project standards, lessons learned, or domain best practices
3. **Format:** Numbered list with action + validation

**Example (for Backend Developer):**

1. Load Complete Task Context
2. Validate Testability
3. Design Test Matrix
4. Test Coverage Targets
5. Flaky Test Classification
6. Regression Alert Threshold
7. Security Checklist
8. Performance Baseline
9. Test Report Format
10. No Code Commits

**Output:** 10-12 numbered guideline statements.

---

### Step 5: Draft 8 Common Scenarios

**Process:**

1. **List:** 8 typical use cases (what will people ask this agent to do?)
2. **Script:** For each, write step-by-step process
3. **Output:** What artifact does each scenario produce?

**Example (for QA Tester):**

1. Scenario: Task is ready for QA; run acceptance criteria validation
2. Scenario: PR shows test coverage drop; investigate and alert
3. Scenario: CI shows flaky test failure; triage and classify
4. Scenario: New feature requires security testing
5. Scenario: Performance regression detected; investigate and report
6. Scenario: Acceptance Criteria missing; request clarification
7. Scenario: Epic marked done-candidate; validate QA readiness
8. Scenario: Post-deployment issue; analyze and create regression test

**Output:** 8 scenarios with bulleted steps + expected outputs.

---

### Step 6: Design 8-Step Runbook

**Process:**

1. **Decompose:** What are the atomic execution steps?
2. **Sequence:** What order makes sense?
3. **Make it testable:** Each step must produce a clear output

**Template for each step:**
```markdown
### Step N: [Title]
- Action 1
- Action 2
- Validation: [how to know step is complete]
- **Output**: [what artifact/result this produces]
```

**Principle:** Every step must be independently verifiable.

**Output:** 8 numbered steps, each with preconditions, actions, and outputs.

---

### Step 7: Define Constraints & Escalation

**Process:**

1. **Ask:** What should this agent NEVER do?
2. **Ask:** What situations require escalation?
3. **Format:** Uppercase constraint names + plain English description

**Escalation Rules Example:**
```markdown
- **ESCALATE_IMMEDIATELY_ON**: Security risks | Infrastructure changes | Data model conflicts
- **ESCALATE_ON_ARCHITECTURAL_CHANGE**: If Task requires arch change, escalate to Architect
- **NO_CODE_COMMITS**: You design; developers implement
```

**Output:** 4-6 hard constraints + 2-3 escalation triggers.

---

### Step 8: Set Permissions & Communication Standards

**Output Permissions Example:**
```markdown
**Read:**
- Task files, Acceptance Criteria, Definition of Done
- EPIC goal.md and state.md
- CI/CD results (GitHub Actions)

**Write:**
- `database/joinerytech-flow/*/milestones/*/EPIC-*/implementation-summary/`

**Create PR / Issues:**
- Yes (for test failure triage, flaky test tracking)
```

**Communication Style Example:**
```markdown
- Technical Precision: Use testing terminology correctly
- Clarity for Developers: Plain-language test failure descriptions
- Language: English for technical, Hungarian for explanations
- Format: Structured reports (Summary | Coverage | Failures | Recommendations)
- Tone: Collaborative problem-solver; help devs understand failures
```

**Output:** Defined read/write/PR permissions + communication style guide.

---

## 📝 Implementation Checklist

When creating a new agent:

- [ ] **Step 1:** Role & purpose defined (2-3 sentences)
- [ ] **Step 2:** 10-12 expertise areas listed
- [ ] **Step 3:** 8-9 approach principles documented
- [ ] **Step 4:** 10-12 operational guidelines defined
- [ ] **Step 5:** 8 common scenarios described
- [ ] **Step 6:** 8-step runbook designed
- [ ] **Step 7:** Constraints & escalations defined
- [ ] **Step 8:** Permissions & communication standards set
- [ ] **YAML Frontmatter:** description, name, model, tools
- [ ] **Response Style:** Communication tone + format
- [ ] **Advanced Capabilities:** 10+ deep expertise areas
- [ ] **File size:** 12-15 KB (professional depth indicator)
- [ ] **Placeholder paths:** Uses `${MILESTONE_ROOT}`, `${TASK_ID}` (not hardcoded)
- [ ] **Markdown formatting:** Consistent, professional structure
- [ ] **No milestone-specific content:** Agent is reusable across M02, M03, M04+

---

## 🎯 Quality Metrics

An agent is **production-ready** when:

| Metric | Target | Validation |
|:-------|:-------|:-----------|
| **File size** | 12-15 KB | Indicates sufficient depth (not overly verbose) |
| **Expertise areas** | 10-12 | Shows domain mastery |
| **Approach principles** | 8-9 | Clear work philosophy |
| **Guidelines** | 10-12 | Operational clarity |
| **Scenarios** | 8 | Real-world coverage |
| **Runbook steps** | 8 | Atomic & reproducible |
| **Constraint clarity** | 4-6 | Hard boundaries set |
| **Placeholder paths** | Used consistently | Reusable across milestones |
| **YAML frontmatter** | Complete | description, name, model, tools |
| **Tone consistency** | Professional | Confident but collaborative |

---

## 🚀 Agents Created Using This Methodology

| Agent | Created | Purpose | Size |
|:------|:--------|:--------|:-----|
| `architect.agent.md` | 2026-03-05 | Strategic architecture planning, ADR drafting | 13 KB |
| `qa_tester.agent.md` | 2026-03-05 | Test planning, flaky test triage, QA sign-offs | 12 KB |
| `tech_lead.agent.md` | 2026-03-05 | Task-level implementation planning, team coordination | 13 KB |
| `devils_advocate.agent.md` | 2026-03-05 | Critical design review, risk identification | 13 KB |
| `backend_developer.core.agent.md` | Earlier | Backend dev task implementation (M02+) | 14 KB |
| `tech_lead.coordination.agent.md` | Earlier | Milestone closure coordination (M01+) | 15 KB |

---

## 💡 Key Principles

1. **Reusability First:** Use placeholder paths; avoid hardcoded milestone references
2. **Professional Depth:** 12-15 KB per agent; show expertise
3. **Structural Consistency:** All agents follow 11-section template
4. **Operational Clarity:** 8-step runbooks make execution repeatable
5. **Constraint Respect:** Hard boundaries prevent scope creep
6. **Scenario Coverage:** 8 real-world use cases validate relevance
7. **Permissions Transparency:** Clear read/write/PR rules
8. **Communication Standards:** Tone, format, language established upfront

---

## 🔮 Future Extensions

This methodology supports:

- **New Roles:** DevOps agent, Security agent, Compliance agent (copy template, customize)
- **New Milestones:** M03, M04+ (create new prompts, reuse agents)
- **Cross-Role Workflows:** Multiple agents coordinating on same Epic
- **Escalation Chains:** Agents escalate to Architect or Principal Engineer as needed
- **Template Updates:** Improve template once, propagate to all agents

---

## 📚 References

- **Professional Template Reference:** `.github/agents/typescript-mcp-expert.agent.md`
- **Agent Connection Model:** `.github/prompts/INDEX.md` (Agents ↔ Prompts)
- **Standards:** `database/standards/` (project-wide guidelines)

---

**End of Methodology Document**

This document should be updated if the professional template pattern evolves or new best practices emerge.
