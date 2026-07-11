---
id: MSG-LIBRARIAN-005-RAG-IMPL
from: librarian
to: root
type: done
priority: high
status: READ
ref: MSG-LIBRARIAN-005
created: 2026-06-24
content_hash: c28573426ffde048eba2664c628decfb0b38dc61ea0d6de64db461a5c820d649
---

# RAG Metadata Enhancement — Implementation Guide

## Executive Summary

A ChromaDB-ben tárolt dokumentumokhoz új metadata mezők hozzáadása az ADR-048 projekt-szintű tiered context támogatásához.

**Célok:**
1. **Projekt-szintű filtering** — csak releváns docs a cold start-hoz
2. **Időbeli filtering** — HOT/WARM/COLD tier alapján
3. **Prioritás filtering** — kritikus vs opcionális docs
4. **Automatikus tier inference** — git history + file mtime alapján

---

## 1. Új Metadata Mezők

### Jelenlegi Schema (spaceos-nexus/knowledge-service)

```typescript
// src/vectorStore.ts - addDocuments()
await collection.add({
  ids: [docId],
  documents: [content],
  metadatas: [{
    source: filePath  // CSAK EZ VAN MOST
  }]
});
```

### Javasolt Bővített Schema

```typescript
interface DocumentMetadata {
  // ✅ Meglévő
  source: string;  // "docs/knowledge/patterns/DATABASE_PATTERNS.md"

  // 🆕 Új mezők (ADR-048)
  project: ProjectType;       // 'general' | 'cutting' | 'portal' | 'kernel' | 'joinery' | 'nexus' | 'identity' | 'inventory'
  tier: TierType;             // 'hot' | 'warm' | 'cold' | 'shared'
  created_date: string;       // "2026-04-15" (git first commit)
  last_updated: string;       // "2026-06-22" (git last commit or file mtime)
  priority: PriorityType;     // 'critical' | 'high' | 'medium' | 'low'
  category: CategoryType;     // 'architecture' | 'patterns' | 'deployment' | 'security' | 'debugging' | 'context' | 'engineering'
}

type ProjectType = 'general' | 'cutting' | 'portal' | 'kernel' | 'joinery' | 'nexus' | 'identity' | 'inventory';
type TierType = 'hot' | 'warm' | 'cold' | 'shared';
type PriorityType = 'critical' | 'high' | 'medium' | 'low';
type CategoryType = 'architecture' | 'patterns' | 'deployment' | 'security' | 'debugging' | 'context' | 'engineering' | 'graph' | 'datahaven' | 'market' | 'reading-list';
```

---

## 2. Metadata Extraction Logic

### 2.1 File Path Parsing

```typescript
// src/indexer.ts — extractMetadata() új függvény

function extractMetadata(filePath: string, content: string): DocumentMetadata {
  // Category inference (folder name)
  const parts = filePath.split('/');
  const folderIdx = parts.indexOf('knowledge') + 1;
  const category = parts[folderIdx] || 'unknown';

  // Last updated (git or file mtime)
  const lastUpdated = getLastModifiedDate(filePath);
  const created = getCreatedDate(filePath);

  // Tier inference (time-based)
  const tier = inferTier(lastUpdated);

  // Project inference (content heuristics)
  const project = inferProject(filePath, content);

  // Priority inference (category + tier)
  const priority = inferPriority(category, tier);

  return {
    source: filePath,
    project,
    tier,
    created_date: created,
    last_updated: lastUpdated,
    priority,
    category: category as CategoryType
  };
}
```

### 2.2 Git History Helpers

```typescript
// src/indexer.ts — git wrapper functions

import { execSync } from 'child_process';
import * as fs from 'fs';

function getLastModifiedDate(filePath: string): string {
  try {
    // Try git first
    const gitDate = execSync(`git log -1 --format=%ai "${filePath}"`, {
      cwd: '/opt/spaceos',
      encoding: 'utf-8'
    }).trim();

    if (gitDate) {
      return gitDate.split(' ')[0]; // "2026-06-22"
    }
  } catch (err) {
    // Fallback to file mtime
    const stats = fs.statSync(filePath);
    return stats.mtime.toISOString().split('T')[0];
  }
}

function getCreatedDate(filePath: string): string {
  try {
    const gitDate = execSync(`git log --reverse --format=%ai "${filePath}" | head -1`, {
      cwd: '/opt/spaceos',
      encoding: 'utf-8'
    }).trim();

    if (gitDate) {
      return gitDate.split(' ')[0];
    }
  } catch (err) {
    // Fallback to file birthtime (unreliable on Linux)
    const stats = fs.statSync(filePath);
    return stats.birthtime.toISOString().split('T')[0];
  }
}
```

