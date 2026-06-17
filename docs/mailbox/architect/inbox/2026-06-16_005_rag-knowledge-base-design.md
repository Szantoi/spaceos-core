---
id: MSG-ARCH-005
from: root
to: architect
type: task
priority: medium
status: READ
model: opus
created: 2026-06-16
---

# Architect — RAG tudásbázis tervezés

## Kontextus

A SpaceOS autonóm tervezési pipeline-ja (Haiku scanner → Sonnet → 2×Sonnet debate → Architect)
jelenleg flat markdown fájlokat olvas szegmentáltan. A `docs/knowledge/` mappa és a terminál
memóriák (`/home/gabor/.claude/projects/*/memory/`) szemantikusan nem kereshetők —
a Haiku rotálva olvassa őket, de nem tud cross-szegmens összefüggéseket keresni.

**Cél:** RAG (Retrieval-Augmented Generation) réteg a meglévő tudásbázis fölé,
hogy a scanner és a többi agent szemantikus kérdéseket tehessen fel:
- *"Mi van már megoldva az inventory területen?"*
- *"Milyen FSM döntések születtek eddig?"*
- *"Melyik terminál ütközött már ezzel a problémával?"*

## Meglévő stack (amire illeszkednie kell)

```
Backend:   .NET 8 + PostgreSQL (van pgvector extension?)
Orch:      Node.js 22 — AI gateway, már hív Anthropic API-t
Librarian: claude haiku session — ő szintetizálja a docs/knowledge/-t
Scanner:   claude -p haiku (headless, --no-session-persistence)
```

Tudásbázis forrásai (ingestion):
- `docs/knowledge/**/*.md` — Librarian által karbantartott
- `/home/gabor/.claude/projects/*/memory/*.md` — terminál memóriák

## Kérdések amiket a tervnek meg kell válaszolnia

1. **Embedding store** — melyik illeszkedik legjobban?
   - pgvector a meglévő Postgresbe (nincs új service)
   - ChromaDB (könnyű, Python, de új process)
   - sqlite-vec (egyszerű, fájl alapú, headless-friendly)
   - Más?

2. **Embedding model** — Anthropic `voyage-3` / `voyage-3-lite`, vagy OpenAI ada-002?
   Tradeoff: cost vs. quality vs. latency a headless scan futásokban

3. **Ingestion pipeline** — ki tölti fel és mikor?
   - Librarian (már 5 óránként fut) → minden `docs/knowledge/` változás után?
   - Külön `rag-ingest.sh` cron?
   - Inkrementális (csak módosult fájlok)?

4. **Query interface** — hogyan hívja a Haiku scanner?
   - HTTP endpoint az Orchestratoron? (`POST /rag/query`)
   - Közvetlen DB query (psql)?
   - MCP tool?

5. **Memória fájlok kezelése** — a terminál memóriák (`/home/gabor/.claude/projects/*/memory/`)
   belekerüljenek-e a RAG-ba, vagy csak a `docs/knowledge/`?
   A memória fájlok volatilisek (Librarian törölheti őket) — ez hogyan kezelendő?

## Elvárt output

Tervdokumentum (`docs/tasks/new/RAG_Knowledge_Base_v1.md`) ami tartalmazza:
- Ajánlott stack döntés indoklással
- Ingestion pipeline vázlat (ki, mikor, hogyan)
- Query interface spec (mit küld a scanner, mit kap vissza)
- Implementációs sorrend (mi blokkolja mit)
- Melyik terminál(ok) valósítják meg

DONE outbox-ban jelezd mikor kész a tervdok.
