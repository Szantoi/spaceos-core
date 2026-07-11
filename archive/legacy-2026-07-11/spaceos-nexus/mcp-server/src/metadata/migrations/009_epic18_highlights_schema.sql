-- Migration 009: EPIC-18 Self-Reflection Highlights Schema
--
-- Adds storage for generated highlights, manual feedback, and ChromaDB sync metadata.
-- Requires `episodes` table (loaded from episodic migration 003_episodes.sql).
-- Safe to re-run (CREATE TABLE IF NOT EXISTS).

CREATE TABLE IF NOT EXISTS episode_highlights (
  id             TEXT PRIMARY KEY,
  episode_id     TEXT NOT NULL UNIQUE,
  key_decisions  TEXT,
  lessons        TEXT,
  next_steps     TEXT,
  quality_score  REAL,
  ai_generated   INTEGER NOT NULL DEFAULT 1,
  ai_model       TEXT,
  ai_tokens_used INTEGER,
  created_at     TEXT NOT NULL DEFAULT (datetime('now')),
  FOREIGN KEY (episode_id) REFERENCES episodes(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS highlight_feedback (
  id             TEXT PRIMARY KEY,
  highlight_id   TEXT NOT NULL,
  rater_agent_id TEXT,
  quality_score  REAL,
  comment        TEXT,
  created_at     TEXT NOT NULL DEFAULT (datetime('now')),
  FOREIGN KEY (highlight_id) REFERENCES episode_highlights(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_highlight_feedback_highlight_id
  ON highlight_feedback(highlight_id);

CREATE TABLE IF NOT EXISTS highlights_chromadb_sync (
  highlight_id     TEXT PRIMARY KEY,
  vector_id        TEXT,
  embedding_model  TEXT,
  last_synced      TEXT,
  FOREIGN KEY (highlight_id) REFERENCES episode_highlights(id) ON DELETE CASCADE
);
