---
id: SPEC-005
source: /opt/spaceos/docs/mailbox/architect/outbox/2026-06-16_007_rag-reference-impl-done.md
type: Architecture validation & Security confirmation
scope: [orch, infra]
priority: high
complexity: 3
dependencies: [ADR-040, RAG_Knowledge_Base_v1.md]
status: NEW
created: 2026-06-17
---

# RAG Reference Implementation Validation (JoineryTech.McpServer)

## Összefoglaló

Az Architect terminál a JoineryTech.McpServer referencia implementációt átvizsgálta az MSG-ARCH-007 spec szempontjából. A SpaceOS RAG spec v3 (**docs/tasks/new/RAG_Knowledge_Base_v1.md**) már tartalmaz minden szükséges döntést; ez a validáció megerősíti, hogy a referencia implementáció egy valós, bevált megoldás, amely SpaceOS spec-kel konzisztens.

## Scope

- **Érintett terminálok:** ORCH (MCP server), INFRA (cron + DDL)
- **Artefaktumok:** ADR-040 proposed (Knowledge Base Query via FTS + MCP)
- **Előfeltételek:** RAG_Knowledge_Base_v1.md v3 már kész

## Validáció eredménye

### 4 kulcskérdés megválaszolva

| # | Kérdés | SpaceOS döntés | Validáció |
|---|---|---|---|
| 1 | **Hol fut a RAG?** | Önálló MCP Server, stdio transport | ✅ MATCH — JoineryTech in-memory fallback pattern átvehető |
| 2 | **Embedding model?** | Nincs Fázis 1-ben (FTS), Fázis 2: Voyage-3-lite | ✅ MATCH — referencia Gemini-t használ, de vendor-agnosztikus pattern |
| 3 | **Forrás mapping?** | docs/knowledge/ + memória fájlok, path-based metadata | ✅ MATCH — JoineryTech frontmatter extraction pattern alkalmazható |
| 4 | **Ingestion?** | Node.js rag-ingest.js, cron 5h, inkrementális SHA-256 | ✅ MATCH — Node.js (pg parameterized) megoldja SEC-P1 finding-et |

### Átvételre javasolt minták

| Elem | JoineryTech | SpaceOS adaptáció | Szint |
|---|---|---|---|
| `@modelcontextprotocol/sdk` | TypeScript 1.27.1+ | Azonos — bevált MCP SDK | ✅ CORE |
| Frontmatter metadata extraction | `parseFrontmatter()` pattern | Alkalmazható `docs/knowledge/**/*.md`-re | ✅ CORE |
| Graceful fallback (DB unavail → fallback) | ChromaDB unavailable → in-memory | psql unavailable → grep fallback | ✅ DEFENSIVE |
| MCP Tool schema (search + read) | 2 Tool: search + read | Azonos (knowledge_search + knowledge_read) | ✅ MATCH |

### Kizárt elemek (indoklás)

| Elem | JoineryTech | Miért NEM veszük át | Alternatíva |
|---|---|---|---|
| ChromaDB vector store | Főleg vector search | VPS 8 GB szűkös, új process | PostgreSQL tsvector FTS |
| LangChain ecosystem (14 csomag) | Chunking + vector embedding | Túl sok dependency erre a scope-ra | pg + @mcp/sdk |
| Gemini embedding-001 | Embedding API | 2. vendor, 103 fájlra overkill | FTS (Fázis 1), Voyage-3 (Fázis 2) |
| Chunking (1000/200) | MarkdownTextSplitter | Fájlok <30 KB, felesleges overhead | Teljes fájl indexálás |

## Implementációs hatás

### 1. ORCH terminál (Node.js MCP server + ingestion)

```typescript
// spaceos-knowledge MCP Server
// Fájl: /backend/spaceos-orchestrator/mcp-servers/spaceos-knowledge/index.ts

import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { Tool, TextContent } from "@modelcontextprotocol/sdk/types.js";
import { Pool } from "pg";

const pool = new Pool({
  connectionString: "postgres://spaceos:***@localhost:5433/spaceos_knowledge",
});

const server = new Server({
  name: "spaceos-knowledge",
  version: "1.0.0",
});

// Tool 1: knowledge_search — FTS + tsvector
server.setRequestHandler(Tool, async (request) => {
  // inputSchema: { query, source_type?, category?, terminal?, limit? }
  // SQL: SELECT ... FROM knowledge.documents WHERE content_tsvector @@ to_tsquery(...)
  // Fallback: if DB fails → grep -r docs/knowledge/
  return { /* results array */ };
});

// Tool 2: knowledge_read — document lookup
server.setRequestHandler(Tool, async (request) => {
  // inputSchema: { file_path }
  // SQL: SELECT content FROM knowledge.documents WHERE file_path = $1
  return { /* content + metadata */ };
});

const transport = new StdioServerTransport();
await server.connect(transport);
```

