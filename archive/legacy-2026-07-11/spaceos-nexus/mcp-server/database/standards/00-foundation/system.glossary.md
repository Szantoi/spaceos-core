---
id: core-terminology-glossary
title: "Agent System Terminology Glossary"
description: "Central terminology reference for the agent system workflow documentation, clarifying what each term means specifically in the VS Code Copilot virtual multi-agent context and how it differs from real multi-agent systems."
type: reference
scope: global
environment: vscode-copilot
created: 2026-02-16
last_updated: 2026-02-16
keywords: ["terminology", "glossary", "agent", "workflow", "vscode"]
---

# Agent System Terminology Glossary

**Environment**: Visual Studio Code + GitHub Copilot

**Purpose**: Central terminology reference for the agent system workflow documentation. This glossary clarifies what each term means **in the VS Code Copilot context** (virtual multi-agent system), and how terms differ from real multi-agent systems.

---

## Agent System Terms

### Agent

**Workflow Usage:**
> "The Architect **agent** plans the Epic..."

**VS Code Copilot Reality:**

- **Virtual Agent** (not a real process or LLM instance)
- Driven by Claude 3.5 Sonnet with **role-based prompt injection**
- A single LLM instance playing a role (Architect, Tech Lead, Developer, etc.)

**In Other Environments:**

- **LangChain Multi-Agent**: Real separate process, separate memory
- **CrewAI**: Real separate agent instance, message passing
- **AutoGPT**: Virtual (similar to VS Code Copilot)

**Example:**

```markdown
Workflow: "Orchestrator hands control to the Architect agent"
Reality: Claude 3.5 Sonnet system prompt injected: "You are a Senior Architect..."
```

---

### Agent Dispatch

**Workflow Usage:**
> "Orchestrator **dispatches** the Backend Developer for Task implementation."

**VS Code Copilot Reality:**

- **Role Prompt Injection** (system prompt modification)
- No real process start or thread spawn
- LLM role context switch (conversation history is preserved)

**In Other Environments:**

- **LangChain**: Real agent process spawn
- **CrewAI**: Placement into task queue, picked up by worker agent

**Example:**

```markdown
Workflow: "Backend Developer dispatched"
Reality: System prompt injected:
---
You are a Backend Developer.
Task: Implement TASK-001
Workflow: src/agent-system/database/roles/engineering/backend_developer/backend_developer.workflow.md
---
```

---

### Handoff

**Workflow Usage:**
> "Tech Lead **hands off** (handoff) the Epic Review to the Architect."

**VS Code Copilot Reality:**

- **Role Context Switch** (previous role prompt → new role prompt)
- Conversation history is preserved (new role sees previous communication)
- No real inter-process communication

**In Other Environments:**

- **LangChain**: Message passing (explicit message object sent)
- **CrewAI**: Task result object transfer

**Example:**

```markdown
Workflow: "Tech Lead handoff to Architect"
Reality:
[LLM Response as Tech Lead]: "epic_review.md completed."
[System Prompt Switch]: "You are now a Senior Architect. Review epic_review.md..."
[LLM Response as Architect]: "Validating epic_review.md..."
```

---

### Agent Communication

**Workflow Usage:**
> "QA Tester **communicates** with the Backend Developer to request a bug fix."

**VS Code Copilot Reality:**

- **Conversation History Reference** (LLM re-reads its own previous output)
- No real message passing protocol
- "Communication" = LLM sees the full chat history

**In Other Environments:**

- **LangChain**: Explicit message passing (MessageBus, Channels)
- **CrewAI**: Inter-agent communication protocol (Crew communication layer)

**Example:**

```markdown
Workflow: "QA Tester communicates bug list to Backend Dev"
Reality:
[LLM as QA Tester]: "Bug list: [BUG-001, BUG-002]"
[System Prompt Switch to Backend Dev]
[LLM as Backend Dev]: "I see the bug list from QA Tester's previous message. Fixing..."
```

---

### Subagent

**Workflow Usage:**
> "Orchestrator starts a **subagent** for critical context isolation."

