-- ============================================================================
-- TASK-12-01: Episodic Memory Schema — episodes table
-- ============================================================================
-- Date: 2026-03-08
-- Author: Backend Developer (Dev D)
-- Status: READY
-- Epic: EPIC-12 (Episodic Memory — Store, Search, Reflect)
-- ============================================================================
-- Design: Standalone table; no FK to agent_sessions (loose coupling)
--         TASK-12-02 adds FTS5 virtual table on top of this schema
--         TASK-12-03 adds ChromaDB semantic index on outcome_summary
-- ============================================================================

-- ============================================================================
-- 1. EPISODES TABLE — Snapshot of agent execution per phase
-- ============================================================================
-- Each row represents one "episode":
--   - A session doing work in a specific domain / track / phase
--   - tool_calls_json: JSON array of { tool, args, result? }
--   - artifacts_json:  JSON array of { type, path, hash? }
--   - outcome_summary: Plain-text summary (used by ChromaDB embedding in 12-03)

CREATE TABLE IF NOT EXISTS episodes (
    id                  TEXT PRIMARY KEY,           -- UUID v4 (ep_<timestamp>_<random>)
    session_id          TEXT NOT NULL,              -- Links to agent session (loose FK)
    domain              TEXT NOT NULL,              -- 'discovery' | 'engineering' | 'testing' | 'deployment'
    track               TEXT NOT NULL,              -- e.g. 'user-story-1', 'bug-fix-xyz'
    phase               TEXT NOT NULL,              -- 'ideation' | 'implementation' | 'review' | 'refinement'
    tool_calls_json     TEXT,                       -- JSON array of ToolCallRecord
    artifacts_json      TEXT,                       -- JSON array of ArtifactRecord
    outcome_summary     TEXT NOT NULL,              -- Human-readable result (1-500 chars recommended)
    created_at          DATETIME DEFAULT (datetime('now'))
);

-- ============================================================================
-- 2. INDEXES — Optimized for TASK-12-02 FTS5 and direct query patterns
-- ============================================================================

-- Composite index: domain + track + session filtering (primary query pattern)
CREATE INDEX IF NOT EXISTS idx_episodes_domain_track_session
    ON episodes(domain, track, session_id);

-- Session index: retrieve all episodes for a given session
CREATE INDEX IF NOT EXISTS idx_episodes_session
    ON episodes(session_id);

-- ============================================================================
-- VERIFICATION
-- ============================================================================
-- SELECT name FROM sqlite_master WHERE type='table' AND name='episodes';
-- Expected: 'episodes'
-- SELECT name FROM sqlite_master WHERE type='index' AND tbl_name='episodes';
-- Expected: 'idx_episodes_domain_track_session', 'idx_episodes_session'

-- END OF MIGRATION
