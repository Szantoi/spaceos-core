-- ============================================================================
-- EPIC-11: FSM Schema & Data Model — Workflow State Tracking
-- ============================================================================
-- Date: 2026-03-08
-- Author: Backend Developer (M02)
-- Status: IMPLEMENTING
-- Related: TASK-11-01
-- ============================================================================

-- ============================================================================
-- 1. WORKFLOW_DEFINITIONS TABLE
-- ============================================================================
-- Purpose: Global workflow templates (delivery vs discovery)
-- track: 'delivery' or 'discovery'
-- states: JSON array of valid state names in order

CREATE TABLE IF NOT EXISTS workflow_definitions (
    workflow_id       TEXT PRIMARY KEY,
    name              TEXT NOT NULL,
    track             TEXT NOT NULL CHECK (track IN ('delivery', 'discovery')),
    states            TEXT NOT NULL, -- JSON array
    version           TEXT DEFAULT '1.0',
    created_at        TEXT DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS idx_workflow_defs_track ON workflow_definitions(track);

-- ============================================================================
-- 2. FSM_STATE_TRANSITIONS TABLE
-- ============================================================================
-- Purpose: Detailed transition rules per workflow
-- valid_transitions: JSON array of allowed target states from this state

CREATE TABLE IF NOT EXISTS fsm_state_transitions (
    workflow_id       TEXT NOT NULL,
    state_name        TEXT NOT NULL,
    state_order       INTEGER NOT NULL,
    valid_transitions TEXT NOT NULL, -- JSON array of state names
    PRIMARY KEY (workflow_id, state_name),
    FOREIGN KEY (workflow_id) REFERENCES workflow_definitions(workflow_id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_fsm_state_transitions_wf ON fsm_state_transitions(workflow_id);

-- ============================================================================
-- 3. AGENT_SESSIONS TABLE
-- ============================================================================
-- Purpose: Active session state tracking
-- Relationship: Linked to roles (domain, role_name) from EPIC-09

CREATE TABLE IF NOT EXISTS agent_sessions (
    session_id        TEXT PRIMARY KEY, -- UUID
    domain            TEXT NOT NULL,
    role_name         TEXT NOT NULL,
    workflow_id       TEXT NOT NULL,
    current_state     TEXT NOT NULL,
    last_action       TEXT,
    updated_at        TEXT DEFAULT (datetime('now')),
    created_at        TEXT DEFAULT (datetime('now')),
    FOREIGN KEY (domain, role_name) REFERENCES roles(domain, role_name),
    FOREIGN KEY (workflow_id) REFERENCES workflow_definitions(workflow_id)
);

CREATE INDEX IF NOT EXISTS idx_sessions_role ON agent_sessions(domain, role_name);
CREATE INDEX IF NOT EXISTS idx_sessions_workflow ON agent_sessions(workflow_id);
CREATE INDEX IF NOT EXISTS idx_sessions_state ON agent_sessions(current_state);

-- ============================================================================
-- 4. SESSION_HISTORY TABLE
-- ============================================================================
-- Purpose: Audit trail of all state transitions

CREATE TABLE IF NOT EXISTS session_history (
    id                INTEGER PRIMARY KEY AUTOINCREMENT,
    session_id        TEXT NOT NULL,
    state_before      TEXT NOT NULL,
    state_after       TEXT NOT NULL,
    action            TEXT NOT NULL,
    metadata          TEXT, -- Optional JSON context
    timestamp         TEXT DEFAULT (datetime('now')),
    FOREIGN KEY (session_id) REFERENCES agent_sessions(session_id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_history_session ON session_history(session_id);

-- ============================================================================
-- 5. SEEDING: DEFAULT WORKFLOW (agile-epic-lifecycle-v1)
-- ============================================================================

INSERT OR IGNORE INTO workflow_definitions (workflow_id, name, track, states)
VALUES ('agile-epic-lifecycle-v1', 'Agile Epic Lifecycle (7-State)', 'delivery',
        '["INITIALIZED", "IN_PROGRESS", "UNDER_REVIEW", "NEEDS_REVISION", "APPROVED", "READY_FOR_MERGE", "COMPLETED"]');

-- Transitions for 'agile-epic-lifecycle-v1'
INSERT OR IGNORE INTO fsm_state_transitions (workflow_id, state_name, state_order, valid_transitions) VALUES
('agile-epic-lifecycle-v1', 'INITIALIZED', 1, '["IN_PROGRESS", "COMPLETED"]'),
('agile-epic-lifecycle-v1', 'IN_PROGRESS', 2, '["IN_PROGRESS", "UNDER_REVIEW", "COMPLETED"]'),
('agile-epic-lifecycle-v1', 'UNDER_REVIEW', 3, '["APPROVED", "NEEDS_REVISION", "IN_PROGRESS", "COMPLETED"]'),
('agile-epic-lifecycle-v1', 'NEEDS_REVISION', 4, '["IN_PROGRESS", "COMPLETED"]'),
('agile-epic-lifecycle-v1', 'APPROVED', 5, '["READY_FOR_MERGE", "COMPLETED"]'),
('agile-epic-lifecycle-v1', 'READY_FOR_MERGE', 6, '["COMPLETED"]'),
('agile-epic-lifecycle-v1', 'COMPLETED', 7, '[]');

-- ============================================================================
-- 6. UPDATE SCHEMA METADATA
-- ============================================================================
-- Increment read-layer version (or fsm-layer if we separate it later)

UPDATE schema_metadata
SET version = 2, last_updated = datetime('now')
WHERE layer = 'read-layer';

-- END OF MIGRATION
