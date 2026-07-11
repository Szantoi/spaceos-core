---
id: workflow-knowledge_steward_calibration
title: "Knowledge Steward Calibration & Skill Integration Workflow"
description: "Integrate Architect-approved skills, patterns, and best practices into the knowledge base after Epic closure. Trigger when the Architect Sign-off contains Calibration Recommendations."
type: workflow
scope: global
category: knowledge-management
last_updated: 2026-03-01
---

## Mission: Integrating Architect-Approved Knowledge into the Knowledge Base

**Role**: Knowledge Steward
**Goal**: Integrate skills, patterns, and best practices approved by the Architect at Epic closure into the global or project-level knowledge base, without duplication.

---

## Cognitive Setup (Prompt Engineering)

Before starting calibration, activate the appropriate **Prompt Engineering Patterns**:

### Core patterns:

1. **Persona Pattern**: You are the Knowledge Curator — you organize and preserve fragments of knowledge.
2. **Fact Check Pattern**: Strict verification for every new knowledge element — no redundancy, no contradiction.
3. **Alternative Approach Pattern**: If conflicting or duplicate knowledge is found, list the resolution options.

### Task-specific patterns:

- **Cognitive Verifier Pattern**: If unclear whether knowledge is global or project-level, ask.
- **Reflection Pattern**: Why is this knowledge element worth sharing? What are its limits?
- **N-shot Prompting**: When creating a new skill, use examples from existing skills.
- **Template Pattern**: Use meta-templates when creating new skills/workflows.

### Required skill to load:

- **context_structure_management.knowledge.md** — Knowledge of the src/agent-system/database/roles structure
- **orchestrator_calibration.knowledge.md** — Calibration protocol

---

## Prerequisites / Input

Before starting, verify:

- [ ] Architect Sign-off document is accessible: `docs/joinerytech-flow/epics/{EPIC_ID}/architect_signoff.md`
- [ ] Sign-off contains the **"Calibration Recommendations"** section
- [ ] Access to `src/agent-system/database/standards/core/knowledge_map.md`
- [ ] `context_structure_management.knowledge.md` loaded

---

## Calibration Workflow

### 1. Architect Sign-off Analysis (Fact Check Pattern)

**Goal**: Identify and classify approved knowledge elements.

#### 1.1 Read the Sign-off

**Acting**: Load the Architect Sign-off document:

```
docs/joinerytech-flow/epics/{EPIC_ID}/architect_signoff.md
```

**Find the following sections**:
- **Calibration Recommendations** (or similar: Lessons Learned, Knowledge Transfer, Best Practices)
- **New Patterns / Skills**
- **Tech Stack Update**
- **Architectural Decisions (ADRs)**

#### 1.2 Classify Knowledge Elements (Cognitive Verifier Pattern)

**For each identified knowledge element**, fill out this checklist:

**Knowledge Element: {Name/Description}**

**Scope determination:**
- [ ] **Global knowledge base**: Useful for all projects? (e.g., general best practice, Clean Architecture pattern)
- [ ] **Project-specific**: Only applies to this project? (e.g., project-specific business logic pattern)

**Type determination:**
- [ ] **New Skill**: Independent, reusable technical capability (e.g., "JWT Authentication Skill")
- [ ] **Existing Skill Update**: Supplements/improves an existing skill (e.g., extend a backend knowledge file)
- [ ] **Workflow Pattern**: Workflow best practice (e.g., "API Versioning Workflow")
- [ ] **Code Pattern (N-shot)**: Code example (e.g., Repository Pattern implementation)
- [ ] **Constraint/Standard**: Rule or restriction (e.g., "Always use async/await")

**Redundancy check:**
- [ ] Does a similar element already exist in the knowledge base?
- [ ] If yes, where? (File path)
- [ ] Does it contradict any existing rule?

**Result**: List of classified knowledge elements.

---

### 2. Redundancy and Conflict Check (Fact Check Pattern)

**Goal**: Ensure the new knowledge is not duplicated and does not conflict.

#### 2.1 Global Search

**Acting**: Search in existing documentation:

```powershell
# Search for keywords in src/agent-system/database/roles folder
$keyword = "JWT" # example
Get-ChildItem "src/agent-system/database/roles" -Recurse -Filter "*.md" | Select-String -Pattern $keyword -Context 2,2
```

**Observation**: Is this knowledge already documented?

**Decision Tree**:

```
Found?
├── NO match → Step 3: Create New Knowledge Element
├── PARTIAL match (incomplete) → Step 4: Update Existing
└── FULL match → REDUNDANT, do not integrate (but notify Architect)
```

#### 2.2 Conflict Detection

**Fact Check Pattern**: Strict verification!

**Questions**:
1. Does the new knowledge contradict `src/agent-system/database/standards/core/constraints.md`?
2. Does it contradict any existing skill?
3. Is there an alternative approach already in use?

**If conflict exists** — **Alternative Approach Pattern**:
- List both approaches
- Analyze the differences
- Suggest a decision (which to keep, or how to harmonize)
- **STOP**: Consult the Architect before proceeding!

