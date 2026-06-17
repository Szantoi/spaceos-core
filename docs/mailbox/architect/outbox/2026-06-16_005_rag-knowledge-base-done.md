---
id: MSG-ARCH-005-DONE
from: architect
to: root
type: done
priority: medium
status: READ
ref: MSG-ARCH-005
created: 2026-06-16
---

## Összefoglaló

RAG tudásbázis tervdokumentum elkészült. **Kulcs döntés: PostgreSQL tsvector FTS + MCP tool**, NEM vector embedding.

### Indoklás
A corpus extrém kicsi: **103 fájl, 556 KB.** Erre a méretre:
- pgvector/ChromaDB/sqlite-vec overkill
- Embedding API cost indokolatlan
- VPS 8 GB szűkös — nincs hely ChromaDB processznek
- FTS keyword recall ~90%+ ezen a corpuson

### Stack döntés
```
Adattárolás:  PostgreSQL tsvector (meglévő DB, 0 új service)
Ingestion:    rag-ingest.sh cron (5 óránként, inkrementális SHA-256 hash)
Interface:    MCP tool (spaceos-knowledge) — stdio transport
Upgrade path: pgvector column ha corpus >500 fájl
```

### Implementációs sorrend
1. `knowledge` schema DDL (INFRA, 0.5 nap)
2. `rag-ingest.sh` + cron (INFRA, 1 nap)
3. MCP server Node.js (ORCH, 2 nap)
4. Claude settings MCP regisztráció (INFRA, 0.5 nap)
5. Scanner integration (INFRA, 0.5 nap)

**Teljes: ~4-5 nap · ADR-040 proposed**

## Output fájl

`docs/tasks/new/RAG_Knowledge_Base_v1.md`

## Kockázatok / kérdések

- FTS recall magyar+angol vegyes szövegenél — `'simple'` dictionary mitigation
- Memória fájlok PII review szükséges mielőtt indexelés indul
- Státusz: DRAFT v1 — DB review (v2) szükséges a DDL véglegesítéséhez
