# SpaceOS RAG Knowledge Base — Architektúra tervdokumentum

> **Verzió:** v3 · **Dátum:** 2026-06-16
> **Státusz:** REVIEW — v2 DB + v3 Security review elvégezve
> **Forrás:** MSG-ARCH-005 (Root kérés)
> **Megvalósító terminál:** ORCH (query interface) + INFRA (pgvector install) + LIBRARIAN (ingestion trigger)

---

## 0. Executive Summary

A jelenlegi tudásbázis **103 fájl, ~556 KB** — ez extrém kis corpus. Teljes értékű vector embedding pipeline (pgvector + voyage model + ingestion cron) **túlméretezett** erre a méretre. A Walking Skeleton elv értelmében:

**Ajánlás: PostgreSQL tsvector (FTS) + strukturált metadata tábla** — 0 új service, 0 új API kulcs, 0 extra cost. Upgrade path embeddings-re ha a corpus >1000 fájl / >10 MB fölé nő.

---

## 1. Döntési mátrix

### 1.1 Embedding store opciók értékelése

| Opció | Előny | Hátrány | Verdikt |
|---|---|---|---|
| **pgvector** | Meglévő PostgreSQL, SQL query | NOT INSTALLED, embedding model cost, ~103 fájlnál overkill | ❌ Nem most — upgrade path |
| **ChromaDB** | Könnyű Python server | Új process a VPS-en (8 GB szűkös!), Python dep | ❌ VPS kapacitás miatt kizárva |
| **sqlite-vec** | Fájl alapú, headless-friendly | Embedding model még kell, egyedi integráció | ❌ Nem jobb mint pgvector |
| **PostgreSQL tsvector** | NINCS új dep, nincs cost, trigram+FTS natív | Nem szemantikus (keyword-based) | ✅ **AJÁNLOTT** — Walking Skeleton |

### 1.2 Miért elég az FTS 103 fájlra?

A scanner kérdései ("Mi van megoldva az inventory területen?") **keyword-keresésre redukálhatók**:
- "inventory" → `WHERE content_tsvector @@ to_tsquery('inventory')`
- "FSM döntés" → `WHERE content_tsvector @@ to_tsquery('FSM & döntés')`
- "melyik terminál ütközött" → `WHERE content_tsvector @@ to_tsquery('blocked | error')`

A ~100 fájlos corpus-on a keyword recall 90%+ — a szemantikus keresés marginális javulást hozna.

### 1.3 Referencia implementáció értékelése (JoineryTech.McpServer)

A `Szantoi/JoineryTech.McpServer` saját RAG implementációnk átvizsgálva:

| Elem | JoineryTech | SpaceOS döntés | Indoklás |
|---|---|---|---|
| Vector store | ChromaDB + in-memory fallback | PostgreSQL tsvector | 0 új process, VPS 8 GB szűkös — ChromaDB kizárva |
| Embedding | Gemini embedding-001 | Nincs (FTS) | 103 fájlra overkill, 0 API cost |
| Chunking | LangChain MarkdownTextSplitter 1000/200 | Teljes fájl indexelés | Fájlok <30 KB — chunking felesleges overhead |
| Dependencies | 14 csomag (LangChain ecosystem) | pg client + @modelcontextprotocol/sdk | Kisebb attack surface, kevesebb dep |
| MCP SDK | `@modelcontextprotocol/sdk ^1.27.1` | **ÁTVESSZÜK** — azonos SDK | Node.js, TypeScript, bevált |
| In-memory fallback | ✅ graceful degradation | **ÁTVESSZÜK** koncepciót — psql fallback grep-re | Headless scanner backup |

**Amit átveszünk a referenciából:**
- `@modelcontextprotocol/sdk` — azonos MCP SDK, bevált TypeScript implementáció
- Frontmatter metadata extraction pattern — `parseFrontmatter()` minta alkalmazható
- Graceful fallback koncepció — ha DB nem elérhető, `grep` fallback

**Amit NEM veszünk át:**
- ChromaDB — új process, VPS kapacitás nem engedi
- LangChain ecosystem (14 csomag) — túl sok dependency erre a scope-ra
- Gemini embedding — 2. vendor, unnecessary cost 103 fájlra
- Chunking — fájlméret nem indokolja

### 1.4 Upgrade path (ha corpus nő)