**VS Code Copilot Reality:**

- **`runSubagent` tool call** (if available — NOT standard!)
- NEW LLM instance, clean context (0 tokens)
- Returns a report that enters the main conversation history

**In Other Environments:**

- **LangChain**: Standard feature (SubChain, AgentExecutor)
- **CrewAI**: Task delegation (crew.task())

**When to use (if available):**

- Context overflow risk (> 150k tokens)
- Critical context isolation (security review, independent analysis)
- Cross-project analysis (reading from another location)

**Example:**

```markdown
Workflow: "Subagent Epic Planning"
Reality (if runSubagent available):
runSubagent(
  prompt: "You are Architect. Plan EPIC-001. Workflow: ...",
  description: "Architect Epic Planning"
)
→ New Claude instance, 0 tokens context
→ Returns: "plan.md created. Summary: ..."
→ Summary added to main conversation
```

---

### Context

**Workflow Usage:**
> "Architect enters **context** for Epic Planning."

**VS Code Copilot Reality:**

- **Conversation History** (full chat thread, max 200k tokens)
- **Workspace Files** (read access via tools)
- **System Prompt** (current role prompt injected)
- **Tool Outputs** (file edits, terminal results)

**Context Persistence:**

- ⚠️ **NO persistence after session ends!**
- ✅ **File-based state tracking (state.md, etc.) is the solution**

**In Other Environments:**

- **LangChain**: Memory (ConversationBufferMemory, DB-backed)
- **CrewAI**: Context management layer (persistent across runs)

**Example:**

```markdown
Workflow: "Context Loading"
Reality:
User: "Orchestrator - Continue the Epic"
Orchestrator: [read_file: state.md, dependency_map.md, decision_log.md]
Orchestrator: "Context loaded. EPIC-001 = In Progress, 3/5 Tasks Done."
```

---

### Context Isolation

**Workflow Usage:**
> "With subagent there is **context isolation**, it does not see the previous Epic history."

**VS Code Copilot Reality:**

- **By default there is NO context isolation** (every role sees full history)
- **Achievable with `runSubagent` tool** (if available)
- With virtual role switch: shared history!

**In Other Environments:**

- **LangChain**: Process isolation (separate memory scope)
- **CrewAI**: Agent isolation (separate context per agent)

**When it is a problem:**

- Architect sees implementation details of the previous 10 Epics (token waste)
- QA Tester sees full codebase history (context noise)

**Solution:**

- Context Hygiene (Knowledge Steward cleanup)
- Use subagent for critical cases

---

### Role

**Workflow Usage:**
> "**Role**: Architect"

**VS Code Copilot Reality:**

- **Prompt Template** (system prompt injection)
- Defines LLM behavior, response style, toolset
- No real process or instance change

**Role Activation:**

```markdown
System Prompt (Architect role):
---
You are a Senior Software Architect.
Your expertise: Clean Architecture, DDD, System Design.
Your workflow: src/agent-system/database/roles/discovery/architect/architect.workflow.md
Your skills: src/agent-system/database/roles/discovery/architect/skills/*.md
Your output format: src/agent-system/database/roles/discovery/architect/templates/*.md
---
```

**Role Switching:**

```markdown
[LLM Response as Architect]: "plan.md completed."
[System Prompt Change to Tech Lead]
[LLM Response as Tech Lead]: "I see the plan.md. Starting Task Breakdown..."
```

---

### Workflow

**Workflow Usage:**
> "Architect follows the **architect.workflow.md**."

**VS Code Copilot Reality:**

- **Structured prompt instructions** (markdown document)
- A step sequence "enforced" on the LLM
- No automatic workflow engine (LLM interprets it)

**Workflow execution:**

1. User or Orchestrator loads the workflow file (file_read tool)
2. LLM reads the workflow steps
3. LLM executes steps sequentially
4. LLM uses tool calls (file edit, terminal, etc.)
5. LLM reports progress

**Not automatic!** The LLM may skip steps if not paying attention.

---

### State Tracking

**Workflow Usage:**
> "Orchestrator **updates the state.md**."

