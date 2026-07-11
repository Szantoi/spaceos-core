-- ============================================================================
-- EPISODE-09: Context Layer Schema — SSOT for role identities + workflows
-- ============================================================================
-- Date: 2026-03-05
-- Author: Backend Developer (M02)
-- Status: APPROVED by Architect (ADR-001)
-- ============================================================================
-- Design: Loose coupling with EPIC-08 write-layer (no FK cross-layer)
-- Tables: 6 (roles, role_schemas, runbooks, workflows, templates, standards)
-- Indexes: 10+ composite on (domain, role_name) for query performance
-- ============================================================================

-- Enable foreign keys (must be done at connection time, but documented here)
-- PRAGMA foreign_keys = ON;

-- ============================================================================
-- 1. ROLES TABLE — Core agent identity repository
-- ============================================================================
-- Purpose: Single source of truth for role definitions
-- Identity: UNIQUE(domain, role_name) composite
-- FK targets: (domain, role_name) from role_schemas, runbooks, workflows, templates

CREATE TABLE IF NOT EXISTS roles (
    id                INTEGER PRIMARY KEY AUTOINCREMENT,
    domain            TEXT NOT NULL,
    role_name         TEXT NOT NULL,
    content           TEXT NOT NULL,
    version           TEXT DEFAULT '1.0',
    last_updated      TEXT DEFAULT (datetime('now')),
    created_at        TEXT DEFAULT (datetime('now')),
    UNIQUE(domain, role_name)
);

CREATE INDEX IF NOT EXISTS idx_roles_domain_role ON roles(domain, role_name);
CREATE INDEX IF NOT EXISTS idx_roles_domain ON roles(domain);

-- ============================================================================
-- 2. ROLE_SCHEMAS TABLE — Capabilities + personalization
-- ============================================================================
-- Purpose: Role-specific capabilities, persona, tool permissions
-- Relationship: 1:1 with roles (one schema per role)
-- Identity: UNIQUE(domain, role_name)
-- JSON Columns: mcp_tool_permissions (array), limitations (array), handoff_triggers (array)