```
Fázis 1 (MOST):   tsvector FTS + metadata tábla     → ~103 fájl, ~556 KB
Fázis 2 (>500 fájl): pgvector install + voyage-3-lite → embedding column hozzáadása
                      LangChain chunking átvétele a referenciából (1000/200)
Fázis 3 (>2000 fájl): hybrid FTS + vector reranking   → legjobb mindkét világból
```

---

## 2. Adatmodell

### 2.1 Tábla: `knowledge_documents`

**Adatbázis döntés:** Saját `spaceos_knowledge` PostgreSQL database (nem a Kernel DB-ben).
Indoklás: ADR-039 — cross-modul nem nyúl más modul DB-jébe. A knowledge nem tenant-scoped.

```sql
-- DB: spaceos_knowledge (port 5433, saját database)
CREATE SCHEMA IF NOT EXISTS knowledge;

CREATE TABLE knowledge.documents (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    file_path       TEXT NOT NULL UNIQUE,          -- relatív: 'knowledge/security/SECURITY_PATTERNS.md'
    source_type     TEXT NOT NULL CHECK (source_type IN ('knowledge', 'memory')),
    category        TEXT,                           -- 'security', 'architecture', 'deployment', stb.
    terminal        TEXT,                           -- 'kernel', 'orch', 'cutting', stb. (ha releváns)
    title           TEXT NOT NULL,
    content         TEXT NOT NULL,
    content_tsvector TSVECTOR GENERATED ALWAYS AS (
        to_tsvector('simple', coalesce(title, '') || ' ' || content)
    ) STORED,
    content_hash    TEXT NOT NULL,                  -- SHA-256 → inkrementális ingestion
    word_count      INT NOT NULL DEFAULT 0,
    indexed_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- RLS: nem tenant-scoped, de SpaceOS pattern miatt explicit policy
ALTER TABLE knowledge.documents ENABLE ROW LEVEL SECURITY;
CREATE POLICY admin_full_access ON knowledge.documents FOR ALL USING (true);

CREATE INDEX idx_documents_tsvector ON knowledge.documents USING GIN (content_tsvector);
CREATE INDEX idx_documents_source ON knowledge.documents (source_type);
CREATE INDEX idx_documents_category ON knowledge.documents (category);
CREATE INDEX idx_documents_terminal ON knowledge.documents (terminal);

-- Fázis 2 upgrade path (pgvector):
-- ALTER TABLE knowledge.documents ADD COLUMN embedding vector(1024);
-- CREATE INDEX idx_documents_embedding ON knowledge.documents USING ivfflat (embedding vector_cosine_ops);
```

### v2 DB Review Findings

| ID | Súly | Terület | Probléma | Javítás | Státusz |
|---|---|---|---|---|---|
| DB-P1 | MEDIUM | Schema isolation | Melyik DB-ben? | Saját `spaceos_knowledge` DB — ADR-039 | ✅ RESOLVED |
| DB-P2 | LOW | RLS | SpaceOS pattern: FORCE RLS minden táblán | Explicit RLS + admin policy hozzáadva | ✅ RESOLVED |
| DB-P3 | LOW | Index | `content TEXT` TOAST threshold nagy fájloknál | Nem kritikus 103 fájlnál — monitoring | ACCEPTED |
| DB-P4 | LOW | Naming | `file_hash` → `content_hash` | Átnevezve | ✅ RESOLVED |

### 2.2 Tábla: `knowledge_chunks` (opcionális, nagy fájlokhoz)

A jelenlegi fájlok kicsik (<30 KB max). **Fázis 1-ben NEM kell chunking** — a teljes fájl tartalom indexelt. Ha a fájlméret >50 KB-ra nő, chunk tábla szükséges:

```sql
-- FÁZIS 2: csak ha fájlméret >50 KB
CREATE TABLE knowledge.chunks (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    document_id UUID REFERENCES knowledge.documents(id) ON DELETE CASCADE,
    chunk_index INT NOT NULL,
    content     TEXT NOT NULL,
    content_tsvector TSVECTOR GENERATED ALWAYS AS (to_tsvector('simple', content)) STORED
);
```

---

## 3. Ingestion pipeline

### 3.1 Ki és mikor?

| Trigger | Felelős | Gyakoriság |
|---|---|---|
| `docs/knowledge/` változás | `rag-ingest.sh` cron | Librarian futás után (5 óránként) |
| Memória fájl változás | Ugyanaz a cron | 5 óránként |
| Kézi trigger | `rag-ingest.sh --force` | Ad-hoc |

