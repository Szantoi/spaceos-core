/**
 * AgentDb Service Class — TypeScript wrapper for better-sqlite3
 *
 * Single entry point for all SQLite database operations in EPIC-09 context layer.
 * Provides type-safe query builders for roles, permissions, runbooks, workflows, templates, and standards.
 *
 * **Architecture:**
 * - Uses DatabaseConnectionManager for dual-pool strategy (admin=RW, agent=RO)
 * - Initializes both EPIC-08 (write layer) and EPIC-09 (context layer) schemas
 * - Enforces PRAGMA settings (foreign_keys, WAL mode)
 * - Provides zero-copy getter methods for context data
 * - Query results are read-only and immutable
 *
 * **Usage:**
 * ```typescript
 * const manager = new DatabaseConnectionManager('./metadata.db');
 * const agentDb = new AgentDb(manager);
 * agentDb.initSchema(); // Call once at startup
 *
 * const role = agentDb.getRole('engineering', 'backend_developer');
 * const roleSchema = agentDb.getRoleSchema('engineering', 'backend_developer');
 * ```
 */

import Database from 'better-sqlite3';
import { readFileSync } from 'fs';
import { join } from 'path';
import { DatabaseConnectionManager } from '../metadata/DatabaseConnectionManager';
import { computeQualityScore } from '../metadata/qualityScoring';
import type { DwiDashboardResponse } from '../metadata/dwi-schema';

// ─────────────────────────────────────────────────────────────────────────────
// TYPE DEFINITIONS — Context Layer (EPIC-09)
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Role definition row from `roles` table.
 *
 * Represents an agent persona (backend_developer, tech_lead, explorer, etc.)
 * tied to a specific domain (engineering, management, discovery).
 */
export interface RoleRow {
  id: number;
  domain: string;
  role_name: string;
  content: string;
  created_at: string;
  last_updated: string;
}

/**
 * Role schema row from `role_schemas` table.
 *
 * Defines MCP tool permissions and input/output schemas for a specific role.
 * Example: which tools can backend_developer use, what input params are allowed.
 */
export interface RoleSchemaRow {
  id: number;
  domain: string;
  role_name: string;
  mcp_tool_permissions: string; // JSON string
  created_at: string;
  last_updated: string;
}

/**
 * Runbook row from `runbooks` table.
 *
 * Step-by-step workflow for agents following a specific role.
 * Example: code review checklist, deployment procedure for backend_developer.
 */
export interface RunbookRow {
  id: number;
  domain: string;
  role_name: string;
  content: string;
  created_at: string;
  last_updated: string;
}

/**
 * Workflow row from `workflows` table.
 *
 * FSM workflow definition for a specific role and workflow type.
 * Example: feature delivery workflow, incident response workflow.
 */
export interface WorkflowRow {
  id: number;
  domain: string;
  role_name: string;
  workflow_type: string;
  content: string;
  created_at: string;
  last_updated: string;
}

/**
 * Template row from `templates` table.
 *
 * Document template for artifact generation.
 * Example: implementation_summary template, test_report template.
 */
export interface TemplateRow {
  id: number;
  domain: string;
  role_name: string;
  template_name: string;
  content: string;
  created_at: string;
  last_updated: string;
}

/**
 * Standard row from `standards` table.
 *
 * Organizational standards, ADRs, and best practices.
 * Example: naming conventions, architecture guidelines.
 */
export interface StandardRow {
  id: string;
  std_id: string;
  content: string;
  created_at: string;
  last_updated: string;
}

/**
 * Aggregated project state for PM query tools.
 */
export interface ProjectStateRow {
  project_id: string;
  milestone: string | null;
  open_tasks_count: number;
  due_date: string | null;
}

/**
 * Lightweight task projection for list/search tools.
 */
export interface PmTaskSummaryRow {
  id: string;
  title: string;
  status: string | null;
  assigned_to: string | null;
  domain: string | null;
  track: string | null;
}

/**
 * Full task context projection for get_task_context.
 */
export interface PmTaskContextRow extends PmTaskSummaryRow {
  description: string | null;
  acceptance_criteria: string | null;
  workflow: string | null;
  template: string | null;
}

// ─────────────────────────────────────────────────────────────────────────────
// AGENTDB SERVICE CLASS
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Domain registry row from `domains` table.
 *
 * Represents a named configuration scope (e.g., 'engineering', 'management').
 * Domains isolate roles, sessions, and knowledge from each other.
 */
export interface DomainRow {
  id: string;
  name: string;
  description: string | null;
  config_json: string | null;
  created_at: string;
  updated_at: string;
}

/**
 * Episode highlight row from `episode_highlights` table (EPIC-18).
 */
export interface EpisodeHighlightRow {
  id: string;
  episode_id: string;
  key_decisions: string | null;
  lessons: string | null;
  next_steps: string | null;
  quality_score: number | null;
  ai_generated: number;
  ai_model: string | null;
  ai_tokens_used: number | null;
  created_at: string;
}

/**
 * Episode highlight row joined with source episode metadata for reflection queries.
 */
export interface SessionEpisodeHighlightRow extends EpisodeHighlightRow {
  session_id: string;
  domain: string;
  track: string;
  outcome_summary: string | null;
}

/**
 * Manual feedback row from `highlight_feedback` table (EPIC-18).
 */
export interface HighlightFeedbackRow {
  id: string;
  highlight_id: string;
  rater_agent_id: string | null;
  quality_score: number | null;
  comment: string | null;
  created_at: string;
}

/**
 * ChromaDB sync metadata row from `highlights_chromadb_sync` table (EPIC-18).
 */
export interface HighlightChromaSyncRow {
  highlight_id: string;
  vector_id: string | null;
  embedding_model: string | null;
  last_synced: string | null;
}

/**
 * Discovery Work Item (DWI) row from `discovery_work_items` table (EPIC-18).
 */
export interface DiscoveryWorkItemRow {
  id: string;
  topic: string;
  status: 'open' | 'in_progress' | 'concluded' | 'archived';
  current_phase: number;
  next_action: string;
  verdict: 'validated' | 'invalidated' | 'pivoted' | null;
  hypothesis_count: number;
  validated_count: number;
  created_at: string;
  updated_at: string;
  created_by: string | null;
  updated_by: string | null;
}

export interface DwiPhaseGateRow {
  id: number;
  dwi_id: string;
  phase: number;
  gate_crossed: number;
  gate_crossed_date: string | null;
  notes: string | null;
  created_at: string;
}

export interface DwiHypothesisRow {
  id: string;
  dwi_id: string;
  statement: string;
  status: 'open' | 'testing' | 'validated' | 'invalidated';
  phase: number;
  artifact_path: string | null;
  created_at: string;
  closed_at: string | null;
  created_by: string | null;
  updated_by: string | null;
}

/**
 * Audit log cost tracking row from `audit_log` table (TASK-18-09).
 *
 * Represents a tool call with cost metadata for reflection operations.
 */
export interface AuditCostRow {
  id: number;
  session_id: string;
  tool_name: string;
  latency_ms: number;
  ai_model: string | null;
  ai_tokens_used: number | null;
  cost_amount_usd: number | null;
  timestamp: string;
}

/**
 * Aggregated cost metrics for a session (TASK-18-09).
 */
export interface SessionCostMetrics {
  session_id: string;
  total_tools_called: number;
  total_latency_ms: number;
  total_tokens_used: number | null;
  total_cost_usd: number | null;
  ai_models_used: string[];
}

// ─────────────────────────────────────────────────────────────────────────────
// AGENTDB SERVICE CLASS (continued)
// ─────────────────────────────────────────────────────────────────────────────

/**
 * AgentDb: Central database service for MCP server.
 *
 * Provides:
 * - Schema initialization (DDL migrations)
 * - Query builders with type safety
 * - FK & PRAGMA enforcement
 * - Prepared statement caching (via better-sqlite3)
 */
export class AgentDb {
  private connectionManager: DatabaseConnectionManager;
  private initialized: boolean = false;

  constructor(connectionManagerOrPath: DatabaseConnectionManager | string) {
    const connectionManager =
      typeof connectionManagerOrPath === 'string'
        ? new DatabaseConnectionManager(connectionManagerOrPath)
        : connectionManagerOrPath;
    /**
     * Initialize AgentDb with dual-pool connection manager.
     *
     * Admin pool is used for schema initialization and writes.
     * Agent pool is used for SELECT queries (read-only enforcement).
     */
    this.connectionManager = connectionManager;
  }

