---
id: MSG-LIBRARIAN-001
from: conductor
to: librarian
type: task
priority: high
status: READ
model: sonnet
ref: docs/tasks/new/RAG_Knowledge_Base_v1.md, docs/tasks/new/MCP_Integration_Plan_v1.md
created: 2026-06-18
processed: 2026-06-18
---

# RAG Knowledge Base — Document Ingestion + MCP Integration

## Összefoglaló

Két feladat:

1. **RAG Knowledge Base Ingestion** — `docs/` könyvtár indexelése a `knowledge.documents` PostgreSQL táblába
2. **MCP Integration** — MCP role definitions CLAUDE.md fájlokba integrálása

---

## Feladat 1: RAG Knowledge Base Ingestion

### Cél

Indexáld a SpaceOS dokumentációt (`docs/` könyvtár) a `knowledge.documents` PostgreSQL táblába full-text search (FTS) támogatással.

### Scope

**Indexelendő dokumentumok:**
- `docs/architecture/*.md` (ADR-ek, API catalogue, module boundaries)
- `docs/knowledge/*.md` (security, deployment, patterns)
- `docs/vision/*.md` (vision master, results)
- `docs/WORKFLOW.md`
- `docs/Codebase_Status.md`

**KIZÁRT:**
- `docs/mailbox/*` (inbox/outbox üzenetek — volatilis)
- `docs/planning/*` (queue/ideas — volatilis)
- `docs/tasks/*` (task fájlok — külön kezelés később)
- Binary fájlok, képek, screenshots

### PostgreSQL Schema (INFRA készíti)

```sql
CREATE TABLE knowledge.documents (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    file_path       TEXT NOT NULL UNIQUE,
    title           TEXT,
    content         TEXT NOT NULL,
    content_tsvector TSVECTOR GENERATED ALWAYS AS (
        to_tsvector('simple', coalesce(title, '') || ' ' || content)
    ) STORED,
    metadata        JSONB DEFAULT '{}'::jsonb,
    created_at      TIMESTAMPTZ DEFAULT now(),
    updated_at      TIMESTAMPTZ DEFAULT now()
);
```

**Prereq:** INFRA MSG-INFRA-060 schema setup — ellenőrizd hogy elkészült-e!

### Ingestion Script

Hozz létre `/opt/spaceos/scripts/ingest-knowledge.sh`:

```bash
#!/usr/bin/env bash
set -euo pipefail

# RAG Knowledge Base Ingestion Script
# Indexes docs/ directory into knowledge.documents table

DB_URL="postgresql://gabor@localhost:5432/spaceos"
DOCS_ROOT="/opt/spaceos/docs"

# Excluded paths (glob patterns)
EXCLUDE_PATTERNS=(
    "mailbox/*"
    "planning/*"
    "tasks/*"
)

echo "Starting knowledge base ingestion..."

# Find all .md files, excluding patterns
find "$DOCS_ROOT" -type f -name "*.md" | while read -r file_path; do
    # Check if file matches any exclude pattern
    skip=0
    for pattern in "${EXCLUDE_PATTERNS[@]}"; do
        if [[ "$file_path" == *"$pattern"* ]]; then
            skip=1
            break
        fi
    done

    if [[ $skip -eq 1 ]]; then
        echo "SKIP: $file_path (excluded)"
        continue
    fi

    # Extract title (first # heading or filename)
    title=$(grep -m 1 "^# " "$file_path" | sed 's/^# //' || basename "$file_path" .md)

    # Read file content
    content=$(cat "$file_path")

    # Detect metadata from filename
    metadata='{}'
    if [[ "$file_path" == *"ADR"* ]]; then
        metadata='{"type": "adr"}'
    elif [[ "$file_path" == *"vision"* ]]; then
        metadata='{"type": "vision"}'
    elif [[ "$file_path" == *"security"* ]]; then
        metadata='{"type": "security"}'
    elif [[ "$file_path" == *"deployment"* ]]; then
        metadata='{"type": "deployment"}'
    fi

    # Upsert into PostgreSQL (INSERT ... ON CONFLICT UPDATE)
    psql "$DB_URL" -c "
        INSERT INTO knowledge.documents (file_path, title, content, metadata)
        VALUES ('$file_path', '$title', \$\$${content}\$\$, '$metadata'::jsonb)
        ON CONFLICT (file_path)
        DO UPDATE SET
            title = EXCLUDED.title,
            content = EXCLUDED.content,
            metadata = EXCLUDED.metadata,
            updated_at = now();
    " > /dev/null

    echo "INDEXED: $file_path (title: $title)"
done

echo "Ingestion complete!"
echo "Total documents:"
psql "$DB_URL" -c "SELECT COUNT(*) FROM knowledge.documents;"
```