### 2.3 Tier Inference (Time-Based)

```typescript
function inferTier(lastUpdated: string): TierType {
  const lastUpdateDate = new Date(lastUpdated);
  const now = new Date();
  const daysSinceUpdate = (now.getTime() - lastUpdateDate.getTime()) / (1000 * 60 * 60 * 24);

  if (daysSinceUpdate <= 2) return 'hot';        // 48 hours
  if (daysSinceUpdate <= 14) return 'warm';      // 2 weeks
  if (daysSinceUpdate <= 90) return 'cold';      // 3 months
  return 'shared';                                // Timeless (architectural foundations)
}
```

### 2.4 Project Inference (Content Heuristics)

```typescript
function inferProject(filePath: string, content: string): ProjectType {
  // File path heuristics
  if (filePath.includes('cutting') || filePath.includes('CUTTING')) return 'cutting';
  if (filePath.includes('portal') || filePath.includes('PORTAL')) return 'portal';
  if (filePath.includes('datahaven') || filePath.includes('nexus') || filePath.includes('NEXUS')) return 'nexus';
  if (filePath.includes('kernel') || filePath.includes('KERNEL')) return 'kernel';
  if (filePath.includes('joinery') || filePath.includes('JOINERY')) return 'joinery';

  // Content heuristics (keyword matching)
  const lowerContent = content.toLowerCase();
  if (lowerContent.includes('cutting') && lowerContent.includes('nesting')) return 'cutting';
  if (lowerContent.includes('portal') && lowerContent.includes('react')) return 'portal';
  if (lowerContent.includes('nexus') || lowerContent.includes('agent infrastructure')) return 'nexus';
  if (lowerContent.includes('kernel') && lowerContent.includes('audit')) return 'kernel';
  if (lowerContent.includes('joinery') && lowerContent.includes('bom')) return 'joinery';

  // Default to general
  return 'general';
}
```

### 2.5 Priority Inference

```typescript
function inferPriority(category: string, tier: TierType): PriorityType {
  // Critical: deployment, security (any tier)
  if (category === 'deployment' || category === 'security') return 'critical';

  // HOT tier patterns → high priority
  if (tier === 'hot' && category === 'patterns') return 'high';

  // WARM tier architecture → critical
  if (tier === 'warm' && category === 'architecture') return 'critical';

  // COLD tier → medium (unless security/deployment)
  if (tier === 'cold') return 'medium';

  // Default
  return 'medium';
}
```

---

## 3. Implementation Steps

### Step 1: Update TypeScript Interfaces

**File:** `src/vectorStore.ts`

```typescript
export interface DocumentMetadata {
  source: string;
  project: ProjectType;
  tier: TierType;
  created_date: string;
  last_updated: string;
  priority: PriorityType;
  category: CategoryType;
}

export type ProjectType = 'general' | 'cutting' | 'portal' | 'kernel' | 'joinery' | 'nexus' | 'identity' | 'inventory';
export type TierType = 'hot' | 'warm' | 'cold' | 'shared';
export type PriorityType = 'critical' | 'high' | 'medium' | 'low';
export type CategoryType = 'architecture' | 'patterns' | 'deployment' | 'security' | 'debugging' | 'context' | 'engineering' | 'graph' | 'datahaven' | 'market' | 'reading-list';
```

### Step 2: Modify Indexer

**File:** `src/indexer.ts`

```typescript
import { extractMetadata, getLastModifiedDate, getCreatedDate, inferTier, inferProject, inferPriority } from './metadataUtils';

// In indexDocuments() function
for (const file of files) {
  const content = fs.readFileSync(file, 'utf-8');
  const metadata = extractMetadata(file, content);

  await vectorStore.addDocuments([{
    pageContent: content,
    metadata: metadata
  }]);
}
```

