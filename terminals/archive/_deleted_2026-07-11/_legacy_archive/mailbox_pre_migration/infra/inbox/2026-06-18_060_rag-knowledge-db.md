---
id: MSG-INFRA-060
from: conductor
to: infra
type: task
priority: high
status: READ
model: haiku
ref: docs/tasks/new/RAG_Knowledge_Base_v1.md
created: 2026-06-18
---

# RAG Knowledge Base — PostgreSQL Schema Setup

## Összefoglaló

Hozd létre a `knowledge` schema-t és `knowledge.documents` táblát a PostgreSQL-ben full-text search (FTS) támogatással.

**Backend:** PostgreSQL FTS (tsvector) + structured metadata
**Scope:** Schema + table + indexes
**No new service** — az Orchestrator query-zi majd (MSG-ORCH-003)

---

## PostgreSQL Schema Migration

### Migration fájl

Hozz létre: `/opt/spaceos/infra/migrations/2026-06-18_001_create_knowledge_schema.sql`

```sql
-- ================================================
-- RAG Knowledge Base — PostgreSQL FTS Schema
-- Created: 2026-06-18
-- ================================================

-- 1. Create knowledge schema
CREATE SCHEMA IF NOT EXISTS knowledge;

-- 2. Create documents table
CREATE TABLE knowledge.documents (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    file_path       TEXT NOT NULL UNIQUE,
    title           TEXT,
    content         TEXT NOT NULL,

    -- Full-text search tsvector (auto-generated)
    content_tsvector TSVECTOR GENERATED ALWAYS AS (
        to_tsvector('simple', coalesce(title, '') || ' ' || content)
    ) STORED,

    -- Structured metadata (JSONB)
    metadata        JSONB DEFAULT '{}'::jsonb,

    -- Timestamps
    created_at      TIMESTAMPTZ DEFAULT now(),
    updated_at      TIMESTAMPTZ DEFAULT now()
);

-- 3. Create indexes
CREATE INDEX idx_documents_tsvector
    ON knowledge.documents USING GIN (content_tsvector);

CREATE INDEX idx_documents_metadata
    ON knowledge.documents USING GIN (metadata);

CREATE INDEX idx_documents_file_path
    ON knowledge.documents (file_path);

-- 4. Add updated_at trigger
CREATE OR REPLACE FUNCTION knowledge.update_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_update_documents_updated_at
    BEFORE UPDATE ON knowledge.documents
    FOR EACH ROW
    EXECUTE FUNCTION knowledge.update_updated_at();

-- 5. Grant permissions (adjust as needed)
GRANT USAGE ON SCHEMA knowledge TO gabor;
GRANT SELECT, INSERT, UPDATE, DELETE ON knowledge.documents TO gabor;

-- 6. Add comment
COMMENT ON TABLE knowledge.documents IS
'RAG knowledge base for SpaceOS documentation and architectural decisions. Uses PostgreSQL FTS (tsvector) instead of vector embeddings.';
```

---

## Execution Steps

### 1. Apply Migration

```bash
psql -h localhost -U gabor -d spaceos -f /opt/spaceos/infra/migrations/2026-06-18_001_create_knowledge_schema.sql
```

**Expected output:**
```
CREATE SCHEMA
CREATE TABLE
CREATE INDEX
CREATE INDEX
CREATE INDEX
CREATE FUNCTION
CREATE TRIGGER
GRANT
COMMENT
```

### 2. Verify Schema

```bash
psql -h localhost -U gabor -d spaceos -c "\dt knowledge.*"
```

**Expected output:**
```
              List of relations
  Schema   |   Name    | Type  | Owner
-----------+-----------+-------+-------
 knowledge | documents | table | gabor
(1 row)
```

### 3. Verify Indexes

```bash
psql -h localhost -U gabor -d spaceos -c "\di knowledge.*"
```

**Expected output:**
```
                              List of relations
  Schema   |           Name            | Type  | Owner |    Table
-----------+---------------------------+-------+-------+-------------
 knowledge | idx_documents_file_path   | index | gabor | documents
 knowledge | idx_documents_metadata    | index | gabor | documents
 knowledge | idx_documents_tsvector    | index | gabor | documents
(3 rows)
```

### 4. Test Insert + FTS Query

```bash
psql -h localhost -U gabor -d spaceos << 'EOF'
-- Test insert
INSERT INTO knowledge.documents (file_path, title, content, metadata)
VALUES (
    'docs/test.md',
    'Test Document',
    'This is a test document about PostgreSQL full-text search and ADR patterns.',
    '{"type": "test", "status": "draft"}'::jsonb
);

-- Test FTS query
SELECT
    file_path,
    title,
    ts_rank(content_tsvector, plainto_tsquery('simple', 'ADR')) AS rank
FROM knowledge.documents
WHERE content_tsvector @@ plainto_tsquery('simple', 'ADR')
ORDER BY rank DESC;

-- Cleanup test
DELETE FROM knowledge.documents WHERE file_path = 'docs/test.md';
EOF
```

**Expected output:**
```
INSERT 0 1

  file_path   |     title      |    rank
--------------+----------------+------------
 docs/test.md | Test Document  | 0.0607927
(1 row)

DELETE 1
```

---

## Migration Registry

Add hozzá a migration-t a migration tracking táblához (ha van):

```sql
INSERT INTO infra.migrations (name, applied_at)
VALUES ('2026-06-18_001_create_knowledge_schema', now());
```

Ha nincs migration tracking tábla, skip-eld.

---

## Backup Before Migration (opcional)

Ha production VPS, előbb backup:

```bash
pg_dump -h localhost -U gabor -d spaceos -Fc -f /tmp/spaceos_backup_$(date +%Y%m%d_%H%M%S).dump
```

Restore (ha szükséges):

```bash
pg_restore -h localhost -U gabor -d spaceos /tmp/spaceos_backup_YYYYMMDD_HHMMSS.dump
```

---

## Rollback (ha szükséges)

```sql
DROP SCHEMA knowledge CASCADE;
```

**FIGYELEM:** Ez törli az összes `knowledge.documents` adatot is!

---

## Definition of Done

- [ ] Migration SQL fájl létrehozva: `/opt/spaceos/infra/migrations/2026-06-18_001_create_knowledge_schema.sql`
- [ ] Migration executed: `psql -f ...`
- [ ] Schema verification: `\dt knowledge.*` (1 table)
- [ ] Index verification: `\di knowledge.*` (3 indexes)
- [ ] Test insert + FTS query successful
- [ ] Migration tracking updated (ha van)
- [ ] DONE outbox message sent to Conductor

---

## Next Steps

**ORCH Terminal (MSG-ORCH-003):** Query interface implementation
**LIBRARIAN Terminal (MSG-LIBRARIAN-001):** Document ingestion

---

**INFRA Terminal: Hozd létre a `knowledge` schema-t és `documents` táblát PostgreSQL FTS-sel!**

Timestamp: 2026-06-18 05:02 UTC