**Implementáció:** ORCH terminál (Node.js, TypeScript, pg client)
**Becsült munka:** 2 nap (MCP server scaffold + SQL integration)

### 2. ORCH terminál (Node.js ingest script, SEC-P1 compliance)

```typescript
// Fájl: /backend/spaceos-orchestrator/scripts/rag-ingest.js
// Parameterized pg queries — SQL injection védelem (SEC-P1)

import { Pool } from "pg";
import { createHash } from "crypto";
import { glob } from "glob";

const pool = new Pool({
  connectionString: "process.env.KNOWLEDGE_DB_URL",
});

const ingestFiles = async () => {
  // Scan: /opt/spaceos/docs/knowledge/ + memória fájlok
  const files = await glob([
    "/opt/spaceos/docs/knowledge/**/*.md",
    "/home/gabor/.claude/projects/*/memory/*.md",
  ]);

  for (const filePath of files) {
    if (filePath.endsWith("MEMORY.md")) continue; // index fájl skip

    const content = fs.readFileSync(filePath, "utf-8");
    const hash = createHash("sha256").update(content).digest("hex");

    // UPSERT: parameterized query (nincs SQL injection)
    await pool.query(
      `INSERT INTO knowledge.documents (file_path, content, content_hash, ...)
       VALUES ($1, $2, $3, ...)
       ON CONFLICT (file_path) DO UPDATE
       SET content = $2, content_hash = $3, updated_at = now()`,
      [filePath, content, hash, /* ... */]
    );
  }

  // Cleanup: deleted files
  await pool.query(
    `DELETE FROM knowledge.documents WHERE file_path NOT IN (...)`,
    [/* jelenlegi fájlok listája */]
  );
};

export { ingestFiles };
```

**Implementáció:** ORCH terminál
**Becsült munka:** 1.5 nap (file scanning + parameterized queries)
**Security:** SEC-P1 (SQL injection) megoldva ✅

### 3. INFRA terminál (DDL + cron setup)

```bash
# Fájl: /infra/scripts/01-knowledge-schema.sql
-- Saját DB: spaceos_knowledge (port 5433)
CREATE DATABASE spaceos_knowledge;
CREATE SCHEMA knowledge;
CREATE TABLE knowledge.documents (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  file_path TEXT NOT NULL UNIQUE,
  source_type TEXT NOT NULL CHECK (source_type IN ('knowledge', 'memory')),
  category TEXT,
  terminal TEXT,
  title TEXT NOT NULL,
  content TEXT NOT NULL,
  content_tsvector TSVECTOR GENERATED ALWAYS AS (
    to_tsvector('simple', coalesce(title, '') || ' ' || content)
  ) STORED,
  content_hash TEXT NOT NULL,
  word_count INT DEFAULT 0,
  indexed_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);
ALTER TABLE knowledge.documents ENABLE ROW LEVEL SECURITY;
CREATE POLICY admin_full_access ON knowledge.documents FOR ALL USING (true);
CREATE INDEX idx_documents_tsvector ON knowledge.documents USING GIN (content_tsvector);
```

**Cron setup:**
```bash
# Librarian futás után (5 óránként)
0 */5 * * * /opt/spaceos/backend/spaceos-orchestrator/scripts/rag-ingest.js
```

**Implementáció:** INFRA terminál
**Becsült munka:** 0.5 nap (DDL + cron)

## Kockázatok

| Kockázat | Likelihood | Impact | Mitigation |
|---|---|---|---|
| FTS recall alacsony vegyes nyelvnél | MEDIUM | MEDIUM | Magyar stopword lista, tesztelés 103 fájlon |
| MCP server crash | LOW | MEDIUM | Fallback: grep pattern maradt (graceful degradation) |
| Memória fájlok PII | MEDIUM | LOW | Terminál memóriák review (nincs ügyféladat) |
| Ingestion performance (5h cron) | LOW | LOW | Inkrementális SHA-256, csak módosult fájlok |

## Döntés: ADR-040

**Status:** PROPOSED
**Title:** Knowledge Base Query via FTS + MCP
**Consequence:** PostgreSQL tsvector (nincs embedding Fázis 1-ben) + Node.js MCP Server
**Rationale:** 103 fájl, 556 KB — vector DB overkill. Walking Skeleton > perfect later.

## Eredeti dokumentum

- **Fájl:** `/opt/spaceos/docs/mailbox/architect/outbox/2026-06-16_007_rag-reference-impl-done.md`
- **Ref:** MSG-ARCH-007 (Root kérés a referencia átvizsgálására)
- **Spec alapja:** `docs/tasks/new/RAG_Knowledge_Base_v1.md` (v3, DB + Security review kész)