### Step 3: Create Metadata Utils Module

**New File:** `src/metadataUtils.ts`

```typescript
// Full implementations of extractMetadata, inferTier, inferProject, inferPriority
// (see section 2.2-2.5 above for complete code)
```

### Step 4: Update API Query Interface

**File:** `src/server.ts`

```typescript
// GET /api/knowledge/search (extended query params)
app.get('/api/knowledge/search', async (req, res) => {
  const { q, topK = 5, project, tier, priority, category } = req.query;

  const results = await vectorStore.similaritySearch(q, topK, {
    project,
    tier: tier ? tier.split(',') : undefined,  // Allow multiple: ?tier=hot,warm
    priority,
    category
  });

  res.json({ results });
});
```

### Step 5: ChromaDB Collection Migration

**Migration Strategy:**

```typescript
// src/migrations/001-add-metadata-fields.ts

export async function migrateMetadata() {
  console.log('Starting metadata migration...');

  const collection = await chromaClient.getCollection({ name: 'spaceos-knowledge' });
  const allDocs = await collection.get();

  for (let i = 0; i < allDocs.ids.length; i++) {
    const docId = allDocs.ids[i];
    const source = allDocs.metadatas[i].source;
    const content = allDocs.documents[i];

    // Extract new metadata
    const newMetadata = extractMetadata(source, content);

    // Update document
    await collection.update({
      ids: [docId],
      metadatas: [newMetadata]
    });

    if (i % 50 === 0) {
      console.log(`Migrated ${i}/${allDocs.ids.length} documents`);
    }
  }

  console.log('Migration complete!');
}
```

**Run migration:**
```bash
cd /opt/spaceos/spaceos-nexus/knowledge-service
npm run migrate:metadata
```

---

## 4. Query API Examples

### Project-Specific Search

```bash
# Only Cutting project docs
curl "http://localhost:3456/api/knowledge/search?q=nesting&project=cutting"

# Only Nexus project docs
curl "http://localhost:3456/api/knowledge/search?q=MCP&project=nexus"
```

### Tier-Based Search

```bash
# HOT tier only (last 48 hours)
curl "http://localhost:3456/api/knowledge/search?q=pattern&tier=hot"

# HOT + WARM (last 2 weeks)
curl "http://localhost:3456/api/knowledge/search?q=pattern&tier=hot,warm"
```

### Priority-Based Search

```bash
# Critical priority only
curl "http://localhost:3456/api/knowledge/search?q=deployment&priority=critical"
```

### Combined Filters

```bash
# Cutting project, HOT+WARM tier, high priority patterns
curl "http://localhost:3456/api/knowledge/search?q=drag-drop&project=cutting&tier=hot,warm&priority=high&category=patterns"
```

---

## 5. Cold Start Optimization

### Minimal Context Loading

```typescript
// src/coldStart.ts — new module

export async function getColdStartContext(
  terminal: string,
  project: ProjectType
): Promise<DocumentMetadata[]> {
  // Load project CONTEXT file first
  const contextDoc = await vectorStore.similaritySearch('', 1, {
    project,
    category: 'context'
  });

  // Load HOT tier docs (last 48h)
  const hotDocs = await vectorStore.similaritySearch('', 10, {
    project,
    tier: 'hot'
  });

  // Load CRITICAL priority docs (deployment, security)
  const criticalDocs = await vectorStore.similaritySearch('', 5, {
    project,
    priority: 'critical'
  });

  return [...contextDoc, ...hotDocs, ...criticalDocs];
}
```

**Usage:**
```bash
# API endpoint for cold start
GET /api/knowledge/cold-start?terminal=backend&project=cutting

# Returns:
# - CUTTING_CONTEXT.md
# - 10 HOT tier Cutting docs (last 48h)
# - 5 CRITICAL priority docs (deployment/security)
```

---

## 6. Testing Plan

### Unit Tests