CREATE TABLE IF NOT EXISTS role_schemas (
    id                    INTEGER PRIMARY KEY AUTOINCREMENT,
    domain                TEXT NOT NULL,
    role_name             TEXT NOT NULL,
    persona_identity      TEXT,
    persona_style         TEXT,
    mcp_tool_permissions  TEXT NOT NULL,
    limitations           TEXT,
    handoff_triggers      TEXT,
    version               TEXT DEFAULT '1.0',
    last_updated          TEXT DEFAULT (datetime('now')),
    created_at            TEXT DEFAULT (datetime('now')),
    UNIQUE(domain, role_name),
    FOREIGN KEY (domain, role_name) REFERENCES roles(domain, role_name) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_schemas_domain_role ON role_schemas(domain, role_name);
CREATE INDEX IF NOT EXISTS idx_schemas_domain ON role_schemas(domain);
CREATE INDEX IF NOT EXISTS idx_schemas_role_name ON role_schemas(role_name);

-- ============================================================================
-- 3. RUNBOOKS TABLE — Procedural guidance (1 per role)
-- ============================================================================
-- Purpose: Step-by-step operational procedures
-- Relationship: 1:1 with roles (one runbook per role)
-- Identity: UNIQUE(domain, role_name)

CREATE TABLE IF NOT EXISTS runbooks (
    id                INTEGER PRIMARY KEY AUTOINCREMENT,
    domain            TEXT NOT NULL,
    role_name         TEXT NOT NULL,
    content           TEXT NOT NULL,
    version           TEXT DEFAULT '1.0',
    last_updated      TEXT DEFAULT (datetime('now')),
    created_at        TEXT DEFAULT (datetime('now')),
    UNIQUE(domain, role_name),
    FOREIGN KEY (domain, role_name) REFERENCES roles(domain, role_name) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_runbooks_domain_role ON runbooks(domain, role_name);
CREATE INDEX IF NOT EXISTS idx_runbooks_domain ON runbooks(domain);

-- ============================================================================
-- 4. WORKFLOWS TABLE — Role-specific execution patterns (N per role)
-- ============================================================================
-- Purpose: FSM definitions, execution flows, state transitions
-- Relationship: N:1 with roles (multiple workflows per role: discovery, delivery, ideation)
-- Identity: UNIQUE(domain, role_name, workflow_type)

CREATE TABLE IF NOT EXISTS workflows (
    id                INTEGER PRIMARY KEY AUTOINCREMENT,
    domain            TEXT NOT NULL,
    role_name         TEXT NOT NULL,
    workflow_type     TEXT NOT NULL DEFAULT 'default',
    content           TEXT NOT NULL,
    version           TEXT DEFAULT '1.0',
    last_updated      TEXT DEFAULT (datetime('now')),
    created_at        TEXT DEFAULT (datetime('now')),
    UNIQUE(domain, role_name, workflow_type),
    FOREIGN KEY (domain, role_name) REFERENCES roles(domain, role_name) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_workflows_domain_role ON workflows(domain, role_name);
CREATE INDEX IF NOT EXISTS idx_workflows_domain ON workflows(domain);
CREATE INDEX IF NOT EXISTS idx_workflows_type ON workflows(workflow_type);
CREATE INDEX IF NOT EXISTS idx_workflows_domain_type ON workflows(domain, workflow_type);

-- ============================================================================
-- 5. TEMPLATES TABLE — Reusable response templates (N per role)
-- ============================================================================
-- Purpose: Prompt templates, response patterns, code snippets
-- Relationship: N:1 with roles (multiple templates per role)
-- Identity: UNIQUE(domain, role_name, template_name)

CREATE TABLE IF NOT EXISTS templates (
    id                INTEGER PRIMARY KEY AUTOINCREMENT,
    domain            TEXT NOT NULL,
    role_name         TEXT NOT NULL,
    template_name     TEXT NOT NULL,
    content           TEXT NOT NULL,
    version           TEXT DEFAULT '1.0',
    last_updated      TEXT DEFAULT (datetime('now')),
    created_at        TEXT DEFAULT (datetime('now')),
    UNIQUE(domain, role_name, template_name),
    FOREIGN KEY (domain, role_name) REFERENCES roles(domain, role_name) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_templates_domain_role ON templates(domain, role_name);
CREATE INDEX IF NOT EXISTS idx_templates_domain ON templates(domain);
CREATE INDEX IF NOT EXISTS idx_templates_name ON templates(template_name);
CREATE INDEX IF NOT EXISTS idx_templates_domain_name ON templates(domain, template_name);

-- ============================================================================
-- 6. STANDARDS TABLE — Global best practices (not role-specific)
-- ============================================================================
-- Purpose: ADRs, standards, patterns, global guidelines (available to all roles)
-- Relationship: None (standalone)
-- Identity: UNIQUE(std_id)

CREATE TABLE IF NOT EXISTS standards (
    id                INTEGER PRIMARY KEY AUTOINCREMENT,
    std_id            TEXT NOT NULL UNIQUE,
    category          TEXT,
    title             TEXT,
    content           TEXT NOT NULL,
    version           TEXT DEFAULT '1.0',
    last_updated      TEXT DEFAULT (datetime('now')),
    created_at        TEXT DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS idx_standards_id ON standards(std_id);
CREATE INDEX IF NOT EXISTS idx_standards_category ON standards(category);
CREATE INDEX IF NOT EXISTS idx_standards_title ON standards(title);

-- ============================================================================
-- SCHEMA METADATA TABLE (TASK-09-04B) — Version tracking for migrations
-- ============================================================================
-- Purpose: Track schema versions for both read-layer (context) and write-layer
-- Usage: Agents load version at startup, detect updates at end of session
-- Identity: UNIQUE(layer) — one version per layer

CREATE TABLE IF NOT EXISTS schema_metadata (
    id               INTEGER PRIMARY KEY,
    layer            TEXT NOT NULL UNIQUE,
    version          INTEGER NOT NULL DEFAULT 1,
    last_updated     TEXT DEFAULT (datetime('now')),
    created_at       TEXT DEFAULT (datetime('now'))
);

-- Initialize with read-layer v1 and write-layer v1
INSERT OR IGNORE INTO schema_metadata (layer, version, created_at, last_updated)
VALUES ('read-layer', 1, datetime('now'), datetime('now'));

INSERT OR IGNORE INTO schema_metadata (layer, version, created_at, last_updated)
VALUES ('write-layer', 1, datetime('now'), datetime('now'));

CREATE INDEX IF NOT EXISTS idx_schema_metadata_layer ON schema_metadata(layer);

-- ============================================================================
-- VERIFICATION QUERY (for ContextSchemaInitializer)
-- ============================================================================
-- SELECT COUNT(*) as table_count FROM sqlite_master WHERE type='table'
--   AND name IN ('roles', 'role_schemas', 'runbooks', 'workflows', 'templates', 'standards', 'schema_metadata');
-- Expected: 7 tables

-- END OF MIGRATION
