# MCP Server Architecture — RAG vs. Full Document Serving

**Purpose:** Design decisions for agent-system/server MCP tool surface. Developer reference for deciding which content to serve via RAG vs. complete documents.

**Date:** 2026-02-24

---

## Alapelv: tartalomtípus határozza meg a kiszolgálási módot

Az agent rendszer két teljesen különböző tartalom-kategóriát kezel. Ezeket **nem szabad felcserélni**.

---

## 1. RAG — `.knowledge.md` fájlok

**Mikor használd:** Passzív referencia-tudás, iparági szabványok, best practice-ek.

**Miért alkalmas RAG-ra:**

- Az agent a konkrét feladathoz releváns részeket keresi — nincs szüksége az egész dokumentumra
- Szemantikus keresés = pontosan erre a tartalomtípusra való
- A tartalom szekvenciálisan nem függ össze (nincs critikus sorrend)

**Fájltípusok:** `*.knowledge.md`

**MCP eszköz:**

```
search_knowledge(query: string, domain?: string, topK?: number) → chunks[]
```

---

## 2. Teljes dokumentum — role, workflow, template, core fájlok

Ezeket a fájlokat **TILOS RAG-gal kezelni**. Indoklás típusonként:

| Fájltípus | Tartalom jellege | Miért nem RAG? |
|:----------|:-----------------|:---------------|
| `.role.md` | Az agent **identitása** (responsibilities, forbidden_actions, handoff_triggers) | Részleges betöltés = sérült identitás, az agent nem tudja ki ő |
| `.workflow.md` | **FSM-alapú folyamat** (lépések, gate feltételek, döntési pontok) | Kimaradhat egy gate → folyamat megszakad, dokumentum nem készül el |
| `.template.md` | Dokumentum struktúra (teljes sablon kell az outputhoz) | Csonka output, nem felel meg a DoD-nak |
| `constraints.md` | **Tiltások — MINDEN sor kötelező** | Kimaradhat egy tiltás → kritikus folyamat- vagy biztonsági hiba |
| `runbook.md` | FSM-állapot router (teljes döntési fa) | Részleges betöltés = hibás routing |
| `definition_of_done.md` | Teljes DoD checklist | Kimaradhat egy kritérium |

**MCP eszközök:**

```
get_role(domain: string, role: string)          → teljes .role.md tartalom
get_workflow(domain: string, role: string, type?: string) → teljes .workflow.md
get_template(domain: string, role: string, name: string) → teljes .template.md
get_core(doc: 'constraints' | 'runbook' | 'dod' | 'error_recovery') → teljes fájl
```

---

## 3. Tervezett MCP tool surface

### 3.1 READ Layer (GET operations)

```
Agent (Copilot Chat / külső kliens)
    │
    ├── search_knowledge(query, domain?)     → RAG → .knowledge.md chunks
    ├── get_role(domain, role)               → teljes .role.md + .schema.yaml
    ├── get_workflow(domain, role, type?)    → teljes .workflow.md
    ├── get_template(domain, role, name)     → teljes .template.md
    ├── get_message(sender, receiver, type)  → teljes .message.md
    └── get_core(doc)                        → constraints / runbook / DoD / error_recovery
```

A helyi fájlrendszer marad az egyetlen forrás (`database/` mappa). Az agenteknek **nem kell helyi másolat** — az MCP server az egyetlen hozzáférési pont.

### 3.2 WRITE Layer (POST operations) — EPIC-08: MCP Write Layer

Az agent nem csak olvas, hanem írhat és állapotot módosíthat:

#### MCP Write Tools

```
Agent (Copilot Chat / teljesítendő munka után)
    │
    ├── submit_artifact(artifact_content, session_id, artifact_type)
    │   → artifact tartalmat küld (implementation_summary, test_report, PR_link, stb.)
    │   → szession history-ban rögzítve
    │   → FSM state léptetésre kész
    │
    ├── update_workflow_state(session_id, new_state, event, evidence?)
    │   → FSM state modification: "IN_PROGRESS" → "SUBMITTED" → "PROCESSED" → "CLOSED"
    │   → event: "artifact_submitted", "tests_failed", "review_approved", stb.
    │   → evidence: artifact_id vagy link az artifact history-ban
    │
    └── store_session_checkpoint(session_id, checkpoint_data)
        → session lépéseit tárolja (debug, audit, session recovery a krach után)
        → data: {timestamp, event, agent_state, context_size, artifacts_count}
```

#### Session State Persistence (EPIC-08 SQLite schema extension)

A `metadata.db` új táblák:

