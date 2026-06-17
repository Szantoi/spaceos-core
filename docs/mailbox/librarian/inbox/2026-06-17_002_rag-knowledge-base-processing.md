---
id: MSG-LIBRARIAN-002
from: root
to: librarian
type: task
priority: high
status: UNREAD
model: sonnet
ref: MSG-CONDUCTOR-005
created: 2026-06-17
---

# RAG Knowledge Base — Tudásbázis szintetizálása & indexelés

## Felkérés

A **PHASE 3** infrastruktúra rész (Datahaven/Resonance foundation) a **Knowledge Service** teljes integrációját igényli.

**Feladat:** `docs/tasks/new/RAG_Knowledge_Base_v1.md` feldolgozása és **docs/knowledge/** szintetizálása.

---

## Kontextus

### Nexus Phase 1 ✅ COMPLETE
- ChromaDB service futó (port 8001)
- Knowledge service operational (port 3456)
- Voyage AI embeddings akktív
- Endpoints: GET/POST /api/knowledge/search, POST /api/knowledge/index
- Systemd deployment kész: `/etc/systemd/system/spaceos-knowledge.service`

### Fájl hely
- **Tervdoc:** `/opt/spaceos/docs/tasks/new/RAG_Knowledge_Base_v1.md`
- **Tudásbázis:** `/opt/spaceos/docs/knowledge/`
- **Indexer script:** `/opt/spaceos/spaceos-nexus/knowledge-service/src/indexer.ts`

---

## Librarian feladat

### 1. RAG_Knowledge_Base_v1.md feldolgozása

Olvasd el és elemezd:
- Mely dokumentum kategóriákat javasol indexelni?
- Milyen szintetizálási sorrend szükséges (security → patterns → architecture → context)?
- Mely meglévő docs/knowledge/ fájlokkal lehet összekapcsolni?

### 2. docs/knowledge/ szintetizálása

**Aktuális **docs/knowledge/** tartalom:**
```
docs/knowledge/
  INDEX.md                      ← TELJES overview (frissítés szükséges)
  security/
    SECURITY_PATTERNS.md
    SECURITY_DECISIONS.md
  deployment/
    DEPLOYMENT_RUNBOOK.md
    KNOWN_GOTCHAS.md
  patterns/
    DEV_DIFFICULTIES.md
    DATABASE_PATTERNS.md
    TESTING_PATTERNS.md
  architecture/
    ADR_CATALOGUE.md
    API_CONTRACT_CATALOGUE.md
    MODULE_BOUNDARIES.md
  context/
    KERNEL_CONTEXT.md
    ORCH_CONTEXT.md
    PORTAL_CONTEXT.md
    JOINERY_CONTEXT.md
    CUTTING_CONTEXT.md
    INFRA_CONTEXT.md
    E2E_CONTEXT.md
```

**Szükséges frissítések:**
- [ ] INDEX.md — PHASE 2 COMPLETE referencia hozzáadása
- [ ] DEPLOYMENT_RUNBOOK.md — Nexus Knowledge Service aktiválása (MSG-ROOT-011)
- [ ] KNOWN_GOTCHAS.md — Knowledge Service indexing best practices
- [ ] ADR_CATALOGUE.md — ADR-043, ADR-044, ADR-045 előzetes placeholder

### 3. Indexer futtatás előkészítése

**VPS aktiváció után szükséges:**

```bash
# Root SSH: gabor@109.122.222.198
# 1. VOYAGE_API_KEY beállítása
cd /opt/spaceos/spaceos-nexus/knowledge-service
npm run build
npm run index

# 2. Teszt
curl http://localhost:3456/api/knowledge/search?q=kernel&topK=5
# Expected: 5+ dokumentum relevancia ponttal
```

### 4. Feldolgozási napló

**Dokumentáció után:** add hozzá a feldolgozási naplóhoz:

```markdown
# PROCESSED_LOG — 2026-06-17

## RAG_Knowledge_Base_v1.md

**Szerző:** Architect
**Típus:** Infrastructure (Knowledge Service)
**Status:** PROCESSED (Librarian feldolgozás)

**Szintetizált:**
- docs/knowledge/INDEX.md v2 (Nexus Phase 1 integráció)
- docs/knowledge/deployment/DEPLOYMENT_RUNBOOK.md (Knowledge Service lépések)
- docs/knowledge/deployment/KNOWN_GOTCHAS.md (Indexing patterns)

**Kontextus update:**
- INFRA_CONTEXT.md — Knowledge Service / Datahaven

**Indexer status:** PENDING (Root VPS aktiválás után)
```

---

## Root időterv

- **2026-06-17 vége:** Librarian feldolgozás + docs/knowledge/ szintetizálása
- **2026-06-18 reggel:** VPS aktiválás + indexer futás
- **2026-06-18 délután:** Knowledge Service full operational, Haiku scanner live

---

## Golden Rule ellenőrzés

A szintetizálás során:

| Szabály | Ellenőrzés |
|---|---|
| **Data → Rules → Geometry** | Indexer csak FTS/vector embedding végez, logika nélkül? |
| **Modular Monolith** | Knowledge Service független a Kernel-től? |
| **Immutability & Trust** | Indexed documents immutable, audit trail? |
| **Need-to-Know RBAC** | RbacFilter: mely terminálok látják mely dokumentumokat? |
| **Walking Skeleton First** | E2E: search → (relevance) → result? |

---

## Státusz & Továbbhaladás

**Librarian GO/NO-GO:**
- [ ] RAG_Knowledge_Base_v1.md feldolgozás
- [ ] docs/knowledge/ szintetizálása
- [ ] Feldolgozási napló update
- [ ] Indexer mock-teszt (dev mód)

**Root megvárja a Librarian DONE-t** 2026-06-17 vége előtt.

---

*Root megjegyzés: Ez nem tervdoc-skrij, hanem tudásbázis integrálás. A Librarian terminál által végzendő tudásbázis gondozás feladat.*