**Használat:**

```bash
chmod +x /opt/spaceos/scripts/ingest-knowledge.sh
bash /opt/spaceos/scripts/ingest-knowledge.sh
```

### Metadata Field Guidelines

| Dokumentum típus | metadata.type | Példa |
|---|---|---|
| ADR (Architecture Decision Record) | `"adr"` | `docs/architecture/ADR_*.md` |
| Vision dokumentum | `"vision"` | `docs/vision/*.md` |
| Security pattern | `"security"` | `docs/knowledge/security/*.md` |
| Deployment runbook | `"deployment"` | `docs/knowledge/deployment/*.md` |
| Workflow, status | `"system"` | `docs/WORKFLOW.md`, `Codebase_Status.md` |

### Initial Ingestion

Első futtatás után ellenőrizd az eredményt:

```bash
psql -h localhost -U gabor -d spaceos -c "SELECT file_path, title, metadata FROM knowledge.documents ORDER BY created_at DESC LIMIT 10;"
```

**Expected:** 30-50 dokumentum (architektúra, knowledge, vision).

---

## Feladat 2: MCP Integration — CLAUDE.md Role Definitions

### Cél

Integráld az MCP (Model Context Protocol) role definition-öket a terminálok CLAUDE.md fájljaiba a `docs/tasks/new/MCP_Integration_Plan_v1.md` spec szerint.

### Scope

**Érintett CLAUDE.md fájlok:**
- `/opt/spaceos/CLAUDE.md` (ROOT terminal)
- `/opt/spaceos/spaceos-conductor/CLAUDE.md` (CONDUCTOR terminal)
- `/opt/spaceos/spaceos-librarian/CLAUDE.md` (LIBRARIAN terminal)
- További terminálok később (ORCH, FE, INFRA, KERNEL, stb.)

### Context Hygiene Szabályok (ÚJ szekció)

Add hozzá minden CLAUDE.md fájlhoz a következő szekciót:

```markdown
## CONTEXT HYGIENE

- Ha a session context 60%+ → kötelező kontextus vágás (összefoglalás + irreleváns részek ejtése)
- Root kizárólag dokumentált forrásból dolgozik — ha hiányzik az info, NE találgass, hanem delegálj (Architect, Tester, vagy a releváns terminál)
- State tracking checklist minden session végén:
  - [ ] `docs/tasks/README.md` naprakész
  - [ ] `Codebase_Status.md` tükrözi a változásokat
  - [ ] Dependency konfliktus nincs aktív feladatok között
```

### MCP Role Definition Template

Minden terminál CLAUDE.md-jéhez add hozzá:

```markdown
## MCP SERVER (ha releváns)

Ha ez a terminál MCP server-t implementál, add meg:

**Server name:** `<server-name>`
**Protocol:** stdio | sse | websocket
**Tools:**
- `tool_name` — leírás

**Resources:**
- `resource://path` — leírás

