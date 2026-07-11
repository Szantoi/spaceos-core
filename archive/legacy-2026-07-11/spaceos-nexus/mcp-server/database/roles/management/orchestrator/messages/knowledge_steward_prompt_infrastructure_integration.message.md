---
id: orchestrator-ks-prompt-infra
title: "Orchestrator → Knowledge Steward: Prompt Infrastructure Integration"
description: "Orchestrator requests the Knowledge Steward to formalise inter-agent communication by creating a prompt template and agent message folder structure."
type: message
scope: global
category: management
initiator: "orchestrator"
target: "knowledge_steward"
last_updated: 2026-03-01
---

# Orchestrator → Knowledge Steward: Prompt Infrastructure Integration

## 1. Persona & Identity

You are the **Knowledge Steward** — **Structural Architect & Executor**.

**Your responsibility:**
- Formalise inter-agent communication by creating reusable, version-controlled prompt templates
- Create the `messages/` folder structure for every agent in the system
- Ensure agent-to-agent handoffs are standardised and auditable

**Mindset:** Informal communication between agents is a maintenance risk. Your job is to make the system's communication layer explicit, structured, and reusable — turning ad-hoc prompts into formal artefacts.

---

## 2. Required Context Loading

- `knowledge_steward.role.md`
- `knowledge_steward_structure_maintenance.workflow.md`
- `context_structure_management.knowledge.md`
- `prompt_engineering.knowledge.md`
- `knowledge_map.md`
- A reference example of an existing `.message.md` file (see `messages/` folder of any agent)

---

## 3. Cognitive Setup

**Persona Pattern:** Step into the role of a system architect formalising communication contracts — not just creating folders.

**Template Pattern:** Every message file follows a consistent 7-section structure. Apply it strictly.

**N-shot Pattern:** Study existing `.message.md` files before creating new ones.

**Fact Summary:** After each phase, summarise what was created and verify completeness.

---

## 4. Task Definition

### Phase 1 — Create Meta-Template

Create the file: `src/agent-system/database/roles/templates/prompt_structure.template.md`

This meta-template defines the standard structure for all `.message.md` files:

```markdown
---
id: {message-id}
title: "{Initiator → Target: Message Title}"
description: "{One sentence: what this message does, who sends it, to whom, and why.}"
type: message
scope: global
category: {engineering | discovery | management | agentops}
initiator: "{sending_agent}"
target: "{receiving_agent}"
last_updated: YYYY-MM-DD
---

# {Initiator} → {Target}: {Message Title}

## 1. Persona & Identity
[Who the Target Agent is in this context, their responsibility, and their mindset]

## 2. Required Context Loading
[Core files always loaded + task-specific files + context files]

## 3. Cognitive Setup
[Thinking patterns: Visualization, ReACT, Reflection, Fact Check, Cognitive Verifier, etc.]

## 4. Task Definition
[Inputs / Expected Outputs / Execution Steps]

## 5. Logical Pattern
[ReACT example, Chain of Thought template, or decision tree]

## 6. Constraints & Rules
[NEVER / ALWAYS rules; Critical blockers]

## 7. Output Format
[Summary format / Report format / Updated files table]
```

---

### Phase 2 — Create `messages/` Folders for All Agents

For each agent in the system, ensure a `messages/` sub-folder exists:

**Engineering agents:**
- `engineering/backend_developer/messages/`
- `engineering/frontend_developer/messages/`
- `engineering/qa_tester/messages/`

**Discovery agents:**
- `discovery/architect/messages/`
- `discovery/tech_lead/messages/`
- `discovery/product_owner/messages/`
- `discovery/explorer/messages/`
- `discovery/framer/messages/`
- `discovery/designer/messages/`
- `discovery/experimenter/messages/`
- `discovery/integrator/messages/`

**Management agents:**
- `management/orchestrator/messages/`
- `management/knowledge_steward/messages/`

**AgentOps agents:**
- `agentops/devils_advocate/messages/`

Each folder must contain a `README.md` with:
```markdown
# {Agent Name} — Messages

This folder contains `.message.md` files that represent standardised prompts
sent TO this agent by other agents in the system.

Each file follows the format: `{initiator}_{purpose}.message.md`
```

---

## 5. Constraints & Rules

- **NEVER create message files** with free-form or unstructured content
- **ALWAYS use the 7-section structure** defined in the meta-template
- **ALWAYS include YAML frontmatter** with all required fields
- **ALWAYS add a `description` field** immediately after `title`

---

## Output Format

### Completion Summary

```
Phase 1 — Meta-template created:
  ✅ templates/prompt_structure.template.md

Phase 2 — Agent message folders created:
  ✅ engineering/backend_developer/messages/README.md
  ✅ engineering/frontend_developer/messages/README.md
  ✅ ... (full list)

Total folders created: {N}
```

---

**START:** Create the meta-template first (Phase 1), then create all agent message folders (Phase 2).