---

### 3. Create New Knowledge Element (Template Pattern + N-shot)

**Goal**: Create a new skill, workflow, or pattern document.

#### 3.1 Create Global Skill

**If**: The knowledge element belongs to the global knowledge base.

**Template Pattern**: Use the appropriate meta-template:

**Creating a new skill:**

```markdown
File: src/agent-system/database/knowledge/{domain}/{knowledge_name}.knowledge.md

Base on templates/skill_structure.template.md:
1. Frontmatter (id, title, type: skill, scope: global)
2. "When to load?" section
3. Architecture and Rules
4. Code Patterns (N-shot Patterns) — Use Architect-approved example code here!
5. Common Errors and Solutions
6. Related Skills
```

**N-shot Prompting**: Look at a similar skill as an example:
- Backend: `src/agent-system/database/knowledge/engineering/backend_dotnet.knowledge.md`
- Frontend: `src/agent-system/database/knowledge/engineering/frontend_react.knowledge.md`

**Acting**: Create the new skill file based on the template.

#### 3.2 Create Workflow Pattern

**If**: The knowledge element is a workflow best practice.

```markdown
File: src/agent-system/database/roles/{target_agent}/{pattern_name}.workflow.md

Base on templates/workflow_structure.template.md:
1. Frontmatter (id, title, type: workflow)
2. Mission and Goal
3. Cognitive Setup (Prompt Engineering patterns)
4. Required Actions (steps)
5. DoD (Definition of Done)
```

#### 3.3 Add Code Pattern (N-shot) to Existing Skill

**If**: The knowledge element is a code pattern that fits an existing skill.

**Acting**: Update the existing skill's "Code Patterns" section:

```markdown
## Code Patterns (N-shot Patterns)

### {New Pattern Name}

**Context**: {When to use?}

**Example code**: (Architect-approved code)

```
// Approved pattern from EPIC-{ID}
public class ExampleClass
{
    // ...
}
```

**Advantages**:
- Advantage 1
- Advantage 2

**Warnings**:
- Do not use when...
```

---

### 4. Update Existing Knowledge Element (Reflection Pattern)

**Goal**: Supplement or improve an existing skill/workflow.

#### 4.1 Incremental Update

**Critical guideline**: Never overwrite a complete file! Targeted, line-level updates only.

**Reflection Pattern**: Before modifying, explain:
1. **Why is** the new knowledge element better than the existing one?
2. **What are the limits** of the new approach?
3. **Is it compatible** with existing code?

**Acting**: Use `replace_string_in_file` or `multi_replace_string_in_file` tool:

```markdown
Example update:

File: src/agent-system/database/knowledge/engineering/backend_dotnet.knowledge.md

Old section:
## Code Patterns
### Repository Pattern
(old implementation)

New section:
## Code Patterns
### Repository Pattern (Updated based on EPIC-{ID})
(updated implementation with Architect approval)
```

#### 4.2 Document the Change

**For every update**:
- Update the frontmatter `last_updated` field
- Add a note: `<!-- Updated based on Architect Sign-off EPIC-{ID} -->`

---

### 5. Record Project-Level Knowledge (Context Slicing)

**Goal**: If the knowledge is NOT general, it should go into the project-level skill map.

#### 5.1 Project Skill Map

**If**: The knowledge element is project-specific.

**Acting**: Create/update the project skill map:

```markdown
File: docs/joinerytech-flow/knowledge/project_skills.md

---
id: project-skills-joinerytech-flow
title: "JoineryTech.Flow Project-Specific Skills"
type: project_knowledge
scope: project
last_updated: 2026-03-01
---

## Project-Specific Skills

These skills are NOT general — they only apply to this project.

### {Skill Name} (From EPIC-{ID})

**Context**: {When/why we use this in this project?}

**Implementation**: {Code/description}

**Why NOT global?**: {Rationale, e.g. specific business rule}
```

---

### 6. Knowledge Map Update (Fact Check Pattern)

**Goal**: Keep `knowledge_map.md` up to date with all new/modified files.

#### 6.1 Add New Files

**Acting**: Update `src/agent-system/database/standards/core/knowledge_map.md`:

**If a new Skill was created**:

```markdown
## Skill Catalog

| Role Name | Skill Type | Physical Path (Load This) |
| :---- | :---- | :---- |
| **{Agent}** | {skill_type} | `src/agent-system/database/knowledge/{domain}/{knowledge_name}.knowledge.md` |
```

**If a new Workflow was created**:

```markdown
## Workflows

| Role Name | Type | Physical Path |
| ------ | ------ | ------ |
| **{Agent}** | {workflow_type} | `src/agent-system/database/roles/{agent}/{workflow_name}.workflow.md` |
```

#### 6.2 Update Last Updated

**Do not forget!**
- Update the `knowledge_map.md` frontmatter `last_updated` field
- Update the `version` number (e.g., 1.1 → 1.2)

---

### 7. Structural Validation (Fact Check Pattern)

**Goal**: Ensure that new/modified files comply with standards.

