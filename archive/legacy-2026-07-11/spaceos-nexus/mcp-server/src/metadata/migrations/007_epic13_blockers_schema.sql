-- Migration: add blockers table for TASK-13-06
-- Tracks discovery blockers logged by agents during ideation/validation/iteration.

CREATE TABLE IF NOT EXISTS blockers (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  session_id TEXT NOT NULL,
  phase TEXT NOT NULL,
  severity TEXT NOT NULL,
  text TEXT NOT NULL,
  created_at TEXT NOT NULL DEFAULT (datetime('now')),
  FOREIGN KEY(session_id) REFERENCES agent_sessions(session_id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_blockers_session_phase
  ON blockers(session_id, phase);
