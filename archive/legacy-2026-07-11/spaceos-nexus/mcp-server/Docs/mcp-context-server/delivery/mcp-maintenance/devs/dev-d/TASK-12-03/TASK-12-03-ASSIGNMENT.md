---
title: "Dev D — TASK-12-03 Assignment Sheet"
subtitle: "ChromaDB Semantic Search — Vector Indexing + Embeddings"
created: 2026-03-08
updated: 2026-03-09
assigned_to: "Dev D"
priority: "P1"
epic: "EPIC-12"
phase: "M02 — Phase 1: Core Functionality"
status: "✅ READY (after TASK-12-01/02 complete)"
effort_estimate: "15-18 hours"
ac_count: 4
---

# 🚀 Dev D — TASK-12-03 Assignment

**Task:** TASK-12-03 (ChromaDB Semantic Search — Vector Indexing + Embeddings)
**Epic:** EPIC-12 (Episodic Memory Layer: Session storage + Semantic search)
**Phase:** M02 Phase 1 — Core Functionality
**Priority:** P1 (semantic understanding of past work)
**Effort Estimate:** 15-18 hours (2 days)
**Dependency:** Requires TASK-12-01 (`episodes` table data must exist)

---

## 🎯 ChromaDB Collection

> **⚠️ KRITIKUS TECHNIKAI FIGYELMEZTETÉSEK (létező kódbázis + package.json alapján):**
>
> 1. **LÉTEZŐ MINTA:** A `src/rag/VectorStore.ts` már implementálja a ChromaDB-t!
>    Használd template-ként — ugyanaz az API pattern kell ide is.
> 2. **Import:** `import { ChromaClient, Collection } from 'chromadb';` (már a package.json-ben: `chromadb@3.3.1`)
> 3. **Embedding model:** A projekt `GoogleGenerativeAIEmbeddings` használ (`gemini-embedding-001`),
>    NEM OpenAI-t! A dimenzionális méret NEM 1536 — a `gemini-embedding-001` 768 dimenz.
> 4. **Config:** A projekt `dotenv`-et használ (`.env` fájl), NEM `config.yaml`-t!
>    Lásd: `src/rag/VectorStore.ts` elején: `import * as dotenv from 'dotenv'; dotenv.config();`
> 5. **Docker:** `npm run start-db` indítja a ChromaDB-t (lásd `package.json` scripts)
> 6. **Fallback:** Ha nincs ChromaDB, a VectorStore.ts MemoryVectorStore-ra fall-back-el.
>    Az episodic search-nél is érdemes hasonló fallback pattern-t használni.

```typescript
// src/episodic/ChromaDBClient.ts
import { ChromaClient, Collection } from 'chromadb';
import { GoogleGenerativeAIEmbeddings } from '@langchain/google-genai';
import * as dotenv from 'dotenv';
import { Episode } from './types';

dotenv.config();

/**
 * ChromaDB semantic search az episodic memóriához.
 * ⚠️ KÖVESD a `src/rag/VectorStore.ts` mintáját!
 */
export class EpisodicChromaClient {
  private collection: Collection | null = null;
  private client: ChromaClient;
  private embeddings: GoogleGenerativeAIEmbeddings;

  constructor() {
    this.client = new ChromaClient(); // default: localhost:8000
    this.embeddings = new GoogleGenerativeAIEmbeddings({
      apiKey: process.env.GOOGLE_API_KEY,
      modelName: 'gemini-embedding-001',
    });
  }

  async init(): Promise<void> {
    try {
      await this.client.heartbeat(); // test connection
      this.collection = await this.client.getOrCreateCollection({
        name: 'mcp_episodes',
        metadata: { description: 'Episodic memory for MCP agent sessions' },
      });
    } catch (err) {
      console.warn('⚠️ [Episodic] ChromaDB not available, semantic search disabled');
      this.collection = null;
    }
  }

  async addEpisode(episode: Episode): Promise<void> {
    if (!this.collection) return;
    // ID: egyedi, determinisztikus
    const id = `episode_${episode.session_id}_${episode.sequence_number}`;
    await this.collection.add({
      ids: [id],
      documents: [episode.outcome_summary],
      metadatas: [{ domain: episode.domain, phase: episode.phase, session_id: episode.session_id }],
    });
  }

  async search(query: string, nResults = 10, threshold = 0.7): Promise<Episode[]> {
    if (!this.collection) return [];
    const results = await this.collection.query({
      queryTexts: [query],
      nResults,
    });
    // ⚠️ A distances/scores szűrése threshold alapján
    // ChromaDB distance: kisebb = jobb (cosine distance)
    // threshold konverzió: similarity = 1 - distance
    return []; // TODO: map results to Episode[]
  }
}
```

---

## Threshold Configuration

> **⚠️ A projekt `.env` fájlt használ, NEM `config.yaml`-t!**

```bash
# .env (már létező fájl — adj hozzá új változókat)
RAG_SIMILARITY_THRESHOLD=0.7
RAG_MAX_RESULTS=10
# GOOGLE_API_KEY már benne van (VectorStore.ts használja)
```

---

## A/B Test Report

```markdown
# Threshold Tuning Report

| Threshold | Precision | Recall | Use Case |
|:----------|:---------:|:------:|:--------:|
| 0.6 | 70% | 95% | Discovery (maximize findings) |
| 0.7 | 82% | 85% | Balanced (default) |
| 0.8 | 92% | 60% | Engineering (quality over quantity) |

**Recommendation:** Use 0.7 as default; domain-specific tuning encouraged
```

---

## ✅ 6 AC Checklist

- [ ] AC-1-3: ChromaDB collection + sync + latency <200ms
- [ ] AC-4: Semantic search working
- [ ] AC-5: Threshold configurable + validated
- [ ] AC-6: A/B test report + recommendation

---

**Status:** 🟡 **READY (after TASK-12-02)**
**Blocks:** TASK-12-04 (E2E validation)