### 3.2 `rag-ingest.sh` — inkrementális

```bash
#!/bin/bash
# Inkrementális: SHA-256 hash összehasonlítás — csak módosult fájlok
# Forrás mappák:
#   /opt/spaceos/docs/knowledge/**/*.md
#   /home/gabor/.claude/projects/*/memory/*.md (kivéve MEMORY.md index fájlok)

for file in $(find /opt/spaceos/docs/knowledge -name "*.md" -type f; \
              find /home/gabor/.claude/projects -path "*/memory/*.md" -not -name "MEMORY.md" -type f); do
    hash=$(sha256sum "$file" | cut -d' ' -f1)
    # psql UPSERT: ha hash változott → UPDATE content, ha új → INSERT
    # Ha fájl törölve → DELETE WHERE file_path NOT IN (...)
done
```

### 3.3 Metadata kinyerés

A fájl path-ból automatikusan:
- `source_type`: `knowledge` vagy `memory` a path alapján
- `category`: alkönyvtár neve (`security`, `architecture`, `deployment`, `patterns`, `context`)
- `terminal`: memória fájloknál a project könyvtár nevéből (pl. `-opt-spaceos-SpaceOS-Kernel` → `kernel`)
- `title`: fájl első `# ` fejléce, vagy fájlnév

### 3.4 Memória fájlok volatilitása

A memória fájlok törölhetők (Librarian, auto-cleanup). A `rag-ingest.sh` minden futásnál:
1. Scan: összes jelenlegi fájl hash
2. Összevet DB-vel: INSERT/UPDATE módosultak, **DELETE WHERE file_path NOT IN (jelenlegi fájlok)**
3. Ez kezeli a törléseket automatikusan

---

## 4. Query interface

### 4.1 Ajánlás: MCP tool (NEM HTTP endpoint)

A scanner és a tervezési pipeline **Claude Code session-ökben fut** — az MCP tool natív integráció, nem kell HTTP client-et írni.

**Miért MCP és nem HTTP endpoint:**
- A scanner (`claude -p haiku`) és a design session (`claude --model opus`) MCP tool-okat tud hívni natívan
- Nincs extra HTTP round-trip az Orchestratoron keresztül
- A tool schema self-documenting — a model tudja mit kap, mit küld

### 4.2 MCP Server spec: `spaceos-knowledge`

```typescript
// MCP Server: spaceos-knowledge
// Transport: stdio (lokális process, nincs hálózat)
// Implementáció: Node.js (illeszkedik az Orch stack-hez)

// Tool 1: knowledge_search
{
  name: "knowledge_search",
  description: "Keresés a SpaceOS tudásbázisban (docs/knowledge + terminál memóriák). Keyword-alapú full-text search.",
  inputSchema: {
    query: string,           // keresési kifejezés (természetes nyelvű vagy keyword)
    source_type?: "knowledge" | "memory" | "all",  // default: "all"
    category?: string,       // szűrés: 'security', 'architecture', 'deployment', stb.
    terminal?: string,       // szűrés terminálra: 'kernel', 'orch', 'cutting', stb.
    limit?: number           // default: 5, max: 20
  },
  output: {
    results: [{
      file_path: string,
      title: string,
      source_type: string,
      category: string,
      relevance_rank: number,
      snippet: string,        // ~200 karakter kontextus a match körül
      word_count: number
    }],
    total_matches: number
  }
}

// Tool 2: knowledge_read
{
  name: "knowledge_read",
  description: "Egy konkrét tudásbázis dokumentum teljes tartalmának olvasása.",
  inputSchema: {
    file_path: string        // a knowledge_search által visszaadott path
  },
  output: {
    content: string,
    metadata: { source_type, category, terminal, indexed_at }
  }
}

// Resource: knowledge_stats (opcionális)
{
  name: "knowledge_stats",
  description: "Tudásbázis statisztikák — fájlok száma, kategóriák, utolsó indexelés.",
}
```

### 4.3 SQL query pattern (a MCP server mögött)