**VS Code Copilot Reality:**

- **File-based persistence** (state.md, dependency_map.md, decision_log.md)
- Conversation history is **NOT persistent** → file-based solution needed!
- "Memory" between sessions

**Why it is critical:**

```
Session 1: Epic Planning done (conversation: 80k tokens)
[User closes VS Code]
Session 2: Conversation: 0 tokens (LOST!)
[Restored from state.md: "EPIC-001 = Ready for Task Breakdown"]
```

**In Other Environments:**

- **LangChain**: Memory persistence (DB, Redis)
- **CrewAI**: Built-in state management

---

### Context Hygiene

**Workflow Usage:**
> "Token usage > 150k → **Context Hygiene** activated."

**VS Code Copilot Reality:**

- **Conversation History cleanup** (removing details of closed Epics)
- Knowledge Steward archiving + conversation summary
- Reducing token usage (150k → 80k)

**Technique:**

1. Closed Epic documentation → archived/
2. Mentions in conversation → summary (1–2 sentences)
3. Detailed history → removed from active context

**Not automatic!** Must be triggered manually (Orchestrator decision).

---

### Token

**Workflow Usage:**
> "Context window: 200k **tokens**."

**VS Code Copilot Reality:**

- **LLM input/output unit of measure** (~4 characters = 1 token for English text)
- Conversation history + workspace files + system prompt + tool outputs = total tokens
- Limit: 200k tokens (Claude 3.5 Sonnet)

**Token Breakdown (Epic execution):**

```
- Conversation History: 120k tokens
- Workspace Files (read): 30k tokens
- System Prompt: 5k tokens
- Tool Outputs: 10k tokens
- Reserved for Response: 35k tokens
──────────────────────────────────
Total: 200k tokens (100% capacity)
```

---

## Terminology Comparison

| Workflow Term | VS Code Copilot (Virtual) | LangChain (Real Multi-Agent) | CrewAI (Real Multi-Agent) |
|:------------------|:--------------------------|:-----------------------------|:--------------------------|
| **Agent** | Role-based prompt (virtual) | Process/Thread (real) | Agent instance (real) |
| **Agent Dispatch** | Prompt injection | Process spawn | Task delegation |
| **Handoff** | Role switch (prompt change) | Message passing | Task result transfer |
| **Communication** | Conversation history ref | MessageBus, Channels | Inter-agent protocol |
| **Subagent** | `runSubagent` tool (opt) | SubChain (standard) | Task delegation (standard) |
| **Context** | Conversation thread | Memory (DB-backed) | Context layer |
| **Context Isolation** | ❌ NO (shared history) | ✅ YES (process isolation) | ✅ YES (agent scope) |
| **State Tracking** | File-based (manual) | Memory API (automatic) | Built-in state management |
| **Parallel Execution** | ❌ NO (sequential) | ✅ YES (multi-thread) | ✅ YES (concurrent tasks) |

---

## Best Practice: Terminology Usage

### In Workflow Documentation (General)

- ✅ Use: "Agent dispatch", "Handoff", "Communication"
- 🎯 Goal: Environment-independent, universally understood terms
- ✅ Advantage: Workflow portability (adaptable to LangChain, CrewAI)

### In Implementation Skills (VS Code Copilot Specific)

- ✅ Use: "Role prompt injection", "Context switch", "History reference"
- 🎯 Goal: Precise technical description, VS Code Copilot operating model
- ✅ Advantage: Clear understanding, appropriate context management strategy

### In User-Facing Prompts

- ✅ Use: "Activate Architect role", "Bring in Tech Lead"
- 🎯 Goal: User-friendly, intuitive phrasing
- ✅ Advantage: User does not need to understand the technical details

---

## Related Documents

- **[orchestrator.workflow.md](../roles/management/orchestrator/orchestrator.workflow.md)** — Epic lifecycle (7 phases)
- **[workflow_diagram.md](workflow_diagram.md)** — Visual overview (prompt communication flow)

---

*This glossary is the central terminology reference. If you are unsure about the meaning of a term, look it up here!*
