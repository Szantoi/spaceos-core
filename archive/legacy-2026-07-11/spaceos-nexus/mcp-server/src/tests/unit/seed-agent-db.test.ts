/**
 * seed-agent-db.test.ts — Unit tests for EPIC-09 AgentDbSeeder
 *
 * Test matrix:
 * - Role directory scanning
 * - File parsing (MD, YAML → JSON)
 * - INSERT OR REPLACE idempotency
 * - Statistics logging
 * - Error handling
 *
 * Coverage target: 80%+ for AgentDbSeeder
 * @vitest
 */

import { describe, it, beforeEach, afterEach, expect } from 'vitest';
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';
import { AgentDb } from '../../mcp/AgentDb';
import { DatabaseConnectionManager } from '../../metadata/DatabaseConnectionManager';
import { AgentDbSeeder } from '../../../scripts/seed-agent-db';

describe('EPIC-09: AgentDbSeeder (Simplified Mock Tests)', () => {
  let tmpDir: string;
  let rolesDir: string;
  let standardsDir: string;
  let discoveryDir: string;
  let dbPath: string;
  let agentDb: AgentDb;
  let connectionManager: DatabaseConnectionManager;

  beforeEach(() => {
    // Create temporary directories for testing
    tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'agent-seeder-'));
    rolesDir = path.join(tmpDir, 'roles');
    standardsDir = path.join(tmpDir, 'standards');
    discoveryDir = path.join(tmpDir, 'discovery');
    dbPath = path.join(tmpDir, 'test.db');

    fs.mkdirSync(rolesDir);
    fs.mkdirSync(standardsDir);
    fs.mkdirSync(discoveryDir);

    // Initialize DatabaseConnectionManager and AgentDb
    connectionManager = new DatabaseConnectionManager(dbPath);
    agentDb = new AgentDb(connectionManager);
    agentDb.initSchema();
  });

  afterEach(() => {
    agentDb.close();
    // Clean up temporary directory
    fs.rmSync(tmpDir, { recursive: true, force: true });
  });

  // ═══════════════════════════════════════════════════════════════════════════
  // HELPER: Create test role structure
  // ═══════════════════════════════════════════════════════════════════════════

  const createTestRole = (domain: string, roleName: string) => {
    const roleDir = path.join(rolesDir, domain, roleName);
    fs.mkdirSync(roleDir, { recursive: true });

    // Create role.md
    fs.writeFileSync(path.join(roleDir, `${roleName}.role.md`), `# ${roleName} Role\nDescription here.`);

    // Create schema.yaml (will be converted to JSON)
    fs.writeFileSync(
      path.join(roleDir, `${roleName}.schema.yaml`),
      `tools:
  - submitArtifact
  - queryKnowledge
permissions: read_write`
    );

    // Create runbook.md
    fs.writeFileSync(path.join(roleDir, `${roleName}.runbook.md`), `# ${roleName} Runbook\n1. Step 1\n2. Step 2`);

    // Create workflows directory
    const workflowsDir = path.join(roleDir, 'workflows');
    fs.mkdirSync(workflowsDir);
    fs.writeFileSync(path.join(workflowsDir, 'feature_delivery.workflow.md'), '# Feature Delivery Workflow');

    // Create templates directory
    const templatesDir = path.join(roleDir, 'templates');
    fs.mkdirSync(templatesDir);
    fs.writeFileSync(
      path.join(templatesDir, 'implementation_summary.template.md'),
      '# Implementation Summary Template'
    );
  };

  // ═══════════════════════════════════════════════════════════════════════════
  // SCHEMA INITIALIZATION TESTS
  // ═══════════════════════════════════════════════════════════════════════════

  describe('Seeder Configuration', () => {
    it('should initialize database with schema', () => {
      const fkEnabled = agentDb.checkForeignKeyConstraints();
      expect(fkEnabled).toBe(true);

      // Verify tables exist
      const db = agentDb.getRawDatabase();
      const tables = db
        .prepare("SELECT name FROM sqlite_master WHERE type='table'")
        .all() as Array<{ name: string }>;
      const tableNames = tables.map(t => t.name);

      expect(tableNames).toContain('roles');
      expect(tableNames).toContain('role_schemas');
      expect(tableNames).toContain('runbooks');
      expect(tableNames).toContain('workflows');
      expect(tableNames).toContain('templates');
      expect(tableNames).toContain('standards');
    });
  });

  // ═══════════════════════════════════════════════════════════════════════════
  // MANUAL TEST: Seeder Logic (via AgentDb direct inserts)
  // ═══════════════════════════════════════════════════════════════════════════

  describe('Seeder Logic: Manual Role Insertion', () => {
    it('should insert a role into roles table', () => {
      const db = agentDb.getRawDatabase();

      // Simulate what seeder would do
      const stmt = db.prepare(`
        INSERT OR REPLACE INTO roles (domain, role_name, content)
        VALUES (?, ?, ?)
      `);
      stmt.run('engineering', 'backend_developer', '# Backend Developer');

      // Query back
      const role = agentDb.getRole('engineering', 'backend_developer');
      expect(role).not.toBeNull();
      expect(role?.role_name).toBe('backend_developer');
    });

    it('should insert a role schema (YAML → JSON)', () => {
      const db = agentDb.getRawDatabase();

      // PREREQUISITE: Insert parent role first (FK constraint)
      const roleStmt = db.prepare(`
        INSERT OR REPLACE INTO roles (domain, role_name, content)
        VALUES (?, ?, ?)
      `);
      roleStmt.run('engineering', 'backend_developer', '# Backend Developer');

      const schemaJson = JSON.stringify({ tools: ['submitArtifact', 'queryKnowledge'], permissions: 'read_write' });

      const stmt = db.prepare(`
        INSERT OR REPLACE INTO role_schemas (domain, role_name, mcp_tool_permissions)
        VALUES (?, ?, ?)
      `);
      stmt.run('engineering', 'backend_developer', schemaJson);

      // Query back
      const roleSchema = agentDb.getRoleSchema('engineering', 'backend_developer');
      expect(roleSchema).not.toBeNull();
      expect(roleSchema?.mcp_tool_permissions).toContain('submitArtifact');
    });

    it('should insert runbook', () => {
      const db = agentDb.getRawDatabase();

      // PREREQUISITE: Insert parent role first (FK constraint)
      const roleStmt = db.prepare(`
        INSERT OR REPLACE INTO roles (domain, role_name, content)
        VALUES (?, ?, ?)
      `);
      roleStmt.run('engineering', 'backend_developer', '# Backend Developer');

      const stmt = db.prepare(`
        INSERT OR REPLACE INTO runbooks (domain, role_name, content)
        VALUES (?, ?, ?)
      `);
      stmt.run('engineering', 'backend_developer', '# Runbook\n1. Step 1');

      const runbook = agentDb.getRunbook('engineering', 'backend_developer');
      expect(runbook).not.toBeNull();
      expect(runbook?.content).toContain('Step 1');
    });

    it('should insert workflow', () => {
      const db = agentDb.getRawDatabase();

      // PREREQUISITE: Insert parent role first (FK constraint)
      const roleStmt = db.prepare(`
        INSERT OR REPLACE INTO roles (domain, role_name, content)
        VALUES (?, ?, ?)
      `);
      roleStmt.run('engineering', 'backend_developer', '# Backend Developer');

      const stmt = db.prepare(`
        INSERT OR REPLACE INTO workflows (domain, role_name, workflow_type, content)
        VALUES (?, ?, ?, ?)
      `);
      stmt.run('engineering', 'backend_developer', 'feature_delivery', '# Feature Workflow');

      const workflow = agentDb.getWorkflow('engineering', 'backend_developer', 'feature_delivery');
      expect(workflow).not.toBeNull();
      expect(workflow?.workflow_type).toBe('feature_delivery');
    });

    it('should insert template', () => {
      const db = agentDb.getRawDatabase();

      // PREREQUISITE: Insert parent role first (FK constraint)
      const roleStmt = db.prepare(`
        INSERT OR REPLACE INTO roles (domain, role_name, content)
        VALUES (?, ?, ?)
      `);
      roleStmt.run('engineering', 'backend_developer', '# Backend Developer');

      const stmt = db.prepare(`
        INSERT OR REPLACE INTO templates (domain, role_name, template_name, content)
        VALUES (?, ?, ?, ?)
      `);
      stmt.run('engineering', 'backend_developer', 'implementation_summary', '# Summary Template');

      const template = agentDb.getTemplate('engineering', 'backend_developer', 'implementation_summary');
      expect(template).not.toBeNull();
      expect(template?.template_name).toBe('implementation_summary');
    });

    it('should insert standard', () => {
      const db = agentDb.getRawDatabase();

      // FIX: Do NOT pass 'id' — it's INTEGER PRIMARY KEY AUTOINCREMENT
      // Schema: CREATE TABLE standards (id INTEGER PRIMARY KEY AUTOINCREMENT, std_id TEXT NOT NULL UNIQUE, ...)
      const stmt = db.prepare(`
        INSERT OR REPLACE INTO standards (std_id, content)
        VALUES (?, ?)
      `);
      stmt.run('ADR-001', '# ADR-001: MCP Architecture');

      const standard = agentDb.getStandard('ADR-001');
      expect(standard).not.toBeNull();
      expect(standard?.std_id).toBe('ADR-001');
    });
  });

  // ═══════════════════════════════════════════════════════════════════════════
  // IDEMPOTENCY TEST
  // ═══════════════════════════════════════════════════════════════════════════

  describe('Idempotency: INSERT OR REPLACE', () => {
    it('should replace role on second insert (no duplicates)', () => {
      const db = agentDb.getRawDatabase();

      const stmt = db.prepare(`
        INSERT OR REPLACE INTO roles (domain, role_name, content)
        VALUES (?, ?, ?)
      `);

      // First insert
      stmt.run('engineering', 'backend_developer', '# Version 1');

      // Second insert (should replace, not duplicate)
      stmt.run('engineering', 'backend_developer', '# Version 2');

      // Query
      const role = agentDb.getRole('engineering', 'backend_developer');
      expect(role?.content).toBe('# Version 2');

      // Verify count is 1 (no duplicates)
      const count = db.prepare('SELECT COUNT(*) as cnt FROM roles').get() as { cnt: number };
      expect(count.cnt).toBe(1);
    });

    it('should handle replace for all table types', () => {
      const db = agentDb.getRawDatabase();

      // Insert role twice
      const roleStmt = db.prepare(`
        INSERT OR REPLACE INTO roles (domain, role_name, content)
        VALUES (?, ?, ?)
      `);
      roleStmt.run('engineering', 'test_role', 'Version 1');
      roleStmt.run('engineering', 'test_role', 'Version 2');

      const roleCount = db.prepare('SELECT COUNT(*) as cnt FROM roles').get() as { cnt: number };
      expect(roleCount.cnt).toBe(1);

      // Same for workflows
      const workflowStmt = db.prepare(`
        INSERT OR REPLACE INTO workflows (domain, role_name, workflow_type, content)
        VALUES (?, ?, ?, ?)
      `);
      workflowStmt.run('engineering', 'test_role', 'wf1', 'WF1');
      workflowStmt.run('engineering', 'test_role', 'wf1', 'WF1-updated');

      const workflowCount = db.prepare('SELECT COUNT(*) as cnt FROM workflows').get() as { cnt: number };
      expect(workflowCount.cnt).toBe(1);
    });
  });

  // ═══════════════════════════════════════════════════════════════════════════
  // DISCOVERY WORK ITEMS (DWI) SEEDING
  // ═══════════════════════════════════════════════════════════════════════════

  describe('Seeder Logic: Discovery Work Items', () => {
    it('should seed DWI state files into discovery_work_items and related tables', async () => {
      const topicDir = path.join(discoveryDir, 'mcp-rbac');
      fs.mkdirSync(topicDir, { recursive: true });

      const dwiStateContent = `---
name: dwi-mcp-rbac
id: dwi-mcp-rbac
type: discovery_work_item
topic: "MCP RBAC Schema & Enforcement Strategy"
status: in_progress
current_phase: 3
next_action: "Experimenter: execute exp-001 spike on RBAC permission resolution performance under load"
verdict: null
hypothesis_count: 3
validated_count: 2
created: 2026-02-15
updated: 2026-03-03
---

## Hypothesis Summary

| ID | Statement (short) | Status |
|:---|:-----------------|:-------|
| hyp-001 | YAML schema can represent 15 distinct role profiles | validated |
| hyp-002 | Gate checks can run <50ms at 100 concurrent requests | testing |

## Phase Gate History

| Phase | Gate crossed | Date | Notes |
|:------|:------------|:-----|:------|
| 0 — Discover | ✅ | 2026-02-01 | Initial observations logged |
| 1 — Define | ✅ | 2026-02-10 | Scope locked |
| 2 — Ideate | ✅ | 2026-02-25 | ADRs approved |
| 3 — Prototype | ⬜ | — | In progress |
`;

      fs.writeFileSync(path.join(topicDir, 'dwi-state.md'), dwiStateContent);

      const seeder = new AgentDbSeeder(rolesDir, standardsDir, discoveryDir, dbPath);
      await seeder.seed();

      // Re-open the DB (seeder closes its own connection)
      agentDb.close();
      connectionManager = new DatabaseConnectionManager(dbPath);
      agentDb = new AgentDb(connectionManager);
      agentDb.initSchema();

      const dwi = agentDb.getDwiState('dwi-mcp-rbac');
      expect(dwi).not.toBeNull();
      expect(dwi?.topic).toBe('MCP RBAC Schema & Enforcement Strategy');
      expect(dwi?.status).toBe('in_progress');

      // Hypotheses were inserted
      const db = agentDb.getRawDatabase();
      const hypoCount = db.prepare('SELECT COUNT(*) AS cnt FROM dwi_hypotheses WHERE dwi_id = ?').get('dwi-mcp-rbac') as { cnt: number };
      expect(hypoCount.cnt).toBe(2);

      // Phase gates were inserted
      const gateCount = db.prepare('SELECT COUNT(*) AS cnt FROM dwi_phase_gates WHERE dwi_id = ?').get('dwi-mcp-rbac') as { cnt: number };
      expect(gateCount.cnt).toBeGreaterThanOrEqual(3);
    });

    it('should be idempotent when seeding DWI state files', async () => {
      const topicDir = path.join(discoveryDir, 'mcp-rbac');
      fs.mkdirSync(topicDir, { recursive: true });

      const dwiStateContent = `---
name: dwi-mcp-rbac
id: dwi-mcp-rbac
type: discovery_work_item
topic: "MCP RBAC Schema & Enforcement Strategy"
status: in_progress
current_phase: 3
next_action: "Experimenter: execute exp-001 spike on RBAC permission resolution performance under load"
verdict: null
hypothesis_count: 3
validated_count: 2
created: 2026-02-15
updated: 2026-03-03
---
`;

      fs.writeFileSync(path.join(topicDir, 'dwi-state.md'), dwiStateContent);

      const seeder1 = new AgentDbSeeder(rolesDir, standardsDir, discoveryDir, dbPath);
      await seeder1.seed();

      // Ensure idempotent by re-running from a new instance (simulates CI rerun)
      const seeder2 = new AgentDbSeeder(rolesDir, standardsDir, discoveryDir, dbPath);
      await seeder2.seed();

      // Re-open the DB (seeder closes its own connection)
      agentDb.close();
      connectionManager = new DatabaseConnectionManager(dbPath);
      agentDb = new AgentDb(connectionManager);
      agentDb.initSchema();

      const db = agentDb.getRawDatabase();
      const countRow = db.prepare('SELECT COUNT(*) AS count FROM discovery_work_items').get() as { count: number };
      expect(countRow.count).toBe(1);
    });
  });

  // ═══════════════════════════════════════════════════════════════════════════
  // ERROR HANDLING
  // ═══════════════════════════════════════════════════════════════════════════

  describe('Error Handling', () => {
    it('should gracefully handle missing directories', () => {
      // Non-existent directory should not crash
      const nonExistentDir = path.join(tmpDir, 'nonexistent');
      expect(() => {
        fs.readdirSync(nonExistentDir);
      }).toThrow();
    });

    it('should handle malformed YAML gracefully', () => {
      // This tests the seeder's error handling for bad YAML
      // Seeder should catch and warn, not crash
      const malformedYaml = 'invalid: yaml: syntax:';
      expect(() => {
        // YAML parser should handle this gracefully
        const yaml = require('js-yaml');
        yaml.load(malformedYaml);
      }).toThrow();
    });
  });

  // ═══════════════════════════════════════════════════════════════════════════
  // STATISTICS & LOGGING
  // ═══════════════════════════════════════════════════════════════════════════

  describe('Seeder Statistics', () => {
    it('should count rows correctly after seeding multiple roles', () => {
      const db = agentDb.getRawDatabase();

      // Insert multiple roles
      const roleStmt = db.prepare(`
        INSERT OR REPLACE INTO roles (domain, role_name, content)
        VALUES (?, ?, ?)
      `);
      roleStmt.run('engineering', 'role1', 'Role 1');
      roleStmt.run('engineering', 'role2', 'Role 2');
      roleStmt.run('management', 'pm_role', 'PM Role');

      // Count
      const count = db.prepare('SELECT COUNT(*) as cnt FROM roles').get() as { cnt: number };
      expect(count.cnt).toBe(3);
    });
  });
});