  /**
   * Initialize database schema (idempotent).
   *
   * Loads and executes DDL migrations:
   * - 002_write_layer_schema.sql (EPIC-08)
   * - 003_epic09_context_schema.sql (EPIC-09)
   *
   * Safe to call multiple times (CREATE TABLE IF NOT EXISTS).
   *
   * Uses admin pool for write operations.
   */
  public initSchema(): void {
    if (this.initialized) {
      return; // Already initialized
    }

    try {
      const db = this.connectionManager.getAdminPool();

      // ┌─ PRAGMA Configuration ─────────────────────────────────────────────┐
      // │ These must run before any DDL                                      │
      // └────────────────────────────────────────────────────────────────────┘

      db.pragma('journal_mode = WAL');
      db.pragma('foreign_keys = ON');
      db.pragma('synchronous = FULL');

      // ┌─ Load & Execute Migrations ────────────────────────────────────────┐
      // │ EPIC-08: Write Layer (sessions, artifacts, workflow_events)        │
      // │ EPIC-09: Context Layer (roles, role_schemas, runbooks, etc.)       │
      // └────────────────────────────────────────────────────────────────────┘

      // EPIC-08: Write Layer Schema
      const writeLayerMigration = this.loadMigration('002_write_layer_schema.sql');
      db.exec(writeLayerMigration);

      // EPIC-09: Context Layer Schema
      const contextLayerMigration = this.loadMigration('003_epic09_context_schema.sql');
      db.exec(contextLayerMigration);

      // EPIC-18: Discovery Work Item (DWI) schema
      // NOTE: This migration is optional; if it is not present, skip it.
      try {
        const dwiSchemaMigration = this.loadMigration('004-dwi-schema.sql');
        db.exec(dwiSchemaMigration);
      } catch {
        // Migration file not found (legacy/optional). Continue initialization.
      }

      // TASK-11-01: FSM Schema (Prerequisite for Two-Track Routing)
      const fsmLayerMigration = this.loadMigration('004_epic11_fsm_schema.sql');
      db.exec(fsmLayerMigration);

      // TASK-11-07: Audit Log Schema
      const auditLogMigration = this.loadMigration('004_task1107_audit_log.sql');
      db.exec(auditLogMigration);

      // TASK-12-01: Episodic Memory Schema (episodes table + 2 indexes)
      const episodicMigrationPath = join(__dirname, '../episodic/migrations', '003_episodes.sql');
      const episodicMigration = readFileSync(episodicMigrationPath, { encoding: 'utf-8' });
      db.exec(episodicMigration);

      // TASK-12-02: FTS5 Virtual Table for Episodes
      const episodicFtsMigrationPath = join(__dirname, '../episodic/migrations', '004_episodes_fts5.sql');
      const episodicFtsMigration = readFileSync(episodicFtsMigrationPath, { encoding: 'utf-8' });
      db.exec(episodicFtsMigration);

      // TASK-13-05: Discovery phase state schema
      const phaseMigration = this.loadMigration('006_epic13_discovery_phase_schema.sql');
      db.exec(phaseMigration);

      // TASK-13-06: Blockers tracking schema
      const blockersMigration = this.loadMigration('007_epic13_blockers_schema.sql');
      db.exec(blockersMigration);

      // EPIC-17: Domain registry schema
      const domainMigration = this.loadMigration('008_epic17_domain_schema.sql');
      db.exec(domainMigration);

      // EPIC-18: Highlights schema
      const highlightsMigration = this.loadMigration('009_epic18_highlights_schema.sql');
      db.exec(highlightsMigration);

      // EPIC-18: FTS5 indexing for highlights
      const highlightsFtsMigration = this.loadMigration('010_epic18_highlights_fts.sql');
      db.exec(highlightsFtsMigration);

      // EPIC-18: Highlight quality score components
      const highlightQualityMigration = this.loadMigration('011_epic18_highlight_quality.sql');
      try {
        db.exec(highlightQualityMigration);
      } catch (err) {
        // This migration is additive (ALTER TABLE ADD COLUMN), so if the column already exists,
        // we can safely ignore the error for idempotency.
        if (err instanceof Error && /duplicate column name/i.test(err.message)) {
          // already applied
        } else {
          throw err;
        }
      }

      // TASK-18-09: Audit log cost tracking fields (ai_model, ai_tokens_used, cost_amount_usd)
      const auditCostMigration = this.loadMigration('012_task1809_audit_log_cost.sql');
      try {
        db.exec(auditCostMigration);
      } catch (err) {
        // Additive migration, safe to ignore if columns already exist
        if (err instanceof Error && /duplicate column name/i.test(err.message)) {
          // already applied
        } else {
          throw err;
        }
      }

      // EPIC-17: Add nullable domain_id FK to roles (idempotent — ALTER TABLE
      // throws "duplicate column name" if already present; caught and ignored)
      try {
        db.exec('ALTER TABLE roles ADD COLUMN domain_id TEXT REFERENCES domains(id)');
      } catch {
        // Column already exists — migration is idempotent.
      }

      // EPIC-17: Add nullable current_domain_id FK to sessions
      try {
        db.exec('ALTER TABLE sessions ADD COLUMN current_domain_id TEXT REFERENCES domains(id)');
      } catch {
        // Column already exists — migration is idempotent.
      }

      // ┌─ WAL Optimization (TASK-09-02B) ───────────────────────────────────┐
      // │ Configure WAL pragmas for optimal concurrency under load          │
      // │ Pragmas: wal_autocheckpoint, journal_size_limit, busy_timeout      │
      // └────────────────────────────────────────────────────────────────────┘
      this.connectionManager.initWalOptimization();

      this.initialized = true;

      // Log initialization (structured for Prometheus scraping)
      console.info(
        '[AgentDb.initSchema] ✅ Database initialized: metadata.db with EPIC-08..EPIC-18 schemas + WAL optimization (FK=ON, WAL, autocheckpoint=1000, busy_timeout=5s)'
      );
    } catch (error) {
      console.error('[AgentDb.initSchema] ❌ Schema initialization failed:', error);
      throw new Error(`Failed to initialize database schema: ${error instanceof Error ? error.message : String(error)}`);
    }
  }

