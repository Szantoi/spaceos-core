import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { BootstrapService } from '../../mcp/BootstrapService';
import { AgentDb } from '../../mcp/AgentDb';
import { SessionManager } from '../../mcp/SessionManager';
import { isBootstrapError } from '../../mcp/ErrorResponses';
import { DatabaseConnectionManager } from '../../metadata/DatabaseConnectionManager';
import { join } from 'path';
import { existsSync, unlinkSync, mkdtempSync } from 'fs';
import { tmpdir } from 'os';

describe('Bootstrap Error Integration Tests', () => {
    let bootstrapService: BootstrapService;
    let agentDb: AgentDb;
    let sessionManager: SessionManager;
    let connectionManager: DatabaseConnectionManager;
    let tempDir: string;
    let dbPath: string;

    beforeEach(() => {
        tempDir = mkdtempSync(join(tmpdir(), 'integration-bootstrap-'));
        dbPath = join(tempDir, 'test.db');
        connectionManager = new DatabaseConnectionManager(dbPath);
        agentDb = new AgentDb(connectionManager);
        agentDb.initSchema();
        sessionManager = new SessionManager(dbPath);
        bootstrapService = new BootstrapService(agentDb, sessionManager);
    });

    afterEach(() => {
        agentDb.close();
        try {
            if (dbPath && existsSync(dbPath)) unlinkSync(dbPath);
            if (tempDir && existsSync(tempDir)) unlinkSync(tempDir);
        } catch { }
    });

    it('should return INVALID_DOMAIN when domain validation fails', async () => {
        const result = await bootstrapService.bootstrap('Invalid-Domain', 'backend_developer');
        expect(isBootstrapError(result)).toBe(true);
        if (isBootstrapError(result)) {
            expect(result.code).toBe('INVALID_DOMAIN');
            expect(result.field).toBe('domain');
        }
    });

    it('should return INVALID_ROLE when role validation fails', async () => {
        const result = await bootstrapService.bootstrap('engineering', 'Backend_Developer');
        expect(isBootstrapError(result)).toBe(true);
        if (isBootstrapError(result)) {
            expect(result.code).toBe('INVALID_ROLE');
            expect(result.field).toBe('role');
        }
    });

    it('should return ROLE_NOT_FOUND when valid input but missing role', async () => {
        const result = await bootstrapService.bootstrap('engineering', 'nonexistent_role');
        expect(isBootstrapError(result)).toBe(true);
        if (isBootstrapError(result)) {
            expect(result.code).toBe('ROLE_NOT_FOUND');
        }
    });

    it('should handle multiple validation errors (reporting the first one found)', async () => {
        // validateBootstrapInput checks domain first
        const result = await bootstrapService.bootstrap('INVALID!', 'INVALID!');
        expect(isBootstrapError(result)).toBe(true);
        if (isBootstrapError(result)) {
            expect(result.code).toBe('INVALID_DOMAIN');
        }
    });
});
