import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { join } from 'path';
import { existsSync, mkdtempSync, rmSync } from 'fs';
import { tmpdir } from 'os';
import { AgentDb } from '../../mcp/AgentDb';
import { DatabaseConnectionManager } from '../../metadata/DatabaseConnectionManager';

describe('EPIC-17 TASK-17-05: Domain-aware query filtering', () => {
    let tempDir: string;
    let dbPath: string;
    let manager: DatabaseConnectionManager;
    let agentDb: AgentDb;

    beforeEach(() => {
        tempDir = mkdtempSync(join(tmpdir(), 'agentdb-domain-filter-'));
        dbPath = join(tempDir, 'metadata.db');
        manager = new DatabaseConnectionManager(dbPath);
        agentDb = new AgentDb(manager);
        agentDb.initSchema();

        const db = manager.getAdminPool();
        db.exec(`
      INSERT INTO domains (id, name, description) VALUES
        ('eng', 'engineering', 'Engineering domain'),
        ('mgt', 'management', 'Management domain');

      INSERT INTO roles (domain, role_name, content, domain_id, created_at, last_updated) VALUES
        ('engineering', 'backend_developer', 'ENG_ROLE', 'eng', datetime('now'), datetime('now')),
        ('engineering', 'legacy_engineer', 'LEGACY_ROLE', NULL, datetime('now'), datetime('now')),
        ('management', 'manager', 'MGT_ROLE', 'mgt', datetime('now'), datetime('now'));

      INSERT INTO workflows (domain, role_name, workflow_type, content, created_at, last_updated) VALUES
        ('engineering', 'backend_developer', 'default', 'ENG_WORKFLOW', datetime('now'), datetime('now')),
        ('management', 'manager', 'default', 'MGT_WORKFLOW', datetime('now'), datetime('now'));

      INSERT INTO templates (domain, role_name, template_name, content, created_at, last_updated) VALUES
        ('engineering', 'backend_developer', 'task', 'ENG_TEMPLATE', datetime('now'), datetime('now')),
        ('management', 'manager', 'task', 'MGT_TEMPLATE', datetime('now'), datetime('now'));
    `);
    });

    afterEach(() => {
        try {
            manager.close();
        } catch {
            // Ignore close errors.
        }
        try {
            if (existsSync(tempDir)) {
                rmSync(tempDir, { recursive: true, force: true });
            }
        } catch {
            // Ignore cleanup errors.
        }
    });

    it('getRole() returns only records in matching domain_id context', () => {
        const eng = agentDb.getRole('engineering', 'backend_developer', 'eng');
        const mgt = agentDb.getRole('engineering', 'backend_developer', 'mgt');

        expect(eng?.content).toBe('ENG_ROLE');
        expect(mgt).toBeNull();
    });

    it('getRole() keeps backward compatibility for legacy null domain_id rows', () => {
        const legacyInEng = agentDb.getRole('engineering', 'legacy_engineer', 'eng');
        const legacyInMgt = agentDb.getRole('engineering', 'legacy_engineer', 'mgt');

        expect(legacyInEng?.content).toBe('LEGACY_ROLE');
        expect(legacyInMgt).toBeNull();
    });

    it('getWorkflow() and getTemplate() enforce domain_id via domains registry mapping', () => {
        const wfEng = agentDb.getWorkflow('engineering', 'backend_developer', 'default', 'eng');
        const wfMgt = agentDb.getWorkflow('engineering', 'backend_developer', 'default', 'mgt');

        const tplEng = agentDb.getTemplate('engineering', 'backend_developer', 'task', 'eng');
        const tplMgt = agentDb.getTemplate('engineering', 'backend_developer', 'task', 'mgt');

        expect(wfEng?.content).toBe('ENG_WORKFLOW');
        expect(wfMgt).toBeNull();
        expect(tplEng?.content).toBe('ENG_TEMPLATE');
        expect(tplMgt).toBeNull();
    });

    it('getRolesByDomain/getWorkflowsByRole/getTemplatesByRole respect domain_id when provided', () => {
        const rolesEng = agentDb.getRolesByDomain('engineering', 'eng');
        const rolesMgt = agentDb.getRolesByDomain('engineering', 'mgt');

        const wfsEng = agentDb.getWorkflowsByRole('engineering', 'backend_developer', 'eng');
        const wfsMgt = agentDb.getWorkflowsByRole('engineering', 'backend_developer', 'mgt');

        const tplsEng = agentDb.getTemplatesByRole('engineering', 'backend_developer', 'eng');
        const tplsMgt = agentDb.getTemplatesByRole('engineering', 'backend_developer', 'mgt');

        expect(rolesEng.length).toBeGreaterThan(0);
        expect(rolesMgt).toHaveLength(0);

        expect(wfsEng).toHaveLength(1);
        expect(wfsMgt).toHaveLength(0);

        expect(tplsEng).toHaveLength(1);
        expect(tplsMgt).toHaveLength(0);
    });
});
