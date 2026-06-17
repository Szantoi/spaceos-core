---
id: MSG-NEXUS-001
from: root
to: nexus
type: task
priority: high
status: READ
model: sonnet
created: 2026-06-16
---

# Nexus — Fázis 1: McpServer Knowledge Service setup

## Kontextus

A JoineryTech.McpServer repo már klónozva van:
```
/opt/spaceos/spaceos-nexus/mcp-server/
```

Ez a referencia implementáció. A céd: SpaceOS-ra adaptálni knowledge service-ként.

## Feladatod — Fázis 1

### 1. Repo felmérés

Olvasd el:
- `mcp-server/src/rag/VectorStore.ts` — embedding + ChromaDB kapcsolat
- `mcp-server/src/rag/indexKnowledgeBase.ts` — indexelési logika
- `mcp-server/package.json` — dependenciák, scripts
- `mcp-server/database/knowledge/engineering/` — a tudásbázis tartalom

### 2. Adaptációs terv

Azonosítsd mi kell megváltoztatni:

**Embedding csere:**
- Jelenleg: Google Gemini `gemini-embedding-001`
- Kell: Anthropic voyage-3-lite (`https://api.anthropic.com/v1/messages` — voyage API)
- Env var: `ANTHROPIC_API_KEY` (már elérhető a VPS-en)

**Forrás fájlok:**
- Jelenleg: `database/*.knowledge.md`
- Kell: `/opt/spaceos/docs/knowledge/**/*.md` (minden `.md`, nem csak `.knowledge.md`)

**SpaceOS engineering knowledge import:**
- `mcp-server/database/knowledge/engineering/*.knowledge.md` fájlok
- Ezek SpaceOS stackre íródtak (.NET, EF Core, React, testing)
- Másold át: `/opt/spaceos/docs/knowledge/engineering/`

### 3. Minimális működő verzió

Cél: `POST /api/knowledge/search` endpoint ami:
- Beolvassa a `docs/knowledge/` fájlokat
- Voyage embedding-gel indexeli ChromaDB-be
- Szemantikus kérdésre releváns chunk-okat ad vissza

### 4. ChromaDB

Ellenőrizd van-e Docker a VPS-en:
```bash
docker --version
docker ps
```

Ha igen: `docker-compose.yml` a `spaceos-nexus/` gyökerében.
Ha nem: sqlite-vec fallback (a VectorStore-ban már van MemoryVectorStore fallback).

## Amit NEM kell most csinálni

- Marvin integráció (Fázis 2)
- RBAC, GuardrailService, WorkflowStateTracker (Fázis 3)
- A meglévő bash pipeline-t ne nyúlj hozzá

## Output

DONE outbox amikor:
1. A knowledge fájlok indexelhetők (tesztelve legalább 5 docs/knowledge/ fájllal)
2. `POST /api/knowledge/search?q=EF+Core+migration` visszaad releváns chunk-ot
3. `docs/agent-infrastructure/ROADMAP.md` Fázis 1 checkbox-ok pipellanálva

Legyen egy `spaceos-nexus/scripts/test-rag.sh` ami demonstrálja a működést.
