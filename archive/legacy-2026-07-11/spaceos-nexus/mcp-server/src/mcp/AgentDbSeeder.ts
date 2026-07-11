/**
 * AgentDbSeeder — Populates the `domains` registry table from the filesystem.
 *
 * Reads the `database/roles/` directory tree and inserts one row per
 * domain subdirectory into `agent.db`'s `domains` table.
 *
 * Usage:
 * ```typescript
 * const seeder = new AgentDbSeeder(agentDb);
 * seeder.seedDomains(path.join(databaseRoot, 'roles'));
 * ```
 *
 * Design constraints:
 * - Idempotent: repeated invocations produce the same final state.
 * - Skips directories beginning with `_` (reserved names).
 * - Graceful no-op if `database/roles/` path does not exist.
 * - If the `domains` table is absent (migration not yet run), logs a warning
 *   and exits without crashing (backward-compatible for cold-start scenarios).
 */

import { readdirSync, existsSync, readFileSync } from 'fs';
import { join } from 'path';
import { AgentDb } from './AgentDb';

import type { Dirent } from 'fs';

export class AgentDbSeeder {
    constructor(private readonly agentDb: AgentDb) { }

    /**
     * Discover domain subdirectories under `rolesDir` and upsert each
     * one as a domain row in the `domains` registry table.
     *
     * @param rolesDir - Absolute path to `database/roles/` directory
     */
    public seedDomains(rolesDir: string): void {
        // Guard: roles directory must exist
        if (!existsSync(rolesDir)) {
            console.warn(`[AgentDbSeeder.seedDomains] Roles directory not found, skipping: ${rolesDir}`);
            return;
        }

        // Guard: domains table must exist (requires EPIC-17 migration 008)
        try {
            this.agentDb.listRegisteredDomains(); // lightweight probe — throws if table missing
        } catch {
            console.warn('[AgentDbSeeder.seedDomains] domains table not available, skipping domain seeding. Run initSchema() first.');
            return;
        }

        // Discover 1-level subdirectories (each is a domain name)
        let entries: Dirent[];
        try {
            entries = readdirSync(rolesDir, { withFileTypes: true, encoding: 'utf-8' }) as unknown as Dirent[];
        } catch (err) {
            console.error('[AgentDbSeeder.seedDomains] Failed to read roles directory:', err);
            return;
        }

        const domainDirs = entries.filter(
            entry => entry.isDirectory() && !entry.name.startsWith('_')
        );

        let seededCount = 0;
        for (const dir of domainDirs) {
            const description = this.readDomainDescription(join(rolesDir, dir.name));
            this.agentDb.upsertDomain(dir.name, description);
            seededCount++;
        }

        console.info(
            `[AgentDbSeeder.seedDomains] ✅ Seeded ${seededCount} domain(s) from ${rolesDir}`
        );
    }

    /**
     * Attempt to read a short description from `<domainDir>/README.md`.
     *
     * Returns the first non-empty line starting with text (skips `#` headings),
     * or `null` if no README exists or parsing fails.
     */
    private readDomainDescription(domainDir: string): string | null {
        const readmePath = join(domainDir, 'README.md');
        if (!existsSync(readmePath)) {
            return null;
        }
        try {
            const lines = readFileSync(readmePath, { encoding: 'utf-8' }).split('\n');
            const descLine = lines.find(l => l.trim().length > 0 && !l.startsWith('#'));
            return descLine?.trim() ?? null;
        } catch {
            return null;
        }
    }
}
