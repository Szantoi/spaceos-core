---
id: standard-message-structure
title: "Agent Message / Prompt Structure Standard"
description: "Structural standard template for creating agent-to-agent message prompts. Ensures consistent context loading, cognitive setup, task definition, and output format across all inter-agent communications."
type: template
initiator: "{AGENT_ID}"
target: "{AGENT_ID}"
scope: global
last_updated: 2026-03-01
---

# Message Structural Standard: {INITIATOR} → {TARGET}

> Use this template when writing a new agent message or prompt. Fill in each section according to the instructions in square brackets. Remove unused sections before sending.

---

## 1. Persona & Identity

> {Who is the target agent? What is their role and mindset?}

You are the **{Role Name}** agent in the JoineryTech.Flow multi-agent system. Your primary focus is {brief focus statement}.

---

## 2. Required Context Loading

### Core Files (always load)
- `{role}.role.md` — Role definition and responsibilities
- `{role}.runbook.md` — Operating procedures and checklist

### Task-specific Files (load as needed)
- `{skill_name}.skill.md` — {Why this skill is relevant}
- `docs/{project}/epics/{EPIC_ID}/` — {Relevant Epic context}
- `{other_file}` — {Why it's needed}

---

## 3. Cognitive Setup

> {Which thinking pattern applies to this task?}

- **Pattern**: Chain of Thought / ReACT / Fact Summary / N-shot examples
- **Instruction**: Think step-by-step before producing output. State your assumptions explicitly.

---

## 4. Task Definition

### Inputs
- What you have: {List of files, messages, or context available}

### Expected Outputs
- {Output 1}: `{filename}` — {brief description}
- {Output 2}: {message to send back}

### Execution Steps
1. {Step 1}
2. {Step 2}
3. {Step 3}

---

## 5. Logical Pattern

> Choose ONE and fill in.

**Option A — ReACT**: For each step: Thought → Action → Observation → Repeat until done.

**Option B — Chain of Thought**: First reasoning, then conclusion. Do not output intermediate thoughts.

**Option C — N-shot Examples**:
- Input: {example input}
- Output: {example output}

---

## 6. Constraints & Rules

- {Constraint 1, e.g., "Do not modify files outside your layer boundary."}
- {Constraint 2, e.g., "Always cite the source file when referencing facts."}

---

## 7. Output Format

```
{Describe the exact output structure. E.g., Markdown with frontmatter, JSON, plain text.}
```

---

## Usage Guide

**When to use**: When writing a new agent message or prompt that will be sent via the messaging system.

**Storage location**: `src/agent-system/database/roles/{initiator}/messages/{target}_{action}.message.md`

**Versioning**: Increment the `last_updated` date whenever this template is materially changed.

**How to fill out**: Replace all `{placeholder}` values. Remove square-bracket instructions before sending. Keep sections minimal — only include what is relevant for the specific task.
