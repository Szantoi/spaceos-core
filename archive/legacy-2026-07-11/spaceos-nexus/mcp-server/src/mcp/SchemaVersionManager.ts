/**
 * TASK-09-04B: SchemaVersionManager — Schema version tracking for migrations
 *
 * Tracks schema versions for both read-layer (context layer) and write-layer.
 * Enables agents to detect schema updates between sessions.
 *
 * **Usage:**
 * ```typescript
 * const manager = new SchemaVersionManager(db);
 * const version = manager.getReadLayerVersion(); // Returns: 1
 * manager.incrementReadLayerVersion();           // Increments: 1 → 2
 * ```
 *
 * **Tracking:**
 * - Agent loads version at startup
 * - Agent checks version at end of session
 * - If version increased: log warning (schema updated by seeder)
 */

import Database from 'better-sqlite3';

export class SchemaVersionManager {
    constructor(private db: Database.Database) { }

    /**
     * Get current read-layer (context) schema version.
     *
     * @returns Version number (default: 1)
     */
    public getReadLayerVersion(): number {
        const stmt = this.db.prepare(
            `SELECT version FROM schema_metadata WHERE layer = 'read-layer' LIMIT 1`
        );
        const result = stmt.get() as { version: number } | undefined;
        return result?.version ?? 1;
    }

    /**
     * Get current write-layer schema version.
     *
     * @returns Version number (default: 1)
     */
    public getWriteLayerVersion(): number {
        const stmt = this.db.prepare(
            `SELECT version FROM schema_metadata WHERE layer = 'write-layer' LIMIT 1`
        );
        const result = stmt.get() as { version: number } | undefined;
        return result?.version ?? 1;
    }

    /**
     * Increment read-layer version and update timestamp.
     *
     * Called by seeder after bulk updates to signal schema change to agents.
     *
     * @returns New version number
     */
    public incrementReadLayerVersion(): number {
        const currentVersion = this.getReadLayerVersion();
        const newVersion = currentVersion + 1;

        const stmt = this.db.prepare(`
      UPDATE schema_metadata
      SET version = ?, last_updated = datetime('now')
      WHERE layer = 'read-layer'
    `);
        stmt.run(newVersion);

        console.info(
            `[SchemaVersionManager] ✓ Read-layer version incremented: ${currentVersion} → ${newVersion}`
        );

        return newVersion;
    }

    /**
     * Increment write-layer version and update timestamp.
     *
     * @returns New version number
     */
    public incrementWriteLayerVersion(): number {
        const currentVersion = this.getWriteLayerVersion();
        const newVersion = currentVersion + 1;

        const stmt = this.db.prepare(`
      UPDATE schema_metadata
      SET version = ?, last_updated = datetime('now')
      WHERE layer = 'write-layer'
    `);
        stmt.run(newVersion);

        console.info(
            `[SchemaVersionManager] ✓ Write-layer version incremented: ${currentVersion} → ${newVersion}`
        );

        return newVersion;
    }

    /**
     * Get all schema metadata (both layers).
     *
     * @returns Array of { layer, version, last_updated, created_at }
     */
    public getAllVersions(): Array<{
        layer: string;
        version: number;
        last_updated: string;
        created_at: string;
    }> {
        const stmt = this.db.prepare(
            `SELECT layer, version, last_updated, created_at FROM schema_metadata ORDER BY layer`
        );
        const results = stmt.all() as Array<{
            layer: string;
            version: number;
            last_updated: string;
            created_at: string;
        }>;
        return results;
    }

    /**
     * Log current versions (for diagnostics).
     */
    public logVersions(): void {
        const versions = this.getAllVersions();

        console.info('[SchemaVersionManager] 📊 Schema Versions:');
        for (const v of versions) {
            console.info(`  - ${v.layer}: version ${v.version} (updated: ${v.last_updated})`);
        }
    }

    /**
     * Reset versions to 1 (DEVELOPMENT ONLY — never use in production).
     */
    public resetVersions(): void {
        console.warn('[SchemaVersionManager] ⚠️  RESET: Resetting all versions to 1 (DEV ONLY)');

        const stmt = this.db.prepare(
            `UPDATE schema_metadata SET version = 1, last_updated = datetime('now')`
        );
        stmt.run();
    }
}
