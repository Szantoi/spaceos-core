---
id: standard-skill-structure
title: "Skill File Structural Standard"
description: "Structural standard and skeleton template for creating a new agent skill file (.skill.md). Defines required frontmatter fields, mandatory sections, code patterns, error tables, and related skills."
type: template
scope: global
last_updated: 2026-03-01
---

# Skill File Structural Standard

> Use this template as a reference when creating or updating a `.skill.md` file for any agent.

---

## 1. Frontmatter (Required)

```yaml
---
id: {skill-id}
title: "{Skill Display Name}"
description: "{One sentence — what knowledge this skill provides}"
type: skill
scope: global | project
version: "1.0"
last_updated: YYYY-MM-DD
keywords:
  - "{keyword1}"
  - "{keyword2}"
---
```

---

## 2. Title and Summary

```markdown
# {Skill Name}
```

**When to use**: {Load this skill when the agent needs to...}

---

## 3. When to Load?

Explicit trigger conditions for loading this skill:

- When working on {domain/layer/task type}
- When the task involves {specific pattern or technology}
- When referenced by a runbook or workflow

---

## 4. Architecture & Rules

Core constraints and standards that apply whenever this skill is active:

- **Rule 1**: {Description}
- **Rule 2**: {Description}
- **Anti-patterns**: {What to avoid}

---

## 5. Code Patterns (N-shot)

> Provide concrete, minimal examples. Prefer real patterns from the codebase.

### Pattern 1 — {Pattern Name}

```csharp
// {Brief explanation}
{code example}
```

### Pattern 2 — {Pattern Name}

```csharp
{code example}
```

---

## 6. Common Errors and Solutions

| Error | Root Cause | Solution |
|:------|:----------|:---------|
| {Error message} | {Why it happens} | {How to fix it} |

---

## 7. Related Skills

- `{other_skill}.skill.md` — {Why it's relevant}

---

## Skill File Skeleton

```markdown
---
id: {skill-id}
title: "{Skill Name}"
description: "{One sentence}"
type: skill
scope: global
version: "1.0"
last_updated: YYYY-MM-DD
keywords:
  - "{keyword}"
---

# {Skill Name}

## When to Load?
{Trigger conditions}

## Architecture & Rules
- {Rule 1}

## Code Patterns
{N-shot examples}

## Common Errors
| Error | Cause | Solution |
|:------|:------|:---------|

## Related Skills
- `{skill}.skill.md`
```
