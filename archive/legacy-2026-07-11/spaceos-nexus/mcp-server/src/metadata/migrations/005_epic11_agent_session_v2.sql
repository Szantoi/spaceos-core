-- ============================================================================
-- MIGRATION: 005_epic11_agent_session_v2.sql
-- EPIC-11: Agent Session FSM Schema — v2 (correct 7-state lowercase model)
-- ============================================================================
-- Date:    2026-03-08
-- Author:  Dev A (TASK-11-01)
-- Epic:    EPIC-11 (Request Context Middleware, RBAC Migration & Error Standardization)
-- Status:  IMPLEMENTING
--
-- Purpose:
--   Extends the existing FSM schema (migration 004) with the canonical
--   7-state agent session model aligned to EPIC-11 FSM specification.
--   Adds retry_count + completed_at to agent_sessions.
--   Adds metadata column to session_history for structured audit context.
--   Seeds the two standard workflow definitions (discovery + delivery).
--
-- Rollback strategy (see section 7 below):
--   DROP TABLE IF EXISTS workflow_definitions_v2;
--   DROP TABLE IF EXISTS fsm_state_transitions_v2;
--   ALTER TABLE agent_sessions DROP COLUMN retry_count;
--   ALTER TABLE agent_sessions DROP COLUMN completed_at;
--   ALTER TABLE session_history DROP COLUMN metadata;
--
-- Backward compatibility:
--   - Migration 004 tables (workflow_definitions, fsm_state_transitions,
--     agent_sessions, session_history) are NOT dropped or modified in a
--     breaking way. Data from 004 remains intact.
--   - New columns (retry_count, completed_at, metadata) use DEFAULT values,
--     so existing rows remain valid.
--   - FSMTransitionValidator (TASK-11-03) references the v2 tables by name.
--
-- Dependencies:
--   - 004_epic11_fsm_schema.sql (must be applied first)
--   - PRAGMA foreign_keys = ON
--   - PRAGMA journal_mode = WAL
-- ============================================================================

-- ============================================================================
-- 1. ADD MISSING COLUMNS TO agent_sessions (idempotent via existence check)
-- ============================================================================
-- SQLite does not support "IF NOT EXISTS" for columns, so we use a
-- two-statement approach: attempt the ADD, ignore the error if already present.
-- In practice, the migrate-on-startup runner catches SQLITE_ERROR and skips.

ALTER TABLE agent_sessions ADD COLUMN retry_count    INTEGER NOT NULL DEFAULT 0;
ALTER TABLE agent_sessions ADD COLUMN completed_at   TEXT;  -- ISO 8601, NULL until terminal

-- ============================================================================
-- 2. ADD METADATA COLUMN TO session_history
-- ============================================================================

ALTER TABLE session_history ADD COLUMN metadata TEXT;  -- JSON context string

-- ============================================================================
-- 3. CANONICAL WORKFLOW DEFINITIONS (v2 — lowercase states)
-- ============================================================================
-- Seeds the two standard tracks as workflow_definitions rows.
-- Uses INSERT OR IGNORE to be idempotent.

-- 3a. Agent Workflow — Discovery Track
INSERT OR IGNORE INTO workflow_definitions (workflow_id, name, track, states, version)
VALUES (
    'agent-discovery-v1',
    'Agent Discovery Workflow (7-State FSM)',
    'discovery',
    '["initialized","briefed","in_progress","awaiting_input","ready_to_submit","submitted","abandoned"]',
    '2.0'
);

-- 3b. Agent Workflow — Delivery Track
INSERT OR IGNORE INTO workflow_definitions (workflow_id, name, track, states, version)
VALUES (
    'agent-delivery-v1',
    'Agent Delivery Workflow (7-State FSM)',
    'delivery',
    '["initialized","briefed","in_progress","awaiting_input","ready_to_submit","submitted","abandoned"]',
    '2.0'
);

-- ============================================================================
-- 4. FSM TRANSITION RULES — agent-discovery-v1
-- ============================================================================

