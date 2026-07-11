import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { join } from 'path';
import { existsSync, mkdirSync, mkdtempSync, rmSync, writeFileSync } from 'fs';
import { tmpdir } from 'os';
import { AgentDb } from '../../mcp/AgentDb';
import { AgentDbSeeder } from '../../mcp/AgentDbSeeder';
import { DatabaseConnectionManager } from '../../metadata/DatabaseConnectionManager';

describe('EPIC-17: AgentDbSeeder.seedDomains()', () => {
    let tempDir: string;
    let rolesDir: string;
    let dbPath: string;
    let manager: DatabaseConnectionManager;
    let agentDb: AgentDb;
    let seeder: AgentDbSeeder;

    beforeEach(() => {
        tempDir = mkdtempSync(join(tmpdir(), 'domain-seeder-'));
        rolesDir = join(tempDir, 'roles');
        dbPath = join(tempDir, 'metadata.db');

        mkdirSync(rolesDir, { recursive: true });

        manager = new DatabaseConnectionManager(dbPath);
        agentDb = new AgentDb(manager);
        agentDb.initSchema();
        seeder = new AgentDbSeeder(agentDb);
    });

    afterEach(() => {
        try {
            manager.close();
        } catch {
            // Ignore.
        }
        try {
            if (existsSync(tempDir)) {
                rmSync(tempDir, { recursive: true, force: true });
            }
        } catch {
            // Ignore.
        }
    });

    // ─── Setup helpers ────────────────────────────────────────────────────────

    const createDomainDir = (name: string, readmeContent?: string) => {
        const dir = join(rolesDir, name);
        mkdirSync(dir, { recursive: true });
        if (readmeContent !== undefined) {
            writeFileSync(join(dir, 'README.md'), readmeContent, 'utf-8');
        }
    };

    // ─── AC-1: Subdirectories → domains table ────────────────────────────────

    it('AC-1: seeds one row per subdirectory into domains table', () => {
        createDomainDir('engineering');
        createDomainDir('management');
        createDomainDir('discovery');

        seeder.seedDomains(rolesDir);

        const domains = agentDb.listRegisteredDomains();
        const names = domains.map(d => d.name).sort();
        expect(names).toEqual(['discovery', 'engineering', 'management']);
    });

    // ─── AC-2: Idempotency ────────────────────────────────────────────────────

    it('AC-2: seeder is idempotent — double-run does not duplicate rows', () => {
        createDomainDir('engineering');

        seeder.seedDomains(rolesDir);
        seeder.seedDomains(rolesDir);

        const domains = agentDb.listRegisteredDomains();
        const engineeringRows = domains.filter(d => d.name === 'engineering');
        expect(engineeringRows).toHaveLength(1);
    });

    // ─── AC-3: listRegisteredDomains returns expected shape ──────────────────

    it('AC-3: listRegisteredDomains() returns id, name, description for each domain', () => {
        createDomainDir('engineering', '# Engineering\nBuilds backend systems.');

        seeder.seedDomains(rolesDir);

        const domains = agentDb.listRegisteredDomains();
        expect(domains).toHaveLength(1);
        expect(domains[0]).toMatchObject({
            id: 'engineering',
            name: 'engineering',
        });
    });

    // ─── AC-4: getRegisteredDomain resolves seeded domain ────────────────────

    it('AC-4: getRegisteredDomain("engineering") returns the seeded row', () => {
        createDomainDir('engineering');
        seeder.seedDomains(rolesDir);

        const domain = agentDb.getRegisteredDomain('engineering');
        expect(domain).not.toBeNull();
        expect(domain!.name).toBe('engineering');
    });

    // ─── AC-5: Graceful fallback — non-existent rolesDir ─────────────────────

    it('AC-5: does not throw when rolesDir does not exist', () => {
        const nonExistentDir = join(tempDir, 'does-not-exist');
        expect(() => seeder.seedDomains(nonExistentDir)).not.toThrow();
    });

    // ─── R1: Skip _-prefixed directories ─────────────────────────────────────

    it('R1: skips directories starting with underscore (_shared, _adrs)', () => {
        createDomainDir('engineering');
        createDomainDir('_shared');
        createDomainDir('_adrs');

        seeder.seedDomains(rolesDir);

        const names = agentDb.listRegisteredDomains().map(d => d.name);
        expect(names).toContain('engineering');
        expect(names).not.toContain('_shared');
        expect(names).not.toContain('_adrs');
    });

    // ─── README description extraction ───────────────────────────────────────

    it('extracts description from README.md (first non-heading line)', () => {
        createDomainDir('management', '# Management Domain\nOversees delivery and reporting.');

        seeder.seedDomains(rolesDir);

        const domain = agentDb.getRegisteredDomain('management');
        expect(domain?.description).toBe('Oversees delivery and reporting.');
    });

    it('sets description to null when no README.md exists', () => {
        createDomainDir('discovery');
        seeder.seedDomains(rolesDir);

        const domain = agentDb.getRegisteredDomain('discovery');
        expect(domain?.description).toBeNull();
    });
});
