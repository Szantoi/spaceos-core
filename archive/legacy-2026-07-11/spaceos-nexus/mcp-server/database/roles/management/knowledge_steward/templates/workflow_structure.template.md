---
id: standard-workflow-structure
title: "Workflow File Structural Standard"
description: "Structural standard and skeleton template for creating a new agent workflow file (.workflow.md). Defines required sections including cognitive setup, prerequisites, steps, communication prompts, and DoD."
type: template
scope: global
last_updated: 2026-03-01
---

# Workflow File Structural Standard

> Use this template as a reference when creating or updating a `.workflow.md` file for any agent.

---

## 1. Frontmatter (Required)

```yaml
---
id: {workflow-id}
title: "{Workflow Display Name}"
description: "{One sentence — what this workflow accomplishes and when to use it}"
type: workflow
scope: global | project
version: "1.0"
last_updated: YYYY-MM-DD
---
```

---

## 2. Mission & Goal

```markdown
# {Workflow Name}
```

**Mission**: {One sentence — what this workflow is designed to achieve}

**Trigger**: {When should an agent start this workflow? What event or message initiates it?}

---

## 3. Cognitive Setup (CRITICAL)

> This section directly shapes how the agent executes the workflow.

- **Primary Pattern**: Chain of Thought / ReACT / Fact Summary
- **Key Instruction**: {e.g., "Complete each step fully before moving to the next."}
- **Anti-patterns**: {e.g., "Do not skip prerequisite validation."}

---

## 4. Prerequisites / Input

What must be true or available before this workflow can begin:

- [ ] {Prerequisite 1, e.g., "Epic plan exists at the expected path"}
- [ ] {Prerequisite 2, e.g., "QA sign-off received"}
- **Required files**: {List of files the agent needs}

---

## 5. Required Actions (Steps)

1. **{Step Name}**
   - Read: {file or input}
   - Do: {concrete action}
   - Write: {output file or message}

2. **{Step Name}**
   - Read: {file or input}
   - Do: {concrete action}
   - Write: {output file or message}

---

## 6. Communication Prompts

> Choose the format most appropriate for this workflow.

**Option A — Table format**:

| Action | Target Agent | Template / File |
|:-------|:------------|:----------------|
| {Send review} | {tech_lead} | `{template_file}` |

**Option B — Inline format** (for simple workflows):
- Notify `{target_agent}` with message: `{message file or instruction}`

**Reference**: See `knowledge_map.md` for all inter-agent communication conventions.

---

## 7. Definition of Done / Output

This workflow is complete when:

- [ ] {Output 1 exists at the correct path}
- [ ] {Message sent to the correct agent}
- [ ] {State updated if required}

---

## Workflow Skeleton

```markdown
---
id: {workflow-id}
title: "{Workflow Name}"
description: "{One sentence}"
type: workflow
scope: global
version: "1.0"
last_updated: YYYY-MM-DD
---

# {Workflow Name}

**Mission**: {One sentence}
**Trigger**: {When to start}

## Cognitive Setup
{Pattern and key instructions}

## Prerequisites
- [ ] {Prerequisite}

## Steps
1. {Step 1}
2. {Step 2}

## Communication
{Who to notify and how}

## Definition of Done
- [ ] {DoD criterion}
```