```sql
-- knowledge_search implementáció
SELECT
    file_path,
    title,
    source_type,
    category,
    ts_rank(content_tsvector, query) AS relevance_rank,
    ts_headline('simple', content, query, 'MaxWords=30, MinWords=15') AS snippet,
    word_count
FROM knowledge.documents,
     to_tsquery('simple', $1) query  -- $1: user query → tsquery konverzió
WHERE content_tsvector @@ query
  AND ($2 IS NULL OR source_type = $2)
  AND ($3 IS NULL OR category = $3)
  AND ($4 IS NULL OR terminal = $4)
ORDER BY relevance_rank DESC
LIMIT $5;
```

### 4.4 Query konverzió

A természetes nyelvű kérdést tsquery-re kell alakítani:
```
"Mi van megoldva az inventory területen?"
→ to_tsquery('simple', 'inventory & megoldva')

"FSM döntések"
→ to_tsquery('simple', 'FSM & döntés')

"blocked problémák kernel"
→ to_tsquery('simple', 'blocked & kernel')
```

**Egyszerű heurisztika:** stopword szűrés (magyar + angol) + AND kapcsolat. Nem kell NLP — a model úgyis keyword-öket fog küldeni ha tud.

---

## 5. Implementációs sorrend

| Sorrend | Feladat | Terminál | Előfeltétel | Becsült nap |
|---|---|---|---|---|
| 1 | `knowledge` schema + tábla DDL | INFRA / VPS Operator | semmi | 0.5 |
| 2 | `rag-ingest.sh` script + cron | INFRA | tábla kész | 1 |
| 3 | MCP server (`spaceos-knowledge`) | ORCH | tábla + adat kész | 2 |
| 4 | MCP regisztráció Claude settings-ben | INFRA | MCP server kész | 0.5 |
| 5 | Scanner integration (Haiku tool use) | INFRA (script módosítás) | MCP kész | 0.5 |

**Teljes:** ~4-5 nap (szekvenciális)

### Függőségi lánc

```
DDL (INFRA) → Ingest script (INFRA) → MCP server (ORCH) → Settings reg (INFRA) → Scanner use
```

---

## 6. MSG-ARCH-007 kérdéseinek megválaszolása

### Kérdés 1: Architektúra — hol fut a RAG?

**Döntés: B opció — Önálló MCP Server (stdio transport, NEM hálózati)**

| Opció | Értékelés |
|---|---|
| A — Orchestrator-ba integrálva | ❌ Orchestrator = AI gateway (tool calling). RAG felelősség sértené a single-responsibility-t. ADR: Orch NEM proxy. |
| **B — Önálló MCP Server** | ✅ **VÁLASZTOTT** — stdio transport (nem hálózati process, nincs port foglalás). A `claude -p` session-ök MCP tool-ként hívják. Minimális memória footprint. |
| C — pgvector a Kernel DB-ben | ❌ ADR-039: cross-modul nem nyúl a Kernel DB-be. Réteg sértés. |

**Implementáció:** Node.js MCP server a `@modelcontextprotocol/sdk`-val (JoineryTech.McpServer referenciából átvéve). Stdio transport → **nincs új port, nincs hálózati process** → VPS 8 GB kapacitás nem érintett.

### Kérdés 2: Embedding model csere

**Döntés: Nincs embedding model Fázis 1-ben**

FTS (tsvector) elégséges 103 fájlra. Fázis 2-ben:
- **Voyage-3-lite (Anthropic)** — vendor egységesség, 1024 dim, cost-effective
- Gemini és OpenAI kizárva (2. és 3. vendor dependency)

### Kérdés 3: Forrás fájlok mapping

| JoineryTech.McpServer | SpaceOS RAG | Megjegyzés |
|---|---|---|
| `database/*.knowledge.md` | `docs/knowledge/**/*.md` | Rekurzív scan |
| — | `/home/gabor/.claude/projects/*/memory/*.md` | Kivéve `MEMORY.md` index fájlok |
| frontmatter: `domain` | frontmatter: `type`, `description` | SpaceOS memória formátum |
| — | path-based metadata: `category`, `terminal` | Automatikus kinyerés |

### Kérdés 4: Ingestion trigger

**Döntés: Önálló `rag-ingest.js` Node.js script, cron-nal (5 óránként)**

- Nem a Librarian session hívja — az ingestion ne függjön a Librarian sikerességétől
- Inkrementális: SHA-256 hash összehasonlítás, csak módosult fájlok
- Node.js (nem bash) — SEC-P1 finding: parameterized pg queries kötelező
- Cron: `0 */5 * * *` — Librarian futás után

