import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { join } from 'path';
import { existsSync, mkdtempSync, rmSync } from 'fs';
import { tmpdir } from 'os';
import { AgentDb } from '../../mcp/AgentDb';
import { DatabaseConnectionManager } from '../../metadata/DatabaseConnectionManager';

describe('EPIC-17: Domain schema migration & query methods', () => {
    let tempDir: string;
    let dbPath: string;
    let manager: DatabaseConnectionManager;
    let agentDb: AgentDb;

    beforeEach(() => {
        tempDir = mkdtempSync(join(tmpdir(), 'agentdb-domain-'));
        dbPath = join(tempDir, 'metadata.db');
        manager = new DatabaseConnectionManager(dbPath);
        agentDb = new AgentDb(manager);
        agentDb.initSchema();
    });

    afterEach(() => {
        try {
            manager.close();
        } catch {
            // Ignore close errors in test teardown.
        }
        try {
            if (existsSync(tempDir)) {
                rmSync(tempDir, { recursive: true, force: true });
            }
        } catch {
            // Ignore cleanup errors.
        }
    });

    // ─── AC-1: domains table schema ──────────────────────────────────────────

    it('AC-1: domains table exists after initSchema()', () => {
        const db = manager.getAdminPool();
        const row = db
            .prepare("SELECT name FROM sqlite_master WHERE type='table' AND name='domains'")
            .get() as { name: string } | undefined;
        expect(row?.name).toBe('domains');
    });

    it('AC-1: domains table has expected columns', () => {
        const db = manager.getAdminPool();
        const cols = (db.prepare('PRAGMA table_info(domains)').all() as Array<{ name: string }>).map(
            r => r.name
        );
        expect(cols).toContain('id');
        expect(cols).toContain('name');
        expect(cols).toContain('description');
        expect(cols).toContain('config_json');
        expect(cols).toContain('created_at');
        expect(cols).toContain('updated_at');
    });

    // ─── AC-2: roles.domain_id column ────────────────────────────────────────

    it('AC-2: roles table has nullable domain_id column after migration', () => {
        const db = manager.getAdminPool();
        const cols = (db.prepare('PRAGMA table_info(roles)').all() as Array<{ name: string; notnull: number }>);
        const domainIdCol = cols.find(c => c.name === 'domain_id');
        expect(domainIdCol).toBeDefined();
        expect(domainIdCol!.notnull).toBe(0); // nullable
    });

    // ─── AC-3: sessions.current_domain_id column ─────────────────────────────

    it('AC-3: sessions table has nullable current_domain_id column after migration', () => {
        const db = manager.getAdminPool();
        const cols = (db.prepare('PRAGMA table_info(sessions)').all() as Array<{ name: string; notnull: number }>);
        const col = cols.find(c => c.name === 'current_domain_id');
        expect(col).toBeDefined();
        expect(col!.notnull).toBe(0); // nullable
    });

    // ─── AC-4: Idempotency — calling initSchema() twice does not throw ────────

    it('AC-4: initSchema() is idempotent (double-call does not throw)', () => {
        // Create a second instance pointing at the same DB file
        const manager2 = new DatabaseConnectionManager(dbPath);
        const agentDb2 = new AgentDb(manager2);
        expect(() => agentDb2.initSchema()).not.toThrow();
        manager2.close();
    });

    // ─── AC-5: Backward compatibility — existing roles have domain_id IS NULL ─

    it('AC-5: existing roles rows have domain_id IS NULL after migration', () => {
        const db = manager.getAdminPool();

        // Seed a role without domain_id (backward compat scenario)
        db.prepare(`
      INSERT INTO roles (domain, role_name, content, created_at, last_updated)
      VALUES ('engineering', 'backend_developer', '{}', datetime('now'), datetime('now'))
    `).run();

        const row = db
            .prepare("SELECT domain_id FROM roles WHERE domain = 'engineering' AND role_name = 'backend_developer'")
            .get() as { domain_id: string | null } | undefined;

        expect(row).toBeDefined();
        expect(row!.domain_id).toBeNull();
    });

    // ─── listRegisteredDomains() ──────────────────────────────────────────────

    it('listRegisteredDomains() returns empty array when no domains seeded', () => {
        const result = agentDb.listRegisteredDomains();
        expect(Array.isArray(result)).toBe(true);
        expect(result).toHaveLength(0);
    });

    it('listRegisteredDomains() returns all seeded domains ordered by name', () => {
        const db = manager.getAdminPool();
        db.exec(`
      INSERT INTO domains (id, name, description) VALUES
        ('mgt', 'management', 'Management domain'),
        ('eng', 'engineering', 'Engineering domain');
    `);

        const result = agentDb.listRegisteredDomains();
        expect(result).toHaveLength(2);
        expect(result[0].name).toBe('engineering'); // alphabetical
        expect(result[1].name).toBe('management');
        expect(result[0].id).toBe('eng');
    });

    // ─── getRegisteredDomain() ───────────────────────────────────────────────

    it('getRegisteredDomain() returns null for unknown domain', () => {
        const result = agentDb.getRegisteredDomain('unknown');
        expect(result).toBeNull();
    });

    it('getRegisteredDomain() resolves by name', () => {
        const db = manager.getAdminPool();
        db.prepare("INSERT INTO domains (id, name, description) VALUES ('eng', 'engineering', 'Eng domain')").run();

        const result = agentDb.getRegisteredDomain('engineering');
        expect(result).not.toBeNull();
        expect(result!.id).toBe('eng');
        expect(result!.name).toBe('engineering');
        expect(result!.description).toBe('Eng domain');
    });

    it('getRegisteredDomain() resolves by id', () => {
        const db = manager.getAdminPool();
        db.prepare("INSERT INTO domains (id, name, description) VALUES ('mgt', 'management', null)").run();

        const result = agentDb.getRegisteredDomain('mgt');
        expect(result).not.toBeNull();
        expect(result!.name).toBe('management');
        expect(result!.description).toBeNull();
    });

    // ─── upsertDomain() ──────────────────────────────────────────────────────

    it('upsertDomain() inserts new domain into registry', () => {
        agentDb.upsertDomain('engineering', 'Engineering domain');

        const result = agentDb.getRegisteredDomain('engineering');
        expect(result).not.toBeNull();
        expect(result!.id).toBe('engineering');
        expect(result!.name).toBe('engineering');
        expect(result!.description).toBe('Engineering domain');
    });

    it('upsertDomain() replaces existing domain (idempotent)', () => {
        const db = manager.getAdminPool();
        db.prepare("INSERT INTO domains (id, name, description) VALUES ('eng', 'engineering', 'Old description')").run();

        agentDb.upsertDomain('engineering', 'New description');

        const result = agentDb.getRegisteredDomain('engineering');
        expect(result).not.toBeNull();
        expect(result!.name).toBe('engineering');
        expect(result!.description).toBe('New description');
    });

    it('upsertDomain() handles null description', () => {
        agentDb.upsertDomain('management');

        const result = agentDb.getRegisteredDomain('management');
        expect(result).not.toBeNull();
        expect(result!.name).toBe('management');
        expect(result!.description).toBeNull();
    });

    // ─── FK Constraint Validation ────────────────────────────────────────────

    it('FK constraint: roles.domain_id must reference domains.id when set', () => {
        const db = manager.getAdminPool();

        // Insert a valid domain first
        db.prepare("INSERT INTO domains (id, name) VALUES ('eng', 'engineering')").run();

        // Insert a role with valid domain_id (should succeed)
        expect(() => {
            db.prepare(`
                INSERT INTO roles (domain, role_name, content, domain_id, created_at, last_updated)
                VALUES ('engineering', 'dev', '{}', 'eng', datetime('now'), datetime('now'))
            `).run();
        }).not.toThrow();

        // Verify the role was created
        const role = db.prepare("SELECT * FROM roles WHERE role_name = 'dev'").get();
        expect(role).toBeDefined();
    });

    it('FK constraint: invalid domain_id on roles causes constraint failure', () => {
        const db = manager.getAdminPool();

        // Try to insert a role with non-existent domain_id (should be rejected by FK)
        expect(() => {
            db.prepare(`
                INSERT INTO roles (domain, role_name, content, domain_id, created_at, last_updated)
                VALUES ('engineering', 'dev', '{}', 'invalid-id', datetime('now'), datetime('now'))
            `).run();
        }).toThrow();
    });

    it('FK constraint: sessions.current_domain_id must reference domains.id when set', () => {
        const db = manager.getAdminPool();

        // Insert a valid domain
        db.prepare("INSERT INTO domains (id, name) VALUES ('eng', 'engineering')").run();

        // Insert a session with valid current_domain_id (should succeed)
        // Note: sessions table has columns: id, agent_id, domain, role, started_at, last_updated_at, fsm_state, outcome, current_domain_id
        expect(() => {
            db.prepare(`
                INSERT INTO sessions (id, agent_id, domain, role, started_at, current_domain_id)
                VALUES ('test-sess-id', 'test-agent', 'engineering', 'backend_developer', datetime('now'), 'eng')
            `).run();
        }).not.toThrow();

        // Verify the session was created
        const session = db.prepare("SELECT * FROM sessions WHERE id = 'test-sess-id'").get();
        expect(session).toBeDefined();
    });

    it('domains table unique constraint on name', () => {
        const db = manager.getAdminPool();

        db.prepare("INSERT INTO domains (id, name) VALUES ('eng1', 'engineering')").run();

        // Try to insert another domain with same name (should fail on UNIQUE constraint)
        expect(() => {
            db.prepare("INSERT INTO domains (id, name) VALUES ('eng2', 'engineering')").run();
        }).toThrow();
    });

    // ─── Index verification ──────────────────────────────────────────────────

    it('idx_domains_name index exists', () => {
        const db = manager.getAdminPool();
        const index = db
            .prepare("SELECT name FROM sqlite_master WHERE type='index' AND name='idx_domains_name'")
            .get() as { name: string } | undefined;
        expect(index?.name).toBe('idx_domains_name');
    });
});
