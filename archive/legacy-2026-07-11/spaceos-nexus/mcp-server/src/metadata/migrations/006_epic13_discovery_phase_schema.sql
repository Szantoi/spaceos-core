-- Migration: add discovery_phases table for TASK-13-05
-- This schema supports tracking phase completion for discovery-track sessions.

CREATE TABLE IF NOT EXISTS discovery_phases (
  session_id TEXT PRIMARY KEY,
  current_phase TEXT NOT NULL DEFAULT 'ideation',
  ideation_complete INTEGER NOT NULL DEFAULT 0,
  validation_complete INTEGER NOT NULL DEFAULT 0,
  iteration_complete INTEGER NOT NULL DEFAULT 0,
  updated_at TEXT NOT NULL DEFAULT (datetime('now')),
  FOREIGN KEY(session_id) REFERENCES agent_sessions(session_id) ON DELETE CASCADE
);
