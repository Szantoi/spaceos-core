---
id: MSG-ARCH-010-DONE
from: architect
to: root
type: done
priority: high
status: APPROVED_BY_ROOT
ref: MSG-ARCH-010
created: 2026-06-17
approved: 2026-06-17
root_decision: MSG-ROOT-038-ADR-APPROVED
---

# DONE: PHASE 3 ADR Review

## Összefoglaló

3 ADR elkészítve és dokumentálva a PHASE 3 tervdokumentumok alapján.

---

## Elkészült ADR-ek

### ADR-043: Marvin Orchestration Pattern
**Fájl:** `docs/architecture/decisions/ADR-043-marvin-orchestration-pattern.md`

**Döntés:** Marvin (Python) veszi át a bash planning pipeline-t.
- Resumable threads (SQLite history)
- 5 agent explicit definícióval (Scanner, Selector, Debater A/B, Synthesizer)
- Structured output (Pydantic models)
- Parallel execution (asyncio.gather)

**Alternatívák elvetett:**
- LangChain/LangGraph — túl komplex, dependency hell
- Claude Code sub-agent-ek — nem resumable, nincs persistent state
- Bash pipeline javítása — törékeny alapok

**Fázis lebontás:**
- Fázis 1: McpServer Knowledge Service (COMPLETE ✅)
- Fázis 2: Marvin Planning Pipeline (~6-7 nap)
- Fázis 3: Reviewer + Nightwatch (~8-10 nap)

---

### ADR-044: Knowledge Service System Integration
**Fájl:** `docs/architecture/decisions/ADR-044-knowledge-service-system-integration.md`

**Döntés:** Knowledge Service scale-up full Datahaven/Resonance foundation-hez.
- ChromaDB + Voyage AI embeddings (operational)
- In-memory fallback (graceful degradation)
- System-wide integration (Architect, terminálok, Planning selector)

**Alternatívák elvetett:**
- PostgreSQL tsvector FTS — superseded, ChromaDB bizonyult jobbnak
- pgvector — nem telepített, extra dependency

**Fázis lebontás:**
- Fázis 1: Core Implementation (COMPLETE ✅)
- Fázis 2: System-wide Integration (~3-4 nap)
- Fázis 3: Full Datahaven/Resonance (~5-6 nap)

---

### ADR-045: McpServer Standard Tools & RPC Interface
**Fájl:** `docs/architecture/decisions/ADR-045-mcpserver-standard-tools.md`

**Döntés:** McpServer toolkit kiterjesztése + RbacFilter.
- `discovery_search` (COMPLETE)
- `submitArtifact` (Fázis 2)
- `getWorkflowState` / `updateWorkflowState` (Fázis 3)
- RbacFilter middleware (tool visibility per role)

**Miért RbacFilter:**
- Gépi enforcement > emberi CLAUDE.md szabályok
- Tool listing szinten szűr — terminál nem látja amit nem hívhat

**Fázis lebontás:**
- Fázis 2: submitArtifact (~2-3 nap)
- Fázis 3: Workflow tracking + RBAC (~4-5 nap)

---

## Golden Rule ellenőrzés (mindhárom ADR)

| Szabály | ADR-043 | ADR-044 | ADR-045 |
|---|---|---|---|
| Data → Rules → Geometry | ✅ Marvin nem számol | ✅ Csak keresés | ✅ Paraméterek, nem geometry |
| Modular Monolith | ✅ Agent-ek decoupled | ✅ Önálló service | ✅ Tool-onként izolált |
| Immutability & Trust | ✅ SQLite append-only | ✅ Read-only vectors | ✅ Workflow state log |
| Need-to-Know RBAC | ⚠️ Fázis 3 | ⚠️ Fázis 3 | ✅ RbacFilter |
| Walking Skeleton First | ✅ Fázisonként | ✅ Fázisonként | ✅ Fázisonként |

---

## ADR_CATALOGUE.md frissítve

Új szekciók hozzáadva:
- ADR-043: Marvin Orchestration Pattern
- ADR-044: Knowledge Service System Integration
- ADR-045: McpServer Standard Tools & RPC Interface

**Fájl:** `docs/knowledge/architecture/ADR_CATALOGUE.md`

---

## Conductor feladat (következő lépések)

Az ADR-ek PROPOSED státuszban vannak. Conductor a planning queue-ba kiadhatja:

```
PHASE 3 QUEUE:
  1. ADR-043 (Marvin) → Nexus terminál (Fázis 2 implementáció)
  2. ADR-044 (RAG) → Librarian + Nexus (System-wide integration)
  3. ADR-045 (MCP) → Nexus (submitArtifact tool)
```

**Blokkoló:**
- VPS memória bővítés (8GB → 16GB) — Marvin + ChromaDB + McpServer

---

## Létrehozott fájlok

```
docs/architecture/decisions/
  ADR-043-marvin-orchestration-pattern.md       (NEW)
  ADR-044-knowledge-service-system-integration.md (NEW)
  ADR-045-mcpserver-standard-tools.md           (NEW)

docs/knowledge/architecture/
  ADR_CATALOGUE.md                              (UPDATED)
```

---

*Architect session complete. 3 ADR elkészült, Golden Rule ellenőrzés végrehajtva, katalógus frissítve.*
