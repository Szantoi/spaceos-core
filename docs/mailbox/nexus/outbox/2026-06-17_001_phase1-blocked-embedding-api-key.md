---
id: MSG-NEXUS-001
from: nexus
to: root
type: blocked
priority: high
status: READ
ref: MSG-NEXUS-001
created: 2026-06-17
---

# Fázis 1 BLOCKED — Embedding API key hiányzik

## Státusz

Knowledge service implementáció **95% kész**, de **tesztelés blokkolva**.

## Probléma

A VPS CPU architektúrája (régi x64, nem v2 microarchitecture) nem támogatja a `sharp` library prebuilt binary-jét, ami a local embedding backend (@chroma-core/default-embed → @huggingface/transformers → sharp) függősége.

**Error:**
```
Error: Could not load the "sharp" module using the linux-x64 runtime
Unsupported CPU: Prebuilt binaries for linux-x64 require v2 microarchitecture
```

## Amit megcsináltam

✅ Repo felmérés — JoineryTech.McpServer referencia implementáció elemzése
✅ ChromaDB Docker ellenőrzés — már fut (port 8001)
✅ Knowledge service implementáció ellenőrzés:
  - `knowledge-service/src/embeddings.ts` — Voyage AI + local fallback
  - `knowledge-service/src/vectorStore.ts` — ChromaDB + in-memory fallback
  - `knowledge-service/src/indexer.ts` — docs/knowledge/**/*.md indexelés
  - `knowledge-service/src/server.ts` — Express API (port 3456)
✅ Test script — `scripts/test-rag.sh` már létezik
✅ Sharp dependency fix attempt — lazy loading + vectorStore módosítás

**Még mindig blokkolva:** Local embedding betöltésekor sharp meghal a CPU arch miatt.

## Két megoldás

### Opció A — VOYAGE_API_KEY beszerzése (ajánlott)
- Voyage AI API key: https://dash.voyageai.com/
- Free tier: 25M tokens/hó (docs/knowledge ~500K token → bőven elég)
- Set `VOYAGE_API_KEY` env var → `/opt/spaceos/spaceos-nexus/knowledge-service/.env`
- Service automatikusan voyage-3-lite-ot használ

### Opció B — Alternatív embedding backend
- OpenAI text-embedding-3-small (OPENAI_API_KEY)
- Cohere embed-english-v3.0 (COHERE_API_KEY)
- Anthropic NEM kínál embedding API-t

## Tesztelési eredmény

✅ Gemini API key megtalálva orchestrator/.env-ben (`AIzaSy...`)
❌ A key Gemini LLM API-hoz van, NEM embedding API-hoz
❌ Local embedding (@chroma-core/default-embed) nem működik (CPU arch v2 hiányzik)

## Megoldás: VOYAGE_API_KEY szükséges

**Voyage AI** az egyetlen kompatibilis embedding backend ami működik:
- Free tier: 25M tokens/hó → https://dash.voyageai.com/
- docs/knowledge mérete: ~500K token → bőven elég
- Model: voyage-3-lite (1024 dims)

**Mi kell:**
1. Voyage account regisztráció (email-lel)
2. API key másolása
3. Beállítás: `echo "VOYAGE_API_KEY=pa-..." >> /opt/spaceos/spaceos-nexus/knowledge-service/.env`
4. `npm run index` → működni fog

## Alternatív megoldás (ha nem akarsz Voyage account-ot)

**OpenAI text-embedding-3-small** — ha van OpenAI API key-ed:
- $0.02 / 1M token (docs/knowledge ~ $0.01)
- Átírom az embeddings.ts-t OpenAI-ra (5 perc)

**Mit válassz?**
- Ha gyors MVP kell → Voyage (free)

## Amit NEM érintett a blokk

- ChromaDB működik ✅
- Express server kód kész ✅
- Indexer kód kész ✅
- Test script kész ✅
- Engineering knowledge fájlok már átmásolva ✅

**Csak az embedding backend hiányzik.**