```sql
CREATE TABLE sessions (
    id TEXT PRIMARY KEY,
    agent_id TEXT NOT NULL,
    domain TEXT NOT NULL,
    role TEXT NOT NULL,
    started_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_updated_at TIMESTAMP,
    fsm_state TEXT NOT NULL DEFAULT 'started',  -- FSM tracker: started, in_progress, submitted, processed, closed
    outcome TEXT,  -- success, error, timeout, abandoned
    FOREIGN KEY (agent_id) REFERENCES agents(id)
);

CREATE TABLE artifacts (
    id TEXT PRIMARY KEY,
    session_id TEXT NOT NULL,
    artifact_type TEXT NOT NULL,  -- "implementation_summary", "test_report", "pr_link", "checkpoint"
    content TEXT NOT NULL,
    submitted_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    embedded BOOLEAN DEFAULT 0,  -- ChromaDB write-back (EPIC-12)
    FOREIGN KEY (session_id) REFERENCES sessions(id)
);

CREATE TABLE workflow_events (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    session_id TEXT NOT NULL,
    event_type TEXT NOT NULL,  -- "artifact_submitted", "test_failed", "review_approved"
    state_before TEXT,
    state_after TEXT,
    evidence_artifact_id TEXT,
    timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (session_id) REFERENCES sessions(id),
    FOREIGN KEY (evidence_artifact_id) REFERENCES artifacts(id)
);

CREATE TABLE checkpoints (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    session_id TEXT NOT NULL,
    checkpoint_data JSON NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (session_id) REFERENCES sessions(id)
);
```

#### Error Handling & RBAC

- `submit_artifact()`: Az agent role-ját ellenőrzik, csak azok tudjak submitálni, akiknek van `submit_artifact` permission
- Konfiguráció: `database/roles/*/submit_artifact_permissions.yaml`
- Error responses: permission denied, invalid session, schema validation fail, DB error

---

## 4. Következmény a knowledge_map.md-re

Ha az MCP server teljesen felváltja a közvetlen fájlolvasást:

| Szekció | Státusz |
|:--------|:--------|
| 1. Core (runbook, constraints) | `get_core()` tool váltja fel → **elhagyható** |
| 2. Roles | `get_role()` tool váltja fel → **elhagyható** |
| 3. Workflows | `get_workflow()` tool váltja fel → **elhagyható** |
| 4. Knowledge Katalógus | `search_knowledge()` RAG váltja fel → **elhagyható** |
| 5. Templates | `get_template()` tool váltja fel → **elhagyható** |
| 6. Messages | `get_message()` tool váltja fel → **elhagyható** |
| 7–9. Sub-agents, Instructions, Standards | Navigációs segédlet — **megtartható** emberi olvasásra |

A `knowledge_map.md` a teljes MCP kiszolgálás után **emberi dokumentációvá** degradálódik. Gépi feldolgozásra nem szükséges.

---

## 5. Resource tracking & audit (projekt fájlok nyilvántartása)

A role rendszer fájljait (role.md, runbook.md, workflows, template, message) az MCP szerver **adatbázisban is nyomon követi**. A táblában csak **relatív elérési út** tárolódik, minden rekordhoz projekt és felhasználó is kapcsolódik. Az áthelyezés frissíthető, törlés helyett archiválás történik.

### SQLite schema

A `metadata.db` a projekt gyökérében jön létre, a `ResourceTracker` modul hozza inicializáláskor.

```sql
CREATE TABLE resources (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    project TEXT NOT NULL,
    user TEXT NOT NULL,
    type TEXT NOT NULL,
    relative_path TEXT NOT NULL,
    hash TEXT,
    status TEXT NOT NULL DEFAULT 'active',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    modified_at TIMESTAMP,
    archived_at TIMESTAMP
);
```

### MCP API kiterjesztés

A szerver további "admin" végpontot is nyújt a könnyű ellenőrzéshez:

```
GET /admin/resources    # böngészhető HTML tábla a nyilvántartott fájlokról
```

A szerver új végpontokat ad a
`ResourceTracker`-hez:

```
POST /api/resource            # add vagy update
POST /api/resource/archive    # jelölj archíváltnak
POST /api/resource/move       # módosítsd a relatív utat
GET  /api/resource            # listázd (szűrhető project szerint)
```

Példa hívás:

```js
await fetch('/api/resource', {
  method:'POST',
  body: JSON.stringify({ project:'flow', user:'szant', type:'role', relative_path:'roles/engineering/backend_developer/backend_developer.role.md'})
});
```

A felhasználó többször is hívhatja (`addOrUpdate` váltja a rekordot); ha a fájlt egyszerűen áthelyezi, a `move` végpontot kell használni; törlés helyett `archive`.

---

## 6. Jelenlegi szerver állapota (2026-02-24)

**Megvalósítva (`src/agent-system/server/`):**

- `POST /api/execute` → role betöltés (RoleLoader) + RAG injektálás (queryKnowledge) + Gemini
- `VectorStore.ts` → ChromaDB + MemoryVectorStore fallback + domain filter
- `indexKnowledgeBase.ts` → csak `.knowledge.md` indexelés + frontmatter domain metaadat

**Hiányzik (MCP tool surface):**

- Dedikált endpointok: `get_role`, `get_workflow`, `get_template`, `get_core`, `get_message`
- MCP protokoll wrapper (SSE vagy stdio transport)
- Schema: tool leírások, input/output typing

**Stack:** TypeScript + Express + LangChain + ChromaDB + Google Gemini 2.5 Flash + text-embedding-004