```typescript
// __tests__/metadataUtils.test.ts

describe('Metadata Extraction', () => {
  it('should infer tier from last_updated date', () => {
    const yesterday = new Date(Date.now() - 24 * 60 * 60 * 1000).toISOString().split('T')[0];
    expect(inferTier(yesterday)).toBe('hot');

    const twoWeeksAgo = new Date(Date.now() - 14 * 24 * 60 * 60 * 1000).toISOString().split('T')[0];
    expect(inferTier(twoWeeksAgo)).toBe('warm');
  });

  it('should infer project from content keywords', () => {
    const content = 'This document describes nesting optimization for the cutting module.';
    expect(inferProject('docs/knowledge/patterns/NESTING.md', content)).toBe('cutting');
  });

  it('should infer priority from category and tier', () => {
    expect(inferPriority('deployment', 'hot')).toBe('critical');
    expect(inferPriority('patterns', 'hot')).toBe('high');
    expect(inferPriority('reading-list', 'warm')).toBe('medium');
  });
});
```

### Integration Tests

```bash
# Reindex with new metadata
curl -X POST http://localhost:3456/api/knowledge/index

# Verify metadata populated
curl "http://localhost:3456/api/knowledge/search?q=test&project=cutting" | jq '.results[0].metadata'

# Expected output:
# {
#   "source": "docs/knowledge/patterns/FRONTEND_DRAG_DROP_PATTERNS.md",
#   "project": "cutting",
#   "tier": "hot",
#   "created_date": "2026-06-22",
#   "last_updated": "2026-06-22",
#   "priority": "high",
#   "category": "patterns"
# }
```

---

## 7. Performance Considerations

### Indexing Performance

**Baseline:** ~5s for 441 documents (no git calls)
**With git metadata:** ~15-20s (3× slowdown due to execSync)

**Optimization:**
```typescript
// Cache git metadata in memory
const gitMetadataCache = new Map<string, { created: string, lastUpdated: string }>();

function getGitMetadata(filePath: string) {
  if (gitMetadataCache.has(filePath)) {
    return gitMetadataCache.get(filePath);
  }

  const metadata = {
    created: getCreatedDate(filePath),
    lastUpdated: getLastModifiedDate(filePath)
  };

  gitMetadataCache.set(filePath, metadata);
  return metadata;
}
```

### Query Performance

**ChromaDB filtering:** O(n) scan (not indexed by metadata)
**Solution:** Pre-filter by tier/project before similarity search.

```typescript
// Efficient filtering: fetch only relevant collection subset
const filteredCollection = await collection.get({
  where: { project: 'cutting', tier: { $in: ['hot', 'warm'] } }
});
```

---

## 8. Deployment Checklist

- [ ] **Code changes:** Update `src/vectorStore.ts`, `src/indexer.ts`, create `src/metadataUtils.ts`
- [ ] **Migration script:** Create `src/migrations/001-add-metadata-fields.ts`
- [ ] **Unit tests:** Write tests for `metadataUtils.ts` functions
- [ ] **Integration tests:** Test full reindex + query with new filters
- [ ] **API docs:** Update `knowledge-service/README.md` with new query params
- [ ] **Reindex:** Run full reindex on VPS (441 docs → ~20s)
- [ ] **Smoke test:** Verify project/tier/priority filtering works
- [ ] **Update CONTEXT files:** Document new query capabilities

---

## 9. Rollback Plan

**If issues arise:**

1. **Revert code changes:** `git revert <commit>`
2. **Re-index without metadata:** ChromaDB will use old schema
3. **Fallback queries:** Remove `project`, `tier`, `priority` params from queries

**Backward compatibility:** Old queries (`?q=search`) still work (new params optional).

---

## Kapcsolódó fájlok

- **Indexer:** `spaceos-nexus/knowledge-service/src/indexer.ts`
- **Vector Store:** `spaceos-nexus/knowledge-service/src/vectorStore.ts`
- **Server:** `spaceos-nexus/knowledge-service/src/server.ts`
- **ADR-048:** `docs/architecture/decisions/ADR-048-project-tiered-context.md`
- **Audit:** `terminals/librarian/outbox/2026-06-24_001_knowledge-audit-tiered-structure.md`

---

## Státusz

- ✅ Metadata schema designed
- ✅ Extraction logic specified
- ✅ Migration strategy defined
- ⏳ Implementation pending (Backend terminal)
- ⏳ Testing pending
- ⏳ Deployment pending
