---
id: MSG-ORCH-003
from: conductor
to: orch
type: task
priority: high
status: READ
model: sonnet
ref: docs/tasks/new/RAG_Knowledge_Base_v1.md
created: 2026-06-18
---

# RAG Knowledge Base — Orchestrator Query Interface

## Összefoglaló

Implementáld az Orchestrator `/knowledge/search` endpoint-ot, amely PostgreSQL full-text search (tsvector) alapú RAG query interfészt biztosít a `knowledge.documents` táblából.

**Backend:** PostgreSQL FTS (nem vector embeddings) + strukturált metadata tábla
**Scope:** Orchestrator HTTP endpoint + query logic
**Prereq:** INFRA PostgreSQL schema setup (MSG-INFRA-060) — párhuzamosan indul

---

## Architektúra

```
Orchestrator (port 3000)
  ↓
POST /knowledge/search
  ↓
PostgreSQL knowledge.documents tábla
  - content_tsvector (GENERATED tsvector)
  - ts_rank() full-text search
  ↓
JSON response (relevancia szerinti top-k)
```

**Nincs új service** — az Orchestrator közvetlenül query-zi a PostgreSQL-t.

---

## PostgreSQL Schema (INFRA készíti)

```sql
CREATE SCHEMA IF NOT EXISTS knowledge;

CREATE TABLE knowledge.documents (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    file_path       TEXT NOT NULL UNIQUE,
    title           TEXT,
    content         TEXT NOT NULL,
    content_tsvector TSVECTOR GENERATED ALWAYS AS (
        to_tsvector('simple', coalesce(title, '') || ' ' || content)
    ) STORED,
    metadata        JSONB,
    created_at      TIMESTAMPTZ DEFAULT now(),
    updated_at      TIMESTAMPTZ DEFAULT now()
);

CREATE INDEX idx_documents_tsvector ON knowledge.documents USING GIN (content_tsvector);
CREATE INDEX idx_documents_metadata ON knowledge.documents USING GIN (metadata);
CREATE INDEX idx_documents_file_path ON knowledge.documents (file_path);
```

**INFRA MSG-INFRA-060 feladata** — ezt NE implementáld te. Várj a schema elkészültére.

---

## Orchestrator Endpoint Spec

### `POST /knowledge/search`

**Request:**
```typescript
{
  query: string,        // search query
  limit?: number,       // top-k results (default: 5, max: 20)
  metadata_filter?: {   // optional JSONB filter
    [key: string]: any
  }
}
```

**Response:**
```typescript
{
  results: [
    {
      id: UUID,
      file_path: string,
      title: string | null,
      snippet: string,    // first 200 chars of content
      rank: number,       // ts_rank score
      metadata: object
    }
  ],
  total: number,
  query: string
}
```

**HTTP Status:**
- `200 OK` — sikeres query
- `400 Bad Request` — invalid query vagy limit
- `500 Internal Server Error` — PostgreSQL hiba

---

## SQL Query Logic

```sql
SELECT
    id,
    file_path,
    title,
    LEFT(content, 200) AS snippet,
    ts_rank(content_tsvector, plainto_tsquery('simple', $1)) AS rank,
    metadata
FROM knowledge.documents
WHERE content_tsvector @@ plainto_tsquery('simple', $1)
  AND ($2::jsonb IS NULL OR metadata @> $2)
ORDER BY rank DESC
LIMIT $3;
```

**Parameters:**
- `$1` — query string
- `$2` — metadata_filter (JSONB, nullable)
- `$3` — limit

**Metadata filter példa:**
```json
{ "type": "adr", "status": "approved" }
```

Ez csak azokat a dokumentumokat adja vissza, ahol `metadata->>'type' = 'adr'` ÉS `metadata->>'status' = 'approved'`.

---

## Implementációs lépések

### 1. PostgreSQL Connection

Az Orchestrator `.env` fájlban már van PostgreSQL connection string:

```bash
DATABASE_URL=postgresql://gabor@localhost:5432/spaceos
```

Ha nincs, add hozzá. Használd a `pg` npm package-et (Node.js PostgreSQL client).

```bash
npm install pg
```

### 2. Database Query Module

Hozz létre egy `src/services/knowledgeService.ts` fájlt:

