# ADR-044: Knowledge Service System Integration

> **Státusz:** PROPOSED
> **Dátum:** 2026-06-17
> **Forrás:** MSG-ARCH-010 (Root konzultáció)
> **Tervdokumentum:** `docs/tasks/new/RAG_Knowledge_Base_v1.md`
> **Előfeltétel:** Nexus Phase 1 COMPLETE ✅

---

## Kontextus

A SpaceOS terminálok és planning pipeline szegmentáltan olvasnak dokumentációt:
- Nincs cross-szegmens szemantikus keresés
- Terminálok hideg indításkor sok fájlt olvasnak (context window töltés)
- Librarian knowledge fájlok szétszórtan használtak
- Planning scanner Haiku nem tud korábbi döntéseket keresni

**Jelenlegi állapot (Phase 1 COMPLETE):**
- Knowledge Service operational (port 3456)
- ChromaDB + Voyage AI embeddings
- Systemd production deployment
- Librarian cron reindex trigger
- Haiku scanner `discovery_search` tool bekötve

**Hiányzik:**
- System-wide terminál integráció
- Architect tool access
- Full Datahaven/Resonance vision

---

## Döntés

**Knowledge Service scale-up a full Datahaven/Resonance foundation-hez.**

### Integráció pontok

| Komponens | Integráció | Státusz |
|---|---|---|
| **Haiku scanner** | `discovery_search` MCP tool | ✅ COMPLETE |
| **Librarian** | Reindex trigger (pipeline.sh) | ✅ COMPLETE |
| **Architect** | Knowledge query tool (ADR/pattern keresés) | ⚠️ PENDING |
| **Terminálok** | Cold-start context enrichment | ⚠️ PENDING |
| **Planning selector** | Knowledge-aware WSJF scoring | ⚠️ PENDING |

---

## Alternatívák értékelése

| Alternatíva | Értékelés | Miért nem |
|---|---|---|
| **PostgreSQL tsvector FTS** | ❌ SUPERSEDED | RAG_Knowledge_Base_v1 eredetileg ezt javasolta. ChromaDB + embeddings bizonyult jobbnak. |
| **ChromaDB + Voyage AI** | ✅ VÁLASZTOTT | Production-tested JoineryTech.McpServer referencia. In-memory fallback. |
| **pgvector** | ❌ ELVETETT | Nem telepített, extra dependency. ChromaDB Docker simpler. |
| **SQLite-vec** | ❌ ELVETETT | Nincs előnye ChromaDB-vel szemben, de nincs PostgreSQL integráció sem. |

---

## Indoklás

### 1. Szemantikus keresés vs keyword FTS

```
Query: "Mi volt az RLS döntés a tenant isolation-ra?"

FTS (tsvector):  @@ to_tsquery('RLS & tenant & isolation')
                 → Exact keyword match required

Embedding:       cosine_similarity(query_vec, doc_vec)
                 → Semantic: "row level security", "multi-tenant", "policy" is találat
```

103 fájlnál az FTS elég lenne, de a knowledge base növekedésével (>500 fájl) a szemantikus keresés kritikus.

### 2. In-memory fallback (graceful degradation)

```typescript
// knowledge-service/src/vectorStore.ts
async search(query: string): Promise<SearchResult[]> {
  try {
    return await this.chromaDB.search(query);
  } catch (error) {
    console.warn('ChromaDB unavailable, using in-memory fallback');
    return this.inMemorySearch(query);
  }
}
```

Ha ChromaDB leáll, a service működik — headless scanner nem blokkolódik.

### 3. Librarian cron integration

```bash
# pipeline.sh - Librarian DONE után
if [[ "$TERMINAL" == "librarian" ]]; then
  /opt/spaceos/scripts/pipeline-knowledge-index.sh
fi
```

Knowledge sync után automatikus reindex — nincs manuális trigger.

---

## Golden Rule ellenőrzés

