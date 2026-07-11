--
-- MIGRATION: 002_write_layer_schema.sql
-- EPIC-08: MCP Write Layer — SQLite Schema for Sessions, Artifacts, Workflow Events, Checkpoints
-- Created: 2026-03-05
--
-- This migration creates all tables needed for EPIC-08 write-layer functionality:
--   1. sessions: agent session-öket nyomon követi
--   2. artifacts: submitted artifact-okat tárolo
--   3. workflow_events: FSM state-change events
--   4. checkpoints: session recovery checkpoints
--
-- Dependencies:
--   - PRAGMA foreign_keys = ON (must be set before executing)
--   - PRAGMA journal_mode = WAL (for concurrency)
--

-- ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
-- TABLE: sessions
-- ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
--
-- Purpose: Nyomon követi az agent session-öket. Az agent indul egy session-nel,
--          és az egész munkája alatt fennáll. A session rögzíti az agent identitását,
--          az indulás idejét, és az aktuális FSM state-et.
--
-- Fields:
--   id: UUID, PRIMARY KEY — session azonosító
--   agent_id: TEXT, UNIQUE — agent felhasználó azonosító (backend_developer, tech_lead, stb.)
--   domain: TEXT — role domain (engineering, management, discovery)
--   role: TEXT — role name (backend_developer, tech_lead, explorer, stb.)
--   started_at: TEXT (ISO 8601) — session indulásának időpontja
--   last_updated_at: TEXT (ISO 8601) — utolsó update időpont
--   fsm_state: TEXT — jelenlegi state az agent workflow-ban (started, in_progress, submitted, processed, closed)
--   outcome: TEXT (nullable) — végeredmény vagy hibaüzenet (advisory field)
--
-- Constraints:
--   PRIMARY KEY (id)
--   UNIQUE (agent_id) — csak egy aktív session per agent
--   UNIQUE (agent_id, started_at) — session-ök time-stamped identifikálása

CREATE TABLE IF NOT EXISTS sessions (
    id TEXT PRIMARY KEY COLLATE NOCASE,
    agent_id TEXT NOT NULL UNIQUE COLLATE NOCASE,
    domain TEXT NOT NULL,
    role TEXT NOT NULL,
    started_at TEXT NOT NULL,
    last_updated_at TEXT,
    fsm_state TEXT NOT NULL DEFAULT 'started',
    outcome TEXT,
    UNIQUE(agent_id, started_at)
);

CREATE INDEX IF NOT EXISTS idx_sessions_agent_id ON sessions(agent_id);
CREATE INDEX IF NOT EXISTS idx_sessions_fsm_state ON sessions(fsm_state);
CREATE INDEX IF NOT EXISTS idx_sessions_started_at ON sessions(started_at);


-- ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
-- TABLE: artifacts
-- ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
--
-- Purpose: Tárolo az agent által submitált artifact-okat (implementation summary, test report, PR link, checkpoint).
--
-- Fields:
--   id: UUID, PRIMARY KEY — artifact azonosító
--   session_id: UUID, FOREIGN KEY → sessions(id) — melyik session-höz tartozik
--   artifact_type: TEXT — típusa (implementation_summary, test_report, pr_link, checkpoint)
--   content: TEXT — teljes artifact tartalom (markdown, JSON, stb.)
--   submitted_at: TEXT (ISO 8601) — submission időpont
--   embedded: BOOLEAN — ChromaDB embedding flag (M02 EPIC-12 integration)
--
-- Constraints:
--   PRIMARY KEY (id)
--   FOREIGN KEY (session_id) → sessions(id) ON DELETE CASCADE
--   UNIQUE (session_id, artifact_type, submitted_at) — egy típus per session per időpont

