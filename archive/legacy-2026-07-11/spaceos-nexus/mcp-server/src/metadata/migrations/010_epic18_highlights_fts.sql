-- Migration 010: EPIC-18 FTS5 Highlights Index
--
-- Adds full-text search indexing for highlights.
-- Indexes: key_decisions, lessons, next_steps for fast keyword retrieval.
-- Requires: episode_highlights table (from migration 009).
-- Safe to re-run (CREATE VIRTUAL TABLE IF NOT EXISTS).

-- FTS5 virtual table for full-text search
CREATE VIRTUAL TABLE IF NOT EXISTS highlights_fts USING fts5(
  highlight_id UNINDEXED,
  content,
  tokenize = 'porter'
);

-- Populate the FTS5 table with existing highlights
-- (This will be a no-op if already populated; actual sync happens in application code)
-- Format: highlight_id || ' ' || key_decisions || ' ' || lessons || ' ' || next_steps
INSERT OR IGNORE INTO highlights_fts
SELECT
  h.id as highlight_id,
  COALESCE(h.key_decisions, '') || ' ' ||
  COALESCE(h.lessons, '') || ' ' ||
  COALESCE(h.next_steps, '') as content
FROM episode_highlights h;

-- Trigger to automatically sync highlights_fts when episode_highlights changes
-- This ensures the FTS5 index stays in sync with base table
DROP TRIGGER IF EXISTS highlights_fts_ai;
CREATE TRIGGER highlights_fts_ai AFTER INSERT ON episode_highlights BEGIN
  INSERT INTO highlights_fts(highlight_id, content)
  VALUES(new.id, COALESCE(new.key_decisions, '') || ' ' || COALESCE(new.lessons, '') || ' ' || COALESCE(new.next_steps, ''));
END;

DROP TRIGGER IF EXISTS highlights_fts_ad;
CREATE TRIGGER highlights_fts_ad AFTER DELETE ON episode_highlights BEGIN
  DELETE FROM highlights_fts WHERE highlight_id = old.id;
END;

DROP TRIGGER IF EXISTS highlights_fts_au;
CREATE TRIGGER highlights_fts_au AFTER UPDATE ON episode_highlights BEGIN
  DELETE FROM highlights_fts WHERE highlight_id = old.id;
  INSERT INTO highlights_fts(highlight_id, content)
  VALUES(new.id, COALESCE(new.key_decisions, '') || ' ' || COALESCE(new.lessons, '') || ' ' || COALESCE(new.next_steps, ''));
END;