  /**
   * Check if foreign key constraints are enforced.
   *
   * Returns: true if PRAGMA foreign_keys = ON
   */
  public checkForeignKeyConstraints(): boolean {
    const result = this.connectionManager.getAdminPool().pragma('foreign_keys') as Array<{ foreign_keys: number }>;
    return Array.isArray(result) && result.length > 0 && result[0].foreign_keys === 1;
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // QUERY BUILDERS — ROLES TABLE (EPIC-09)
  // ═══════════════════════════════════════════════════════════════════════════

  /**
   * Get a role definition by domain and role_name.
   *
   * @param domain - Domain name (e.g., 'engineering', 'management', 'discovery')
   * @param role - Role name (e.g., 'backend_developer', 'tech_lead')
   * @returns RoleRow if found, null otherwise
   * @throws Error if database operation fails
   */
  public getRole(domain: string, role: string, domainId?: string | null): RoleRow | null {
    try {
      const stmt = this.connectionManager.getAgentPool().prepare(`
        SELECT id, domain, role_name, content, created_at, last_updated
        FROM roles
        WHERE domain = ?
          AND role_name = ?
          AND (
            ? IS NULL
            OR domain_id = ?
            OR (
              domain_id IS NULL
              AND EXISTS (
                SELECT 1 FROM domains d WHERE d.id = ? AND d.name = roles.domain
              )
            )
          )
        LIMIT 1
      `);

      const result = stmt.get(domain, role, domainId ?? null, domainId ?? null, domainId ?? null) as RoleRow | undefined;
      return result ?? null;
    } catch (error) {
      console.error(
        `[AgentDb.getRole] Database error fetching role ${domain}/${role}:`,
        error instanceof Error ? error.message : String(error)
      );
      throw new Error(
        `Failed to fetch role ${domain}/${role}: ${error instanceof Error ? error.message : String(error)}`
      );
    }
  }

  /**
   * Get all roles for a specific domain.
   *
   * @param domain - Domain name
   * @returns Array of RoleRow objects
   */
  public getRolesByDomain(domain: string, domainId?: string | null): RoleRow[] {
    const stmt = this.connectionManager.getAgentPool().prepare(`
      SELECT id, domain, role_name, content, created_at, last_updated
      FROM roles
      WHERE domain = ?
        AND (
          ? IS NULL
          OR domain_id = ?
          OR (
            domain_id IS NULL
            AND EXISTS (
              SELECT 1 FROM domains d WHERE d.id = ? AND d.name = roles.domain
            )
          )
        )
      ORDER BY role_name ASC
    `);

    return stmt.all(domain, domainId ?? null, domainId ?? null, domainId ?? null) as RoleRow[];
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // QUERY BUILDERS — ROLE_SCHEMAS TABLE (EPIC-09)
  // ═══════════════════════════════════════════════════════════════════════════

  /**
   * Get MCP tool permissions schema for a role.
   *
   * @param domain - Domain name
   * @param role - Role name
   * @returns RoleSchemaRow with mcp_tool_permissions JSON, or null if not found
   * @throws Error if database operation fails
   */
  public getRoleSchema(domain: string, role: string): RoleSchemaRow | null {
    try {
      const stmt = this.connectionManager.getAgentPool().prepare(`
        SELECT id, domain, role_name, mcp_tool_permissions, created_at, last_updated
        FROM role_schemas
        WHERE domain = ? AND role_name = ?
        LIMIT 1
      `);

      const result = stmt.get(domain, role) as RoleSchemaRow | undefined;
      return result ?? null;
    } catch (error) {
      console.error(
        `[AgentDb.getRoleSchema] Database error fetching schema ${domain}/${role}:`,
        error instanceof Error ? error.message : String(error)
      );
      throw new Error(
        `Failed to fetch role schema ${domain}/${role}: ${error instanceof Error ? error.message : String(error)}`
      );
    }
  }

  /**
   * Get MCP tool permissions schema for a role by name (latest/any domain).
   *
   * @param role - Role name
   * @returns RoleSchemaRow with mcp_tool_permissions JSON, or null if not found
   * @throws Error if database operation fails
   */
  public findSchemaByRoleName(role: string): RoleSchemaRow | null {
    try {
      const stmt = this.connectionManager.getAgentPool().prepare(`
        SELECT id, domain, role_name, mcp_tool_permissions, created_at, last_updated
        FROM role_schemas
        WHERE role_name = ?
        LIMIT 1
      `);

      const result = stmt.get(role) as RoleSchemaRow | undefined;
      return result ?? null;
    } catch (error) {
      console.error(
        `[AgentDb.findSchemaByRoleName] Database error fetching schema for role ${role}:`,
        error instanceof Error ? error.message : String(error)
      );
      throw new Error(
        `Failed to find role schema for ${role}: ${error instanceof Error ? error.message : String(error)}`
      );
    }
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // QUERY BUILDERS — RUNBOOKS TABLE (EPIC-09)
  // ═══════════════════════════════════════════════════════════════════════════

  /**
   * Get a runbook for a specific role.
   *
   * @param domain - Domain name
   * @param role - Role name
   * @returns RunbookRow with markdown content, or null if not found
   * @throws Error if database operation fails
   */
  public getRunbook(domain: string, role: string): RunbookRow | null {
    try {
      const stmt = this.connectionManager.getAgentPool().prepare(`
        SELECT id, domain, role_name, content, created_at, last_updated
        FROM runbooks
        WHERE domain = ? AND role_name = ?
        LIMIT 1
      `);

      const result = stmt.get(domain, role) as RunbookRow | undefined;
      return result ?? null;
    } catch (error) {
      console.error(
        `[AgentDb.getRunbook] Database error fetching runbook ${domain}/${role}:`,
        error instanceof Error ? error.message : String(error)
      );
      throw new Error(
        `Failed to fetch runbook ${domain}/${role}: ${error instanceof Error ? error.message : String(error)}`
      );
    }
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // QUERY BUILDERS — WORKFLOWS TABLE (EPIC-09)
  // ═══════════════════════════════════════════════════════════════════════════

  /**
   * Get a workflow definition for a role & workflow type.
   *
   * @param domain - Domain name
   * @param role - Role name
   * @param workflowType - Workflow type (e.g., 'feature_delivery', 'code_review')
   * @returns WorkflowRow with FSM definition, or null if not found
   * @throws Error if database operation fails
   */
  public getWorkflow(domain: string, role: string, workflowType: string, domainId?: string | null): WorkflowRow | null {
    try {
      const stmt = this.connectionManager.getAgentPool().prepare(`
        SELECT id, domain, role_name, workflow_type, content, created_at, last_updated
        FROM workflows
        WHERE domain = ?
          AND role_name = ?
          AND workflow_type = ?
          AND (
            ? IS NULL
            OR EXISTS (
              SELECT 1 FROM domains d WHERE d.id = ? AND d.name = workflows.domain
            )
          )
        LIMIT 1
      `);

      const result = stmt.get(domain, role, workflowType, domainId ?? null, domainId ?? null) as WorkflowRow | undefined;
      return result ?? null;
    } catch (error) {
      console.error(
        `[AgentDb.getWorkflow] Database error fetching workflow ${domain}/${role}/${workflowType}:`,
        error instanceof Error ? error.message : String(error)
      );
      throw new Error(
        `Failed to fetch workflow ${domain}/${role}/${workflowType}: ${error instanceof Error ? error.message : String(error)}`
      );
    }
  }

  /**
   * Get all workflows for a role.
   *
   * @param domain - Domain name
   * @param role - Role name
   * @returns Array of WorkflowRow objects
   */
  public getWorkflowsByRole(domain: string, role: string, domainId?: string | null): WorkflowRow[] {
    const stmt = this.connectionManager.getAgentPool().prepare(`
      SELECT id, domain, role_name, workflow_type, content, created_at, last_updated
      FROM workflows
      WHERE domain = ?
        AND role_name = ?
        AND (
          ? IS NULL
          OR EXISTS (
            SELECT 1 FROM domains d WHERE d.id = ? AND d.name = workflows.domain
          )
        )
      ORDER BY workflow_type ASC
    `);

    return stmt.all(domain, role, domainId ?? null, domainId ?? null) as WorkflowRow[];
  }

  /**
   * Insert a blocker record for a session.
   */
  public trackBlocker(sessionId: string, phase: string, severity: string, text: string): void {
    const stmt = this.connectionManager.getAdminPool().prepare(`
      INSERT INTO blockers(session_id, phase, severity, text)
      VALUES(?, ?, ?, ?)
    `);
    stmt.run(sessionId, phase, severity, text);
  }

  /**
   * Query blockers for a session, optionally filtered by phase.
   */
  public getBlockers(sessionId: string, phase?: string): any[] {
    let stmt;
    if (phase) {
      stmt = this.connectionManager.getAgentPool().prepare(`
        SELECT * FROM blockers WHERE session_id = ? AND phase = ? ORDER BY created_at ASC
      `);
      return stmt.all(sessionId, phase);
    }
    stmt = this.connectionManager.getAgentPool().prepare(`
      SELECT * FROM blockers WHERE session_id = ? ORDER BY created_at ASC
    `);
    return stmt.all(sessionId);
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // QUERY BUILDERS — TEMPLATES TABLE (EPIC-09)
  // ═══════════════════════════════════════════════════════════════════════════

  /**
   * Get a template by domain, role, and template name.
   *
   * @param domain - Domain name
   * @param role - Role name
   * @param templateName - Template name (e.g., 'implementation_summary', 'test_report')
   * @returns TemplateRow with markdown template, or null if not found
   * @throws Error if database operation fails
   */
  public getTemplate(domain: string, role: string, templateName: string, domainId?: string | null): TemplateRow | null {
    try {
      const stmt = this.connectionManager.getAgentPool().prepare(`
        SELECT id, domain, role_name, template_name, content, created_at, last_updated
        FROM templates
        WHERE domain = ?
          AND role_name = ?
          AND template_name = ?
          AND (
            ? IS NULL
            OR EXISTS (
              SELECT 1 FROM domains d WHERE d.id = ? AND d.name = templates.domain
            )
          )
        LIMIT 1
      `);

      const result = stmt.get(domain, role, templateName, domainId ?? null, domainId ?? null) as TemplateRow | undefined;
      return result ?? null;
    } catch (error) {
      console.error(
        `[AgentDb.getTemplate] Database error fetching template ${domain}/${role}/${templateName}:`,
        error instanceof Error ? error.message : String(error)
      );
      throw new Error(
        `Failed to fetch template ${domain}/${role}/${templateName}: ${error instanceof Error ? error.message : String(error)}`
      );
    }
  }

  /**
   * Get all templates for a role.
   *
   * @param domain - Domain name
   * @param role - Role name
   * @returns Array of TemplateRow objects
   */
  public getTemplatesByRole(domain: string, role: string, domainId?: string | null): TemplateRow[] {
    const stmt = this.connectionManager.getAgentPool().prepare(`
      SELECT id, domain, role_name, template_name, content, created_at, last_updated
      FROM templates
      WHERE domain = ?
        AND role_name = ?
        AND (
          ? IS NULL
          OR EXISTS (
            SELECT 1 FROM domains d WHERE d.id = ? AND d.name = templates.domain
          )
        )
      ORDER BY template_name ASC
    `);

    return stmt.all(domain, role, domainId ?? null, domainId ?? null) as TemplateRow[];
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // QUERY BUILDERS — STANDARDS TABLE (EPIC-09)
  // ═══════════════════════════════════════════════════════════════════════════

  /**
   * Get a standard by standard ID.
   *
   * @param stdId - Standard ID (e.g., 'ADR-001', 'STD-NAMING-2026')
   * @returns StandardRow with content, or null if not found
   * @throws Error if database operation fails
   */
  public getStandard(stdId: string): StandardRow | null {
    try {
      const stmt = this.connectionManager.getAgentPool().prepare(`
        SELECT id, std_id, content, created_at, last_updated
        FROM standards
        WHERE std_id = ?
        LIMIT 1
      `);

      const result = stmt.get(stdId) as StandardRow | undefined;
      return result ?? null;
    } catch (error) {
      console.error(
        `[AgentDb.getStandard] Database error fetching standard ${stdId}:`,
        error instanceof Error ? error.message : String(error)
      );
      throw new Error(
        `Failed to fetch standard ${stdId}: ${error instanceof Error ? error.message : String(error)}`
      );
    }
  }

  /**
   * Get all standards (with pagination support).
   *
   * @param limit - Max results to return (default: 1000)
   * @param offset - Pagination offset (default: 0)
   * @returns Array of StandardRow objects
   */
  public getAllStandards(limit: number = 1000, offset: number = 0): StandardRow[] {
    const stmt = this.connectionManager.getAgentPool().prepare(`
      SELECT id, std_id, content, created_at, last_updated
      FROM standards
      ORDER BY std_id ASC
      LIMIT ? OFFSET ?
    `);

    return stmt.all(limit, offset) as StandardRow[];
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // QUERY BUILDERS — FSM / TRACKING (EPIC-11)
  // ═══════════════════════════════════════════════════════════════════════════

  /**
   * Get the track classification for a specific workflow.
   *
   * @param workflowId - Unique workflow identifier (e.g., 'agile-epic-lifecycle-v1')
   * @returns 'discovery' | 'delivery' or null if not found
   */
  public getWorkflowTrack(workflowId: string): string | null {
    try {
      const stmt = this.connectionManager.getAgentPool().prepare(`
        SELECT track FROM workflow_definitions WHERE workflow_id = ? LIMIT 1
      `);
      const result = stmt.get(workflowId) as { track: string } | undefined;
      return result?.track ?? null;
    } catch (error) {
      console.error(`[AgentDb.getWorkflowTrack] Error:`, error);
      return null;
    }
  }

  /**
   * Get the track for a specific session.
   *
   * @param sessionId - Session UUID
   * @returns 'discovery' | 'delivery' or null if not found
   */
  public getSessionTrack(sessionId: string): string | null {
    try {
      const stmt = this.connectionManager.getAgentPool().prepare(`
        SELECT wd.track
        FROM agent_sessions as ASESS
        JOIN workflow_definitions as wd ON ASESS.workflow_id = wd.workflow_id
        WHERE ASESS.session_id = ?
        LIMIT 1
      `);
      const result = stmt.get(sessionId) as { track: string } | undefined;
      return result?.track ?? null;
    } catch (error) {
      console.error(`[AgentDb.getSessionTrack] Error:`, error);
      return null;
    }
  }

  /**
   * Search the audit log by various criteria.
   *
   * @param query - Filter parameters
   * @returns Array of audit log entries
   */
  public searchAuditLog(query: { session_id?: string; timestamp_since?: string; limit?: number }): any[] {
    try {
      const db = this.connectionManager.getAgentPool();
      let sql = 'SELECT * FROM audit_log WHERE 1=1';
      const params: any[] = [];

      if (query.session_id) {
        sql += ' AND session_id = ?';
        params.push(query.session_id);
      }

      if (query.timestamp_since) {
        sql += ' AND timestamp >= ?';
        params.push(query.timestamp_since);
      }

      sql += ' ORDER BY timestamp DESC';

      const limit = query.limit && query.limit > 0 ? Math.min(query.limit, 100) : 50;
      sql += ' LIMIT ?';
      params.push(limit);

      const stmt = db.prepare(sql);
      return stmt.all(...params) as any[];
    } catch (error) {
      console.error(`[AgentDb.searchAuditLog] Error:`, error);
      return [];
    }
  }

  /**
   * Query cost and token usage data for reflection tools in a session (TASK-18-09).
   *
   * @param sessionId - Session ID to query costs for
   * @param toolName - Optional filter by tool name (e.g., 'generate_episode_highlights')
   * @returns Array of AuditCostRow entries with cost metadata
   */
  public queryAuditCostData(sessionId: string, toolName?: string): AuditCostRow[] {
    try {
      const db = this.connectionManager.getAgentPool();
      let sql = `
        SELECT id, session_id, tool_name, latency_ms, ai_model, ai_tokens_used, cost_amount_usd, timestamp
        FROM audit_log
        WHERE session_id = ?
      `;
      const params: any[] = [sessionId];

      if (toolName) {
        sql += ' AND tool_name = ?';
        params.push(toolName);
      }

      sql += ' ORDER BY timestamp DESC LIMIT 100';

      const stmt = db.prepare(sql);
      return stmt.all(...params) as AuditCostRow[];
    } catch (error) {
      console.error('[AgentDb.queryAuditCostData] Error:', error instanceof Error ? error.message : String(error));
      return [];
    }
  }

  /**
   * Compute aggregated cost metrics for a session (TASK-18-09).
   *
   * @param sessionId - Session ID to compute metrics for
   * @returns SessionCostMetrics with total tokens, latency, cost, and models used
   */
  public computeSessionCostMetrics(sessionId: string): SessionCostMetrics {
    try {
      const db = this.connectionManager.getAgentPool();
      const stmt = db.prepare(`
        SELECT
          COUNT(*) AS total_tools_called,
          COALESCE(SUM(latency_ms), 0) AS total_latency_ms,
          SUM(ai_tokens_used) AS total_tokens_used,
          SUM(cost_amount_usd) AS total_cost_usd,
          GROUP_CONCAT(ai_model, '|') AS models_csv
        FROM audit_log
        WHERE session_id = ?
      `);

      const result = stmt.get(sessionId) as any;
      const modelsSet = new Set<string>();
      if (result?.models_csv) {
        result.models_csv.split('|').forEach((m: string) => {
          if (m && m !== 'null') modelsSet.add(m);
        });
      }

      return {
        session_id: sessionId,
        total_tools_called: result?.total_tools_called ?? 0,
        total_latency_ms: result?.total_latency_ms ?? 0,
        total_tokens_used: result?.total_tokens_used ?? null,
        total_cost_usd: result?.total_cost_usd ?? null,
        ai_models_used: Array.from(modelsSet),
      };
    } catch (error) {
      console.error('[AgentDb.computeSessionCostMetrics] Error:', error instanceof Error ? error.message : String(error));
      return {
        session_id: sessionId,
        total_tools_called: 0,
        total_latency_ms: 0,
        total_tokens_used: null,
        total_cost_usd: null,
        ai_models_used: [],
      };
    }
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // UTILITY METHODS
  // ═══════════════════════════════════════════════════════════════════════════

  /**
   * Get the current schema version for a specific layer.
   */
  public getSchemaVersion(layer: string = 'read-layer'): number {
    try {
      const stmt = this.connectionManager.getAgentPool().prepare(`
        SELECT version FROM schema_metadata WHERE layer = ? LIMIT 1
      `);
      const result = stmt.get(layer) as { version: number } | undefined;
      return result?.version ?? 0;
    } catch (error) {
      return 0;
    }
  }

  /**
   * Close database connections (both admin and agent pools).
   *
   * Should be called during application shutdown.
   */
  public close(): void {
    this.connectionManager.close();
  }

  /**
   * Get raw better-sqlite3 database instance (for advanced use cases).
   *
   * ⚠️ Use with caution! Bypasses type safety and validation.
   * Returns the admin pool (read-write).
   */
  public getRawDatabase(): Database.Database {
    return this.connectionManager.getAdminPool();
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // QUERY BUILDERS — EPIC-16: Legacy Tool DB-first support
  // ═══════════════════════════════════════════════════════════════════════════

  /**
   * List all distinct domains that have at least one role in the DB.
   * Equivalent to listing folders in database/roles/.
   *
   * @returns Array of domain strings (e.g. ['engineering', 'discovery'])
   */
  public listDomains(): string[] {
    try {
      const stmt = this.connectionManager.getAgentPool().prepare(`
        SELECT DISTINCT domain FROM roles ORDER BY domain ASC
      `);
      const rows = stmt.all() as { domain: string }[];
      return rows.map(r => r.domain);
    } catch (error) {
      console.error('[AgentDb.listDomains] Error:', error);
      return [];
    }
  }

  /**
   * List all role names for a given domain.
   * Equivalent to listing subdirectories under database/roles/{domain}/.
   *
   * @param domain - e.g. 'engineering'
   * @returns Array of role_name strings
   */
  public listRoleNames(domain: string): string[] {
    try {
      const stmt = this.connectionManager.getAgentPool().prepare(`
        SELECT role_name FROM roles WHERE domain = ? ORDER BY role_name ASC
      `);
      const rows = stmt.all(domain) as { role_name: string }[];
      return rows.map(r => r.role_name);
    } catch (error) {
      console.error('[AgentDb.listRoleNames] Error:', error);
      return [];
    }
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // QUERY BUILDERS — EPIC-15: PM Query Tools (read-only)
  // ═══════════════════════════════════════════════════════════════════════════

  /**
   * Returns aggregated project state from PM tables when available.
   * If PM tables are not present, returns null.
   */
  public getProjectState(projectId: string): ProjectStateRow | null {
    try {
      if (!this.tableExists('projects')) {
        return null;
      }

      const db = this.connectionManager.getAgentPool();
      const projectCols = this.getTableColumns('projects');
      const epicsCols = this.tableExists('epics') ? this.getTableColumns('epics') : new Set<string>();
      const tasksCols = this.tableExists('tasks') ? this.getTableColumns('tasks') : new Set<string>();

      const projectRow = db.prepare('SELECT id FROM projects WHERE id = ? LIMIT 1').get(projectId) as { id: string } | undefined;
      if (!projectRow) {
        return null;
      }

      let dueDate: string | null = null;
      if (projectCols.has('due_date')) {
        const dueRow = db.prepare('SELECT due_date FROM projects WHERE id = ? LIMIT 1').get(projectId) as { due_date: string | null } | undefined;
        dueDate = dueRow?.due_date ?? null;
      }

      let milestone: string | null = null;
      if (this.tableExists('epics') && epicsCols.has('project_id')) {
        const milestoneQuery = epicsCols.has('state')
          ? `SELECT id FROM epics WHERE project_id = ? AND LOWER(COALESCE(state, '')) NOT IN ('done','closed','completed') ORDER BY id ASC LIMIT 1`
          : `SELECT id FROM epics WHERE project_id = ? ORDER BY id ASC LIMIT 1`;
        const milestoneRow = db.prepare(milestoneQuery).get(projectId) as { id: string } | undefined;
        milestone = milestoneRow?.id ?? null;
      }

      let openTasksCount = 0;
      if (this.tableExists('tasks')) {
        const clauses: string[] = [];
        const params: unknown[] = [];

        if (tasksCols.has('project_id')) {
          clauses.push('project_id = ?');
          params.push(projectId);
        } else if (tasksCols.has('epic_id') && this.tableExists('epics') && epicsCols.has('project_id')) {
          clauses.push('epic_id IN (SELECT id FROM epics WHERE project_id = ?)');
          params.push(projectId);
        }

        if (tasksCols.has('status')) {
          clauses.push("LOWER(COALESCE(status, '')) NOT IN ('done','closed','completed')");
        }

        const where = clauses.length > 0 ? `WHERE ${clauses.join(' AND ')}` : '';
        const countRow = db.prepare(`SELECT COUNT(*) AS total FROM tasks ${where}`).get(...params) as { total: number } | undefined;
        openTasksCount = countRow?.total ?? 0;
      }

      return {
        project_id: projectId,
        milestone,
        open_tasks_count: openTasksCount,
        due_date: dueDate,
      };
    } catch (error) {
      console.error('[AgentDb.getProjectState] Error:', error);
      return null;
    }
  }

  /**
   * Lists tasks filtered by optional domain/track/status/query.
   * Returns an empty array when PM tables are unavailable.
   */
  public listPmTasks(options?: {
    domain?: string;
    track?: string;
    status?: string;
    query?: string;
    limit?: number;
  }): PmTaskSummaryRow[] {
    try {
      if (!this.tableExists('tasks')) {
        return [];
      }

      const db = this.connectionManager.getAgentPool();
      const taskCols = this.getTableColumns('tasks');
      const filters: string[] = [];
      const params: unknown[] = [];

      if (options?.domain && taskCols.has('domain')) {
        filters.push('domain = ?');
        params.push(options.domain);
      }

      if (options?.track && taskCols.has('track')) {
        filters.push('track = ?');
        params.push(options.track);
      }

      if (options?.status && taskCols.has('status')) {
        filters.push("LOWER(COALESCE(status, '')) = LOWER(?)");
        params.push(options.status);
      }

      if (options?.query) {
        const queryClauses: string[] = [];
        if (taskCols.has('title')) queryClauses.push("LOWER(COALESCE(title, '')) LIKE LOWER(?)");
        if (taskCols.has('description')) queryClauses.push("LOWER(COALESCE(description, '')) LIKE LOWER(?)");
        if (queryClauses.length > 0) {
          filters.push(`(${queryClauses.join(' OR ')})`);
          if (taskCols.has('title')) params.push(`%${options.query}%`);
          if (taskCols.has('description')) params.push(`%${options.query}%`);
        }
      }

      const where = filters.length > 0 ? `WHERE ${filters.join(' AND ')}` : '';
      const limit = options?.limit && options.limit > 0 ? Math.min(options.limit, 100) : 20;

      const sql = `
        SELECT
          id,
          ${taskCols.has('title') ? 'title' : `'' AS title`},
          ${taskCols.has('status') ? 'status' : 'NULL AS status'},
          ${taskCols.has('assigned_to') ? 'assigned_to' : taskCols.has('owner') ? 'owner AS assigned_to' : 'NULL AS assigned_to'},
          ${taskCols.has('domain') ? 'domain' : 'NULL AS domain'},
          ${taskCols.has('track') ? 'track' : 'NULL AS track'}
        FROM tasks
        ${where}
        ORDER BY id ASC
        LIMIT ?
      `;

      params.push(limit);
      return db.prepare(sql).all(...params) as PmTaskSummaryRow[];
    } catch (error) {
      console.error('[AgentDb.listPmTasks] Error:', error);
      return [];
    }
  }

  /**
   * Returns detailed task context for the given task id.
   */
  public getTaskContext(taskId: string): PmTaskContextRow | null {
    try {
      if (!this.tableExists('tasks')) {
        return null;
      }

      const db = this.connectionManager.getAgentPool();
      const taskCols = this.getTableColumns('tasks');

      const sql = `
        SELECT
          id,
          ${taskCols.has('title') ? 'title' : `'' AS title`},
          ${taskCols.has('status') ? 'status' : 'NULL AS status'},
          ${taskCols.has('assigned_to') ? 'assigned_to' : taskCols.has('owner') ? 'owner AS assigned_to' : 'NULL AS assigned_to'},
          ${taskCols.has('domain') ? 'domain' : 'NULL AS domain'},
          ${taskCols.has('track') ? 'track' : 'NULL AS track'},
          ${taskCols.has('description') ? 'description' : 'NULL AS description'},
          ${taskCols.has('acceptance_criteria') ? 'acceptance_criteria' : 'NULL AS acceptance_criteria'},
          ${taskCols.has('workflow') ? 'workflow' : taskCols.has('workflow_type') ? 'workflow_type AS workflow' : 'NULL AS workflow'},
          ${taskCols.has('template') ? 'template' : taskCols.has('template_name') ? 'template_name AS template' : 'NULL AS template'}
        FROM tasks
        WHERE id = ?
        LIMIT 1
      `;

      const row = db.prepare(sql).get(taskId) as PmTaskContextRow | undefined;
      return row ?? null;
    } catch (error) {
      console.error('[AgentDb.getTaskContext] Error:', error);
      return null;
    }
  }

  /**
   * Get Discovery Work Item (DWI) state by ID.
   * Returns null if the DWI does not exist or the DWI schema is not installed.
   */
  public getDwiState(dwiId: string): DiscoveryWorkItemRow | null {
    try {
      if (!this.tableExists('discovery_work_items')) {
        return null;
      }

      const stmt = this.connectionManager.getAgentPool().prepare(
        `SELECT id, topic, status, current_phase, next_action, verdict,
                hypothesis_count, validated_count, created_at, updated_at,
                created_by, updated_by
         FROM discovery_work_items
         WHERE id = ?
         LIMIT 1`
      );
      const row = stmt.get(dwiId) as DiscoveryWorkItemRow | undefined;
      return row ?? null;
    } catch (error) {
      console.error('[AgentDb.getDwiState] Error:', error instanceof Error ? error.message : String(error));
      throw new Error(`Failed to fetch DWI state for ${dwiId}`);
    }
  }

  /**
   * Insert or update a discovery work item state row.
   * Uses INSERT OR REPLACE to ensure idempotency.
   */
  public upsertDwiState(dwi: {
    id: string;
    topic: string;
    status: 'open' | 'in_progress' | 'concluded' | 'archived';
    current_phase: number;
    next_action: string;
    verdict?: 'validated' | 'invalidated' | 'pivoted' | null;
    hypothesis_count?: number;
    validated_count?: number;
    created_at?: string | null;
    updated_at?: string | null;
    created_by?: string | null;
    updated_by?: string | null;
  }): void {
    const stmt = this.connectionManager.getAdminPool().prepare(`
      INSERT OR REPLACE INTO discovery_work_items (
        id, topic, status, current_phase, next_action, verdict,
        hypothesis_count, validated_count, created_at, updated_at,
        created_by, updated_by
      ) VALUES (
        ?, ?, ?, ?, ?, ?, ?, ?,
        COALESCE(?, datetime('now')),
        COALESCE(?, datetime('now')),
        ?, ?
      )
    `);

    stmt.run(
      dwi.id,
      dwi.topic,
      dwi.status,
      dwi.current_phase,
      dwi.next_action,
      dwi.verdict ?? null,
      dwi.hypothesis_count ?? 0,
      dwi.validated_count ?? 0,
      dwi.created_at ?? null,
      dwi.updated_at ?? null,
      dwi.created_by ?? null,
      dwi.updated_by ?? null
    );
  }

  /**
   * List all known DWI topics (unique, sorted).
   */
  public listDwiTopics(): string[] {
    try {
      if (!this.tableExists('discovery_work_items')) {
        return [];
      }

      const stmt = this.connectionManager.getAgentPool().prepare(
        `SELECT DISTINCT topic FROM discovery_work_items ORDER BY topic ASC`
      );
      const rows = stmt.all() as Array<{ topic: string }>;
      return rows.map((r) => r.topic);
    } catch (error) {
      console.error('[AgentDb.listDwiTopics] Error:', error instanceof Error ? error.message : String(error));
      return [];
    }
  }

  /**
   * Return a filtered dashboard view of Discovery Work Items.
   */
  public listDwiDashboard(options?: {
    status?: string;
    currentPhase?: number;
    topic?: string;
    limit?: number;
  }): DwiDashboardResponse[] {
    try {
      if (!this.tableExists('discovery_work_items')) {
        return [];
      }

      const clauses: string[] = [];
      const params: unknown[] = [];

      if (options?.status) {
        clauses.push('LOWER(status) = LOWER(?)');
        params.push(options.status);
      }

      if (typeof options?.currentPhase === 'number') {
        clauses.push('current_phase = ?');
        params.push(options.currentPhase);
      }

      if (options?.topic) {
        clauses.push('LOWER(topic) LIKE LOWER(?)');
        params.push(`%${options.topic}%`);
      }

      const where = clauses.length > 0 ? `WHERE ${clauses.join(' AND ')}` : '';
      const limit = options?.limit && options.limit > 0 ? Math.min(options.limit, 200) : 100;

      const sql = `
        SELECT
          id AS dwi_id,
          topic,
          status,
          current_phase,
          next_action,
          verdict,
          hypothesis_count,
          validated_count,
          updated_at
        FROM discovery_work_items
        ${where}
        ORDER BY updated_at DESC
        LIMIT ?
      `;

      const stmt = this.connectionManager.getAgentPool().prepare(sql);
      return stmt.all(...params, limit) as DwiDashboardResponse[];
    } catch (error) {
      console.error('[AgentDb.listDwiDashboard] Error:', error instanceof Error ? error.message : String(error));
      return [];
    }
  }

  /**
   * List phase gate history for a given DWI.
   * Results are ordered by phase.
   */
  public listDwiPhaseGates(dwiId: string): DwiPhaseGateRow[] {
    try {
      if (!this.tableExists('dwi_phase_gates')) {
        return [];
      }

      const stmt = this.connectionManager.getAgentPool().prepare(`
        SELECT id, dwi_id, phase, gate_crossed, gate_crossed_date, notes, created_at
        FROM dwi_phase_gates
        WHERE dwi_id = ?
        ORDER BY phase ASC, id ASC
      `);
      return stmt.all(dwiId) as DwiPhaseGateRow[];
    } catch (error) {
      console.error('[AgentDb.listDwiPhaseGates] Error:', error instanceof Error ? error.message : String(error));
      return [];
    }
  }

  /**
   * Upsert a phase gate row. Uses unique index (dwi_id, phase) for idempotency.
   */
  public upsertDwiPhaseGate(gate: {
    dwi_id: string;
    phase: number;
    gate_crossed: boolean;
    gate_crossed_date?: string | null;
    notes?: string | null;
  }): void {
    const stmt = this.connectionManager.getAdminPool().prepare(`
      INSERT OR REPLACE INTO dwi_phase_gates (
        dwi_id, phase, gate_crossed, gate_crossed_date, notes, created_at
      ) VALUES (?, ?, ?, ?, ?, datetime('now'))
    `);
    stmt.run(
      gate.dwi_id,
      gate.phase,
      gate.gate_crossed ? 1 : 0,
      gate.gate_crossed_date ?? null,
      gate.notes ?? null
    );
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // PRIVATE HELPERS
  // ═══════════════════════════════════════════════════════════════════════════

  // ═══════════════════════════════════════════════════════════════════════════
  // DOMAIN REGISTRY QUERIES (EPIC-17)
  // ═══════════════════════════════════════════════════════════════════════════

  /**
   * List all registered domains from the `domains` registry table (EPIC-17).
   *
   * NOTE: The existing `listDomains(): string[]` (EPIC-16) returns distinct
   * domain strings from the `roles` table. This method queries the dedicated
   * domain registry table added in migration 008.
   *
   * @returns Array of DomainRow ordered by name (A-Z). Empty array if none.
   */
  public listRegisteredDomains(): DomainRow[] {
    try {
      const stmt = this.connectionManager
        .getAgentPool()
        .prepare('SELECT id, name, description, config_json, created_at, updated_at FROM domains ORDER BY name ASC');
      return stmt.all() as DomainRow[];
    } catch (error) {
      console.error('[AgentDb.listRegisteredDomains] Error:', error instanceof Error ? error.message : String(error));
      throw new Error(`Failed to list registered domains: ${error instanceof Error ? error.message : String(error)}`);
    }
  }

  /**
   * Get a single domain from the `domains` registry table by its id or name (EPIC-17).
   *
   * @param nameOrId - Domain `id` or `name` value
   * @returns DomainRow if found, null otherwise
   */
  public getRegisteredDomain(nameOrId: string): DomainRow | null {
    try {
      const stmt = this.connectionManager
        .getAgentPool()
        .prepare('SELECT id, name, description, config_json, created_at, updated_at FROM domains WHERE id = ? OR name = ? LIMIT 1');
      const result = stmt.get(nameOrId, nameOrId) as DomainRow | undefined;
      return result ?? null;
    } catch (error) {
      console.error('[AgentDb.getRegisteredDomain] Error:', error instanceof Error ? error.message : String(error));
      throw new Error(`Failed to get registered domain "${nameOrId}": ${error instanceof Error ? error.message : String(error)}`);
    }
  }

  /**
   * Load SQL migration file.
  /**
   * Insert or update a domain in the `domains` registry table (EPIC-17).
   *
   * Idempotent: uses INSERT OR REPLACE (keyed on `id` which equals `name`).
   *
   * @param name        - Domain name (also used as id, e.g., 'engineering')
   * @param description - Optional human-readable description
   */
  public upsertDomain(name: string, description?: string | null): void {
    const stmt = this.connectionManager
      .getAdminPool()
      .prepare(
        `INSERT OR REPLACE INTO domains (id, name, description, updated_at)
         VALUES (?, ?, ?, datetime('now'))`
      );
    stmt.run(name, name, description ?? null);
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // EPISODE HIGHLIGHTS QUERIES (EPIC-18)
  // ═══════════════════════════════════════════════════════════════════════════

  /**
   * Insert or replace a highlight row for a specific episode (unique by episode_id).
   */
  public upsertEpisodeHighlight(row: {
    id: string;
    episode_id: string;
    key_decisions?: string | null;
    lessons?: string | null;
    next_steps?: string | null;
    quality_score?: number | null;
    ai_generated?: boolean;
    ai_model?: string | null;
    ai_tokens_used?: number | null;
  }): void {
    const stmt = this.connectionManager.getAdminPool().prepare(`
      INSERT OR REPLACE INTO episode_highlights (
        id, episode_id, key_decisions, lessons, next_steps,
        quality_score, ai_generated, ai_model, ai_tokens_used, created_at
      ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, datetime('now'))
    `);

    stmt.run(
      row.id,
      row.episode_id,
      row.key_decisions ?? null,
      row.lessons ?? null,
      row.next_steps ?? null,
      row.quality_score ?? null,
      row.ai_generated === false ? 0 : 1,
      row.ai_model ?? null,
      row.ai_tokens_used ?? null
    );
  }

  /**
   * Get highlight row by source episode id.
   */
  public getEpisodeHighlightByEpisodeId(episodeId: string): EpisodeHighlightRow | null {
    try {
      const stmt = this.connectionManager.getAgentPool().prepare(`
        SELECT id, episode_id, key_decisions, lessons, next_steps,
               quality_score, ai_generated, ai_model, ai_tokens_used, created_at
        FROM episode_highlights
        WHERE episode_id = ?
        LIMIT 1
      `);
      const row = stmt.get(episodeId) as EpisodeHighlightRow | undefined;
      return row ?? null;
    } catch (error) {
      console.error('[AgentDb.getEpisodeHighlightByEpisodeId] Error:', error instanceof Error ? error.message : String(error));
      throw new Error(`Failed to fetch episode highlight by episode_id ${episodeId}`);
    }
  }

  /**
   * List all highlight rows for a session, newest first.
   */
  public listEpisodeHighlightsBySession(sessionId: string): SessionEpisodeHighlightRow[] {
    try {
      const stmt = this.connectionManager.getAgentPool().prepare(`
        SELECT eh.id, eh.episode_id, eh.key_decisions, eh.lessons, eh.next_steps,
               eh.quality_score, eh.ai_generated, eh.ai_model, eh.ai_tokens_used, eh.created_at,
               e.session_id, e.domain, e.track, e.outcome_summary
        FROM episode_highlights eh
        INNER JOIN episodes e ON e.id = eh.episode_id
        WHERE e.session_id = ?
        ORDER BY eh.created_at DESC, eh.id ASC
      `);
      return stmt.all(sessionId) as SessionEpisodeHighlightRow[];
    } catch (error) {
      console.error('[AgentDb.listEpisodeHighlightsBySession] Error:', error instanceof Error ? error.message : String(error));
      throw new Error(`Failed to list highlights for session ${sessionId}`);
    }
  }

  /**
   * List all highlights with source episode metadata.
   */
  public listEpisodeHighlights(limit?: number): SessionEpisodeHighlightRow[] {
    try {
      const sql = `
        SELECT eh.id, eh.episode_id, eh.key_decisions, eh.lessons, eh.next_steps,
               eh.quality_score, eh.ai_generated, eh.ai_model, eh.ai_tokens_used, eh.created_at,
               e.session_id, e.domain, e.track, e.outcome_summary
        FROM episode_highlights eh
        INNER JOIN episodes e ON e.id = eh.episode_id
        ORDER BY eh.created_at DESC, eh.id ASC
        ${typeof limit === 'number' ? 'LIMIT ?' : ''}
      `;
      const stmt = this.connectionManager.getAgentPool().prepare(sql);
      return (typeof limit === 'number' ? stmt.all(limit) : stmt.all()) as SessionEpisodeHighlightRow[];
    } catch (error) {
      console.error('[AgentDb.listEpisodeHighlights] Error:', error instanceof Error ? error.message : String(error));
      throw new Error('Failed to list episode highlights');
    }
  }

  /**
   * Get a highlight row by highlight id with source episode metadata.
   */
  public getEpisodeHighlightWithContextById(highlightId: string): SessionEpisodeHighlightRow | null {
    try {
      const stmt = this.connectionManager.getAgentPool().prepare(`
        SELECT eh.id, eh.episode_id, eh.key_decisions, eh.lessons, eh.next_steps,
               eh.quality_score, eh.ai_generated, eh.ai_model, eh.ai_tokens_used, eh.created_at,
               e.session_id, e.domain, e.track, e.outcome_summary
        FROM episode_highlights eh
        INNER JOIN episodes e ON e.id = eh.episode_id
        WHERE eh.id = ?
        LIMIT 1
      `);
      const row = stmt.get(highlightId) as SessionEpisodeHighlightRow | undefined;
      return row ?? null;
    } catch (error) {
      console.error('[AgentDb.getEpisodeHighlightWithContextById] Error:', error instanceof Error ? error.message : String(error));
      throw new Error(`Failed to fetch episode highlight ${highlightId}`);
    }
  }

  /**
   * Search prior highlight rows using keyword matching against serialized highlight fields.
   */
  public searchEpisodeHighlightsByKeywords(params: {
    excludeSessionId?: string;
    keywords?: string[];
    limit?: number;
  }): SessionEpisodeHighlightRow[] {
    try {
      const keywords = Array.from(
        new Set(
          (params.keywords ?? [])
            .map((keyword) => keyword.trim().toLowerCase())
            .filter((keyword) => keyword.length >= 3)
        )
      );
      const limit = params.limit ?? 5;
      const sqlParts = [
        `SELECT eh.id, eh.episode_id, eh.key_decisions, eh.lessons, eh.next_steps,`,
        `       eh.quality_score, eh.ai_generated, eh.ai_model, eh.ai_tokens_used, eh.created_at,`,
        `       e.session_id, e.domain, e.track, e.outcome_summary`,
        `FROM episode_highlights eh`,
        `INNER JOIN episodes e ON e.id = eh.episode_id`,
        `WHERE 1 = 1`,
      ];
      const args: unknown[] = [];

      if (params.excludeSessionId) {
        sqlParts.push('AND e.session_id <> ?');
        args.push(params.excludeSessionId);
      }

      if (keywords.length > 0) {
        const keywordClauses: string[] = [];
        for (const keyword of keywords) {
          keywordClauses.push(`(
            LOWER(COALESCE(eh.key_decisions, '')) LIKE ? OR
            LOWER(COALESCE(eh.lessons, '')) LIKE ? OR
            LOWER(COALESCE(eh.next_steps, '')) LIKE ? OR
            LOWER(COALESCE(e.outcome_summary, '')) LIKE ?
          )`);
          const likeValue = `%${keyword}%`;
          args.push(likeValue, likeValue, likeValue, likeValue);
        }
        sqlParts.push(`AND (${keywordClauses.join(' OR ')})`);
      }

      sqlParts.push('ORDER BY eh.created_at DESC, eh.id ASC');
      sqlParts.push('LIMIT ?');
      args.push(limit);

      const stmt = this.connectionManager.getAgentPool().prepare(sqlParts.join('\n'));
      return stmt.all(...args) as SessionEpisodeHighlightRow[];
    } catch (error) {
      console.error('[AgentDb.searchEpisodeHighlightsByKeywords] Error:', error instanceof Error ? error.message : String(error));
      throw new Error('Failed to search prior episode highlights');
    }
  }

  /**
   * Search highlights using FTS5 for fast keyword-based retrieval.
   * Sanitizes query to prevent FTS syntax injection.
   */
  public searchHighlightsFts(params: {
    query: string;
    limit?: number;
  }): SessionEpisodeHighlightRow[] {
    try {
      const query = (params.query ?? '').trim();
      const limit = params.limit ?? 10;

      // Guard against empty query
      if (query.length === 0) {
        return [];
      }

      // Sanitize FTS query: escape double quotes and remove problematic operators
      // FTS5 operators: AND, OR, NOT, NEAR, *, :, etc.
      // Safe approach: use simple tokenization fallback if raw query contains operators
      const sanitizedQuery = this._sanitizeFtsQuery(query);

      // FTS5 MATCH against highlights_fts virtual table, join to get full episode context
      const stmt = this.connectionManager.getAgentPool().prepare(`
        SELECT eh.id, eh.episode_id, eh.key_decisions, eh.lessons, eh.next_steps,
               eh.quality_score, eh.ai_generated, eh.ai_model, eh.ai_tokens_used, eh.created_at,
               e.session_id, e.domain, e.track, e.outcome_summary
        FROM highlights_fts hf
        INNER JOIN episode_highlights eh ON eh.id = hf.highlight_id
        INNER JOIN episodes e ON e.id = eh.episode_id
        WHERE hf.highlights_fts MATCH ?
        ORDER BY hf.rank DESC, eh.created_at DESC
        LIMIT ?
      `);

      return stmt.all(sanitizedQuery, limit) as SessionEpisodeHighlightRow[];
    } catch (error) {
      console.error('[AgentDb.searchHighlightsFts] Error:', error instanceof Error ? error.message : String(error));
      // Fallback: return empty array on syntax error rather than throwing
      // This allows graceful degradation if FTS query is malformed
      return [];
    }
  }

  /**
   * Sanitize FTS5 query to prevent injection and syntax errors.
   * If query contains dangerous operators, fall back to simple token search.
   */
  private _sanitizeFtsQuery(query: string): string {
    // Detect problematic FTS5 operators that could cause parsing errors
    const problematicPatterns = /["':*\-(){}[\]]/g;

    if (problematicPatterns.test(query)) {
      // Fall back to AND-joined quoted phrases per token
      // This is safer and often what users expect anyway
      const tokens = query
        .split(/\s+/)
        .filter((token) => token.length >= 2)
        .map((token) => `"${token.replace(/"/g, '')}"`);
      return tokens.length > 0 ? tokens.join(' AND ') : query;
    }

    return query;
  }

  /**
   * Insert manual feedback for a highlight.
   */
  public insertHighlightFeedback(row: {
    id: string;
    highlight_id: string;
    rater_agent_id?: string | null;
    quality_score?: number | null;
    comment?: string | null;
  }): void {
    const stmt = this.connectionManager.getAdminPool().prepare(`
      INSERT INTO highlight_feedback (id, highlight_id, rater_agent_id, quality_score, comment, created_at)
      VALUES (?, ?, ?, ?, ?, datetime('now'))
    `);

    stmt.run(
      row.id,
      row.highlight_id,
      row.rater_agent_id ?? null,
      row.quality_score ?? null,
      row.comment ?? null
    );
  }

  /**
   * Return feedback numeric scores for a highlight.
   */
  public getHighlightFeedbackScores(highlightId: string): number[] {
    try {
      const stmt = this.connectionManager.getAgentPool().prepare(`
        SELECT quality_score
        FROM highlight_feedback
        WHERE highlight_id = ?
          AND quality_score IS NOT NULL
      `);
      const rows = stmt.all(highlightId) as Array<{ quality_score: number }>;
      return rows.map((row) => row.quality_score);
    } catch (error) {
      console.error('[AgentDb.getHighlightFeedbackScores] Error:', error instanceof Error ? error.message : String(error));
      return [];
    }
  }

  /**
   * Recalculate and persist the computed quality score for a highlight.
   *
   * Uses AI confidence (stored on the highlight row) and accumulated feedback.
   */
  public recalculateHighlightQuality(highlightId: string): number {
    try {
      const highlightStmt = this.connectionManager.getAgentPool().prepare(`
        SELECT id, quality_score, ai_quality_score
        FROM episode_highlights
        WHERE id = ?
        LIMIT 1
      `);
      const highlightRow = highlightStmt.get(highlightId) as
        | { id: string; quality_score: number | null; ai_quality_score: number | null }
        | undefined;

      if (!highlightRow) {
        throw new Error(`Highlight not found: ${highlightId}`);
      }

      const feedbackScores = this.getHighlightFeedbackScores(highlightId);
      const { computedScore, aiScore, feedbackAvg } = computeQualityScore(
        highlightRow.ai_quality_score ?? highlightRow.quality_score,
        feedbackScores
      );

      const updateStmt = this.connectionManager.getAdminPool().prepare(`
        UPDATE episode_highlights
        SET quality_score = ?,
            ai_quality_score = ?,
            feedback_quality_score = ?,
            quality_score_version = ?
        WHERE id = ?
      `);

      updateStmt.run(computedScore, aiScore, feedbackAvg, 1, highlightId);

      return computedScore;
    } catch (error) {
      console.error('[AgentDb.recalculateHighlightQuality] Error:', error);
      if (error instanceof Error) {
        console.error(error.stack);
      }
      throw new Error(`Failed to recalculate highlight quality for ${highlightId}`);
    }
  }

  /**
   * List all feedback entries for a highlight id.
   */
  public listHighlightFeedback(highlightId: string): HighlightFeedbackRow[] {
    try {
      const stmt = this.connectionManager.getAgentPool().prepare(`
        SELECT id, highlight_id, rater_agent_id, quality_score, comment, created_at
        FROM highlight_feedback
        WHERE highlight_id = ?
        ORDER BY created_at ASC
      `);
      return stmt.all(highlightId) as HighlightFeedbackRow[];
    } catch (error) {
      console.error('[AgentDb.listHighlightFeedback] Error:', error instanceof Error ? error.message : String(error));
      throw new Error(`Failed to list highlight feedback for ${highlightId}`);
    }
  }

  /**
   * Upsert ChromaDB sync metadata for a highlight.
   */
  public upsertHighlightChromaSync(row: {
    highlight_id: string;
    vector_id?: string | null;
    embedding_model?: string | null;
    last_synced?: string | null;
  }): void {
    const stmt = this.connectionManager.getAdminPool().prepare(`
      INSERT OR REPLACE INTO highlights_chromadb_sync (highlight_id, vector_id, embedding_model, last_synced)
      VALUES (?, ?, ?, ?)
    `);

    stmt.run(
      row.highlight_id,
      row.vector_id ?? null,
      row.embedding_model ?? null,
      row.last_synced ?? new Date().toISOString()
    );
  }

  /**
   * Get ChromaDB sync metadata for a highlight.
   */
  public getHighlightChromaSync(highlightId: string): HighlightChromaSyncRow | null {
    try {
      const stmt = this.connectionManager.getAgentPool().prepare(`
        SELECT highlight_id, vector_id, embedding_model, last_synced
        FROM highlights_chromadb_sync
        WHERE highlight_id = ?
        LIMIT 1
      `);
      const row = stmt.get(highlightId) as HighlightChromaSyncRow | undefined;
      return row ?? null;
    } catch (error) {
      console.error('[AgentDb.getHighlightChromaSync] Error:', error instanceof Error ? error.message : String(error));
      throw new Error(`Failed to fetch highlight chroma sync row for ${highlightId}`);
    }
  }

  /**
   * Load SQL migration file.
   *
   * @param filename - Migration file name (e.g., '002_write_layer_schema.sql')
   * @returns SQL content as string
   *
   * @throws Error if migration file not found
   */
  private loadMigration(filename: string): string {
    try {
      const migrationPath = join(__dirname, '../metadata/migrations', filename);
      return readFileSync(migrationPath, { encoding: 'utf-8' });
    } catch (error) {
      throw new Error(
        `Failed to load migration file: ${filename}. ${error instanceof Error ? error.message : String(error)}`
      );
    }
  }

  private tableExists(tableName: string): boolean {
    const row = this.connectionManager
      .getAgentPool()
      .prepare('SELECT name FROM sqlite_master WHERE type = ? AND name = ? LIMIT 1')
      .get('table', tableName) as { name: string } | undefined;
    return !!row?.name;
  }

  private getTableColumns(tableName: string): Set<string> {
    const rows = this.connectionManager
      .getAgentPool()
      .prepare(`PRAGMA table_info(${tableName})`)
      .all() as Array<{ name: string }>;
    return new Set(rows.map(r => r.name));
  }
}
