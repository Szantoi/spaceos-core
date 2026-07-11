---
id: workflow-knowledge_steward_frontmatter_enrichment
title: "Knowledge Steward Frontmatter Enrichment Workflow"
description: "Role-by-role frontmatter audit and description field addition. Trigger this when the description (or other required) field is missing from a role's files after the §9 policy was introduced."
type: workflow
scope: global
category: maintenance
last_updated: 2026-03-01
---

## Mission: Frontmatter Enrichment — Role-by-Role

**Role**: Knowledge Steward
**Goal**: Supplement the frontmatter of all files under `src/agent-system/database/roles` with the mandatory fields prescribed by the `knowledge_structure.policy.md §9` — especially the `description` field — **handling one role at a time**.

> **Principle**: Not mass, automatic overwriting. Every `description` value must be formulated based on the *actual content* of the file — not copied from a template.

---

## Cognitive Setup

1. **Fact Check Pattern**: Read the file *first*, then formulate the description.
2. **Template Pattern**: For description formatting, see the `description` semantics table in §9.
3. **ReACT Pattern**: Reasoning (what is the file's purpose?) → Acting (write description) → Observation (fits within the 500 char limit?).
4. **Refusal Pattern**: If the file content is unclear, ask the Orchestrator — do not invent a description.

### Files to load

- **knowledge_structure.policy.md §9** — mandatory fields table
- **role_structure.template.md** — frontmatter examples

---

## Prerequisites

- [ ] The Orchestrator has specified **which role** to process now
- [ ] The target role folder is accessible (`src/agent-system/database/roles/{roleName}/`)
- [ ] Policy §9 table loaded (see above)

---

## Workflow Steps

### 1. Determine Role Scope

The Orchestrator or user specifies the role to process (e.g., `architect`).
All affected file types in the role folder:

```
src/agent-system/database/roles/{roleName}/
├── {roleName}.role.md
├── {roleName}.runbook.md
├── skills/*.skill.md
├── workflows/*.workflow.md
├── messages/*.message.md
└── templates/*.template.md
```

> **Excluded**: `agents/*.agent.md` and `instructions/*.instructions.md` — these come from the awesome-copilot source, where `description`, `name`/`applyTo` are already present.

---

### 2. Assess Missing Fields

For each file, check the first 15 lines (YAML frontmatter block):

```
Present? | Field         | File type
---------|---------------|----------
✅/❌   | description   | all own types
✅/❌   | id            | all own types
✅/❌   | type          | all own types
✅/❌   | last_updated  | all own types
```

If all fields are present → the file is **done**, move to the next one.

---

### 3. Formulate Description (file by file)

Read the entire file content, then answer the type-specific guiding question:

| File type | Guiding question for the description |
| :-------- | :----------------------------------- |
| `.role.md` | What is the role's **unique focus**? In what situation should it be loaded? |
| `.runbook.md` | **When is** this runbook active? In what mode does it operate? |
| `.skill.md` | What **specialized knowledge** does it provide? When is it necessary to load? |
| `.workflow.md` | What is the workflow's **trigger** and what is the **expected output**? |
| `.message.md` | **Who** uses it, **when** and **why**? (Replaces the old `usage` field.) |
| `.template.md` | What is the template's **purpose**? Which file type does it provide a structural example for? |

**Quality requirements:**

- Max. 500 characters
- Concise, informative — not a repetition of the title
- If the file had a `usage:` field (in `.message.md` files), its content can be incorporated into the `description`, then the `usage:` field **must be removed** (would be duplication)

---

### 4. Update Frontmatter

Placement of the `description` field: directly after `title:`, before `type:`.

**Required order:**

```yaml
---
id: {type}-{name}
title: "..."
description: "..."
type: {type}
scope: global
# ... optional fields (keywords, category, etc.)
last_updated: YYYY-MM-DD
---
```

> **Important**: Do **not modify** `last_updated` unless the content also changed — adding the description alone does not count as a content change. Exception: if the `usage:` field is migrated to description, `last_updated` should be refreshed to today's date.

---

### 5. Validation After Modification

For each updated file, verify:

- [ ] Is the `description` field shorter than 500 characters?
- [ ] Is the order correct: `id → title → description → type → scope → ... → last_updated`?
- [ ] If `usage:` field was present, has it been removed?
- [ ] Are the frontmatter `---` delimiters present (opening and closing)?

---

### 6. Summary Report to the Orchestrator

After processing the role, a brief bullet-point report:

```
Role: {roleName}
Files processed: N
  - Updated: X (description added)
  - Already complete: Y
  - usage→description migration: Z files
Issues / decisions pending: (if any)
```

---

## Untouched Areas (Do Not Touch)

| Area | Why |
| :--- | :-- |
| `agents/*.agent.md` | awesome-copilot source — modification prohibited (policy §7) |
| `instructions/*.instructions.md` | awesome-copilot source — modification prohibited (policy §7) |
| `core/*.md` (non-own types) | Separate scope, only Architect may approve changes |
| Content (text below H1) | This workflow only touches frontmatter |
