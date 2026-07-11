-- TASK-12-02: FTS5 Search for Episodes
-- This creates a content-synced virtual table for episodic memory
-- and the corresponding triggers to maintain it automatically.

-- 1. Create content-sync FTS5 virtual table
CREATE VIRTUAL TABLE IF NOT EXISTS episodes_fts USING fts5(
  outcome_summary,
  phase,
  domain,
  content='episodes',
  content_rowid='rowid'
);

-- 2. Trigger: auto-sync INSERT
-- Explicitly pass the rowid mapping since it's a content-synced table
CREATE TRIGGER IF NOT EXISTS episodes_ai AFTER INSERT ON episodes BEGIN
  INSERT INTO episodes_fts(rowid, outcome_summary, phase, domain)
  VALUES (NEW.rowid, NEW.outcome_summary, NEW.phase, NEW.domain);
END;

-- 3. Trigger: auto-sync DELETE
-- Use the special 'delete' command as required by FTS5 content tables
CREATE TRIGGER IF NOT EXISTS episodes_ad AFTER DELETE ON episodes BEGIN
  INSERT INTO episodes_fts(episodes_fts, rowid, outcome_summary, phase, domain)
  VALUES ('delete', OLD.rowid, OLD.outcome_summary, OLD.phase, OLD.domain);
END;

-- 4. Trigger: auto-sync UPDATE
-- UPDATE is implemented as a DELETE followed by an INSERT in FTS5
CREATE TRIGGER IF NOT EXISTS episodes_au AFTER UPDATE ON episodes BEGIN
  INSERT INTO episodes_fts(episodes_fts, rowid, outcome_summary, phase, domain)
  VALUES ('delete', OLD.rowid, OLD.outcome_summary, OLD.phase, OLD.domain);
  INSERT INTO episodes_fts(rowid, outcome_summary, phase, domain)
  VALUES (NEW.rowid, NEW.outcome_summary, NEW.phase, NEW.domain);
END;
