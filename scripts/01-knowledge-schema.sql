-- SpaceOS Knowledge Service — Phase 1 DDL
-- Database: spaceos_knowledge
-- Schema: knowledge
-- Indexes: TSVECTOR (GIN) for FTS search
-- Security: RLS + admin_full_access policy
--
-- INFRA Phase 1 Task
-- Deploy: sudo -u postgres psql -p 5433 < /opt/spaceos/scripts/01-knowledge-schema.sql

-- Create database (if not exists)
CREATE DATABASE IF NOT EXISTS spaceos_knowledge;

-- Switch to knowledge database
\c spaceos_knowledge

-- Create schema
CREATE SCHEMA IF NOT EXISTS knowledge;

-- Main documents table
CREATE TABLE IF NOT EXISTS knowledge.documents (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    file_path       TEXT NOT NULL UNIQUE,
    source_type     TEXT NOT NULL CHECK (source_type IN ('knowledge', 'memory')),
    category        TEXT,
    terminal        TEXT,
    title           TEXT NOT NULL,
    content         TEXT NOT NULL,
    content_tsvector TSVECTOR GENERATED ALWAYS AS (
        to_tsvector('simple', coalesce(title, '') || ' ' || content)
    ) STORED,
    content_hash    TEXT NOT NULL,
    word_count      INT NOT NULL DEFAULT 0,
    indexed_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Enable Row Level Security
ALTER TABLE knowledge.documents ENABLE ROW LEVEL SECURITY;

-- RLS Policy: admin full access (no restriction, all operations)
CREATE POLICY IF NOT EXISTS admin_full_access ON knowledge.documents
FOR ALL USING (true);

-- Indexes
-- 1. TSVECTOR index (GIN for FTS)
CREATE INDEX IF NOT EXISTS idx_documents_tsvector
ON knowledge.documents USING GIN (content_tsvector);

-- 2. Source type index (filtering)
CREATE INDEX IF NOT EXISTS idx_documents_source
ON knowledge.documents (source_type);

-- 3. Category index (browsing)
CREATE INDEX IF NOT EXISTS idx_documents_category
ON knowledge.documents (category);

-- 4. Terminal index (project memory tracking)
CREATE INDEX IF NOT EXISTS idx_documents_terminal
ON knowledge.documents (terminal);

-- 5. Updated timestamp (cron ingestion ordering)
CREATE INDEX IF NOT EXISTS idx_documents_updated
ON knowledge.documents (updated_at DESC);

-- Verification: Display created tables and indexes
\echo '=== Schema Created ==='
\d knowledge.documents
\di knowledge.*
