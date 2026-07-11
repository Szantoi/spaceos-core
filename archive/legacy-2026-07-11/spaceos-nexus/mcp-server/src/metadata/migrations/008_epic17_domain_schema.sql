-- Migration 008: EPIC-17 Domain Registry Schema
-- Creates the `domains` table for domain-agnostic multi-tenant support.
--
-- NOTE: ALTER TABLE for nullable FK columns on `roles` and `sessions`
-- is performed in AgentDb.initSchema() with try/catch for idempotency,
-- because SQLite has no IF NOT EXISTS clause for ALTER TABLE ADD COLUMN.
--
-- Safe to re-run (all DDL uses IF NOT EXISTS).

-- ─── Domain Registry ───────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS domains (
  id          TEXT PRIMARY KEY,
  name        TEXT NOT NULL UNIQUE,
  description TEXT,
  config_json TEXT,
  created_at  TEXT NOT NULL DEFAULT (datetime('now')),
  updated_at  TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS idx_domains_name ON domains(name);