INSERT OR IGNORE INTO fsm_state_transitions (workflow_id, state_name, state_order, valid_transitions) VALUES
('agent-discovery-v1', 'initialized',     1, '["briefed","abandoned"]'),
('agent-discovery-v1', 'briefed',         2, '["in_progress","abandoned"]'),
('agent-discovery-v1', 'in_progress',     3, '["in_progress","awaiting_input","ready_to_submit","abandoned"]'),
('agent-discovery-v1', 'awaiting_input',  4, '["in_progress","abandoned"]'),
('agent-discovery-v1', 'ready_to_submit', 5, '["submitted","in_progress","abandoned"]'),
('agent-discovery-v1', 'submitted',       6, '[]'),
('agent-discovery-v1', 'abandoned',       7, '[]');

-- ============================================================================
-- 5. FSM TRANSITION RULES — agent-delivery-v1
-- ============================================================================

INSERT OR IGNORE INTO fsm_state_transitions (workflow_id, state_name, state_order, valid_transitions) VALUES
('agent-delivery-v1', 'initialized',     1, '["briefed","abandoned"]'),
('agent-delivery-v1', 'briefed',         2, '["in_progress","abandoned"]'),
('agent-delivery-v1', 'in_progress',     3, '["in_progress","awaiting_input","ready_to_submit","abandoned"]'),
('agent-delivery-v1', 'awaiting_input',  4, '["in_progress","abandoned"]'),
('agent-delivery-v1', 'ready_to_submit', 5, '["submitted","in_progress","abandoned"]'),
('agent-delivery-v1', 'submitted',       6, '[]'),
('agent-delivery-v1', 'abandoned',       7, '[]');

-- ============================================================================
-- 6. INDEXES FOR PERFORMANCE
-- ============================================================================

-- Quickly find sessions ready for resumption
CREATE INDEX IF NOT EXISTS idx_sessions_state_v2       ON agent_sessions(current_state);
CREATE INDEX IF NOT EXISTS idx_sessions_retry          ON agent_sessions(retry_count)
    WHERE retry_count > 0;

-- Quickly filter completed/abandoned sessions
CREATE INDEX IF NOT EXISTS idx_sessions_completed      ON agent_sessions(completed_at)
    WHERE completed_at IS NOT NULL;

-- History lookup by session (most common query pattern)
CREATE INDEX IF NOT EXISTS idx_history_session_ts      ON session_history(session_id, timestamp);

-- ============================================================================
-- 7. SCHEMA METADATA UPDATE
-- ============================================================================

UPDATE schema_metadata
SET version      = version + 1,
    last_updated = datetime('now')
WHERE layer = 'fsm-layer';

-- ============================================================================
-- ROLLBACK STRATEGY
-- ============================================================================
--
-- If this migration must be reverted:
--
--   BEGIN TRANSACTION;
--
--   -- Remove new indexes
--   DROP INDEX IF EXISTS idx_sessions_state_v2;
--   DROP INDEX IF EXISTS idx_sessions_retry;
--   DROP INDEX IF EXISTS idx_sessions_completed;
--   DROP INDEX IF EXISTS idx_history_session_ts;
--
--   -- Remove seeded v2 workflows
--   DELETE FROM fsm_state_transitions
--     WHERE workflow_id IN ('agent-discovery-v1', 'agent-delivery-v1');
--   DELETE FROM workflow_definitions
--     WHERE workflow_id IN ('agent-discovery-v1', 'agent-delivery-v1');
--
--   -- NOTE: SQLite does not support DROP COLUMN below version 3.35.0.
--   -- For older SQLite, recreate agent_sessions and session_history
--   -- without the new columns.
--   --
--   -- For SQLite >= 3.35.0:
--   ALTER TABLE agent_sessions   DROP COLUMN retry_count;
--   ALTER TABLE agent_sessions   DROP COLUMN completed_at;
--   ALTER TABLE session_history  DROP COLUMN metadata;
--
--   COMMIT;
--
-- ============================================================================
-- END OF MIGRATION
-- ============================================================================