| Szabály | Ellenőrzés | Státusz |
|---|---|---|
| **Data → Rules → Geometry** | Knowledge Service csak keresés — nem számol, nem módosít | ✅ OK |
| **Modular Monolith** | Önálló service (port 3456), MCP interface, decoupled | ✅ OK |
| **Immutability & Trust** | Embedding vectors read-only, source files immutable (git) | ✅ OK |
| **Need-to-Know RBAC** | RbacFilter Fázis 3-ban — tool visibility per role | ⚠️ PENDING |
| **Walking Skeleton First** | Phase 1 COMPLETE, incrementally adding integrations | ✅ OK |

---

## Kritikus függőségek

| Függőség | Blokkoló? | Státusz |
|---|---|---|
| ChromaDB Docker | **HIGH** | ✅ Fut (`spaceos_chromadb` container, port 8001) |
| VOYAGE_API_KEY | **HIGH** | ✅ Configured (`/etc/spaceos/knowledge.env`) |
| Systemd service | **HIGH** | ✅ Active (`spaceos-knowledge.service`) |
| Librarian cron | MEDIUM | ✅ Integrated (`pipeline-knowledge-index.sh`) |

---

## Fázis lebontás

### Fázis 1: Core Implementation (COMPLETE ✅)
- [x] JoineryTech.McpServer klónozás
- [x] Voyage AI embedding integration
- [x] ChromaDB Docker service
- [x] HTTP API (search, index, health)
- [x] Systemd production deployment
- [x] Librarian cron reindex
- [x] Haiku scanner tool

### Fázis 2: System-wide Integration (~3-4 nap)
- [ ] Architect MCP tool bekötés (ADR/pattern keresés)
- [ ] Terminál cold-start hook (CLAUDE.md kiegészítés)
- [ ] Planning selector knowledge-aware scoring
- [ ] Expanded documentation indexing (mailbox/outbox archive)

### Fázis 3: Full Datahaven/Resonance (~5-6 nap, Slice 2)
- [ ] Episodic memory indexing (terminál session highlights)
- [ ] Cross-terminál context sharing
- [ ] Knowledge quality scoring (freshness, relevance)
- [ ] Admin dashboard (indexed docs, query stats)

---

## Technikai specifikáció

### Endpoints
```
GET  /health                           → { status, documents, lastIndex }
GET  /api/knowledge/search?q=...       → SearchResult[]
POST /api/knowledge/search             → { q, topK?, category? }
POST /api/knowledge/index              → { success, documents, duration }
```

### Embedding
- **Model:** Voyage AI voyage-3-lite (512 dim)
- **Fallback:** Google Gemini text-embedding-004
- **Local fallback:** TF-IDF similarity (degraded mode)

### Vector Store
- **Primary:** ChromaDB (Docker, port 8001)
- **Collection:** `spaceos_knowledge`
- **Distance:** Cosine similarity

### Source directories
```
docs/knowledge/**/*.md          → Core knowledge base
docs/mailbox/*/archive/*.md     → Historical decisions (Phase 2)
~/.claude/projects/*/memory/    → Episodic memory (Phase 3)
```

---

## Rollback terv

```bash
# Knowledge service leállítása
systemctl stop spaceos-knowledge

# ChromaDB leállítása
docker stop spaceos_chromadb

# Scanner/terminálok: fallback grep/read működik
# Nincs hard dependency
```

A Knowledge Service **optional enhancement** — terminálok működnek nélküle is (degraded mode).

---

## Megvalósító terminál

**NEXUS** (spaceos-nexus/) — Knowledge Service fejlesztés
- API bővítések
- New tool integrations

**LIBRARIAN** — Knowledge base curáció
- Document quality
- Index coverage

**ROOT** — Terminál CLAUDE.md kiegészítések
- Cold-start hooks
- Tool registration

---

## Referenciák

- Tervdokumentum: `docs/tasks/new/RAG_Knowledge_Base_v1.md`
- Agent Infrastructure Roadmap: `docs/agent-infrastructure/ROADMAP.md`
- Knowledge Service README: `spaceos-nexus/knowledge-service/README.md`
- Phase 1 DONE: MSG-NEXUS-001
