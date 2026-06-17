---
id: MSG-ARCH-007-DONE
from: architect
to: root
type: done
priority: high
status: READ
ref: MSG-ARCH-007
created: 2026-06-16
---

## Összefoglaló

JoineryTech.McpServer referencia implementáció átvizsgálva. A SpaceOS RAG spec (MSG-ARCH-005) frissítve a referencia tükrében.

### Referenciából átvéve
- `@modelcontextprotocol/sdk` — azonos MCP SDK, TypeScript
- Frontmatter metadata extraction pattern
- Graceful fallback koncepció (DB unavailable → grep fallback)

### Referenciából ELUTASÍTVA
- ChromaDB — VPS 8 GB, új process nem fér el
- LangChain ecosystem (14 csomag) — túl sok dependency
- Gemini embedding — 2. vendor, 103 fájlra overkill
- Chunking (1000/200) — fájlok <30 KB, teljes fájl indexelés elég

### 4 kérdés megválaszolva

| # | Kérdés | Döntés |
|---|---|---|
| 1 | Hol fut? | **Önálló MCP Server, stdio transport** — nem Orchestrator-ban, nem Kernel DB-ben |
| 2 | Embedding model? | **Nincs Fázis 1-ben** (FTS elég). Fázis 2: Voyage-3-lite |
| 3 | Forrás mapping? | `docs/knowledge/**/*.md` + memória fájlok, path-based metadata |
| 4 | Ingestion? | `rag-ingest.js` Node.js script, cron 5h, inkrementális SHA-256 |

### Spec státusz

`docs/tasks/new/RAG_Knowledge_Base_v1.md` — **v3 (DB + Security review kész)**
- ADR-040 proposed: Knowledge Base Query via FTS + MCP
- Megvalósító: ORCH (MCP server + ingestion) + INFRA (DDL + cron)
- Nincs CRITICAL/HIGH nyitott finding

## Output fájl

`docs/tasks/new/RAG_Knowledge_Base_v1.md` (v3, frissítve)

## Kockázatok / kérdések

Nincsenek nyitottak — minden MSG-ARCH-007 kérdés megválaszolva.
