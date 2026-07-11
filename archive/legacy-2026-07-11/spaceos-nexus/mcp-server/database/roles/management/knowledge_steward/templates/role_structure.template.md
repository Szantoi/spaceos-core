---
id: standard-role-structure
title: "Role File Structural Standard"
description: "Structural standard and skeleton template for creating a new agent role file (.role.md). Defines required frontmatter fields, mandatory sections, and the expected skeleton structure."
type: template
scope: global
last_updated: 2026-03-01
---

# Role File Structural Standard

> Use this template as a reference when creating or updating a `.role.md` file for any agent.

---

## 1. Frontmatter (Required)

```yaml
---
id: {role-id}                    # kebab-case, unique
title: "{Display Name}"
description: "{One sentence — what this role does and why it exists}"
type: role
scope: global | project
version: "1.0"
last_updated: YYYY-MM-DD
---
```

---

## 2. File Location

```
src/agent-system/database/roles/{layer}/{role_name}/
  {role_name}.role.md       ← this file
  {role_name}.runbook.md    ← operating procedures
  workflows/                ← workflow files
  messages/                 ← outgoing message templates
  skills/                   ← role-specific skills
  templates/                ← document templates
```

---

## 3. Persona & Communication (CRITICAL — Prompt Engineering Integration)

This section loads directly into the agent's active context. It must establish:
- **Who** the agent is (identity, mindset)
- **How** it communicates (tone, language rules)
- **What** it prioritizes

---

## 4. Work Intake

How the agent starts work:
- Which files to load first (runbook, skills, context)
- How to read and interpret its inbox (`messages/`)
- What constitutes a valid task assignment

---

## 5. Focus and Responsibilities

Bullet list of primary responsibilities. Be specific and non-overlapping with other roles.

---

## 6. Mindset

Key operating principles — how this agent makes decisions under uncertainty.

---

## 7. Checklist

Pre-flight and closing checklists for the agent's standard workflows.

---

## 8. Closing Work

What "done" looks like: output artifacts, messages to send, and state updates to make.

---

## Role File Skeleton

```markdown
---
id: {role-id}
title: "{Role Name}"
description: "{One sentence}"
type: role
scope: global
version: "1.0"
last_updated: YYYY-MM-DD
---

# Role: {Name}

## Persona
{Who you are, mindset, and communication style}

## Work Intake
{How to start: files to load, inbox to check}

## Focus & Responsibilities
- {Responsibility 1}
- {Responsibility 2}

## Mindset
- {Principle 1}
- {Principle 2}

## Checklist
- [ ] {Pre-flight item}
- [ ] {Closing item}

## Error Handling
{What to do when blocked or when input is missing}

## Closing
{How to declare work done and what to output}
```