### Megvalósító terminál

**ORCH terminál** — a MCP server és az ingestion script egyaránt Node.js/TypeScript, az Orchestrator stack-jében él. Az INFRA terminál a DDL-t és a cron regisztrációt végzi.

---

## 7. Architekturális döntések

### ADR-040 (proposed): Knowledge Base Query via FTS + MCP

**Kontextus:** A tervezési pipeline-nak szemantikus keresés kell a tudásbázisban.

**Döntés:** PostgreSQL tsvector FTS + MCP tool interface. Embedding-ek (pgvector) halasztva Fázis 2-re.

**Indoklás:**
- Corpus méret (103 fájl, 556 KB) nem indokolja vector DB-t
- 0 extra cost (nincs embedding API hívás)
- 0 új service (8 GB VPS szűkös — ChromaDB kizárva)
- Walking Skeleton elv — működő FTS most > tökéletes RAG 2 hét múlva
- Upgrade path tiszta: pgvector column hozzáadása a meglévő táblához

**Elvetett alternatívák:**
| Alternatíva | Miért nem |
|---|---|
| pgvector + voyage-3 | 103 fájlra overkill, embedding API cost, pgvector install szükséges |
| ChromaDB | Új Python process, VPS memória szűkös (8 GB) |
| sqlite-vec | Nincs előnye pgvector-ral szemben, de nincs PostgreSQL integráció sem |
| HTTP endpoint Orchestratoron | Claude Code MCP natívan támogatott, HTTP felesleges overhead |

---

## 7. Kockázatok

| Kockázat | Severity | Mitigation |
|---|---|---|
| FTS recall alacsony magyar+angol vegyes szövegenél | MEDIUM | `'simple'` dictionary (nem nyelv-specifikus), magyar stopword lista |
| `knowledge` schema RLS | LOW | Nem tenant-scoped adat — nincs RLS szükséglet, admin-only |
| Memória fájlok PII tartalma | MEDIUM | Memória fájlok review — nincs ügyféladat a terminál memóriákban |
| MCP server crash → scanner fallback | LOW | Fallback: scanner `grep` pattern maradt backup, graceful degradation |

---

## 8. v3 Security Review Findings

| ID | Súly | Terület | Probléma | Javítás | Státusz |
|---|---|---|---|---|---|
| SEC-P1 | MEDIUM | SQL injection | `rag-ingest.sh` bash psql UPSERT — fájl content injection kockázat | **Ingestion Node.js-ben** (pg parameterized queries), nem bash psql | ⚠️ OPEN → impl módosítás |
| SEC-P2 | LOW | Path traversal | `find` parancs path-jai | Fix hardcoded paths, nincs user input | ✅ ACCEPTED |
| SEC-P3 | LOW | PII exposure | Memória fájlok a DB-be kerülnek | Terminál memóriák technikai tartalmúak, nincs ügyféladat | ✅ ACCEPTED |
| SEC-P4 | LOW | MCP auth | MCP server ki hívhatja? | stdio transport, lokális process, nincs hálózati exposure | ✅ ACCEPTED |

**SEC-P1 hatása:** Az ingestion pipeline-t bash script helyett **Node.js script**-ként kell implementálni (pg client parameterized queries-szel). Ez az ORCH terminál scope-jába illik — a MCP server és az ingestion egy Node.js projektben élhet.

**Módosított implementációs sorrend:**

| Sorrend | Feladat | Terminál | Becsült nap |
|---|---|---|---|
| 1 | `spaceos_knowledge` DB + DDL | INFRA | 0.5 |
| 2 | Node.js ingestion script (pg parameterized) | ORCH | 1.5 |
| 3 | MCP server (`spaceos-knowledge`) | ORCH | 2 |
| 4 | MCP regisztráció + cron | INFRA | 0.5 |
| 5 | Scanner integration | INFRA | 0.5 |

---

## 9. Ami NEM része ennek a tervnek

- **Embedding / vector search** — Fázis 2, ha corpus >500 fájl
- **Chunk-olás** — Fázis 2, ha fájlméret >50 KB
- **Orchestrator API endpoint** — MCP interface elegendő
- **Real-time indexelés** — cron 5 óránként elég (nem high-frequency mutation)
- **Multi-tenant knowledge isolation** — a tudásbázis platform-szintű, nem tenant-scoped
