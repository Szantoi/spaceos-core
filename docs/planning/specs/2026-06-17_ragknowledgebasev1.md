---
id: SPEC-003
source: /opt/spaceos/docs/mailbox/architect/outbox/2026-06-16_005_rag-knowledge-base-done.md
type: Architecture spec & Implementation plan
scope: [infra, orch]
priority: high
complexity: 3
dependencies: [PostgreSQL kernel, knowledge base content ingestion]
status: NEW
created: 2026-06-17
---

# RAG Knowledge Base — PostgreSQL FTS + MCP

## Összefoglaló

PostgreSQL tsvector-alapú full-text search (FTS) implementálása a tudásbázishoz. A megoldás MCP tool-ként (stdio transport) érhetősége az LLM agenteknek. Kis corpus (103 fájl, 556 KB) — FTS elég, vector embedding túlkapacitás.

## Scope

- **INFRA:** PostgreSQL DDL, rag-ingest.sh cron ingestion, Scanner integrációs hook
- **ORCH:** MCP server Node.js implementáció (spaceos-knowledge)

Érinti: Librarian, Root, összes terminál (tudásbázis access).

## Stack döntés

```
Adattárolás:    PostgreSQL tsvector (meglévő DB, 0 új service)
Ingestion:      rag-ingest.sh cron (5 óránként, inkrementális SHA-256 hash)
Interface:      MCP tool (spaceos-knowledge) — stdio transport
Upgrade path:   pgvector column ha corpus >500 fájl
```

### Miért FTS, nem embedding?

- Corpus mérete: **103 fájl, 556 KB** — vector embedding költsége indokolatlan
- VPS memória szűke: 8 GB, ChromaDB saját process — FTS egyebe áll a meglévő DB-ben
- Keyword recall: ~90%+ ezen a méreten — elegendő domain searchhez
- Cost: 0 (postgres builtin), vs. API cost (OpenAI embedding: $0.02/1M tokens)

## Implementációs sorrend

1. **knowledge schema DDL** (INFRA, 0.5 nap)
   - `documents` tábla: path, content, tsvector_index, content_hash (SHA-256), ingested_at
   - Trigger: auto-update tsvector mikor content változik

2. **rag-ingest.sh + cron** (INFRA, 1 nap)
   - Inkrementális scan: docs/ alatt új/módosított fájlok (SHA-256 hash alapján)
   - 5 óránkénti futás
   - Memória fájlok kizárása (PII review szükséges v2-ben)

3. **MCP server Node.js** (ORCH, 2 nap)
   - `search` tool: FTS query → top N dokumentum + snippet
   - `get_document` tool: teljes dokumentum fetch
   - stdio transport (claude CLI-hez direkt csatolható)

4. **Claude settings MCP regisztráció** (INFRA, 0.5 nap)
   - ~/.claude/mcp.json vagy settings.json módosítás
   - spaceos-knowledge server indítás

5. **Scanner integration** (INFRA, 0.5 nap)
   - nightwatch.sh: rag-ingest trigger ellenőrzés
   - Failover logging (ha ingest meghiúsul, Root notifikáció)

**Teljes: ~4-5 nap**

## Kockázatok

| Kockázat | Mitigáció |
|---|---|
| FTS recall magyar+angol vegyes szövegenél | `'simple'` dictionary config, corpus tanulás |
| Memória fájlok (MEMORY.md, memory/*.md) PII/sensitive info | v2: explicit whitelist, scanner hook |
| Hosszú szövegek (50 KB+) indexelési lassúsága | Partial indexing (headers + eleje), v1 teszt |
| Corpus nővekedés (>500 fájl) — FTS limit | pgvector upgrade path már tervben (ADR-040) |

## Döntések & ADR

- **ADR-040 proposed:** FTS vs. vector embedding trade-off, upgrade path
- **v2 szükséges:** PII review memory fájlokra, retention policy

## Eredeti dokumentum

`/opt/spaceos/docs/mailbox/architect/outbox/2026-06-16_005_rag-knowledge-base-done.md`