```typescript
import { Pool } from 'pg';

const pool = new Pool({
  connectionString: process.env.DATABASE_URL,
});

export async function searchDocuments(
  query: string,
  limit: number = 5,
  metadataFilter?: Record<string, any>
) {
  const sql = `
    SELECT
      id,
      file_path,
      title,
      LEFT(content, 200) AS snippet,
      ts_rank(content_tsvector, plainto_tsquery('simple', $1)) AS rank,
      metadata
    FROM knowledge.documents
    WHERE content_tsvector @@ plainto_tsquery('simple', $1)
      AND ($2::jsonb IS NULL OR metadata @> $2)
    ORDER BY rank DESC
    LIMIT $3;
  `;

  const params = [
    query,
    metadataFilter ? JSON.stringify(metadataFilter) : null,
    limit,
  ];

  const result = await pool.query(sql, params);
  return result.rows;
}
```

### 3. HTTP Endpoint

Hozz létre `src/routes/knowledge.ts`:

```typescript
import express from 'express';
import { searchDocuments } from '../services/knowledgeService';

const router = express.Router();

router.post('/search', async (req, res) => {
  const { query, limit = 5, metadata_filter } = req.body;

  if (!query || typeof query !== 'string') {
    return res.status(400).json({ error: 'Invalid query parameter' });
  }

  if (limit > 20 || limit < 1) {
    return res.status(400).json({ error: 'Limit must be between 1 and 20' });
  }

  try {
    const results = await searchDocuments(query, limit, metadata_filter);

    res.json({
      results,
      total: results.length,
      query,
    });
  } catch (error) {
    console.error('Knowledge search error:', error);
    res.status(500).json({ error: 'Internal server error' });
  }
});

export default router;
```

### 4. Route Registration

`src/index.ts`-ben register-áld a knowledge route-ot:

```typescript
import knowledgeRouter from './routes/knowledge';

app.use('/knowledge', knowledgeRouter);
```

Ez a `/knowledge/search` endpoint-ot létrehozza a `http://localhost:3000/knowledge/search` címen.

---

## Tesztelés

### Manuális teszt (cURL)

```bash
curl -X POST http://localhost:3000/knowledge/search \
  -H "Content-Type: application/json" \
  -d '{"query": "ADR", "limit": 5}'
```

**Expected response:**
```json
{
  "results": [
    {
      "id": "...",
      "file_path": "docs/architecture/ADR_CATALOGUE.md",
      "title": "ADR Catalogue",
      "snippet": "# ADR Catalogue\n\nAll architectural decisions...",
      "rank": 0.0607927,
      "metadata": { "type": "adr" }
    }
  ],
  "total": 1,
  "query": "ADR"
}
```

### Integration Test (opcional)

Ha van test suite, adj hozzá egy integration testet:

```typescript
describe('POST /knowledge/search', () => {
  it('should return search results', async () => {
    const response = await request(app)
      .post('/knowledge/search')
      .send({ query: 'security', limit: 5 });

    expect(response.status).toBe(200);
    expect(response.body).toHaveProperty('results');
    expect(response.body.results).toBeInstanceOf(Array);
  });

  it('should reject invalid limit', async () => {
    const response = await request(app)
      .post('/knowledge/search')
      .send({ query: 'test', limit: 50 });

    expect(response.status).toBe(400);
  });
});
```

---

## Error Handling

**PostgreSQL connection fail:**
- Log error: `console.error('PostgreSQL connection failed:', error)`
- Return HTTP 500: `{ error: 'Database connection failed' }`

**Empty results:**
- Return HTTP 200: `{ results: [], total: 0, query: "..." }`

**Invalid query:**
- Return HTTP 400: `{ error: 'Invalid query parameter' }`

---

## Definition of Done

- [ ] `POST /knowledge/search` endpoint implemented
- [ ] PostgreSQL FTS query logic (ts_rank + plainto_tsquery)
- [ ] Metadata JSONB filter support
- [ ] Request validation (query string, limit 1-20)
- [ ] Error handling (400, 500)
- [ ] Manual cURL test successful
- [ ] PM2 restart + log check (0 errors)
- [ ] DONE outbox message sent to Conductor

---

## Prereq Check

**Előfeltétel:** INFRA MSG-INFRA-060 PostgreSQL schema elkészült.

**Ellenőrzés:**

```bash
psql -h localhost -U gabor -d spaceos -c "SELECT * FROM knowledge.documents LIMIT 1;"
```

Ha ez nem működik, várj az INFRA DONE-ra. Ha működik, kezdheted az implementációt.

---

## Referenciák

- Spec: `docs/tasks/new/RAG_Knowledge_Base_v1.md`
- PostgreSQL FTS docs: https://www.postgresql.org/docs/current/textsearch.html
- Orchestrator: `backend/spaceos-orchestrator/` (port 3000)

---

**ORCH Terminal: Implementáld a `/knowledge/search` endpoint-ot PostgreSQL FTS-sel. Várj az INFRA schema setup-ra!**

Timestamp: 2026-06-18 05:02 UTC