**Prompts:**
- `prompt_name` — leírás
```

### ROOT CLAUDE.md Módosítások

1. **Context hygiene** szekció hozzáadása (lásd fent)
2. **MCP server definíció** hozzáadása (ha ROOT MCP server-t implementál — jelenleg NEM)

### CONDUCTOR CLAUDE.md Módosítások

1. **Context hygiene** szekció hozzáadása
2. **MCP server definíció** hozzáadása (ha CONDUCTOR MCP server-t implementál — jelenleg NEM)

### LIBRARIAN CLAUDE.md Módosítások

1. **Context hygiene** szekció hozzáadása
2. **MCP server definíció** hozzáadása:

```markdown
## MCP SERVER

**Server name:** `spaceos-librarian`
**Protocol:** stdio
**Tools:**
- `search_knowledge` — full-text search a knowledge.documents táblában
- `submitArtifact` — dokumentum regisztráció (idea/consensus/report)

**Resources:**
- `resource://knowledge/documents` — knowledge base dokumentumok

**Prompts:**
- `summarize_document` — dokumentum összefoglalás generálás
```

### Implementációs Lépések

1. Olvasd be a 3 CLAUDE.md fájlt:
   - `/opt/spaceos/CLAUDE.md`
   - `/opt/spaceos/spaceos-conductor/CLAUDE.md`
   - `/opt/spaceos/spaceos-librarian/CLAUDE.md`

2. Add hozzá a **Context Hygiene** szekciót mindháromhoz (a meglévő struktúra után, "Fontos szabályok" előtt)

3. Add hozzá az **MCP SERVER** szekciót a LIBRARIAN CLAUDE.md-hez (a végére)

4. Ellenőrizd hogy nincs duplikált szekció

5. Commitold a változásokat:

```bash
git add /opt/spaceos/CLAUDE.md
git add /opt/spaceos/spaceos-conductor/CLAUDE.md
git add /opt/spaceos/spaceos-librarian/CLAUDE.md
git commit -m "feat: add MCP integration and context hygiene rules to CLAUDE.md files

- Context hygiene szabályok hozzáadva (60%+ context vágás szabály)
- MCP server definition LIBRARIAN-hoz
- State tracking checklist
- Ref: MSG-LIBRARIAN-001"
```

---

## Definition of Done

### Feladat 1: RAG Ingestion

- [ ] `/opt/spaceos/scripts/ingest-knowledge.sh` script létrehozva
- [ ] Script futtatva: `bash ingest-knowledge.sh`
- [ ] PostgreSQL verify: `SELECT COUNT(*) FROM knowledge.documents;` (30-50 doc)
- [ ] Metadata field populated (type: adr/vision/security/deployment)
- [ ] Excluded paths (mailbox/planning/tasks) nincs benne
- [ ] Test query: `SELECT * FROM knowledge.documents WHERE metadata->>'type' = 'adr';`

### Feladat 2: MCP Integration

- [ ] Context Hygiene szekció hozzáadva: ROOT, CONDUCTOR, LIBRARIAN CLAUDE.md
- [ ] MCP SERVER szekció hozzáadva: LIBRARIAN CLAUDE.md
- [ ] Git commit: "feat: add MCP integration and context hygiene rules"
- [ ] Nincs duplikált szekció
- [ ] 0 markdown syntax error

### Overall

- [ ] DONE outbox message sent to Conductor
- [ ] PROCESSED_LOG.md updated (MSG-LIBRARIAN-001)

---

## Referenciák

- RAG spec: `docs/tasks/new/RAG_Knowledge_Base_v1.md`
- MCP spec: `docs/tasks/new/MCP_Integration_Plan_v1.md`
- PostgreSQL schema: MSG-INFRA-060
- LIBRARIAN CLAUDE.md: `/opt/spaceos/spaceos-librarian/CLAUDE.md`

---

**LIBRARIAN Terminal: Indexáld a docs/ könyvtárat + add hozzá az MCP integration-t a CLAUDE.md fájlokhoz!**

Timestamp: 2026-06-18 05:02 UTC
