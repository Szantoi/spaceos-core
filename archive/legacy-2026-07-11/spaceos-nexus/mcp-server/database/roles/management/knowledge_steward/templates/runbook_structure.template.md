---
id: standard-runbook-structure
title: "Runbook File Structural Standard"
description: "Structural standard and skeleton template for creating a new agent runbook (.runbook.md). Defines required sections including context loading, cognitive setup, operating mode, and next-step guidance."
type: template
scope: global
last_updated: 2026-03-01
---

# Runbook File Structural Standard

> Use this template as a reference when creating or updating a `.runbook.md` file for any agent.

---

## 1. Frontmatter (Required)

```yaml
---
id: {runbook-id}
title: "{Display Name} Runbook"
description: "{One sentence — what operating procedures this runbook defines}"
type: runbook
scope: global | project
version: "1.0"
last_updated: YYYY-MM-DD
---
```

---

## 2. Heading and Identity

```markdown
# {Role Name} — Runbook
```

Brief orientation: *This runbook defines the default operating procedure for the {role} agent.*

---

## 3. Context and References

Files this agent must load before starting any task:

| Priority | File | Purpose |
|:---------|:-----|:--------|
| 1 | `{role}.role.md` | Identity and persona |
| 2 | `{skill}.skill.md` | Domain knowledge |
| 3 | `docs/{project}/_program.md` | Active project context |

---

## 4. Operating Mode (Optional)

Use this section if the agent has distinct operating phases (e.g., discovery vs. execution).

- **Mode A — {Name}**: {When to use and what to do}
- **Mode B — {Name}**: {When to use and what to do}

---

## 5. Cognitive Setup (CRITICAL)

> This section directly shapes how the agent reasons.

- **Primary Pattern**: Chain of Thought / ReACT / Fact Summary
- **Instruction**: {Explicit instruction, e.g., "Always state your assumptions before acting."}
- **Anti-patterns to avoid**: {e.g., "Do NOT skip reading input files before writing output."}

---

## 6. Where to Look (Priority Order)

When unsure what to do or where to start:

1. Check inbox: `messages/{role}/` for new assignments
2. Check project state: `docs/{project}/state.md`
3. Check active Epic: `docs/{project}/epics/{EPIC_ID}/`
4. Fall back to: `{role}.role.md` → Responsibilities section

---

## 7. Next Step

How to end a work session:

- [ ] Output artifact written to the correct location
- [ ] Outgoing message queued in `messages/{target}/`
- [ ] State updated (if applicable)
- [ ] Runbook checklist completed

---

## Runbook Skeleton

```markdown
---
id: {role}-runbook
title: "{Role Name} Runbook"
description: "{One sentence}"
type: runbook
scope: global
version: "1.0"
last_updated: YYYY-MM-DD
---

# {Role Name} — Runbook

## Context Loading
{List of files to load on startup}

## Cognitive Setup
{Thinking pattern and key instructions}

## Next Step
{How to determine what to do when starting a session}
```
