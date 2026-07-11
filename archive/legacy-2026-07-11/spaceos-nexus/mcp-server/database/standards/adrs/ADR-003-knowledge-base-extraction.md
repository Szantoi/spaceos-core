---
id: ADR-003
title: "ADR-003: Extracting the Agent Knowledge Base to the agent-system/ Root"
description: "Architectural decision to move agent role definitions and standards from docs/ into src/agent-system/database/ to improve RAG indexing quality and semantic separation."
type: architecture_decision_record
date: 2026-02-24
status: accepted
---

# Architecture Decision Record: Extracting the Agent Knowledge Base

## 1. Context

The project previously stored agent-related documentation inside `docs/`, following a conventional software project layout. However, the newly developed Agent Execution Engine (Multi-Agent RAG system) treats `src/agent-system/database/roles/` and `src/agent-system/database/standards/` as the system's **declarative source code, operational database, and rule set** — not as ordinary documentation.

The RAG (Retrieval-Augmented Generation) vectorisation pipeline needs to index these files precisely. However, the `docs/` directory contains significant noise:
- Experimental notes (`docs/Plans/`)
- Historical decision logs (`logs/`)
- Project landing pages and goal documents (`docs/joinerytech-flow/agent-system-v2/`)

Indexing this noise degrades agent response quality and increases token usage.

## 2. Decision

A new directory is created inside `src/` named `agent-system/database/`, and all structures essential to agent operation are relocated there. This keeps all code-related artefacts under `src/`.

The `docs/` directory is retained for traditional human documentation (plans, meeting notes, project states).

**New structure:**

```text
JoineryTech.Flow/
├── src/
│   ├── JoineryTech.Flow.Api/
│   ├── JoineryTech.Flow.Web/
│   └── agent-system/           # LLM runtime environment
│       ├── server/             # Express Engine code, Node modules
│       └── database/           # Declarative agent database
│           ├── roles/          # Role definitions (role.md, workflow, runbook)
│           └── standards/      # System-wide operational standards
├── docs/                       # Human documentation, project dashboards, plans
```

## 3. Rationale

1. **Repository clarity:** All code and application artefacts live under `src/`. The root directory remains clean and navigable.
2. **RAG context hygiene:** The ChromaDB / RAG engine simply indexes the entire `src/agent-system/database/` folder. No complex exclusion rules are needed.
3. **Semantic separation:** Both humans and machines can clearly distinguish static descriptions (`docs/`) from operational rules (`src/agent-system/database/`) — where modifications **directly affect agent behaviour**.

## 4. Execution Steps

1. Create `src/agent-system/database/` folder.
2. Move role files to `src/agent-system/database/roles/`.
3. Move standards files to `src/agent-system/database/standards/`.
4. Run global search-and-replace across all files:
   - Update any absolute path references to use filename-only or relative references.
5. Verify RAG indexing target is updated to point to `src/agent-system/database/`.

## 5. Consequences

**Positive:**
- Clean RAG indexing with no noise from `docs/`
- All agent-critical files are co-located and version-controlled together
- Clear boundary between human docs and machine-readable operational rules

**Trade-offs:**
- Path references in existing files need updating
- Developers must understand that `src/agent-system/database/` is functional code, not static documentation