#### 7.1 Frontmatter Validation

**Fact Check Pattern**: Check every new/modified file:

- [ ] YAML frontmatter present?
- [ ] `id:` field format correct? (`{type}-{name}`)
- [ ] `type:` field valid? (skill, workflow, standard, etc.)
- [ ] `scope:` correct? (global or project)
- [ ] `last_updated:` date current (YYYY-MM-DD)?

#### 7.2 Template Compliance

**Acting**: Verify the created document follows the meta-template structure:

- **Skill**: templates/skill_structure.template.md
- **Workflow**: templates/workflow_structure.template.md
- **Role**: templates/role_structure.template.md

**If not compliant** — Fix the structure!

#### 7.3 Link Integrity

**Acting**: Verify no broken references:

```powershell
# Check links in the new file
$newFile = "src/agent-system/database/knowledge/{domain}/{knowledge_name}.knowledge.md"
$content = Get-Content $newFile -Raw
$links = [regex]::Matches($content, '\[.*?\]\((.*?)\)')

foreach ($link in $links) {
    $linkPath = $link.Groups[1].Value
    if ($linkPath -notmatch '^https?://') {
        # Check relative link
        $basePath = Split-Path $newFile
        $fullPath = Join-Path $basePath $linkPath
        if (-not (Test-Path $fullPath)) {
            Write-Host "Broken link: $newFile -> $linkPath" -ForegroundColor Red
        }
    }
}
```

---

### 8. Context Structure Management Skill Update

**If a new skill was added to an agent**, update the statistics table:

```markdown
File: src/agent-system/database/knowledge/management/context_structure_management.knowledge.md

## Structural Statistics (Reference)

**Current Status:**

| Agent | Role | Workflow | Runbook | Skills | Templates |
|-------|:----:|:--------:|:-------:|:------:|:---------:|
| architect | ✅ | ✅ (2) | ✅ | ✅ (0) | ✅ (2) |
```

---

### 9. Reporting and Documentation (Fact Summary Pattern)

**Goal**: Concise report for the Orchestrator and Architect.

#### 9.1 Calibration Report

**Fact Summary Pattern**: Bullet-point format

```markdown
## Knowledge Steward - Calibration Report

**Date:** 2026-03-01
**Source:** Architect Sign-off EPIC-{ID}

### Incoming Recommendations:
- **Recommendation 1**: {Brief description}
  - Scope: {Global/Project}
  - Type: {Skill/Workflow/Pattern}

### Integrated Knowledge Elements:

**Global Knowledge Base:**
- New skill created: `src/agent-system/database/knowledge/{domain}/{knowledge_name}.knowledge.md`
- Existing skill updated: `src/agent-system/database/knowledge/{domain}/{knowledge_name}.knowledge.md` (Section: {section})

**Project Level:**
- Project skill recorded: `docs/joinerytech-flow/knowledge/project_skills.md` (Section: {skill})

### Rejected/Redundant:
- **Recommendation X**: Already exists as `src/agent-system/database/knowledge/{domain}/{knowledge_name}.knowledge.md`

### Registry Update:
- Knowledge Map updated: ✅
- Context Structure Management Skill statistics updated: ✅

### Next Steps:
- [ ] Architect review: Rationale for rejected recommendations
- [ ] Orchestrator: Communicate new skills to the team
- [ ] Dev Team: Apply new skills in the next Epic
```

---

## DoD (Definition of Done)

The calibration workflow is considered complete when:

### Knowledge Integration:
- [ ] All Architect-approved recommendations analyzed
- [ ] Redundancy and conflict check performed
- [ ] Global skills created/updated (if relevant)
- [ ] Project-level skills recorded (if relevant)
- [ ] Rejected recommendations justified

### Documentation:
- [ ] Knowledge Map updated with all new/modified files
- [ ] Context Structure Management Skill statistics up to date
- [ ] All new files have valid frontmatter
- [ ] No broken links

### Structural Compliance:
- [ ] New files follow meta-templates
- [ ] File names consistent
- [ ] `last_updated` fields current

### Reporting:
- [ ] Calibration report completed (Fact Summary Pattern)
- [ ] Report delivered to Orchestrator
- [ ] Architect notified about rejected recommendations (if any)

---

## Workflow Triggers

When to start this calibration workflow:

1. **After Architect Sign-off**: At Epic closure, if "Calibration Recommendations" section is present
2. **Multiple Epic consolidation**: If small recommendations have accumulated, batch processing
3. **Before Major Release**: Knowledge base cleanup and consolidation
4. **Orchestrator request**: If strategic knowledge-sharing decision is made

---

## Related Documents

- **orchestrator_calibration.knowledge.md** — Detailed calibration protocol
- **context_structure_management.knowledge.md** — Structural knowledge
- **knowledge_structure.policy.md** — Role definitions
- **skill_structure.template.md** — Meta-template for skills
- **workflow_structure.template.md** — Meta-template for workflows

---

*Start with Architect Sign-off analysis (Step 1) and apply the Fact Check Pattern at every step: strict redundancy and conflict verification!*
