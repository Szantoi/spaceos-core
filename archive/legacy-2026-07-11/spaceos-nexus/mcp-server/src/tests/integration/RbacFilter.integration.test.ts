import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { RbacFilter } from '../../mcp/RbacFilter';
import { AgentDb } from '../../mcp/AgentDb';
import { DatabaseConnectionManager } from '../../metadata/DatabaseConnectionManager';
import { join } from 'path';
import { existsSync, unlinkSync, mkdtempSync } from 'fs';
import { tmpdir } from 'os';

describe.sequential('RbacFilter Integration', () => {
    let agentDb: AgentDb;
    let connectionManager: DatabaseConnectionManager;
    let rbacFilter: RbacFilter;
    let tempDir: string;
    let dbPath: string;

    beforeEach(() => {
        // Setup temp DB
        tempDir = mkdtempSync(join(tmpdir(), 'rbac-integ-'));
        dbPath = join(tempDir, 'test.db');
        connectionManager = new DatabaseConnectionManager(dbPath);
        agentDb = new AgentDb(connectionManager);
        agentDb.initSchema();

        rbacFilter = new RbacFilter(agentDb);
    });

    afterEach(() => {
        agentDb.close();
        try {
            if (dbPath && existsSync(dbPath)) unlinkSync(dbPath);
            if (tempDir && existsSync(tempDir)) unlinkSync(tempDir);
        } catch { }
    });

    it('should correctly fetch allowed tools from a real database', () => {
        // Seed database
        const db = agentDb.getRawDatabase();

        // Insert role first (required for FK)
        db.prepare('INSERT INTO roles (domain, role_name, content) VALUES (?, ?, ?)')
            .run('engineering', 'dev', 'Developer role');

        const permissions = JSON.stringify(['tool_a', 'tool_b']);
        db.prepare('INSERT INTO role_schemas (domain, role_name, mcp_tool_permissions) VALUES (?, ?, ?)')
            .run('engineering', 'dev', permissions);

        // Test
        const tools = rbacFilter.getAllowedTools('dev');

        expect(tools.has('tool_a')).toBe(true);
        expect(tools.has('tool_b')).toBe(true);
        expect(tools.has('list_domains')).toBe(true); // public tool
        expect(tools.size).toBe(6); // 2 custom + 4 public
    });

    it('should handle missing role by returning only public tools', () => {
        const tools = rbacFilter.getAllowedTools('missing_role');
        expect(tools.size).toBe(4);
        expect(tools.has('list_domains')).toBe(true);
    });

    it('should handle malformed JSON in database gracefully', () => {
        const db = agentDb.getRawDatabase();
        db.prepare('INSERT INTO roles (domain, role_name, content) VALUES (?, ?, ?)')
            .run('broken', 'role', 'Broken role');

        db.prepare('INSERT INTO role_schemas (domain, role_name, mcp_tool_permissions) VALUES (?, ?, ?)')
            .run('broken', 'role', '{ invalid json }');

        const tools = rbacFilter.getAllowedTools('role');
        expect(tools.size).toBe(4); // Falls back to public tools
    });

    it('should respect actual database version bumps', () => {
        const db = agentDb.getRawDatabase();

        // Initial state
        db.prepare('INSERT INTO roles (domain, role_name, content) VALUES (?, ?, ?)')
            .run('eng', 'dev', 'Dev');
        db.prepare('INSERT INTO role_schemas (domain, role_name, mcp_tool_permissions) VALUES (?, ?, ?)')
            .run('eng', 'dev', JSON.stringify(['tool1']));

        // Cache first call
        expect(rbacFilter.getAllowedTools('dev').has('tool1')).toBe(true);

        // Update database DIRECTLY (bypassing caching if any in AgentDb)
        db.prepare('UPDATE role_schemas SET mcp_tool_permissions = ? WHERE role_name = ?')
            .run(JSON.stringify(['tool2']), 'dev');

        // Since version hasn't changed (we didn't bump it in schema_metadata), it should still have tool1
        expect(rbacFilter.getAllowedTools('dev').has('tool1')).toBe(true);
        expect(rbacFilter.getAllowedTools('dev').has('tool2')).toBe(false);

        // Now bump version in schema_metadata to 3 (since it starts at 2)
        const currentVersion = agentDb.getSchemaVersion('read-layer');

        db.prepare('INSERT OR REPLACE INTO schema_metadata (layer, version) VALUES (?, ?)')
            .run('read-layer', currentVersion + 1);


        // Next call should detect bump and invalidate cache
        const updatedTools = rbacFilter.getAllowedTools('dev');
        expect(updatedTools.has('tool1')).toBe(false);
        expect(updatedTools.has('tool2')).toBe(true);
    });
});
