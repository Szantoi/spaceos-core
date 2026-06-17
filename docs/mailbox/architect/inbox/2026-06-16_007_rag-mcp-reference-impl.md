---
id: MSG-ARCH-007
from: root
to: architect
type: task
priority: high
status: READ
model: opus
ref: MSG-ARCH-005
created: 2026-06-16
---

# Architect — RAG tervdok: referencia implementáció vizsgálata

## Kontextus

Az MSG-ARCH-005-ben kiadott RAG knowledge base tervdokhoz (amit most kell elkészítened)
van egy működő referencia implementáció a saját portfóliónkban:

**`https://github.com/Szantoi/JoineryTech.McpServer`**

## Mit tartalmaz ez a projekt

```
src/rag/
  VectorStore.ts          — ChromaDB + in-memory fallback, Google Gemini embeddings
  indexKnowledgeBase.ts   — *.knowledge.md fájlok indexelése LangChain splitter-rel
  episodicMemory.ts       — epizodikus memória
  RetryableSeeder.ts      — retry logika seeding-hez
```

### Technikai részletek

| Komponens | Implementáció |
|-----------|---------------|
| Vector store | ChromaDB (localhost:8000) + in-memory fallback |
| Embedding model | Google Gemini `gemini-embedding-001` |
| Chunking | LangChain `MarkdownTextSplitter` — 1000 char, 200 overlap |
| Forrás fájlok | `database/*.knowledge.md` (frontmatter + content) |
| Lekérdezés | `queryKnowledge(text, domain?)` → top-K chunks |
| Stack | TypeScript / Node.js — ugyanaz mint az Orchestrator |

A VectorStore gracefully fallback-el in-memory store-ra ha ChromaDB nem elérhető —
ez headless scanner futásoknál előnyös lehet.

## Amit a tervdoknak el kell döntenie

### 1. Architektúra: hol fut a RAG?

**A opció — Orchestrator-ba integrálva:**
- A meglévő Node.js Orchestrator kap egy `/rag/query` endpoint-ot
- A JoineryTech.McpServer RAG kódja portolható — ugyanaz a stack
- Előny: nincs új service
- Hátrány: Orchestrator-ba kerül egy nem-BFF felelősség

**B opció — Önálló MCP Server:**
- A JoineryTech.McpServer adaptálva SpaceOS-ra
- MCP protokollon hívható Claude-tól és a scanner-ektől is
- Előny: a headless `claude -p` session-ök MCP tool-ként hívhatják
- Hátrány: új process a VPS-en (kapacitás szűkös — 8GB RAM)

**C opció — pgvector a Kernel PostgreSQL-ben:**
- Nincs új service, nincs ChromaDB
- Előny: meglévő infrastruktúra, nincs külön process
- Hátrány: Kernel-be kerül egy tudásbázis-felelősség (réteg sértés?)

### 2. Embedding model csere

A referencia implementáció Google Gemini-t használ.
SpaceOS kontextusban az opciók:
- **Voyage-3-lite (Anthropic)** — vendor egységesség, 1024 dim, olcsóbb
- **Gemini embedding-001** — kész implementáció, de 2. vendor
- **OpenAI ada-002** — jól dokumentált, de 3. vendor

### 3. Forrás fájlok mapping

| JoineryTech.McpServer | SpaceOS |
|----------------------|---------|
| `database/*.knowledge.md` | `docs/knowledge/**/*.md` |
| — | `/home/gabor/.claude/projects/*/memory/*.md` |
| frontmatter: domain | frontmatter: type, domain |

### 4. Ingestion trigger

Ki és mikor indexel?
- Librarian session után automatikusan? (cron-librarian.sh hívja)
- Önálló `rag-ingest.sh` cron?
- Inkrementális (csak módosult fájlok — hash alapú)?

## Elvárt output (spaceos-arch-planner pipeline kötelező)

```
docs/tasks/new/SpaceOS_RAG_KnowledgeBase_v1.md   ← draft
docs/tasks/new/SpaceOS_RAG_KnowledgeBase_v2.md   ← DB/infrastruktúra review
docs/tasks/new/SpaceOS_RAG_KnowledgeBase_v3.md   ← security review
```

Minimum v2 szükséges mielőtt outbox-ba kerül.

A tervdoknak meg kell válaszolnia a fenti 4 kérdést döntési indoklással,
és meg kell nevezni a megvalósító terminált (ORCH? Új RAG terminál?).

ADR bejegyzés kötelező: `docs/knowledge/architecture/ADR_CATALOGUE.md`

DONE outbox mikor a tervdok legalább v2 státuszban van.
