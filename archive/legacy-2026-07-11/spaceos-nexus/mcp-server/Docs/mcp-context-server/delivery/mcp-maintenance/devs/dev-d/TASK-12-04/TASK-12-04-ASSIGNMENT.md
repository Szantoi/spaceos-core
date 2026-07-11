---
title: "Dev D — TASK-12-04 Assignment Sheet"
subtitle: "E2E Integration + Performance Benchmarks — Full Episodic Memory Validation"
created: 2026-03-08
updated: 2026-03-09
assigned_to: "Dev D"
priority: "P0"
epic: "EPIC-12"
phase: "M02 — Phase 1: Core Functionality"
status: "🔴 BLOCKED BY ALL PRIOR TASKS"
effort_estimate: "12-15 hours"
ac_count: 4
---

# 🚀 Dev D — TASK-12-04 Assignment

**Task:** TASK-12-04 (E2E Integration + Performance Benchmarks — Full Episodic Memory Validation)
**Epic:** EPIC-12 (Episodic Memory Layer: Session storage + Semantic search)
**Phase:** M02 Phase 1 — Core Functionality
**Priority:** P0 (final validation of entire memory system)
**Effort Estimate:** 12-15 hours (1.5 days)
**Dependencies:** Requires TASK-12-01, TASK-12-02, TASK-12-03 completion first

---

## 🎯 E2E Workflow

> **⚠️ KRITIKUS TECHNIKAI FIGYELMEZTETÉSEK:**
>
> 1. **`EpisodeStore` MÁR IMPLEMENTÁLVA:** Lásd `src/episodic/EpisodeStore.ts`
>    — ne írd újra, használd a meglévő `storeExperience()` metódust.
> 2. **SZINKRON SQLite API:** `better-sqlite3` szinkron — `storeExperience()` NEM async!
>    Csak a ChromaDB hívások asyncok.
> 3. **Test framework:** `vitest` (NEM Jest!) — lásd `package.json` scripts: `"test": "vitest"`
> 4. **E2E tests:** Playwright (`@playwright/test@1.58.2`) — lásd `playwright.config.ts`
> 5. **Episode types:** Lásd `src/episodic/types.ts` — `StoreExperienceParams`, `Episode`, `EpisodeDomain`, `EpisodePhase`

```typescript
// E2E workflow test (vitest + playwright)
import { EpisodeStore } from '../../src/episodic/EpisodeStore';
import { EpisodicChromaClient } from '../../src/episodic/ChromaDBClient';
import { StoreExperienceParams } from '../../src/episodic/types';
import Database from 'better-sqlite3';

// 1. SQLite rész SZINKRON (better-sqlite3)
const db = new Database(':memory:');
const store = new EpisodeStore(db);

// 2. Store 100 experience episodes (SZINKRON!)
for (let i = 0; i < 100; i++) {
  store.storeExperience({
    session_id: 'test-session-1',
    domain: 'engineering',
    phase: 'implementation',
    outcome_summary: `Implemented feature ${i}: database migration pattern`,
    tool_calls: [],
    artifacts: [],
    duration_ms: 1000,
  });
}

// 3. FTS5 search (SZINKRON!)
import { searchExperience } from '../../src/episodic/FtsSearch';
const ftsResults = searchExperience(db, 'database migration');

// 4. Hybrid search: FTS5 + ChromaDB (ChromaDB rész ASYNC)
const chroma = new EpisodicChromaClient();
await chroma.init();
const semanticResults = await chroma.search('database migration', 10, 0.7);

// 5. Merge + deduplicate results
assert(ftsResults.length > 0);
```

---

## Search Quality Rubric (5-Point Scale)

```markdown
# Search Quality Rubric

| Score | Criteria | Example |
|:-----:|:---------|:--------|
| **5** | Perfect | Query: "database", Result mentions "database migration" + context ✓ |
| **4** | Strong | Query: "error handling", Result: error handling code + related patterns ✓ |
| **3** | Moderate | Query: "testing", Result: mentions tests but tangential ✓ |
| **2** | Weak | Query: "performance", Result: mentions "fast" but no implementation detail ✗ |
| **1** | Not relevant | Query: "database", Result: random unrelated topic ✗ |

**Target: ≥80% scored 3+ (meaningful relevance)**
```

---

## Embedding Cache Implementation

> **⚠️ A projekt `node-cache@5.1.2`-t használ (`NodeCache`), NEM `lru-cache`-t!**
> Lásd: `src/mcp/RbacFilter.ts` — már használja a NodeCache-t (stdTTL: 1800, maxKeys: 50).

```typescript
// src/episodic/EmbeddingCache.ts
import NodeCache from 'node-cache';

export class EmbeddingCache {
  // Document cache: hosszabb TTL (7 nap), max 500 entry
  private documentCache = new NodeCache({ stdTTL: 7 * 24 * 60 * 60, maxKeys: 500 });
  // Query cache: rövidebb TTL (1 nap), max 500 entry
  private queryCache = new NodeCache({ stdTTL: 24 * 60 * 60, maxKeys: 500 });
  private metrics = { cacheHit: 0, cacheMiss: 0 };

  /**
   * ⚠️ Az embedding hívás ASYNC (Google Generative AI HTTP call),
   * de a cache lookup SZINKRON (NodeCache).
   */
  async getEmbedding(text: string, type: 'doc' | 'query'): Promise<number[]> {
    const cache = type === 'doc' ? this.documentCache : this.queryCache;

    const cached = cache.get<number[]>(text);
    if (cached) {
      this.metrics.cacheHit++;
      return cached;
    }

    // ⚠️ embedModel = GoogleGenerativeAIEmbeddings (gemini-embedding-001)
    // Lásd: src/rag/VectorStore.ts mintáját
    const embedding = await this.embedModel.embedQuery(text);
    cache.set(text, embedding);
    this.metrics.cacheMiss++;
    return embedding;
  }

  getMetrics() {
    const total = this.metrics.cacheHit + this.metrics.cacheMiss;
    return {
      hits: this.metrics.cacheHit,
      misses: this.metrics.cacheMiss,
      hitRate: total > 0 ? this.metrics.cacheHit / total : 0,
    };
  }
}
```

---

## ✅ 8 AC Checklist

- [ ] AC-1-4: E2E workflow + hybrid search + performance + filters
- [ ] AC-5: Embedding cache <500 entries, TTL configured
- [ ] AC-6: Quality rubric defined + documented
- [ ] AC-7-8: Precision ≥80%, recall 100%, kappa ≥0.8

---

**Status:** 🟡 **READY (after TASK-12-03)**
**Epic**: EPIC-12 Phase 1 COMPLETE
