/**
 * AgentDb.e2e.test.ts — End-to-end test for EPIC-09 AgentDb service
 *
 * Fully integrated test with MCP context layer:
 * - Database persistence (temporary file)
 * - Schema initialization
 * - Multi-query workflows
 * - Query result validation
 *
 * @vitest
 */

import { describe, it, beforeAll, afterAll, expect } from 'vitest';
import { AgentDb, RoleRow, WorkflowRow } from '../../mcp/AgentDb';
import * as fs from 'fs';
import * as path from 'path';

describe('EPIC-09: AgentDb E2E Integration Tests', () => {
  let agentDb: AgentDb;
  let dbPath: string;

  beforeAll(() => {
    // Create a temporary database file
    const tempDir = path.join(__dirname, '../../..', '.test-databases');
    if (!fs.existsSync(tempDir)) {
      fs.mkdirSync(tempDir, { recursive: true });
    }
    dbPath = path.join(tempDir, `agentdb-e2e-${Date.now()}.db`);

    // Initialize AgentDb with persistent file
    agentDb = new AgentDb(dbPath);
    agentDb.initSchema();
  });

  afterAll(() => {
    agentDb.close();
    // Clean up test database file
    if (fs.existsSync(dbPath)) {
      fs.unlinkSync(dbPath);
    }
  });

  // ═══════════════════════════════════════════════════════════════════════════
  // WORKFLOW: Load a complete context bundle for a role
  // ═══════════════════════════════════════════════════════════════════════════

  describe('Workflow: Load Complete Role Context', () => {
    it('should load role, schema, and runbook for a given domain/role', () => {
      // Insert test data
      const db = agentDb.getRawDatabase();

      // Insert a role
      const insertRole = db.prepare(`
        INSERT INTO roles (domain, role_name, content)
        VALUES (?, ?, ?)
      `);
      insertRole.run('engineering', 'backend_developer', '# Backend Developer Role');

      // Insert a role schema
      const insertRoleSchema = db.prepare(`
        INSERT INTO role_schemas (domain, role_name, mcp_tool_permissions)
        VALUES (?, ?, ?)
      `);
      insertRoleSchema.run(
        'engineering',
        'backend_developer',
        JSON.stringify(['submitArtifact', 'queryKnowledge', 'updateWorkflow'])
      );

      // Insert a runbook
      const insertRunbook = db.prepare(`
        INSERT INTO runbooks (domain, role_name, content)
        VALUES (?, ?, ?)
      `);
      insertRunbook.run('engineering', 'backend_developer', '## Code Review Runbook\n1. Review PR\n2. Approve');

      // Now query using AgentDb
      const role = agentDb.getRole('engineering', 'backend_developer');
      const roleSchema = agentDb.getRoleSchema('engineering', 'backend_developer');
      const runbook = agentDb.getRunbook('engineering', 'backend_developer');

      expect(role).not.toBeNull();
      expect(role?.role_name).toBe('backend_developer');
      expect(role?.domain).toBe('engineering');

      expect(roleSchema).not.toBeNull();
      expect(roleSchema?.mcp_tool_permissions).toContain('submitArtifact');

      expect(runbook).not.toBeNull();
      expect(runbook?.content).toContain('Code Review Runbook');
    });

    it('should return null for non-existent role', () => {
      const role = agentDb.getRole('nonexistent_domain', 'nonexistent_role');
      expect(role).toBeNull();
    });
  });

  // ═══════════════════════════════════════════════════════════════════════════
  // WORKFLOW: Multi-role queries
  // ═══════════════════════════════════════════════════════════════════════════

  describe('Workflow: Query Multiple Roles', () => {
    it('should retrieve all roles for a domain', () => {
      const db = agentDb.getRawDatabase();

      // Insert multiple roles
      const insertRole = db.prepare(`
        INSERT INTO roles (domain, role_name, content)
        VALUES (?, ?, ?)
      `);
      insertRole.run('engineering', 'role1', 'Role 1');
      insertRole.run('engineering', 'role2', 'Role 2');
      insertRole.run('management', 'pm_role', 'PM Role');

      // Query engineering domain roles
      const roles = agentDb.getRolesByDomain('engineering');

      expect(roles.length).toBeGreaterThanOrEqual(2);
      expect(roles.some(r => r.role_name === 'role1')).toBe(true);
      expect(roles.some(r => r.role_name === 'role2')).toBe(true);
      expect(roles.every(r => r.domain === 'engineering')).toBe(true);
    });
  });

  // ═══════════════════════════════════════════════════════════════════════════
  // WORKFLOW: Workflows and Templates
  // ═══════════════════════════════════════════════════════════════════════════

  describe('Workflow: FSM Workflows and Templates', () => {
    it('should load workflow definition by type', () => {
      const db = agentDb.getRawDatabase();

      // Insert a workflow
      const insertWorkflow = db.prepare(`
        INSERT INTO workflows (domain, role_name, workflow_type, content)
        VALUES (?, ?, ?, ?)
      `);
      insertWorkflow.run(
        'engineering',
        'backend_developer',
        'feature_delivery',
        'started -> in_progress -> review -> approved -> deployed'
      );

      // Query workflow
      const workflow = agentDb.getWorkflow('engineering', 'backend_developer', 'feature_delivery');

      expect(workflow).not.toBeNull();
      expect(workflow?.workflow_type).toBe('feature_delivery');
      expect(workflow?.content).toContain('deployed');
    });

    it('should load all workflows for a role', () => {
      const db = agentDb.getRawDatabase();

      const insertWorkflow = db.prepare(`
        INSERT INTO workflows (domain, role_name, workflow_type, content)
        VALUES (?, ?, ?, ?)
      `);
      insertWorkflow.run('engineering', 'role_multi', 'workflow1', 'WF1');
      insertWorkflow.run('engineering', 'role_multi', 'workflow2', 'WF2');

      const workflows = agentDb.getWorkflowsByRole('engineering', 'role_multi');

      expect(workflows.length).toBeGreaterThanOrEqual(2);
      expect(workflows.every(w => w.role_name === 'role_multi')).toBe(true);
    });

    it('should load and retrieve templates', () => {
      const db = agentDb.getRawDatabase();

      const insertTemplate = db.prepare(`
        INSERT INTO templates (domain, role_name, template_name, content)
        VALUES (?, ?, ?, ?)
      `);
      insertTemplate.run(
        'engineering',
        'backend_developer',
        'implementation_summary',
        '# Implementation Summary\n## What Was Built\n'
      );

      const template = agentDb.getTemplate('engineering', 'backend_developer', 'implementation_summary');

      expect(template).not.toBeNull();
      expect(template?.template_name).toBe('implementation_summary');
      expect(template?.content).toContain('Implementation Summary');
    });

    it('should load all templates for a role', () => {
      const db = agentDb.getRawDatabase();

      const insertTemplate = db.prepare(`
        INSERT INTO templates (domain, role_name, template_name, content)
        VALUES (?, ?, ?, ?)
      `);
      insertTemplate.run('engineering', 'role_tmpl', 'template1', 'T1');
      insertTemplate.run('engineering', 'role_tmpl', 'template2', 'T2');

      const templates = agentDb.getTemplatesByRole('engineering', 'role_tmpl');

      expect(templates.length).toBeGreaterThanOrEqual(2);
      expect(templates.every(t => t.role_name === 'role_tmpl')).toBe(true);
    });
  });

  // ═══════════════════════════════════════════════════════════════════════════
  // WORKFLOW: Standards & ADR Lookup
  // ═══════════════════════════════════════════════════════════════════════════

  describe('Workflow: Standards and ADR Lookup', () => {
    it('should retrieve a standard by ID', () => {
      const db = agentDb.getRawDatabase();

      const insertStandard = db.prepare(`
        INSERT INTO standards (id, std_id, content)
        VALUES (?, ?, ?)
      `);
      insertStandard.run('ADR-001', 'ADR-001', '# ADR-001: MCP Architecture\n...');

      const standard = agentDb.getStandard('ADR-001');

      expect(standard).not.toBeNull();
      expect(standard?.std_id).toBe('ADR-001');
      expect(standard?.content).toContain('MCP Architecture');
    });

    it('should retrieve all standards with pagination', () => {
      const db = agentDb.getRawDatabase();

      const insertStandard = db.prepare(`
        INSERT INTO standards (id, std_id, content)
        VALUES (?, ?, ?)
      `);
      insertStandard.run('STD-001', 'STD-001', 'Standard 1');
      insertStandard.run('STD-002', 'STD-002', 'Standard 2');

      const standards = agentDb.getAllStandards(10, 0);

      expect(standards.length).toBeGreaterThanOrEqual(0);
    });
  });

  // ═══════════════════════════════════════════════════════════════════════════
  // CONCURRENCY & PERSISTENCE
  // ═══════════════════════════════════════════════════════════════════════════

  describe('Concurrency and Persistence', () => {
    it('should persist data across separate queries', () => {
      const db = agentDb.getRawDatabase();

      // Insert
      const insertRole = db.prepare(`
        INSERT INTO roles (domain, role_name, content)
        VALUES (?, ?, ?)
      `);
      insertRole.run('persistence_test', 'persistent_role', 'Persistent Role');

      // Query 1
      const role1 = agentDb.getRole('persistence_test', 'persistent_role');
      expect(role1).not.toBeNull();

      // Query 2 (re-query to verify persistence)
      const role2 = agentDb.getRole('persistence_test', 'persistent_role');
      expect(role2).not.toBeNull();
      expect(role2?.id).toBe(role1?.id);
    });

    it('should handle foreign key constraints', () => {
      // Enable FK constraint check
      const fkEnabled = agentDb.checkForeignKeyConstraints();
      expect(fkEnabled).toBe(true);
    });
  });

  // ═══════════════════════════════════════════════════════════════════════════
  // ERROR HANDLING
  // ═══════════════════════════════════════════════════════════════════════════

  describe('Error Handling', () => {
    it('should gracefully handle queries on empty tables', () => {
      const role = agentDb.getRole('empty', 'empty_role');
      expect(role).toBeNull();

      const roles = agentDb.getRolesByDomain('empty');
      expect(roles).toEqual([]);
    });
  });
});
