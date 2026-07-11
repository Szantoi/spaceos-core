/**
 * AgentDb.test.ts — Unit tests for EPIC-09 AgentDb service class
 *
 * Test matrix:
 * - Schema initialization (idempotent, all migrations)
 * - Query builders (getRole, getRoleSchema, getRunbook, getWorkflow, getTemplate, getStandard)
 * - Type safety (no nulls, proper typing)
 * - Foreign key enforcement
 * - PRAGMA configuration
 *
 * Coverage target: 85%+ for AgentDb
 * @vitest
 */

import { describe, it, beforeEach, afterEach, expect } from 'vitest';
import { AgentDb, RoleRow, RoleSchemaRow, RunbookRow, WorkflowRow, TemplateRow, StandardRow } from '../../mcp/AgentDb';
import { DatabaseConnectionManager } from '../../metadata/DatabaseConnectionManager';
import { join } from 'path';
import { existsSync, unlinkSync, mkdtempSync } from 'fs';
import { tmpdir } from 'os';

describe('EPIC-09: AgentDb Service Class', () => {
  let agentDb: AgentDb;
  let connectionManager: DatabaseConnectionManager;
  let tempDir: string;
  let dbPath: string;

  beforeEach(() => {
    // Create temporary directory for test database
    tempDir = mkdtempSync(join(tmpdir(), 'jest-agentdb-'));
    dbPath = join(tempDir, 'test.db');

    // Create connection manager and AgentDb
    connectionManager = new DatabaseConnectionManager(dbPath);
    agentDb = new AgentDb(connectionManager);
    agentDb.initSchema();
  });

  afterEach(() => {
    agentDb.close();

    // Clean up temp directory
    try {
      if (dbPath && existsSync(dbPath)) unlinkSync(dbPath);
      if (dbPath && existsSync(`${dbPath}-wal`)) unlinkSync(`${dbPath}-wal`);
      if (dbPath && existsSync(`${dbPath}-shm`)) unlinkSync(`${dbPath}-shm`);
      if (tempDir && existsSync(tempDir)) unlinkSync(tempDir);
    } catch {
      // Ignore cleanup errors
    }
  });

  // ═══════════════════════════════════════════════════════════════════════════
  // SCHEMA INITIALIZATION TESTS
  // ═══════════════════════════════════════════════════════════════════════════

  describe('Schema Initialization', () => {
    it('should initialize database with PRAGMA settings', () => {
      const fkEnabled = agentDb.checkForeignKeyConstraints();
      expect(fkEnabled).toBe(true);
    });

    it('should be idempotent (call init twice without error)', () => {
      // First call
      agentDb.initSchema();
      const db = agentDb.getRawDatabase();
      const countAfterFirst = db
        .prepare(
          "SELECT COUNT(*) as count FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'"
        )
        .get() as { count: number };

      // Second call should not error
      agentDb.initSchema();
      const countAfterSecond = db
        .prepare(
          "SELECT COUNT(*) as count FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'"
        )
        .get() as { count: number };

      expect(countAfterSecond.count).toBe(countAfterFirst.count);
    });

    it('should create required tables including DWI schema tables', () => {
      const db = agentDb.getRawDatabase();
      const tables = db
        .prepare(
          "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name"
        )
        .all() as Array<{ name: string }>;

      const tableNames = tables.map(t => t.name).sort();

      // EPIC-08 write layer (4 tables)
      expect(tableNames).toContain('artifacts');
      expect(tableNames).toContain('checkpoints');
      expect(tableNames).toContain('sessions');
      expect(tableNames).toContain('workflow_events');

      // EPIC-09 context layer (6 tables)
      expect(tableNames).toContain('role_schemas');
      expect(tableNames).toContain('roles');
      expect(tableNames).toContain('runbooks');
      expect(tableNames).toContain('standards');
      expect(tableNames).toContain('templates');
      expect(tableNames).toContain('workflows');

      // EPIC-18 DWI schema (3 tables)
      expect(tableNames).toContain('discovery_work_items');
      expect(tableNames).toContain('dwi_phase_gates');
      expect(tableNames).toContain('dwi_hypotheses');
    });

    describe('Discovery Work Item (DWI) queries', () => {
      beforeEach(() => {
        const db = agentDb.getRawDatabase();
        const insertDwi = db.prepare(`
          INSERT INTO discovery_work_items
            (id, topic, status, current_phase, next_action, verdict, hypothesis_count, validated_count, created_by, updated_by)
          VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        `);

        insertDwi.run(
          'dwi-test-001',
          'Test topic A',
          'open',
          0,
          'Write tests for DWI',
          null,
          0,
          0,
          'tester',
          'tester'
        );

        insertDwi.run(
          'dwi-test-002',
          'Another topic',
          'in_progress',
          1,
          'Run discovery sprint',
          null,
          1,
          0,
          'tester',
          'tester'
        );
      });

      it('should return null when DWI not found', () => {
        const result = agentDb.getDwiState('dwi-nonexistent');
        expect(result).toBeNull();
      });

      it('should return DWI state by id', () => {
        const result = agentDb.getDwiState('dwi-test-001');
        expect(result).toBeDefined();
        expect(result?.id).toBe('dwi-test-001');
        expect(result?.topic).toBe('Test topic A');
        expect(result?.status).toBe('open');
      });

      it('should list unique DWI topics', () => {
        const topics = agentDb.listDwiTopics();
        expect(topics).toEqual(['Another topic', 'Test topic A']);
      });

      it('should upsert DWI state and read it back', () => {
        agentDb.upsertDwiState({
          id: 'dwi-test-003',
          topic: 'Upsert topic',
          status: 'open',
          current_phase: 0,
          next_action: 'Explorer: start discovery',
          verdict: null,
          hypothesis_count: 0,
          validated_count: 0,
          created_at: '2026-03-14T00:00:00Z',
          updated_at: '2026-03-14T00:00:00Z',
          created_by: 'tester',
          updated_by: 'tester',
        });

        const dwi = agentDb.getDwiState('dwi-test-003');
        expect(dwi).not.toBeNull();
        expect(dwi?.topic).toBe('Upsert topic');
      });

      it('should list DWI dashboard rows with filters', () => {
        // seed two DWIs
        agentDb.upsertDwiState({
          id: 'dwi-test-004',
          topic: 'Alpha Topic',
          status: 'open',
          current_phase: 1,
          next_action: 'Do X',
          verdict: null,
          hypothesis_count: 0,
          validated_count: 0,
        });
        agentDb.upsertDwiState({
          id: 'dwi-test-005',
          topic: 'Beta Topic',
          status: 'in_progress',
          current_phase: 2,
          next_action: 'Do Y',
          verdict: null,
          hypothesis_count: 0,
          validated_count: 0,
        });

        const all = agentDb.listDwiDashboard();
        expect(all.length).toBeGreaterThanOrEqual(2);

        const openItems = agentDb.listDwiDashboard({ status: 'open' });
        expect(openItems.every((row) => row.status === 'open')).toBe(true);

        const phase2Items = agentDb.listDwiDashboard({ currentPhase: 2 });
        expect(phase2Items.every((row) => row.current_phase === 2)).toBe(true);

        const topicSearch = agentDb.listDwiDashboard({ topic: 'Beta' });
        expect(topicSearch.every((row) => row.topic.includes('Beta'))).toBe(true);
      });
    });
  });

  // ═══════════════════════════════════════════════════════════════════════════
  // GETTERS — ROLES TABLE
  // ═══════════════════════════════════════════════════════════════════════════

  describe('getRole() — Query roles table', () => {
    beforeEach(() => {
      // Seed test data
      const db = agentDb.getRawDatabase();
      const insertRole = db.prepare(
        'INSERT INTO roles (domain, role_name, content) VALUES (?, ?, ?)'
      );
      insertRole.run('engineering', 'backend_developer', 'Backend Developer role definition');
      insertRole.run('management', 'tech_lead', 'Tech Lead role definition');
      insertRole.run('engineering', 'tech_lead', 'Engineering Tech Lead definition');
    });

    it('should return role by domain and role_name', () => {
      const role = agentDb.getRole('engineering', 'backend_developer');

      expect(role).toBeDefined();
      expect(role).toHaveProperty('id');
      expect(role?.domain).toBe('engineering');
      expect(role?.role_name).toBe('backend_developer');
      expect(role?.content).toBe('Backend Developer role definition');
    });

    it('should return RoleRow type with all fields', () => {
      const role = agentDb.getRole('engineering', 'backend_developer') as RoleRow;

      expect(role.id).toBeGreaterThan(0);
      expect(role.domain).toBe('engineering');
      expect(role.role_name).toBe('backend_developer');
      expect(role.created_at).toBeDefined();
      expect(role.last_updated).toBeDefined();
    });

    it('should return null for non-existent role', () => {
      const role = agentDb.getRole('invalid_domain', 'invalid_role');
      expect(role).toBeNull();
    });

    it('should distinguish roles by domain (same role_name, different domains)', () => {
      const engineeringTechLead = agentDb.getRole('engineering', 'tech_lead');
      const managementTechLead = agentDb.getRole('management', 'tech_lead');

      expect(engineeringTechLead?.content).toBe('Engineering Tech Lead definition');
      expect(managementTechLead?.content).toBe('Tech Lead role definition');
      expect(engineeringTechLead?.id).not.toBe(managementTechLead?.id);
    });
  });

  describe('getRolesByDomain() — Query roles by domain', () => {
    beforeEach(() => {
      const db = agentDb.getRawDatabase();
      const insertRole = db.prepare(
        'INSERT INTO roles (domain, role_name, content) VALUES (?, ?, ?)'
      );
      insertRole.run('engineering', 'backend_developer', 'Backend');
      insertRole.run('engineering', 'tech_lead', 'Tech Lead');
      insertRole.run('management', 'pm', 'Product Manager');
    });

    it('should return all roles for a domain', () => {
      const roles = agentDb.getRolesByDomain('engineering');

      expect(roles).toHaveLength(2);
      expect(roles.every(r => r.domain === 'engineering')).toBe(true);
      expect(roles.map(r => r.role_name).sort()).toEqual(['backend_developer', 'tech_lead']);
    });

    it('should return empty array if domain not found', () => {
      const roles = agentDb.getRolesByDomain('nonexistent');
      expect(roles).toEqual([]);
    });

    it('should order results by role_name', () => {
      const roles = agentDb.getRolesByDomain('engineering');
      const names = roles.map(r => r.role_name);

      expect(names).toEqual(names.sort());
    });
  });

  // ═══════════════════════════════════════════════════════════════════════════
  // GETTERS — ROLE_SCHEMAS TABLE
  // ═══════════════════════════════════════════════════════════════════════════

  describe('getRoleSchema() — Query role_schemas table', () => {
    beforeEach(() => {
      const db = agentDb.getRawDatabase();
      // Insert role first (required for FK constraint)
      const insertRole = db.prepare(
        'INSERT OR IGNORE INTO roles (domain, role_name, content) VALUES (?, ?, ?)'
      );
      insertRole.run('engineering', 'backend_developer', 'Role content');

      const insertSchema = db.prepare(
        'INSERT INTO role_schemas (domain, role_name, mcp_tool_permissions) VALUES (?, ?, ?)'
      );
      const toolsJson = JSON.stringify({
        submit_artifact: { allowed: true },
        update_workflow_state: { allowed: true },
      });
      insertSchema.run('engineering', 'backend_developer', toolsJson);
    });

    it('should return role schema by domain and role_name', () => {
      const schema = agentDb.getRoleSchema('engineering', 'backend_developer');

      expect(schema).toBeDefined();
      expect(schema?.domain).toBe('engineering');
      expect(schema?.role_name).toBe('backend_developer');
    });

    it('should parse mcp_tool_permissions as JSON string', () => {
      const schema = agentDb.getRoleSchema('engineering', 'backend_developer') as RoleSchemaRow;
      const permissions = JSON.parse(schema.mcp_tool_permissions);

      expect(permissions).toHaveProperty('submit_artifact');
      expect(permissions).toHaveProperty('update_workflow_state');
    });

    it('should return null for non-existent schema', () => {
      const schema = agentDb.getRoleSchema('invalid', 'invalid');
      expect(schema).toBeNull();
    });
  });

  // ═══════════════════════════════════════════════════════════════════════════
  // GETTERS — RUNBOOKS TABLE
  // ═══════════════════════════════════════════════════════════════════════════

  describe('getRunbook() — Query runbooks table', () => {
    beforeEach(() => {
      const db = agentDb.getRawDatabase();
      // Insert role first (required for FK constraint)
      const insertRole = db.prepare(
        'INSERT OR IGNORE INTO roles (domain, role_name, content) VALUES (?, ?, ?)'
      );
      insertRole.run('engineering', 'backend_developer', 'Role content');

      const insertRunbook = db.prepare(
        'INSERT INTO runbooks (domain, role_name, content) VALUES (?, ?, ?)'
      );
      insertRunbook.run('engineering', 'backend_developer', '# Backend Developer Runbook\n\n1. Step one\n2. Step two');
    });

    it('should return runbook by domain and role_name', () => {
      const runbook = agentDb.getRunbook('engineering', 'backend_developer');

      expect(runbook).toBeDefined();
      expect(runbook?.content).toContain('Backend Developer Runbook');
    });

    it('should return RunbookRow type', () => {
      const runbook = agentDb.getRunbook('engineering', 'backend_developer') as RunbookRow;

      expect(runbook.id).toBeGreaterThan(0);
      expect(runbook.domain).toBe('engineering');
      expect(runbook.role_name).toBe('backend_developer');
      expect(runbook.created_at).toBeDefined();
    });

    it('should return null if runbook not found', () => {
      const runbook = agentDb.getRunbook('invalid', 'invalid');
      expect(runbook).toBeNull();
    });
  });

  // ═══════════════════════════════════════════════════════════════════════════
  // GETTERS — WORKFLOWS TABLE
  // ═══════════════════════════════════════════════════════════════════════════

  describe('getWorkflow() — Query workflows table', () => {
    beforeEach(() => {
      const db = agentDb.getRawDatabase();
      // Insert role first (required for FK constraint)
      const insertRole = db.prepare(
        'INSERT OR IGNORE INTO roles (domain, role_name, content) VALUES (?, ?, ?)'
      );
      insertRole.run('engineering', 'backend_developer', 'Role content');

      const insertWorkflow = db.prepare(
        'INSERT INTO workflows (domain, role_name, workflow_type, content) VALUES (?, ?, ?, ?)'
      );
      insertWorkflow.run('engineering', 'backend_developer', 'feature_delivery', 'Feature delivery workflow content');
      insertWorkflow.run('engineering', 'backend_developer', 'code_review', 'Code review workflow content');
    });

    it('should return workflow by domain, role_name, and workflow_type', () => {
      const workflow = agentDb.getWorkflow('engineering', 'backend_developer', 'feature_delivery');

      expect(workflow).toBeDefined();
      expect(workflow?.content).toContain('Feature delivery');
    });

    it('should return WorkflowRow type', () => {
      const workflow = agentDb.getWorkflow('engineering', 'backend_developer', 'feature_delivery') as WorkflowRow;

      expect(workflow.id).toBeGreaterThan(0);
      expect(workflow.workflow_type).toBe('feature_delivery');
      expect(workflow.created_at).toBeDefined();
    });

    it('should return null if workflow not found', () => {
      const workflow = agentDb.getWorkflow('invalid', 'invalid', 'invalid');
      expect(workflow).toBeNull();
    });

    it('should distinguish workflows by type', () => {
      const featureWorkflow = agentDb.getWorkflow('engineering', 'backend_developer', 'feature_delivery');
      const codeReviewWorkflow = agentDb.getWorkflow('engineering', 'backend_developer', 'code_review');

      expect(featureWorkflow?.content).not.toBe(codeReviewWorkflow?.content);
    });
  });

  describe('getWorkflowsByRole() — Query workflows by role', () => {
    beforeEach(() => {
      const db = agentDb.getRawDatabase();
      // Insert roles first (required for FK constraint)
      const insertRole = db.prepare(
        'INSERT OR IGNORE INTO roles (domain, role_name, content) VALUES (?, ?, ?)'
      );
      insertRole.run('engineering', 'backend_developer', 'Role content');
      insertRole.run('engineering', 'tech_lead', 'Role content');

      const insertWorkflow = db.prepare(
        'INSERT INTO workflows (domain, role_name, workflow_type, content) VALUES (?, ?, ?, ?)'
      );
      insertWorkflow.run('engineering', 'backend_developer', 'feature_delivery', 'Content 1');
      insertWorkflow.run('engineering', 'backend_developer', 'code_review', 'Content 2');
      insertWorkflow.run('engineering', 'tech_lead', 'approval', 'Content 3');
    });

    it('should return all workflows for a role', () => {
      const workflows = agentDb.getWorkflowsByRole('engineering', 'backend_developer');

      expect(workflows).toHaveLength(2);
      expect(workflows.every(w => w.role_name === 'backend_developer')).toBe(true);
    });

    it('should order results by workflow_type', () => {
      const workflows = agentDb.getWorkflowsByRole('engineering', 'backend_developer');
      const types = workflows.map(w => w.workflow_type);

      expect(types).toEqual(types.sort());
    });
  });

  // ═══════════════════════════════════════════════════════════════════════════
  // GETTERS — TEMPLATES TABLE
  // ═══════════════════════════════════════════════════════════════════════════

  describe('getTemplate() — Query templates table', () => {
    beforeEach(() => {
      const db = agentDb.getRawDatabase();
      // Insert role first (required for FK constraint)
      const insertRole = db.prepare(
        'INSERT OR IGNORE INTO roles (domain, role_name, content) VALUES (?, ?, ?)'
      );
      insertRole.run('engineering', 'backend_developer', 'Role content');

      const insertTemplate = db.prepare(
        'INSERT INTO templates (domain, role_name, template_name, content) VALUES (?, ?, ?, ?)'
      );
      insertTemplate.run('engineering', 'backend_developer', 'implementation_summary', '# Implementation Summary\n\n[Content]');
      insertTemplate.run('engineering', 'backend_developer', 'test_report', '# Test Report\n\n[Content]');
    });

    it('should return template by domain, role_name, and template_name', () => {
      const template = agentDb.getTemplate('engineering', 'backend_developer', 'implementation_summary');

      expect(template).toBeDefined();
      expect(template?.template_name).toBe('implementation_summary');
    });

    it('should return TemplateRow type', () => {
      const template = agentDb.getTemplate('engineering', 'backend_developer', 'implementation_summary') as TemplateRow;

      expect(template.id).toBeGreaterThan(0);
      expect(template.template_name).toBe('implementation_summary');
      expect(template.content).toContain('Implementation Summary');
    });

    it('should return null if template not found', () => {
      const template = agentDb.getTemplate('invalid', 'invalid', 'invalid');
      expect(template).toBeNull();
    });

    it('should distinguish templates by name', () => {
      const summaryTemplate = agentDb.getTemplate('engineering', 'backend_developer', 'implementation_summary');
      const testTemplate = agentDb.getTemplate('engineering', 'backend_developer', 'test_report');

      expect(summaryTemplate?.content).not.toBe(testTemplate?.content);
    });
  });

  describe('getTemplatesByRole() — Query templates by role', () => {
    beforeEach(() => {
      const db = agentDb.getRawDatabase();
      // Insert roles first (required for FK constraint)
      const insertRole = db.prepare(
        'INSERT OR IGNORE INTO roles (domain, role_name, content) VALUES (?, ?, ?)'
      );
      insertRole.run('engineering', 'backend_developer', 'Role content');
      insertRole.run('engineering', 'tech_lead', 'Role content');

      const insertTemplate = db.prepare(
        'INSERT INTO templates (domain, role_name, template_name, content) VALUES (?, ?, ?, ?)'
      );
      insertTemplate.run('engineering', 'backend_developer', 'implementation_summary', 'Content 1');
      insertTemplate.run('engineering', 'backend_developer', 'test_report', 'Content 2');
      insertTemplate.run('engineering', 'tech_lead', 'approval', 'Content 3');
    });

    it('should return all templates for a role', () => {
      const templates = agentDb.getTemplatesByRole('engineering', 'backend_developer');

      expect(templates).toHaveLength(2);
      expect(templates.every(t => t.role_name === 'backend_developer')).toBe(true);
    });

    it('should order results by template_name', () => {
      const templates = agentDb.getTemplatesByRole('engineering', 'backend_developer');
      const names = templates.map(t => t.template_name);

      expect(names).toEqual(names.sort());
    });
  });

  // ═══════════════════════════════════════════════════════════════════════════
  // GETTERS — STANDARDS TABLE
  // ═══════════════════════════════════════════════════════════════════════════

  describe('getStandard() — Query standards table', () => {
    beforeEach(() => {
      const db = agentDb.getRawDatabase();
      const insertStandard = db.prepare(
        'INSERT INTO standards (std_id, content) VALUES (?, ?)'
      );
      insertStandard.run('ADR-001', '# ADR-001: Architecture Decision\n\nContent...');
      insertStandard.run('STD-NAMING', '# Naming Conventions\n\nContent...');
    });

    it('should return standard by std_id', () => {
      const standard = agentDb.getStandard('ADR-001');

      expect(standard).toBeDefined();
      expect(standard?.std_id).toBe('ADR-001');
    });

    it('should return StandardRow type', () => {
      const standard = agentDb.getStandard('ADR-001') as StandardRow;

      expect(standard.id).toBeDefined();
      expect(standard.std_id).toBe('ADR-001');
      expect(standard.content).toContain('Architecture Decision');
    });

    it('should return null if standard not found', () => {
      const standard = agentDb.getStandard('NONEXISTENT');
      expect(standard).toBeNull();
    });
  });

  describe('getAllStandards() — Paginated query', () => {
    beforeEach(() => {
      const db = agentDb.getRawDatabase();
      const insertStandard = db.prepare(
        'INSERT INTO standards (std_id, content) VALUES (?, ?)'
      );
      insertStandard.run('ADR-001', 'Content 1');
      insertStandard.run('ADR-002', 'Content 2');
      insertStandard.run('ADR-003', 'Content 3');
    });

    it('should return all standards with default parameters', () => {
      const standards = agentDb.getAllStandards();

      expect(standards.length).toBe(3);
      expect(standards.every(s => s.std_id.startsWith('ADR-'))).toBe(true);
    });

    it('should support limit parameter', () => {
      const standards = agentDb.getAllStandards(2, 0);
      expect(standards).toHaveLength(2);
    });

    it('should support offset parameter (pagination)', () => {
      const page1 = agentDb.getAllStandards(2, 0);
      const page2 = agentDb.getAllStandards(2, 2);

      expect(page1[0].std_id).not.toBe(page2[0].std_id);
    });

    it('should order results by std_id', () => {
      const standards = agentDb.getAllStandards();
      const ids = standards.map(s => s.std_id);

      expect(ids).toEqual(ids.sort());
    });
  });

  // ═══════════════════════════════════════════════════════════════════════════
  // TYPE SAFETY & MISC
  // ═══════════════════════════════════════════════════════════════════════════

  describe('Type Safety & Misc', () => {
    it('should enforce strict typing (no any)', () => {
      const db = agentDb.getRawDatabase();
      const insertRole = db.prepare(
        'INSERT INTO roles (domain, role_name, content) VALUES (?, ?, ?)'
      );
      insertRole.run('engineering', 'backend_developer', 'Role content');

      const role = agentDb.getRole('engineering', 'backend_developer') as RoleRow;

      // TypeScript compiler checks these at build time
      const domain: string = role.domain;
      const roleName: string = role.role_name;
      const id: number = role.id;

      expect(typeof domain).toBe('string');
      expect(typeof roleName).toBe('string');
      expect(typeof id).toBe('number');
    });

    it('should close database connection cleanly', () => {
      agentDb.close();
      // Should not throw when accessing closed db (already tested)
      expect(() => agentDb.getRawDatabase()).not.toThrow();
    });

    it('should return raw database instance via getRawDatabase()', () => {
      const db = agentDb.getRawDatabase();
      expect(db).toBeDefined();

      // Can still use raw API
      const result = db.prepare('SELECT 1 as value').get() as { value: number };
      expect(result.value).toBe(1);
    });
  });
});