CREATE TABLE IF NOT EXISTS artifacts (
    id TEXT PRIMARY KEY COLLATE NOCASE,
    session_id TEXT NOT NULL COLLATE NOCASE,
    artifact_type TEXT NOT NULL,
    content TEXT NOT NULL,
    submitted_at TEXT NOT NULL,
    embedded BOOLEAN DEFAULT 0,
    FOREIGN KEY (session_id) REFERENCES sessions(id) ON DELETE CASCADE,
    UNIQUE(session_id, artifact_type, submitted_at)
);

CREATE INDEX IF NOT EXISTS idx_artifacts_session_id ON artifacts(session_id);
CREATE INDEX IF NOT EXISTS idx_artifacts_type ON artifacts(artifact_type);
CREATE INDEX IF NOT EXISTS idx_artifacts_submitted_at ON artifacts(submitted_at);


-- ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
-- TABLE: workflow_events
-- ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
--
-- Purpose: Naplózza az FSM state-change event-eket az agent workflow-ján belül.
--          Audit trail: ki milyen state-ből milyen state-be ment, mikor, mi indította ki.
--
-- Fields:
--   id: UUID, PRIMARY KEY — event azonosító
--   session_id: UUID, FOREIGN KEY → sessions(id) — melyik session-höz tartozik
--   event_type: TEXT — event típusa (state_change, artifact_submitted, error, stb.)
--   state_before: TEXT (nullable) — az előző FSM state
--   state_after: TEXT (nullable) — az új FSM state
--   evidence_artifact_id: UUID (nullable), FOREIGN KEY → artifacts(id) — mely artifact indította az event-et
--   timestamp: TEXT (ISO 8601) — event időpont
--
-- Constraints:
--   PRIMARY KEY (id)
--   FOREIGN KEY (session_id) → sessions(id) ON DELETE CASCADE
--   FOREIGN KEY (evidence_artifact_id) → artifacts(id) ON DELETE SET NULL

CREATE TABLE IF NOT EXISTS workflow_events (
    id TEXT PRIMARY KEY COLLATE NOCASE,
    session_id TEXT NOT NULL COLLATE NOCASE,
    event_type TEXT NOT NULL,
    state_before TEXT,
    state_after TEXT,
    evidence_artifact_id TEXT COLLATE NOCASE,
    timestamp TEXT NOT NULL,
    FOREIGN KEY (session_id) REFERENCES sessions(id) ON DELETE CASCADE,
    FOREIGN KEY (evidence_artifact_id) REFERENCES artifacts(id) ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS idx_workflow_events_session_id ON workflow_events(session_id);
CREATE INDEX IF NOT EXISTS idx_workflow_events_event_type ON workflow_events(event_type);
CREATE INDEX IF NOT EXISTS idx_workflow_events_timestamp ON workflow_events(timestamp);


-- ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
-- TABLE: checkpoints
-- ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
--
-- Purpose: Tárolo a session checkpoint-okat session recovery-hez (M02 EPIC-12).
--          Az agent munka közben elmenthet egy checkpoint-ot, és később vissza tud tölteni.
--
-- Fields:
--   id: UUID, PRIMARY KEY — checkpoint azonosító
--   session_id: UUID, FOREIGN KEY → sessions(id) — melyik session-höz tartozik
--   checkpoint_data: TEXT (JSON) — checkpoint state (JSON serialized)
--   created_at: TEXT (ISO 8601) — checkpoint időpont
--
-- Constraints:
--   PRIMARY KEY (id)
--   FOREIGN KEY (session_id) → sessions(id) ON DELETE CASCADE

CREATE TABLE IF NOT EXISTS checkpoints (
    id TEXT PRIMARY KEY COLLATE NOCASE,
    session_id TEXT NOT NULL COLLATE NOCASE,
    checkpoint_data TEXT NOT NULL,
    created_at TEXT NOT NULL,
    FOREIGN KEY (session_id) REFERENCES sessions(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_checkpoints_session_id ON checkpoints(session_id);
CREATE INDEX IF NOT EXISTS idx_checkpoints_created_at ON checkpoints(created_at);

--
-- End of migration: 002_write_layer_schema.sql
--
